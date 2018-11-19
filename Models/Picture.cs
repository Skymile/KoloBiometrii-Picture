using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Models
{
    public class Picture
    {
        public Picture(string filename) : this(new Bitmap(filename)) { }

        internal Picture(Bitmap bitmap) => this.bitmap = bitmap;

        public void Save(string filename) => this.bitmap.Save(filename);

        public BitmapSource Source => NativeMethods.GetBitmapSource(this.bitmap);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BitmapData LockBits(ImageLockMode mode) =>
            this.bitmap.LockBits(
                new Rectangle(Point.Empty, this.bitmap.Size), 
                mode, 
                this.bitmap.PixelFormat
            );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnlockBits(BitmapData data) =>
            this.bitmap.UnlockBits(data);

        public unsafe Picture ApplySobel()
        {
            Bitmap readBmp = this.bitmap;
            var writePicture = new Picture(
                new Bitmap(readBmp.Width, readBmp.Height, PixelFormat.Format24bppRgb)
            );

            BitmapData readData  = LockBits(ImageLockMode.ReadOnly);
            BitmapData writeData = writePicture.LockBits(ImageLockMode.WriteOnly);

            byte* r = (byte*)readData .Scan0.ToPointer();
            byte* w = (byte*)writeData.Scan0.ToPointer();

            int[] vertical =
            {
                1, 0, -1,
                2, 0, -2,
                1, 0, -1
            };

            int[] horizontal =
            {
                 1,  2,  1,
                 0,  0,  0,
                -1, -2, -1
            };

            for (int i = 1; i < readData.Height - 1; i++)
                for (int j = 3; j < readData.Stride - 3; j++)
                {
                    int offset = i * readData.Stride + j;

                    int sumVertical = 0, sumHorizontal = 0;
                    for (int x = -1; x < 2; x++)
                        for (int y = -1; y < 2; y++)
                        {
                            int value = r[offset + x * 3 + y * readData.Stride];

                            int internalOffset = (y + 1) * 3 + x + 1;

                            sumVertical += value * vertical[internalOffset];
                            sumHorizontal += value * horizontal[internalOffset];
                        }

                    int sum = (sumVertical * sumVertical + sumHorizontal * sumHorizontal) / 24;

                    w[offset] = (byte)sum;
                }

            UnlockBits(readData);
            writePicture.UnlockBits(writeData);

            return writePicture;
        }

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
                bitmap.PixelFormat
            );

            BitmapData oldData = this.bitmap.LockBits(
                new Rectangle(Point.Empty, this.bitmap.Size),
                ImageLockMode.ReadOnly,
                bitmap.PixelFormat
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

            int bpp = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

            for (int i = 0; i < histData.Width; ++i)
                for (int j = 0; j < histData.Height; ++j)
                {
                    int x = (int)(i * widthRatio);
                    int y = (int)(j * heightRatio);

                    int o = i * bpp + j * histData.Stride;

                    byte value = 255 - pixels[x] > y ? Byte.MaxValue : Byte.MinValue;

                    for (int b = 0; b < bpp; b++)
                        write[o + b] = value;
                }

            histogram.UnlockBits(histData);
            this.bitmap.UnlockBits(oldData);
            return new Picture(histogram);
        }

        public static unsafe Picture Apply(Picture readPicture, int[] matrix, int maskWidth)
        {
            var readBmp = readPicture.bitmap;
            var writeBmp = new Bitmap(readBmp.Width, readBmp.Height);
            var rect = new Rectangle(Point.Empty, writeBmp.Size);

            BitmapData writeData = writeBmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            BitmapData readData = readBmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            int matrixSum = matrix[0];
            for (int i = 1; i < matrix.Length; i++)
                matrixSum += matrix[i];

            if (matrixSum == 0)
                matrixSum = 1;

            int threshold = 0xFF * 0xFF;

            int stride = readBmp.Width * 3;
            int halfMaskHeight = matrix.Length / maskWidth >> 1;
            int height = readBmp.Height;

            int strideTrimmed = stride - 3;

            int heightOffset = height - halfMaskHeight;

            byte* read = (byte*)readData.Scan0.ToPointer();
            byte* write = (byte*)writeData.Scan0.ToPointer();

            if (height > 1000 && stride > 3000)
            {
                int chunk = heightOffset / 12;

                Task[] borders = new[]
                {
                    Task.Run(() =>
                    {
                        for (int i = 0; i < height; i++)
                        {
                            int o = i * stride;

                            write[o + 0] = read[o + 0];
                            write[o + 1] = read[o + 1];
                            write[o + 2] = read[o + 2];

                            write[o + stride - 1] = read[o + stride - 1];
                            write[o + stride - 2] = read[o + stride - 2];
                            write[o + stride - 3] = read[o + stride - 3];
                        }
                    }),

                    Task.Run(() =>
                    {
                        for (int i = 0; i < stride; i++)
                        {
                            write[i] = read[i];
                            int o = (height - 1) * stride + i;
                            write[o] = read[o];
                        }
                    })
                };

                var tasks = new Task[12];
                for (int t = 0; t < tasks.Length; t++)
                {
                    int ch = t * chunk;
                    tasks[t] = Task.Run(() => InternalLoop(ch, ch + chunk + 3));
                }

                InternalLoop(tasks.Length * chunk, heightOffset);
                Task.WaitAll(tasks);
                Task.WaitAll(borders);
            }
            else
            {
                for (int i = 0; i < height; i++)
                {
                    int o = i * stride;

                    write[o + 0] = read[o + 0];
                    write[o + 1] = read[o + 1];
                    write[o + 2] = read[o + 2];

                    write[o + stride - 1] = read[o + stride - 1];
                    write[o + stride - 2] = read[o + stride - 2];
                    write[o + stride - 3] = read[o + stride - 3];
                }

                for (int i = 0; i < stride; i++)
                {
                    write[i] = read[i];
                    int o = (height - 1) * stride + i;
                    write[o] = read[o];
                }

                InternalLoop(0, height - halfMaskHeight);
            }

            readBmp.UnlockBits(readData);
            writeBmp.UnlockBits(writeData);

            return new Picture(writeBmp);

            void InternalLoop(int offsetLeft, int offsetRight)
            {
                int center = (halfMaskHeight + offsetLeft) * stride + 6;
                int first = center - stride - 3;
                int sumR, sumG, sumB, offset, current;

                for (int i = halfMaskHeight + offsetLeft; i < offsetRight; ++i)
                    for (int j = 3; j < strideTrimmed; j += 3)
                    {
                        byte* r = read + first;

                        sumR = sumG = sumB = 0;
                        for (int k = 0; k < matrix.Length; k++)
                        {
                            current = matrix[k];
                            offset = (k % maskWidth) * 3 + (k / maskWidth) * stride;

                            sumR += r[offset + 0] * current;
                            sumG += r[offset + 1] * current;
                            sumB += r[offset + 2] * current;
                        }

                        byte* p = write + center;

                        p[1] = (byte)(sumR > 0 ? sumR > threshold ? Byte.MaxValue : (byte)(sumR / matrixSum) : Byte.MinValue);
                        p[0] = (byte)(sumR > 0 ? sumG > threshold ? Byte.MaxValue : (byte)(sumG / matrixSum) : Byte.MinValue);
                        p[2] = (byte)(sumR > 0 ? sumB > threshold ? Byte.MaxValue : (byte)(sumB / matrixSum) : Byte.MinValue);

                        center += 3;
                        first += 3;
                    }
            }
        }

        private readonly Bitmap bitmap;
    }
}
