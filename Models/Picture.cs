using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Models
{
    public unsafe class Picture
    {
        public Picture(string filename) : this(new Bitmap(filename)) { }

        public Picture(Picture picture, PixelFormat format) : 
            this(new Bitmap(picture.bitmap.Width, picture.bitmap.Height, format)) { }

        internal Picture(Bitmap bitmap) => this.bitmap = bitmap;

        public void Save(string filename) => this.bitmap.Save(filename);

        public Picture KMM()
        {
            int width  = this.bitmap.Width;
            int height = this.bitmap.Height;
            int stride = width * 3;

            var rect = new Rectangle(Point.Empty, bitmap.Size);

            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            byte* ptr = (byte*)data.Scan0.ToPointer();

            var tmpOnes = new List<int>();
            for (int i = stride + 3; i < stride * height - stride - 3; i += 3)
                if (ptr[i] == One)
                    tmpOnes.Add(i);
            int[] ones = tmpOnes.ToArray();

            bool hasDeleted = true;
            while (hasDeleted)
            {
                hasDeleted = false;

                foreach (int i in ones)
                    if (ptr[i + 3] == Zero || ptr[i + stride] == Zero ||
                        ptr[i - 3] == Zero || ptr[i - stride] == Zero)
                    {
                        ptr[i] = ptr[i + 1] = ptr[i + 2] = Two;
                    }
                    else if (ptr[i + stride + 3] == Zero || ptr[i + stride - 3] == Zero ||
                             ptr[i - stride + 3] == Zero || ptr[i - stride - 3] == Zero)
                    {
                        ptr[i] = ptr[i + 1] = ptr[i + 2] = Three;
                    }

                foreach (int i in ones)
                    if (Fours.Contains(ComputeSum(i)))
                        ptr[i] = ptr[i + 1] = ptr[i + 2] = Zero;

                foreach (int i in ones)
                    if (ptr[i] == Two && Deletion.Contains(ComputeSum(i)))
                    {
                        ptr[i] = ptr[i + 1] = ptr[i + 2] = Zero;
                        hasDeleted = true;
                    }

                foreach (int i in ones)
                    if (ptr[i] == Three && Deletion.Contains(ComputeSum(i)))
                    {
                        ptr[i] = ptr[i + 1] = ptr[i + 2] = Zero;
                        hasDeleted = true;
                    }

                var tmp = new List<int>(ones.Length >> 1);
                foreach (var i in ones)
                    if (ptr[i] != Zero)
                        tmp.Add(i);
                ones = tmp.ToArray();
            }
            bitmap.UnlockBits(data);
            return this;

            int ComputeSum(int sumOffset)
            {
                int sum = 0;

                if (ptr[sumOffset + 3] != Zero)
                    sum += Matrix[5];
                if (ptr[sumOffset - 3] != Zero)
                    sum += Matrix[3];

                sumOffset += stride;

                if (ptr[sumOffset + 3] != Zero)
                    sum += Matrix[2];
                if (ptr[sumOffset] != Zero)
                    sum += Matrix[1];
                if (ptr[sumOffset - 3] != Zero)
                    sum += Matrix[0];

                sumOffset -= stride + stride;

                if (ptr[sumOffset + 3] != Zero)
                    sum += Matrix[8];
                if (ptr[sumOffset] != Zero)
                    sum += Matrix[7];
                if (ptr[sumOffset - 3] != Zero)
                    sum += Matrix[6];

                return sum;
            }
        }

        private static readonly int[] Matrix =
        {
            128,  1, 2,
             64,  0, 4,
             32, 16, 8
        };

        private const byte Zero  = 255;
        private const byte One   = 0;
        private const byte Two   = 2;
        private const byte Three = 3;

        private static HashSet<int> ComputeFours()
        {
            var set = new HashSet<int>(new int[] { 3, 6, 12, 24, 48, 96, 192, 129, 7, 14, 28, 56, 112, 224, 193, 131, 15, 30, 60, 120, 240, 225, 195, 135 });
            set.IntersectWith(Deletion);
            return set;
        }

        private static readonly HashSet<int> Deletion = new HashSet<int>(new int[] { 3, 5, 7, 12, 13, 14, 15, 20, 21, 22, 23, 28, 29, 30, 31, 48, 52, 53, 54, 55, 56, 60, 61, 62, 63, 65, 67, 69, 71, 77, 79, 80, 81, 83, 84, 85, 86, 87, 88, 89, 91, 92, 93, 94, 95, 97, 99, 101, 103, 109, 111, 112, 113, 115, 116, 117, 118, 119, 120, 121, 123, 124, 125, 126, 127, 131, 133, 135, 141, 143, 149, 151, 157, 159, 181, 183, 189, 191, 192, 193, 195, 197, 199, 205, 207, 208, 209, 211, 212, 213, 214, 215, 216, 217, 219, 220, 221, 222, 223, 224, 225, 227, 229, 231, 237, 239, 240, 241, 243, 244, 245, 246, 247, 248, 249, 251, 252, 253, 254, 255 });
        private static readonly HashSet<int> Fours = ComputeFours();

        public Picture ApplySobel()
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

        public Picture Histogram(Size? size = null)
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

        public Picture Apply(int[] matrix, int maskWidth)
        {
            var writePicture = new Picture(this, PixelFormat.Format24bppRgb);

            BitmapData writeData = writePicture.LockBits(ImageLockMode.WriteOnly);
            BitmapData readData  = LockBits(ImageLockMode.ReadOnly);

            int matrixSum = GetMatrixSum(matrix);

            int stride = this.Width * 3;

            int halfMaskHeight = matrix.Length / maskWidth >> 1;
            int height = this.Height;

            int threshold = Byte.MaxValue * matrixSum;
            int strideTrimmed = stride - 3;
            int heightOffset = height - halfMaskHeight;

            IntPtr readHandle = readData.Scan0;
            IntPtr writeHandle = writeData.Scan0;

            byte* read  = (byte*)readHandle.ToPointer();
            byte* write = (byte*)writeHandle.ToPointer();

            if (height + stride > 400)
            {
                int parallelCount = Environment.ProcessorCount;

                Task[] borders = new[]
                {
                    Task.Run(() => CopyHorizontalBorder(readHandle, writeHandle, height, stride)),
                    Task.Run(() => CopyVerticalBorder(readHandle, writeHandle, height, stride))
                };

                int chunk = heightOffset / parallelCount;
                var tasks = new Task[parallelCount];

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
                CopyHorizontalBorder(readHandle, writeHandle, height, stride);
                CopyVerticalBorder(readHandle, writeHandle, height, stride);
                InternalLoop(0, heightOffset);
            }

            UnlockBits(readData);
            writePicture.UnlockBits(writeData);

            return writePicture;

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
                            offset = k % maskWidth * 3 + k / maskWidth * stride;

                            sumR += r[offset + 0] * current;
                            sumG += r[offset + 1] * current;
                            sumB += r[offset + 2] * current;
                        }

                        byte* p = write + center;

                        p[0] = sumR > 0 ? sumR > threshold ? Byte.MaxValue : (byte)(sumR / matrixSum) : Byte.MinValue;
                        p[1] = sumG > 0 ? sumG > threshold ? Byte.MaxValue : (byte)(sumG / matrixSum) : Byte.MinValue;
                        p[2] = sumB > 0 ? sumB > threshold ? Byte.MaxValue : (byte)(sumB / matrixSum) : Byte.MinValue;

                        center += 3;
                        first += 3;
                    }
            }
        }

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

        public BitmapSource Source => NativeMethods.GetBitmapSource(this.bitmap);

        public int Width => bitmap.Width;

        public int Height => bitmap.Height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetMatrixSum(in int[] matrix)
        {
            int sum = matrix[0];
            for (int i = 1; i < matrix.Length; i++)
                sum += matrix[i];
            return sum == 0 ? 1 : sum;
        }

        private void CopyVerticalBorder(IntPtr read, IntPtr write, int height, int stride)
        {
            byte* r = (byte*)read.ToPointer();
            byte* w = (byte*)write.ToPointer();

            int o = (height - 1) * stride;
            for (int i = 0; i < stride; ++i)
            {
                w[i] = r[i];
                w[i + o] = r[i + o];
            }
        }

        private void CopyHorizontalBorder(IntPtr read, IntPtr write, int height, int stride)
        {
            byte* r = (byte*)read.ToPointer();
            byte* w = (byte*)write.ToPointer();

            int o = 0;
            for (int i = 0; i < height; ++i)
            {
                w[o + 0] = r[o + 0];
                w[o + 1] = r[o + 1];
                w[o + 2] = r[o + 2];

                w[o + stride - 1] = r[o + stride - 1];
                w[o + stride - 2] = r[o + stride - 2];
                w[o + stride - 3] = r[o + stride - 3];

                o += stride;
            }
        }


        private readonly Bitmap bitmap;
    }
}
