using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows;
using Size = System.Drawing.Size;
using IronOcr;
using System.Text.RegularExpressions;
using System.Linq;

namespace ScoreScanner.Services
{
    internal class ImageProcessor
    {
        public static OcrResult ScanImage(string filepath, int x, int y, int h, int w)
        {
            var image = Image.FromFile(filepath);
            OcrResult result;
            var Ocr = new IronTesseract();
            using (var Input = new OcrInput())
            {
                var rectArea = new Rectangle() { X = x, Y = y, Height = h, Width = w };
                var contentArea = new CropRectangle(rectArea);
                Input.AddImage(image, contentArea);
                result = Ocr.Read(Input);
            }
            return result;
        }


        public static int[] ShrinkScanResult(OcrResult scan)
        {
            var result = new int[2];
            string p;

            var s = (scan.Text.Substring(scan.Text.IndexOf("Sidka60:") + 8, 7));
            s = s.Contains('b') ? s.Replace('b', '4') : s;
            result[0] = int.Parse(new String(s.Where(Char.IsDigit).ToArray()));

            if (!scan.Text.Contains("size:")) p = "1";
            else
            {
                p = scan.Text.Substring(scan.Text.IndexOf("size:") + 5, 2);
                result[1] = int.Parse(p.Remove(p.IndexOf(' '), 1));
            }

            return result;
        }


        public static Image Crop(Image img, Rectangle cropArea)
        {
            Bitmap bmpImage = new(img);
            return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
        }


        public static BitmapSource BitmapToBitmapSource(Bitmap bitmap)
        {
            return Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), 
                IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }


        public static Image Resize(Image img, Size size)
        {
            int sourceWidth = img.Width;
            int sourceHeight = img.Height;
            float nPercentW = size.Width / (float)sourceWidth;
            float nPercentH = size.Height / (float)sourceHeight;
            float nPercent;

            if (nPercentH < nPercentW) nPercent = nPercentH;
            else nPercent = nPercentW;

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap bmp = new(destWidth, destHeight);
            Graphics g = Graphics.FromImage(bmp);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.DrawImage(img, 0, 0, destWidth, destHeight);
            g.Dispose();

            return bmp;
        }
    }
}
