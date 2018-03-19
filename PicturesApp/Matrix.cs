namespace PicturesApp
{
	public static class Matrix
	{
		public static readonly int[] ShiftUp = new int[]
		{
			0, 1, 0,
			0, 0, 0,
			0, 0, 0
		};

		public static readonly int[] GaussianBlur = new int[]
		{
			1, 2, 1,
			2, 4, 2,
			1, 2, 1
		};

		public static readonly int[] BoxBlur = new int[]
		{
			1, 1, 1,
			1, 1, 1,
			1, 1, 1
		};

		public static readonly int[] Sharpen = new int[]
		{
			0, -1, 0,
			-1, 5, -1,
			0, -1, 0
		};

		public static readonly int[] Emboss = new int[]
		{
			-2, -1, 0,
			-1,  2, 1,
			 0,  2, 2
		};

		public static readonly int[] EdgeDetect = new int[]
		{
			0, -1, 0,
			-1, 4, -1,
			0, -1, 0
		};

		public static readonly int[] UnsharpMasking = new int[]
		{
			1, 2, 1,
			2, -8, 2,
			1, 2, 1
		};

		public static readonly int[] HorizontalLineDetection = new int[]
		{
			-1, -1, -1,
			2, 2, 2,
			-1, -1, -1
		};

		public static readonly int[] VerticalLineDetection = new int[]
		{
			-1, 2, -1,
			-1, 2, -1,
			-1, 2, -1
		};

		public static readonly int[] AngleLineDetection = new int[]
		{
			-1, -1, 2,
			-1, 2, -1,
			2, -1, -1
		};

		public static readonly int[] SobelVertical = new int[]
		{
			-1, 0, 1,
			-2, 0, 2,
			-1, 0, 1
		};

		public static readonly int[] SobelHorizontal = new int[]
		{
			-1, -2, -1,
			0, 0, 0,
			1, 2, 1
		};
	}
}
