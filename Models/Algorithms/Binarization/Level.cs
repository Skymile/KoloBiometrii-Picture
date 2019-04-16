namespace Models.Algorithms.Binarization
{
	public class Level : AlgorithmBase
	{
		public Level(byte threshold) => this.Threshold = threshold;

		public byte Threshold { get; private set; }

		public override unsafe void Apply(byte** ptr, int length, int width, int height)
		{
			byte* p = *ptr;

			for (int i = 0; i < length; i++)
				p[i] = p[i] > this.Threshold ? byte.MaxValue : byte.MinValue;
		}
	}
}
