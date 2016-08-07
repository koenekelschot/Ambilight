using System;
using System.Drawing;

namespace Ambilight
{
    public static class ColorExtensions
    {
        /// <summary>Interpolates the specified colors based on the duration (transition speed).</summary>
        /// <param name="color">Color to transition to.</param>
        /// <param name="prevColor">Color to transition from.</param>
        /// <param name="duration">Speed of the transition.</param>
        /// <returns>The interpolated color.</returns>
        public static Color Transition(this Color color, Color prevColor, int duration)
        {
            duration = duration > 255 ? 255 : duration < 0 ? 0 : duration;
            if (duration == 0)
                return color;

            var r = color.R + (int)((double)(prevColor.R - color.R)) / duration;
            var g = color.G + (int)((double)(prevColor.G - color.G)) / duration;
            var b = color.B + (int)((double)(prevColor.B - color.B)) / duration;

            return Color.FromArgb(r, g, b);
        }

        //The following code converts between RGB and HSV using the algorithms described on Wikipedia. 
        //http://en.wikipedia.org/wiki/HSL_and_HSV
        //The ranges are 0 - 360 for hue, and 0 - 1 for saturation or value.
        public static HsvColor ToHsv(this Color color)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            double hue = color.GetHue();
            double saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            double value = max / 255d;

            return new HsvColor(hue, saturation, value);
        }

        public static Color ToRgb(this HsvColor color)
        {
            var hi = Convert.ToInt32(Math.Floor(color.Hue / 60)) % 6;
            var f = color.Hue / 60 - Math.Floor(color.Hue / 60);

            var value = color.Value * 255;
            var v = Convert.ToInt32(value);
            var p = Convert.ToInt32(value * (1 - color.Saturation));
            var q = Convert.ToInt32(value * (1 - f * color.Saturation));
            var t = Convert.ToInt32(value * (1 - (1 - f) * color.Saturation));

            switch (hi)
            {
                case 0:
                    return Color.FromArgb(255, v, t, p);
                case 1:
                    return Color.FromArgb(255, q, v, p);
                case 2:
                    return Color.FromArgb(255, p, v, t);
                case 3:
                    return Color.FromArgb(255, p, q, v);
                case 4:
                    return Color.FromArgb(255, t, p, v);
            }
            return Color.FromArgb(255, v, p, q);
        }

        //http://www.tannerhelland.com/4435/convert-temperature-rgb-algorithm-code/
        public static Color AdjustTemperature(this Color color, int targetTemperature)
        {
            if (targetTemperature == 6600) 
                return color;

            var targetColor = GetTemperature(targetTemperature);
            var hsvColor = color.ToHsv();

            //Blend the original and new RGB values using the specified strength
            var blendedColor = color.Blend(targetColor, 50);
            //luminance is being preserved, we need to determine the initial luminance value
            var blendedHsvColor = blendedColor.ToHsv();
            blendedHsvColor.Value = hsvColor.Value;

            return blendedHsvColor.ToRgb();
        }

        //http://www.tannerhelland.com/4435/convert-temperature-rgb-algorithm-code/
        //tempStrength is a user-submitted value between 1 and 100, then divided by 200 to yield a floating-point in the range [0.005, 0.5]
        private static Color Blend(this Color color, Color targetColor, double tempStrength)
        {
            tempStrength /= 200;

            var r = color.R*(1 - tempStrength) + targetColor.R*tempStrength;
            var g = color.G*(1 - tempStrength) + targetColor.G*tempStrength;
            var b = color.B*(1 - tempStrength) + targetColor.B*tempStrength;

            return Color.FromArgb(0, (int)r, (int)g, (int)b);
        }

        //http://www.tannerhelland.com/4435/convert-temperature-rgb-algorithm-code/
        public static Color GetTemperature(long tempKelvin)
        {
            double red, green, blue;

            //Temperature must fall between 1000 and 40000 degrees
            tempKelvin = tempKelvin < 1000 ? tempKelvin : tempKelvin > 40000 ? 40000 : tempKelvin;
            //All calculations require tmpKelvin / 100, so only do the conversion once
            tempKelvin /= 100;

            if (tempKelvin <= 66)
            {
                red = 255;
            }
            else
            {
                //Note: the R-squared value for this approximation is .988
                red = 329.698727446*Math.Pow(tempKelvin - 60, -0.1332047592);
            }

            if (tempKelvin <= 66)
            {
                //Note: the R-squared value for this approximation is .996
                green = 99.4708025861*Math.Log(tempKelvin) - 161.1195681661;
            }
            else
            {
                //Note: the R-squared value for this approximation is .987
                green = 288.1221695283*Math.Pow(tempKelvin - 60, -0.0755148492);
            }

            if (tempKelvin >= 66)
            {
                blue = 255;
            }
            else if (tempKelvin <= 19)
            {
                blue = 0;
            }
            else
            {
                //Note: the R-squared value for this approximation is .998
                blue = 138.5177312231*Math.Log(tempKelvin - 10) - 305.0447927307;
            }

            red = red < 0 ? 0 : red > 255 ? 255 : red;
            green = green < 0 ? 0 : green > 255 ? 255 : green;
            blue = blue < 0 ? 0 : blue > 255 ? 255 : blue;

            return Color.FromArgb(0, (int)red, (int)green, (int)blue);
        }
    }
}
