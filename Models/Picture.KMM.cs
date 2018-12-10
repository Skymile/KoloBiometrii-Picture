using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;

namespace Models
{
    public static class Algorithms
    {
        public static Picture KMM(Picture picture) =>
            InternalKMM.Apply(picture);

        private unsafe static class InternalKMM
        {
            public static Picture Apply(Picture picture) =>
                Apply(picture, picture.Width * 3, picture.Height);

            public static Picture Wrap(Picture picture, BitmapData data, Action<BitmapData> action)
            {
                try
                {
                    action(data);
                }
                finally
                {
                    picture.UnlockBits(data);
                }
                return picture;
            }

            private static List<int> GetBlackPixels(byte* ptr, int length, int offset)
            {
                var tmpOnes = new List<int>();
                for (int i = offset; i < length - offset; i += 3)
                    if (ptr[i] == One)
                        tmpOnes.Add(i);
                return tmpOnes;
            }

            public static void Apply(byte* ptr, int stride, int height)
            {
                int[] neighbours = Neighbours(stride);
                List<int> ones = GetBlackPixels(ptr, stride * height, stride + 3);

                bool AreSidesZeroes(int i) =>
                    ptr[i + 3] == Zero || ptr[i + stride] == Zero ||
                    ptr[i - 3] == Zero || ptr[i - stride] == Zero;

                bool AreCornersZeroes(int i) => ptr[i] != Two && (
                    ptr[i + stride + 3] == Zero || ptr[i + stride - 3] == Zero ||
                    ptr[i - stride + 3] == Zero || ptr[i - stride - 3] == Zero);

                Func<int, bool> Contains(HashSet<int> set) => 
                    i => set.Contains(ComputeSum(i));

                Func<int, bool> IsValueToDelete(byte value) =>
                    i => ptr[i] == value && Deletion.Contains(ComputeSum(i));

                Action<int> Stage(Func<int, bool> condition, byte setTo) =>
                    i =>
                    {
                        if (condition(i))
                            ptr[i] = ptr[i + 1] = ptr[i + 2] = setTo;
                    };

                var sequence = new List<Action<int>>
                {
                    Stage(AreSidesZeroes, Two),
                    Stage(AreCornersZeroes, Three),
                    Stage(IsValueToDelete(Four), Four),
                    Stage(IsValueToDelete(Four), Zero),
                    Stage(IsValueToDelete(Two), Zero),
                    Stage(IsValueToDelete(Three), Zero)
                };

                int count = 0;
                while (count != ones.Count)
                {
                    count = ones.Count;

                    sequence.ForEach(ones.ForEach);

                    ones = ones.Where(i => ptr[i] != Zero).ToList();
                }

                int ComputeSum(int sumOffset) => Enumerable.Range(0, 9)
                    .Where(i => ptr[sumOffset + neighbours[i]] != Zero)
                    .Sum(i => Matrix[i]);
            }

            private static int[] Neighbours(int stride) => new[]
            {
                stride  - 3,  stride + 0,  stride + 3,
                        - 3,         + 0,           3,
                -stride - 3, -stride + 0, -stride + 3
            };

            public static Picture Apply(Picture picture, int stride, int height) => 
                Wrap(picture,
                    picture.LockBits(ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb),
                    data => Apply((byte*)data.Scan0.ToPointer(), stride, height)
                );

            private static readonly int[] Matrix =
            {
                128,  1, 2,
                 64,  0, 4,
                 32, 16, 8
            };

            private const byte Zero = 255;
            private const byte One = 0;
            private const byte Two = 2;
            private const byte Three = 3;
            private const byte Four = 4;

            private static readonly HashSet<int> Deletion = new HashSet<int>(new int[] { 3, 5, 7, 12, 13, 14, 15, 20, 21, 22, 23, 28, 29, 30, 31, 48, 52, 53, 54, 55, 56, 60, 61, 62, 63, 65, 67, 69, 71, 77, 79, 80, 81, 83, 84, 85, 86, 87, 88, 89, 91, 92, 93, 94, 95, 97, 99, 101, 103, 109, 111, 112, 113, 115, 116, 117, 118, 119, 120, 121, 123, 124, 125, 126, 127, 131, 133, 135, 141, 143, 149, 151, 157, 159, 181, 183, 189, 191, 192, 193, 195, 197, 199, 205, 207, 208, 209, 211, 212, 213, 214, 215, 216, 217, 219, 220, 221, 222, 223, 224, 225, 227, 229, 231, 237, 239, 240, 241, 243, 244, 245, 246, 247, 248, 249, 251, 252, 253, 254, 255 });
            private static readonly HashSet<int> Fours = new HashSet<int>(new int[] { 3, 6, 12, 24, 48, 96, 192, 129, 7, 14, 28, 56, 112, 224, 193, 131, 15, 30, 60, 120, 240, 225, 195, 135 });
        }
    }

    public partial class Picture
    {
        public Picture KMM() => Algorithms.KMM(this);
    }
}
