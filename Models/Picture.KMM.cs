using System.Collections.Generic;
using System.Drawing.Imaging;

namespace Models
{
	public static partial class Algorithms
    {
		public unsafe static Picture KMM(Picture picture)
		{
			var data = picture.LockBits(ImageLockMode.ReadWrite);

			byte* ptr = (byte*)data.Scan0.ToPointer();

			new InternalKMM().Apply(&ptr, data.Stride * data.Height, data.Width, data.Height);

			picture.UnlockBits(data);

			return picture;
		}

		public unsafe class InternalKMM : AlgorithmBase
        {
			public override void Apply(byte** p, int length, int width, int height)
			{
				byte* ptr = *p;

				int bpp = length / width / height;
				int stride = length / height;

				var tmpOnes = new List<int>();
				for (int i = stride + bpp; i < stride * height - stride - bpp; i += bpp)
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

					foreach (int i in ones)
						if (ptr[i] != Two && (
							ptr[i + stride + 3] == Zero || ptr[i + stride - 3] == Zero ||
							ptr[i - stride + 3] == Zero || ptr[i - stride - 3] == Zero))
						{
							ptr[i] = ptr[i + 1] = ptr[i + 2] = Three;
						}

					foreach (int i in ones)
						if (Fours.Contains(ComputeSum(i)))
							ptr[i] = ptr[i + 1] = ptr[i + 2] = Four;

					foreach (int i in ones)
						if (ptr[i] == Four && Deletion.Contains(ComputeSum(i)))
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
					for (int i = 0; i < ones.Length; i++)
						if (ptr[ones[i]] != Zero)
							tmp.Add(ones[i]);
					ones = tmp.ToArray();
				}

				int ComputeSum(int sumOffset)
				{
					int sum = 0;

					if (ptr[sumOffset + bpp] != Zero) sum += Matrix[5];
					if (ptr[sumOffset - bpp] != Zero) sum += Matrix[3];

					sumOffset += stride;

					if (ptr[sumOffset + bpp] != Zero) sum += Matrix[2];
					if (ptr[sumOffset      ] != Zero) sum += Matrix[1];
					if (ptr[sumOffset - bpp] != Zero) sum += Matrix[0];

					sumOffset -= stride + stride;

					if (ptr[sumOffset + bpp] != Zero) sum += Matrix[8];
					if (ptr[sumOffset      ] != Zero) sum += Matrix[7];
					if (ptr[sumOffset - bpp] != Zero) sum += Matrix[6];

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
            private const byte One   =   0;
            private const byte Two   =   2;
            private const byte Three =   3;
            private const byte Four  =   4;

            private static readonly HashSet<int> Deletion = new HashSet<int>(new int[] { 3, 5, 7, 12, 13, 14, 15, 20, 21, 22, 23, 28, 29, 30, 31, 48, 52, 53, 54, 55, 56, 60, 61, 62, 63, 65, 67, 69, 71, 77, 79, 80, 81, 83, 84, 85, 86, 87, 88, 89, 91, 92, 93, 94, 95, 97, 99, 101, 103, 109, 111, 112, 113, 115, 116, 117, 118, 119, 120, 121, 123, 124, 125, 126, 127, 131, 133, 135, 141, 143, 149, 151, 157, 159, 181, 183, 189, 191, 192, 193, 195, 197, 199, 205, 207, 208, 209, 211, 212, 213, 214, 215, 216, 217, 219, 220, 221, 222, 223, 224, 225, 227, 229, 231, 237, 239, 240, 241, 243, 244, 245, 246, 247, 248, 249, 251, 252, 253, 254, 255 });
            private static readonly HashSet<int> Fours = new HashSet<int>(new int[] { 3, 6, 12, 24, 48, 96, 192, 129, 7, 14, 28, 56, 112, 224, 193, 131, 15, 30, 60, 120, 240, 225, 195, 135 });
        }
    }

    public partial class Picture
    {
        public Picture KMM() => Algorithms.KMM(this);
    }
}
