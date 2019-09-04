namespace WaveFunctionCollapse.Extensions
{
    /// <summary>
    /// Auxiliary extension methods for the <c>int</c> primitive type
    /// </summary>
    public static class IntExtensions
    {
        /// <summary>
        /// Returns the result of the <paramref name="baseValue"/> elevated to the <paramref name="power"/> potency
        /// </summary>
        /// <param name="baseValue"></param>
        /// <param name="power">Potency to elevate the number to</param>
        public static long Power(this int baseValue, int power)
        {
            long result = 1;

            for (int iterator = 0; iterator < power; iterator++)
            {
                result *= baseValue;
            }

            return result;
        }
    }
}
