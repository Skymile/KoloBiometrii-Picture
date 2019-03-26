using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public class K3M : AlgorithmBase
	{
		public override unsafe void Apply(byte** p, int length, int width, int height)
		{
			byte* ptr = *p;
			int stride = length / height;
			int bpp = stride / width;

			int[] offsets =
			{
				-bpp - stride, -stride, bpp -stride,
				-bpp, 0, bpp,
				-bpp + stride,  stride, bpp + stride,
			};



		}

		private unsafe int Compute(byte* ptr, int[] offsets)
		{
			int sum = 0;
			for (int i = 0; i < offsets.Length; i++)
				if (ptr[offsets[i]] == Black)
					sum += offsets[i];
			return sum;
		}

		private readonly static int[] Matrix =
		{
			128,  1, 2,
			 64,  0, 4,
		     32, 16, 8
		};

		private const byte White = 255;
		private const byte Black = 0;

		private readonly static HashSet<int> A0    = new HashSet<int>(new[] { 3, 6, 7, 12, 14, 15, 24, 28, 30, 31, 48, 56, 60, 62, 63, 96, 112, 120, 124, 126, 127, 129, 131, 135, 143, 159, 191, 192, 193, 195, 199, 207, 223, 224, 225, 227, 231, 239, 240, 241, 243, 247, 248, 249, 251, 252, 253, 254 });
		private readonly static HashSet<int> A1    = new HashSet<int>(new[] { 7, 14, 28, 56, 112, 131, 193, 224 });
		private readonly static HashSet<int> A2    = new HashSet<int>(new[] { 7, 14, 15, 28, 30, 56, 60, 112, 120, 131, 135, 193, 195, 224, 225, 240 });
		private readonly static HashSet<int> A3    = new HashSet<int>(new[] { 7, 14, 15, 28, 30, 31, 56, 60, 62, 112, 120, 124, 131, 135, 143, 193, 195, 199, 224, 225, 227, 240, 241, 248 });
		private readonly static HashSet<int> A4    = new HashSet<int>(new[] { 7, 14, 15, 28, 30, 31, 56, 60, 62, 63, 112, 120, 124, 126, 131, 135, 143, 159, 193, 195, 199, 207, 224, 225, 227, 231, 240, 241, 243, 248, 249, 252 });
		private readonly static HashSet<int> A5    = new HashSet<int>(new[] { 7, 14, 15, 28, 30, 31, 56, 60, 62, 63, 112, 120, 124, 126, 131, 135, 143, 159, 191, 193, 195, 199, 207, 224, 225, 227, 231, 239, 240, 241, 243, 248, 249, 251, 252, 254 });
		private readonly static HashSet<int> A0pix = new HashSet<int>(new[] { 3, 6, 7, 12, 14, 15, 24, 28, 30, 31, 48, 56, 60, 62, 63, 96, 112, 120, 124, 126, 127, 129, 131, 135, 143, 159, 191, 192, 193, 195, 199, 207, 223, 224, 225, 227, 231, 239, 240, 241, 243, 247, 248, 249, 251, 252, 253, 254 });
	}
}
