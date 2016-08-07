using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO.Ports;
using System.Windows.Forms;

namespace Ambilight
{
    static class Program
    {
        // CONFIGURABLE PROGRAM CONSTANTS --------------------------------------------

        // Minimum LED brightness; some users prefer a small amount of backlighting
        // at all times, regardless of screen content. Higher values are brighter,
        // or set to 0 to disable this feature.
        private static readonly short MinBrightness = 120;

        // LED transition speed; it's sometimes distracting if LEDs instantaneously
        // track screen contents (such as during bright flashing sequences), so this
        // feature enables a gradual fade to each new LED state. Higher numbers yield
        // slower transitions (max of 255), or set to 0 to disable this feature
        // (immediate transition of all LEDs).
        private static readonly short Fade = 75;

        // This enables LED color to more closely match on-screen color; defines the 
        // amount of pure white, yellow, red and blue in a light. Another way to think 
        // of the color temperature is how 'warm' or 'cool' the white LED light bulb 
        // appears. Color temperature is measured in degrees Kelvin and is a measure 
        // of the part of the color spectrum that is found in light. Higher values 
        // yield a more blue-ish color (max of 40000) and lower values yield a more 
        // red-ish color (min of 0). Set to 6600 to disable this feature.
        private static readonly int AdjustedColorTemperature = 6600;

        // Whether or not to show a preview
        private static readonly bool Preview = true;

        // Pixel size for the live preview image.
        private static readonly int PixelSize = 20;

        // Serial device timeout (in milliseconds), for locating Arduino device
        // running the corresponding LEDstream code. See notes later in the code...
        // in some situations you may want to entirely comment out that block.
        private static readonly int Timeout = 5000; // 5 seconds

        // PER-LED INFORMATION -------------------------------------------------------

        // This array contains the 2D coordinates corresponding to each pixel in the
        // LED strand, in the order that they're connected (i.e. the first element
        // here belongs to the first LED in the strand, second element is the second
        // LED, and so forth). Each triplet in this array consists of a display
        // number (an index into the display array above, NOT necessarily the same as
        // the system screen number) and an X and Y coordinate specified in the grid
        // units given for that display. {0,0,0} is the top-left corner (when facing 
        // the screen) of the first // display in the array. Modify this to match 
        // your own setup:
        private static readonly int[,] Leds = {
            {0,0,27},{0,1,27},{0,2,27},{0,3,27},{0,4,27},{0,5,27},{0,6,27},{0,7,27},{0,8,27},{0,9,27},{0,10,27},{0,11,27},{0,12,27},{0,13,27},{0,14,27},{0,15,27},{0,16,27},{0,17,27},{0,18,27},{0,19,27},{0,20,27},{0,21,27},{0,22,27},{0,23,27},{0,24,27},{0,25,27},{0,26,27},{0,27,27},{0,28,27},{0,29,27},{0,30,27},{0,31,27},{0,32,27},{0,33,27},{0,34,27},{0,35,27},{0,36,27},{0,37,27},{0,38,27},{0,39,27},{0,40,27},{0,41,27},{0,42,27},{0,43,27},{0,44,27},{0,45,27},{0,46,27},{0,47,27},{0,48,27},{0,49,27},{0,50,27},
            {0,50,26},{0,50,25},{0,50,24},{0,50,23},{0,50,22},{0,50,21},{0,50,20},{0,50,19},{0,50,18},{0,50,17},{0,50,16},{0,50,15},{0,50,14},{0,50,13},{0,50,12},{0,50,11},{0,50,10},{0,50,9},{0,50,8},{0,50,7},{0,50,6},{0,50,5},{0,50,4},{0,50,3},{0,50,2},{0,50,1},
            {0,50,0},{0,49,0},{0,48,0},{0,47,0},{0,46,0},{0,45,0},{0,44,0},{0,43,0},{0,42,0},{0,41,0},{0,40,0},{0,39,0},{0,38,0},{0,37,0},{0,36,0},{0,35,0},{0,34,0},{0,33,0},{0,32,0},{0,31,0},{0,30,0},{0,29,0},{0,28,0},{0,27,0},{0,26,0},{0,25,0},{0,24,0},{0,23,0},{0,22,0},{0,21,0},{0,20,0},{0,19,0},{0,18,0},{0,17,0},{0,16,0},{0,15,0},{0,14,0},{0,13,0},{0,12,0},{0,11,0},{0,10,0},{0,9,0},{0,8,0},{0,7,0},{0,6,0},{0,5,0},{0,4,0},{0,3,0},{0,2,0},{0,1,0},{0,0,0},
            {0,0,1},{0,0,2},{0,0,3},{0,0,4},{0,0,5},{0,0,6},{0,0,7},{0,0,8},{0,0,9},{0,0,10},{0,0,11},{0,0,12},{0,0,13},{0,0,14},{0,0,15},{0,0,16},{0,0,17},{0,0,18},{0,0,19},{0,0,20},{0,0,21},{0,0,22},{0,0,23},{0,0,24},{0,0,25},{0,0,26}
        };

        // INITIALIZATION ------------------------------------------------------------
        private static readonly int NumHorizontalLeds = Leds.Get2DimensionalMaxValue(3, 1) + 1;
        private static readonly int NumVerticalLeds = Leds.Get2DimensionalMaxValue(3, 2) + 1;
        private static readonly int NumLeds = Leds.Get2DimensionalLength(3);
        private static readonly Color[] PrevColors = new Color[NumLeds];
        private static readonly Color[] LedColors = new Color[NumLeds];

        private static PreviewForm _previewForm;
        private static readonly int PreviewWidth = NumHorizontalLeds * PixelSize;
        private static readonly int PreviewHeight = NumVerticalLeds * PixelSize;

        private static SerialPort _port = null;
        private static readonly string MagicWord = "Ada";
        private static readonly byte[] SerialData = new byte[6 + NumLeds * 3];

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (Preview)
            {
                _previewForm = new PreviewForm();
                _previewForm.Show(null);

                _previewForm.preview.Width = PreviewWidth;
                _previewForm.preview.Height = PreviewHeight;
                _previewForm.Width = PreviewWidth + 20;
                _previewForm.Height = PreviewHeight + 40;
            }

            _port = FindPort();
            if (_port != null && !_port.IsOpen)
            {
                _port.Open();
            }
            SetSerialDataFrameHeader();

            var worker = new BackgroundWorker();
            worker.DoWork += (sender, args) => Loop();
            worker.RunWorkerAsync();

            Application.Run();
        }
        
        private static void Loop()
        {

            using (Bitmap capture = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height), ledBitmap = new Bitmap(NumHorizontalLeds, NumVerticalLeds))
            using (Graphics captureGraphics = Graphics.FromImage(capture), ledGraphics = Graphics.FromImage(ledBitmap))
            {
                while (true)
                {
                    captureGraphics.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, capture.Size, CopyPixelOperation.SourceCopy);
                    ledGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic; //the Processing code most closely resembles 'Default'
                    ledGraphics.DrawImage(capture, 0, 0, NumHorizontalLeds, NumVerticalLeds);

                    FillColorArray(ledBitmap);
                    SendSerialData();

                    if (Preview)
                    {
                        DrawFramePreview();
                    }
                }
            }
        }

        private static void FillColorArray(Bitmap ledBitmap)
        {
            for (var i = 0; i < NumLeds; i++)
            {
                var currentColor = ledBitmap.GetPixel(Leds[i, 1], Leds[i, 2]);
                LedColors[i] = currentColor.AdjustTemperature(AdjustedColorTemperature).Transition(PrevColors[i], Fade);
                PrevColors[i] = currentColor;
            }
        }

        private static void DrawFramePreview()
        {
            using (var previewBitmap = new Bitmap(PreviewWidth, PreviewHeight))
            using (var previewGraphics = Graphics.FromImage(previewBitmap))
            {
                previewGraphics.FillRectangle(new SolidBrush(Color.Black), 0, 0, PreviewWidth, PreviewHeight);
                for (var i = 0; i < NumLeds; i++)
                {
                    var ledX = Leds[i, 1];
                    var ledY = Leds[i, 2];
                    var pixelX = ledX*PixelSize;
                    var pixelY = ledY*PixelSize;
                    previewGraphics.FillRectangle(new SolidBrush(LedColors[i]), pixelX, pixelY, PixelSize, PixelSize);
                }

                _previewForm.preview.Image?.Dispose();
                _previewForm.preview.Image = previewBitmap.Clone(new Rectangle(0, 0, PreviewWidth, PreviewHeight), PixelFormat.DontCare);
            }
        }

        private static SerialPort FindPort()
        {
            var sw = new Stopwatch();
            var ports = SerialPort.GetPortNames();
            foreach (var port in ports)
            {
                var serialPort = new SerialPort(port, 115200);
                try
                {
                    serialPort.Open();
                }
                catch (Exception)
                {
                    //Can't open port, probably in use by other software.
                    continue;
                }
                
                sw.Restart();
                // Port open...watch for acknowledgement string...
                while (sw.ElapsedMilliseconds < Timeout)
                {
                    var ack = serialPort.ReadExisting();
                    if (!string.IsNullOrEmpty(ack) && ack.Contains($"{MagicWord}\n"))
                    {
                        sw.Stop();
                        return serialPort;
                    }
                }
                serialPort.Close();
            }

            // Didn't locate a device returning the acknowledgment string.
            // Maybe it's out there but running the old LEDstream code, which
            // didn't have the ACK. Can't say for sure, so we'll take our
            // changes with the first/only serial device out there...
            if (ports.Length == 1)
            {
                return new SerialPort(ports[0], 115200);
            }
            return null;
        }

        private static void SetSerialDataFrameHeader()
        {
            // A special header / magic word is expected by the corresponding LED
            // streaming code running on the Arduino. This only needs to be initialized
            // once (not in draw() loop) because the number of LEDs remains constant.
            SerialData[0] = Convert.ToByte(MagicWord[0]); // Magic word
            SerialData[1] = Convert.ToByte(MagicWord[1]);
            SerialData[2] = Convert.ToByte(MagicWord[2]);
            SerialData[3] = (byte)((NumLeds - 1) >> 8); // LED count high byte
            SerialData[4] = (byte)((NumLeds - 1) & 0xff); // LED count low byte
            SerialData[5] = (byte)(SerialData[3] ^ SerialData[4] ^ 0x55); // Checksum
        }

        private static void SendSerialData()
        {
            var serialOffset = 6;
            foreach (var c in LedColors)
            {
                SerialData[serialOffset++] = c.R;
                SerialData[serialOffset++] = c.G;
                SerialData[serialOffset++] = c.B;
            }
            if (_port != null && _port.IsOpen)
            {
                _port.Write(SerialData, 0, SerialData.Length);
            }
        }
    }
}
