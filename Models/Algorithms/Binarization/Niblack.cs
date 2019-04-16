using System;

namespace Models.Algorithms.Binarization
{
	public delegate double Compute(double std, double mean);

	public class Niblack : AlgorithmBase
	{
		public double Threshold { get; set; }

		protected Compute computeResult;

		public Niblack(double threshold)
		{
			this.Threshold = threshold;
			this.computeResult = (std, mean) => std * this.Threshold + mean;
		}

		public override unsafe void Apply(byte** ptr, int length, int width, int height)
		{
			byte* p = *ptr;

			int bpp = 3;
			int stride = width * bpp;

			int offset = stride + bpp;

			int[] offsets =
			{
				-bpp - stride, -stride, -stride + bpp,
				-bpp         ,       0,           bpp,
				-bpp + stride,  stride,  stride + bpp,
			};

			byte[] write = new byte[length];

			for (int i = offset; i < length - offset; i += 3)
			{
				int sum = 0;
				foreach (int o in offsets)
					sum += p[i + o];

				double mean = (double)sum / offsets.Length;
				double std = 0;

				foreach (int o in offsets)
					std += (p[i + o] - mean) * (p[i + o] - mean);
				std /= offsets.Length - 1;
				std = Math.Sqrt(std);

				double result = computeResult(std, mean);

				write[i] = write[i + 1] = write[i + 2] =
					p[i] >= result ? byte.MaxValue : byte.MinValue;
			}

			for (int i = 0; i < length; i++)
				p[i] = write[i];
		}
	}
}
