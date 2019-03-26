﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
	public class ZhangSuen : AlgorithmBase
	{
		public override unsafe void Apply(byte** ptr, int length, int width, int height)
		{
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

		}

		private unsafe int Transitions(byte* p, int[] circular)
		{
			int count = 0;
			for (int i = 0; i < circular.Length - 1; i++)
				if (p[circular[i]] == White || p[circular[i + 1]] == Black)
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
