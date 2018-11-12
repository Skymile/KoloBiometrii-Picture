using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Media.Imaging;

namespace Models
{
    public class Picture
    {
        public Picture(string filename) : this(new Bitmap(filename)) { }

        internal Picture(Bitmap bitmap) => this.bitmap = bitmap;

        public void Save(string filename) => this.bitmap.Save(filename);

        public BitmapSource Source => NativeMethods.GetBitmapSource(this.bitmap);

        public unsafe Picture Histogram(Size? size = null)
        {
            if (this.bitmap is null)
                throw new NullReferenceException($"{nameof(this.bitmap)} was null");

            Bitmap histogram = size is null
                ? new Bitmap(this.bitmap.Width, this.bitmap.Height)
                : new Bitmap(size.Value.Width, size.Value.Height);

            BitmapData histData = histogram.LockBits(
                new Rectangle(Point.Empty, histogram.Size),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb
            );

            BitmapData oldData = this.bitmap.LockBits(
                new Rectangle(Point.Empty, this.bitmap.Size),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb
            );

            byte* read = (byte*)oldData.Scan0.ToPointer();
            byte* write = (byte*)histData.Scan0.ToPointer();

            int len = oldData.Stride * oldData.Height;

            int[] pixels = new int[256];
            for (int i = 0; i < len; i += 3)
                ++pixels[read[i]];

            int max = pixels.Max();

            double ratio = histData.Height / (double)max;

            for (int i = 0; i < pixels.Length; ++i)
                pixels[i] = (int)(pixels[i] * ratio);

            double widthRatio  = 256.0 / histData.Width;
            double heightRatio = 256.0 / histData.Height;

            for (int i = 0; i < histData.Width; ++i)
                for (int j = 0; j < histData.Height; ++j)
                {
                    int x = (int)(i * widthRatio);
                    int y = (int)(j * heightRatio);

                    int o = i * 3 + j * histData.Stride;

                    int range = 255 - pixels[x];

                    write[o] = write[o + 1] = write[o + 2] =
                        range > y ? Byte.MaxValue : Byte.MinValue;
                }

            histogram.UnlockBits(histData);
            this.bitmap.UnlockBits(oldData);
            return new Picture(histogram);
        }

        public unsafe Picture Apply(int[] matrix)
        {
            var newBmp = new Bitmap(this.bitmap.Width, this.bitmap.Height);

            BitmapData newData = newBmp.LockBits(
                new Rectangle(Point.Empty, newBmp.Size),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb
            );

            BitmapData oldData = this.bitmap.LockBits(
                new Rectangle(Point.Empty, this.bitmap.Size),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb
            );

            byte* read = (byte*)oldData.Scan0.ToPointer();
            byte* write = (byte*)newData.Scan0.ToPointer();

            int matrixSum = matrix.Sum();
            if (matrixSum == 0)
                matrixSum = 1;

            int[] offsets = new int[9];
            for (int x = 3; x < oldData.Stride - 3; x++)
                for (int y = 1; y < this.bitmap.Height - 1; y++)
                {
                    int offset = x - 1 + (y - 1) * oldData.Stride;

                    for (int i = 0; i < 3; i++)
                        for (int j = 0; j < 3; j++)
                            offsets[i + j * 3] =
                                offset + i * 3 + j * oldData.Stride;

                    int sum = 0;
                    for (int i = 0; i < 9; i++)
                        sum += read[offsets[i]] * matrix[i];
                    sum /= matrixSum;

                    write[offsets[4]] =
                        sum < 0 ? Byte.MinValue :
                        sum > 255 ? Byte.MaxValue : (byte)sum;
                }

            this.bitmap.UnlockBits(oldData);
            newBmp.UnlockBits(newData);

            return new Picture(newBmp);
        }

        private readonly Bitmap bitmap;
    }
}
