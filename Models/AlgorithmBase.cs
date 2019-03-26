namespace Models
{
	public abstract unsafe class AlgorithmBase
	{
		public virtual void Apply(byte** ptr, int length, int width, int height)
		{
			byte[] write = new byte[length];
			fixed (byte* p = write)
			{
				Apply(&p, *ptr, length, width, height);
				*ptr = p;
			}
		}

		public virtual void Apply(byte** write, byte* read, int length, int width, int height)
		{
			for (int i = 0; i < length; i++)
				*write[i] = read[i];
			Apply(write, length, width, height);
		}
	}
}
