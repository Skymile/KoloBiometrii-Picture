namespace Models
{
	public interface IAlgorithm
	{
		unsafe void Apply(byte** write, byte* read, int length, int width, int height);
		unsafe void Apply(byte** ptr, int length, int width, int height);
	}
}