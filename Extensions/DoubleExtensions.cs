using System.Linq;

namespace WaveFunctionCollapse.Extensions
{
    public static class DoubleExtensions
    {
        public static int Random(this double[] entryData, double randomFactor)
        {
            double sum = entryData.Sum();
            for (int index = 0; index < entryData.Length; index++)
            {
                entryData[index] /= sum;
            }

            int iterator = 0;
            double checkFactor = 0;

            while (iterator < entryData.Length)
            {
                checkFactor += entryData[iterator];
                if (randomFactor <= checkFactor) return iterator;

                iterator++;
            }

            return 0;
        }
    }
}
