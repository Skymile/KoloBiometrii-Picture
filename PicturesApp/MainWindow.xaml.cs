using System;
using System.Drawing;
using System.Drawing.Imaging;

using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;

using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Windows = System.Windows;

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

			this.bitmap = new Bitmap("lenna.png");

			SetSource(this.bitmap);
		}

		/// <summary>
		///		Przeksztalca podana bitmape
		/// </summary>
		/// <param name="bmp">
		///		Bitmapa do odczytu pikseli
		///		(zakladamy 3 wartosci RGB)
		///	</param>
		/// <param name="matrix">
		///		Macierz konwolucji 
		///		(zakladamy 3x3 jako 9 elementowa tablice)
		/// </param>
		private unsafe Bitmap ApplyEffect(Bitmap bmp, int[] matrix = null)
		{
			const int bytesPerPixel = 3;

			int width = bmp.Width;
			int stride = bmp.Width * bytesPerPixel;

			int sum = 0;
			foreach (var n in matrix)
				sum += n;

			if (sum == 0)
				sum = 1;

			Bitmap newBitmap = new Bitmap(width, bmp.Height);

			BitmapData readData = bmp.LockBits(
				new Rectangle(Point.Empty, bmp.Size),
				ImageLockMode.ReadOnly,
				PixelFormat.Format24bppRgb
			);

			BitmapData writeData = newBitmap.LockBits(
				new Rectangle(Point.Empty, bmp.Size),
				ImageLockMode.WriteOnly,
				PixelFormat.Format24bppRgb
			);

			byte* read = (byte*)readData.Scan0.ToPointer();
			byte* write = (byte*)writeData.Scan0.ToPointer();

			Parallel.For(1, bmp.Height - 1, j =>
			{
				for (int i = bytesPerPixel; i < stride - bytesPerPixel; i++)
				{
					int offset = j * stride + i;

					int result = (
						matrix[4] * read[offset] +
						matrix[5] * read[offset + bytesPerPixel] +
						matrix[3] * read[offset - bytesPerPixel] +

						matrix[1] * read[offset + stride] +
						matrix[2] * read[offset + stride + bytesPerPixel] +
						matrix[0] * read[offset + stride - bytesPerPixel] +

						matrix[7] * read[offset - stride] +
						matrix[8] * read[offset - stride + bytesPerPixel] +
						matrix[6] * read[offset - stride - bytesPerPixel]
					) / sum;

					write[offset] = result > 255 ? Byte.MaxValue :
									result < 0 ? Byte.MinValue : (byte)result;
				}
			});

			bmp.UnlockBits(readData);
			newBitmap.UnlockBits(writeData);

			return newBitmap;
		}

		private void TestButton_Click(object sender, Windows.RoutedEventArgs e)
		{
			Stopwatch stopwatch = new Stopwatch();

			stopwatch.Start();
			this.bitmap = ApplyEffect(this.bitmap, Matrix.Emboss);
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
