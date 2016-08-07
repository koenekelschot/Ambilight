using System;

namespace Ambilight
{
    public class HsvColor
    {
        public double Hue;
        public double Saturation;
        public double Value;

        public HsvColor(double hue, double saturation, double value)
        {
            if (hue < 0 || hue > 360)
            {
                throw new ArgumentOutOfRangeException(nameof(hue), "Value must be between 0 and 360");
            }
            if (saturation < 0 || saturation > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(saturation), "Value must be between 0 and 360");
            }
            if (value < 0 || value > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0 and 360");
            }

            Hue = hue;
            Saturation = saturation;
            Value = value;
        }
    }
}
