using System;
using System.Drawing;
using System.Drawing.Imaging;

using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;

using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Windows = System.Windows;
using System.Linq;

namespace PicturesApp
{
	/// <summary>
	///		Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Windows.Window
	{
		public MainWindow()
		{
			InitializeComponent();

			this.bitmap = new Bitmap("apple.png");

			SetSource(this.bitmap);
		}

		private int Sum(int[] array)
		{
			int sum = 0;
			foreach (var n in array)
				sum += n;
			return sum;
		}

		private int MatrixSum(int[] array)
		{
			int sum = Sum(array);
			return sum == 0 ? 1 : sum;
		}

		private BitmapData LockBits(Bitmap bitmap, ImageLockMode lockMode, PixelFormat pixelFormat) => 
			bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), lockMode, pixelFormat);

		private byte ReturnNormal(int result) =>
			result > 255 ? Byte.MaxValue :
			result < 0 ? Byte.MinValue : (byte)result;
		
		private unsafe Bitmap ApplyEffect(
			Bitmap bmp, 
			Func<int, byte> output,
			params (int[] matrix, Func<byte[], int[], int> func)[] effect)
		{
			const int bytesPerPixel = 3;

			int width = bmp.Width;
			int stride = bmp.Width * bytesPerPixel;

			int[] sum = new int[effect.Length];
			for (int i = 0; i < effect.Length; i++)
				sum[i] = MatrixSum(effect[i].matrix);

			Bitmap newBitmap = new Bitmap(width, bmp.Height);

			BitmapData readData = LockBits(bmp, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
			BitmapData writeData = LockBits(newBitmap, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

			byte* read = (byte*)readData.Scan0.ToPointer();
			byte* write = (byte*)writeData.Scan0.ToPointer();

			Parallel.For(1, bmp.Height - 1, j =>
			{
				for (int i = bytesPerPixel; i < stride - bytesPerPixel; i++)
				{
					int offset = j * stride + i;

					byte[] array = Disperse(read, offset, bytesPerPixel, stride);

					int result = 0;
					for (int k = 0; k < effect.Length; k++)
						result += effect[k].func(array, effect[k].matrix) / sum[k];

					write[offset] = output(result);
				}
			});

			bmp.UnlockBits(readData);
			newBitmap.UnlockBits(writeData);

			return newBitmap;
		}

		private int ApplyConvolution(byte[] output, int[] matrix)
		{
			int result = 0;

			for (int i = 0; i < output.Length; i++)
				result += output[i] * matrix[i];

			return result;
		}

		private int ApplyMedian(byte[] output, int[] matrix)
		{
			var res = output.ToList();

			res.Sort();

			return res[res.Count / 2];
		}

		private unsafe byte[] Disperse(byte* array, int offset, int x, int y) =>
			new[]
			{
				array[offset + y - x], array[offset + y], array[offset + y + x],
				array[offset - x]    , array[offset]    , array[offset + x],
				array[offset - y - x], array[offset - y], array[offset - y + x],
			};

		private byte ReturnSobel(int result)
		{
			result = (result * result + result * result) / 256;
			return ReturnNormal(result);
		}

		private void TestButton_Click(object sender, Windows.RoutedEventArgs e)
		{
			Stopwatch stopwatch = new Stopwatch();

			stopwatch.Start();
			this.bitmap = ApplyEffect(
				this.bitmap,
				ReturnNormal,
				(Matrix.ShiftUp, ApplyMedian)
			);

			//this.bitmap = ApplyEffect(
			//	this.bitmap, 
			//	ReturnSobel,
			//	(Matrix.SobelHorizontal, ApplyConvolution),
			//	(Matrix.SobelVertical, ApplyConvolution)
			//);
			stopwatch.Stop();

			SetSource(this.bitmap);

			this.LabelStatus.Content = $"{stopwatch.ElapsedMilliseconds} ms\n{stopwatch.ElapsedTicks} ticks";
		}

		private void SetSource(Bitmap bitmap)
		{
			IntPtr handle = bitmap.GetHbitmap();
			this.WindowImage.Source = Imaging.CreateBitmapSourceFromHBitmap(
				handle, IntPtr.Zero, Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()
			);
			if (!DeleteObject(handle))
				throw new MarshalDirectiveException();
		}

		[DllImport("gdi32.dll")]
		private static extern bool DeleteObject(IntPtr handle);

		private Bitmap bitmap;
	}
}
