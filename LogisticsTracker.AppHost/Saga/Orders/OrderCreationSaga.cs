using Events.Messaging;
using Events.Orders;
using LogisticsTracker.Orders.Clients;
using LogisticsTracker.Orders.Models;
using LogisticsTracker.Orders.Repository;
using Microsoft.Extensions.Logging;
using Saga.Orders.Payment;

namespace Saga.Orders
{
    public class OrderCreationSaga(
    IInventoryClient inventoryClient,
    IPaymentClient paymentClient,
    IOrderRepository orderRepository,
    IEventPublisher eventPublisher,
    ILogger<OrderCreationSaga> logger)
    : SagaBase<OrderCreationSagaContext>(logger), ISaga<OrderCreationSagaContext, OrderResult>
    {
        private const string StepValidateOrder = "ValidateOrder";
        private const string StepReserveInventory = "ReserveInventory";
        private const string StepProcessPayment = "ProcessPayment";
        private const string StepConfirmOrder = "ConfirmOrder";

        protected override async Task<SagaResult> ExecuteStepsAsync(OrderCreationSagaContext context, CancellationToken cancellationToken)
        {
            var validateResult = await ExecuteStepAsync(
                context,
                StepValidateOrder,
                () => ValidateOrderAsync(context));

            if (!validateResult.IsSuccess)
            {
                return validateResult;
            }

            var reserveResult = await ExecuteStepAsync(
                context,
                StepReserveInventory,
                () => ReserveInventoryAsync(context, cancellationToken));

            if (!reserveResult.IsSuccess)
            {
                return reserveResult;
            }

            var paymentResult = await ExecuteStepAsync(
                context,
                StepProcessPayment,
                () => ProcessPaymentAsync(context, cancellationToken));

            if (!paymentResult.IsSuccess)
            {
                return paymentResult;
            }

            var confirmResult = await ExecuteStepAsync(
                context,
                StepConfirmOrder,
                () => ConfirmOrderAsync(context, cancellationToken));

            return !confirmResult.IsSuccess ? confirmResult  : (SagaResult)SagaResult.Succeeded(new OrderResult
            {
                OrderId = context.OrderId,
                ConfirmationNumber = context.ConfirmationNumber!,
                PaymentId = context.PaymentId!.Value,
                ReservationIds = context.ReservationIds
            });
        }

        protected override async Task<bool> CompensateStepAsync(OrderCreationSagaContext context, string stepName, CancellationToken cancellationToken)
        {
            return stepName switch
            {
                StepConfirmOrder => await CancelOrderConfirmationAsync(context, cancellationToken),
                StepProcessPayment => await RefundPaymentAsync(context, cancellationToken),
                StepReserveInventory => await ReleaseInventoryReservationsAsync(context, cancellationToken),
                StepValidateOrder => true, // No compensation needed for validation
                _ => throw new InvalidOperationException($"Unknown step for compensation: {stepName}")
            };
        }

        async Task<SagaResult<OrderResult>> ISaga<OrderCreationSagaContext, OrderResult>.ExecuteAsync(
         OrderCreationSagaContext context,
         CancellationToken cancellationToken)
        {
            var result = await ExecuteAsync(context, cancellationToken);

            return result switch
            {
                SagaResult.Success success => SagaResult<OrderResult>.Succeeded((OrderResult)success.Data!),
                SagaResult.Failed failed => SagaResult<OrderResult>.FailedAt(failed.FailedStep, failed.Reason, failed.WasCompensated),
                SagaResult.CompensationFailed compFailed => SagaResult<OrderResult>.FailedToCompensate(compFailed.FailedStep, compFailed.Reason, compFailed.CompensationError),
                _ => throw new InvalidOperationException("Unknown saga result type")
            };
        }

        #region private methods
        private Task<StepResult> ValidateOrderAsync(OrderCreationSagaContext context)
        {
            if (context.Items.Count == 0)
            {
                return Task.FromResult(StepResult.FailedWith("Order must contain at least one item"));
            }

            if (context.TotalAmount <= 0)
            {
                return Task.FromResult(StepResult.FailedWith("Order total must be greater than zero"));
            }

            foreach (var item in context.Items)
            {
                if (item.Quantity <= 0)
                {
                    return Task.FromResult(StepResult.FailedWith($"Invalid quantity for product {item.ProductId}"));
                }

                if (item.UnitPrice <= 0)
                {
                    return Task.FromResult(StepResult.FailedWith($"Invalid price for product {item.ProductId}"));
                }
            }

            return Task.FromResult(StepResult.Succeeded());
        }

        private async Task<StepResult> ReserveInventoryAsync(OrderCreationSagaContext context, CancellationToken cancellationToken)
        {
            try
            {
                var reservations = await inventoryClient.ReserveInventoryForOrderAsync(
                    context.OrderId,
                    context.Items.Select(i => new OrderItem(
                        i.ProductId,
                        string.Empty, 
                        i.StockKeepingUnit,
                        i.Quantity,
                        i.UnitPrice
                    )).ToList(),
                    cancellationToken);

                var failedReservations = reservations.Where(r => !r.Success).ToList();
                if (failedReservations.Any())
                {
                    var errors = string.Join(", ", failedReservations.Select(r => r.Message));
                    return StepResult.FailedWith($"Failed to reserve inventory: {errors}");
                }

                context.ReservationIds = reservations.Select(r => r.ReservationId).ToList();
                return StepResult.Succeeded(
                    data: reservations,
                    compensationData: new Dictionary<string, object>
                    {
                        ["ReservationIds"] = context.ReservationIds
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error reserving inventory for saga {SagaId}", context.SagaId);
                return StepResult.FailedWith($"Inventory reservation error: {ex.Message}");
            }
        }

        private async Task<StepResult> ProcessPaymentAsync(OrderCreationSagaContext context, CancellationToken cancellationToken)
        {
            try
            {
                var paymentRequest = new PaymentRequest(
                    context.OrderId,
                    context.CustomerId,
                    context.TotalAmount,
                    "USD"
                );

                var paymentResult = await paymentClient.ProcessPaymentAsync(paymentRequest, cancellationToken);

                if (!paymentResult.Success)
                {
                    return StepResult.FailedWith($"Payment failed: {paymentResult.Message}");
                }

                context.PaymentId = paymentResult.PaymentId;
                return StepResult.Succeeded(
                    data: paymentResult,
                    compensationData: new Dictionary<string, object>
                    {
                        ["PaymentId"] = context.PaymentId.Value
                    });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing payment for saga {SagaId}", context.SagaId);
                return StepResult.FailedWith($"Payment processing error: {ex.Message}");
            }
        }

        private async Task<StepResult> ConfirmOrderAsync(OrderCreationSagaContext context, CancellationToken cancellationToken)
        {
            try
            {
                var order = await orderRepository.GetByIdAsync(context.OrderId, cancellationToken);
                if (order == null)
                {
                    return StepResult.FailedWith("Order not found");
                }

                order.Status = OrderStatus.Confirmed;
                order.UpdatedAt = DateTimeOffset.UtcNow;
                await orderRepository.UpdateAsync(order, cancellationToken);

                context.ConfirmationNumber = $"CONF-{context.OrderId:N}".Substring(0, 20).ToUpperInvariant();

                await eventPublisher.PublishAsync(new OrderConfirmedEvent
                {
                    OrderId = context.OrderId,
                    OrderNumber = order.OrderNumber,
                    CustomerId = context.CustomerId,
                    ConfirmedAt = DateTimeOffset.UtcNow
                }, cancellationToken);
                return StepResult.Succeeded(data: context.ConfirmationNumber);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error confirming order for saga {SagaId}", context.SagaId);
                return StepResult.FailedWith($"Order confirmation error: {ex.Message}");
            }
        }

        private async Task<bool> CancelOrderConfirmationAsync(OrderCreationSagaContext context, CancellationToken cancellationToken)
        {
            try
            {
                var order = await orderRepository.GetByIdAsync(context.OrderId, cancellationToken);
                if (order == null)
                {
                    Logger.LogWarning("Order {OrderId} not found during compensation", context.OrderId);
                    return true; // Consider it compensated if not found
                }

                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTimeOffset.UtcNow;
                await orderRepository.UpdateAsync(order, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error cancelling order confirmation for saga {SagaId}", context.SagaId);
                return false;
            }
        }

        private async Task<bool> RefundPaymentAsync(OrderCreationSagaContext context, CancellationToken cancellationToken)
        {
            if (context.PaymentId == null)
            {
                Logger.LogWarning("No payment to refund for saga {SagaId}", context.SagaId);
                return true;
            }

            try
            {
                var refundResult = await paymentClient.RefundPaymentAsync(
                    context.PaymentId.Value,
                    context.TotalAmount,
                    cancellationToken);

                if (!refundResult.Success)
                {
                    Logger.LogError("Failed to refund payment for saga {SagaId}: {Message}",
                        context.SagaId, refundResult.Message);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error refunding payment for saga {SagaId}", context.SagaId);
                return false;
            }
        }

        private async Task<bool> ReleaseInventoryReservationsAsync(OrderCreationSagaContext context, CancellationToken cancellationToken)
        {
            if (context.ReservationIds.Count == 0)
            {
                Logger.LogWarning("No reservations to release for saga {SagaId}", context.SagaId);
                return true;
            }

            try
            {
                var result = await inventoryClient.ReleaseOrderReservationsAsync(
                    context.OrderId,
                    context.ReservationIds,
                    cancellationToken);

                if (!result)
                {
                    Logger.LogError("Failed to release all reservations for saga {SagaId}", context.SagaId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error releasing reservations for saga {SagaId}", context.SagaId);
                return false;
            }
        }
        #endregion
    }
}
