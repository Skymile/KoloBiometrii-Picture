namespace Models.Algorithms.Binarization
{
	public class Bernsen : AlgorithmBase
	{
		public Bernsen(int threshold) => this.Threshold = threshold;

		public int Threshold { get; private set; }

		public override unsafe void Apply(byte** ptr, int length, int width, int height)
		{
			byte* p = *ptr;

			int windowSize = 3;

			int bpp = 3;
			int stride = width * bpp;

			int[] offsets =
			{
				-bpp - stride, -stride, -stride + bpp,
				-bpp         ,       0,           bpp,
				-bpp + stride,  stride,  stride + bpp,
			};

			if (offsets.Length == 0)
				return;

			int offset = windowSize * bpp + windowSize * stride;

			byte[] write = new byte[length];

			for (int i = offset; i < write.Length - offset; i += bpp)
			{
				int min = p[i + offsets[0]];
				int max = p[i + offsets[0]];

				for (int j = 1; j < offsets.Length; ++j)
				{
					int o = i + offsets[j];

					if (p[o] > max)
						max = p[o];
					else if (p[o] < min)
						min = p[o];
				}

				int contrast = max - min;
				byte mean = (byte)((max + min) / 2);

				byte threshold = contrast < Threshold ? (byte)128 : mean;
				byte result = p[i] > threshold ? byte.MaxValue : byte.MinValue;

				for (int j = 0; j < bpp; j++)
					write[i + j] = result;
			}

			for (int i = 0; i < write.Length; ++i)
				p[i] = write[i];
		}
	}
}
