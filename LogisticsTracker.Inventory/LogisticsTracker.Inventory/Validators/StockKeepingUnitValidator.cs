using System;
using System.Buffers;

namespace LogisticsTracker.Inventory.Validators
{
    /// <summary>
    /// SKU Format: AAA-NNN-XXX (e.g., "WID-001-BLK")
    /// - 3 uppercase letters
    /// - hyphen
    /// - 3 digits
    /// - hyphen
    /// - 3 uppercase letters or digits
    /// </summary>
    public static class StockKeepingUnitValidator
    {
        private static readonly SearchValues<char> _validLetters = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZ");

        private static readonly SearchValues<char> _validDigits = SearchValues.Create("0123456789");

        private static readonly SearchValues<char> _validAlphanumeric = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");

        public static bool IsValidSku(ReadOnlySpan<char> stockKeepingUnit)
        {
            if (stockKeepingUnit.Length != 11)
            {
                return false;
            }

            if (stockKeepingUnit[3] != '-' || stockKeepingUnit[7] != '-')
            {
                return false;
            }

            var part1 = stockKeepingUnit[0..3];
            if (!part1.ContainsAny(_validLetters))
            {
                return false;
            }

            var part2 = stockKeepingUnit[4..7];
            if (!part2.ContainsAny(_validDigits))
            {
                return false;
            }

            var part3 = stockKeepingUnit[8..11];
            if (!part3.ContainsAny(_validAlphanumeric))
            {
                return false;
            }

            return true;
        }

        public static (bool IsValid, string? ErrorMessage) ValidateSkuDetailed(ReadOnlySpan<char> stockKeepingUnit)
        {
            if (stockKeepingUnit.Length != 11)
                return (false, $"Stock keeping unit must be exactly 11 characters (format: XXX-NNN-XXX), got {stockKeepingUnit.Length}");

            if (stockKeepingUnit[3] != '-' || stockKeepingUnit[7] != '-')
                return (false, "Stock keeping unit must have hyphens at positions 3 and 7 (format: XXX-NNN-XXX)");

            var part1 = stockKeepingUnit[0..3];
            if (!part1.ContainsAny(_validLetters))
            {
                return (false, "First 3 characters must be uppercase letters (A-Z)");
            }

            var part2 = stockKeepingUnit[4..7];
            if (!part2.ContainsAny(_validDigits))
            {
                return (false, "Characters 4-6 must be digits (0-9)");
            }

            var part3 = stockKeepingUnit[8..11];
            if (!part3.ContainsAny(_validAlphanumeric))
            {
                return (false, "Last 3 characters must be uppercase letters or digits");
            }

            return (true, null);
        }

        public static string NormalizeSku(string sku)
        {
            return sku.Trim().ToUpperInvariant();
        }

        public static string GenerateRandomSku()
        {
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string alphanumeric = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            var random = Random.Shared;

            var part1 = new string(Enumerable.Range(0, 3)
                .Select(_ => letters[random.Next(letters.Length)])
                .ToArray());

            var part2 = new string(Enumerable.Range(0, 3)
                .Select(_ => digits[random.Next(digits.Length)])
                .ToArray());

            var part3 = new string(Enumerable.Range(0, 3)
                .Select(_ => alphanumeric[random.Next(alphanumeric.Length)])
                .ToArray());

            return $"{part1}-{part2}-{part3}";
        }

    }
}
