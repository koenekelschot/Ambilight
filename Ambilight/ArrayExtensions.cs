using System;

namespace Ambilight
{
    public static class ArrayExtensions
    {
        public static T Get2DimensionalMaxValue<T>(this T[,] inputArray, int lengthDimension2, int indexInDimension2) where T : IComparable<T>
        {
            if (lengthDimension2 < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(lengthDimension2));
            }
            if (indexInDimension2 < 0 || lengthDimension2 <= indexInDimension2)
            {
                throw new ArgumentOutOfRangeException(nameof(indexInDimension2));
            }

            var maxValue = default(T);
            var iterator = 0;

            foreach (var inputValue in inputArray)
            {
                if (iterator == indexInDimension2 && inputValue.CompareTo(maxValue) > 0)
                {
                    maxValue = inputValue;
                }
                iterator++;
                if (iterator == lengthDimension2)
                {
                    iterator = 0;
                }
            }

            return maxValue;
        }

        public static int Get2DimensionalLength<T>(this T[,] inputArray, int lengthDimension2)
        {
            if (inputArray.Length == 0)
            {
                throw new ArgumentException("Total length cannot be zero.", nameof(inputArray));
            }
            if (lengthDimension2 == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lengthDimension2));
            }
            return inputArray.Length / lengthDimension2;
        }
    }
}
