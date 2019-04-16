using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public class ZhangSuen : AlgorithmBase
	{
		public override unsafe void Apply(byte** p, int length, int width, int height)
		{
			byte* ptr = *p;

			int stride = length / height;
			int bpp = stride / width;

			int[] offsets =
			{
				-bpp - stride, -stride, bpp -stride,
				-bpp, bpp,
				-bpp + stride,  stride, bpp + stride,
			};

			int[] circular =
			{
				-stride, -stride + bpp, bpp, stride + bpp, 
				stride, stride - bpp, -bpp, -stride - bpp,
				-stride
			};

			int offset = stride + bpp;

			for (int i = offset; i < length - offset; i += bpp)
				if (ptr[i] == Black)
				{
					int count = BlackNeighbours(ptr + i, offsets);

					if (2 <= count && count <= 6 &&
						Transitions(ptr + i, circular) == 1)
					{
						byte p2 = ptr[i - stride];
						byte p4 = ptr[i + bpp];
						byte p6 = ptr[i + stride];
						byte p8 = ptr[i - bpp];

						if ((p2 == White || p4 == White || p6 == White) &&
							(p8 == White || p4 == White || p6 == White))
						{
							for (int k = 0; k < bpp; k++)
								ptr[i + k] = White;
						}

						if ((p2 == White || p4 == White || p8 == White) &&
							(p2 == White || p6 == White || p8 == White))
						{
							for (int k = 0; k < bpp; k++)
								ptr[i + k] = White;
						}
					}
				}

		}

		private unsafe int Transitions(byte* p, int[] circular)
		{
			int count = 0;
			for (int i = 0; i < circular.Length - 1; i++)
				if (p[circular[i]] == White && p[circular[i + 1]] == Black)
					++count;
			return count;
		}

		private unsafe int BlackNeighbours(byte* p, int[] offsets)
		{
			int count = 0;
			for (int i = 0; i < offsets.Length; i++)
				if (p[offsets[i]] == Black)
					++count;
			return count;
		}

		private const byte White = 255;
		private const byte Black =   0;
	}
}
