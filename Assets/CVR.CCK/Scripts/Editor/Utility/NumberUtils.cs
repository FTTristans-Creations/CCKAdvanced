using System;

namespace CVR.CCKEditor.Util
{
    public static class NumberUtils
    {
        /// <summary>
        /// Calculates the percentage of a current value relative to a total.
        /// </summary>
        /// <param name="current">The current value.</param>
        /// <param name="total">The total or maximum possible value.</param>
        /// <returns>The percentage (0–100) as a double.</returns>
        public static float CalculatePercentage(double current, double total)
        {
            if (total <= 0) return 0; // Avoid divide-by-zero
            return (float)((current / total) * 100.0);
        }
        
        /// <summary>
        /// Converts a byte count into a human-readable string using binary units (B, KB, MB, etc.).
        /// </summary>
        /// <param name="bytes">The number of bytes.</param>
        /// <param name="decimalPlaces">The number of decimal places to include in the formatted value.</param>
        /// <returns>A formatted string representing the size in the largest appropriate unit.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="bytes"/> is negative.</exception>
        public static string FormatBytes(long bytes, int decimalPlaces = 2)
        {
            if (bytes < 0) throw new ArgumentOutOfRangeException(nameof(bytes), "Value must be non-negative.");
            string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            string formattedNumber = len.ToString("F" + decimalPlaces);
            return formattedNumber + " " + sizes[order];
        }
    }
}