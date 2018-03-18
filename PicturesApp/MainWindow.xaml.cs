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

			// Dlugosc (ilosc pikseli w jednym wierszu)
			int width = bmp.Width;
			// Dlugosc (ilosc bajtow w jednym wierszu)
			int stride = bmp.Width * bytesPerPixel;

			int sum = 0;
			// Mozna uzyc System.Linq i zrobic
			// int sum = matrix.Sum()
			// jednakze jest to wolniejsze rozwiazanie
			foreach (var n in matrix)
				sum += n;

			// W petli dzielimy przez sume zatem 
			// nie moze byc zerem
			if (sum == 0)
				sum = 1;

			// Bitmapa do ktorej bedziemy zapisywac nowe wartosci
			Bitmap newBitmap = new Bitmap(width, bmp.Height);

			// Dane do czytania
			BitmapData readData = bmp.LockBits(
				new Rectangle(Point.Empty, bmp.Size),
				ImageLockMode.ReadOnly,
				PixelFormat.Format24bppRgb
			);

			// Dane do zapisywania
			BitmapData writeData = newBitmap.LockBits(
				new Rectangle(Point.Empty, bmp.Size),
				ImageLockMode.WriteOnly,
				PixelFormat.Format24bppRgb
			);

			// Wskazniki na pierwszy bajt danej bitmapy -- bitmapy sa zapisywane
			// w postaci jednowymiarowych tablic bajtow
			byte* read = (byte*)readData.Scan0.ToPointer();
			byte* write = (byte*)writeData.Scan0.ToPointer();

			// Rownolegly for zeby przyspieszyc
			Parallel.For(1, bmp.Height - 1, j =>
			{
				for (int i = bytesPerPixel; i < stride - bytesPerPixel; i++)
				{
					// Dla kazdego piksela oprocz pikseli granicznych
					int offset = j * stride + i;

					int result = (
						// [offset] to srodkowy piksel ktory w danym momencie ustawiamy
						matrix[4] * read[offset] +
						matrix[5] * read[offset + bytesPerPixel] +
						matrix[3] * read[offset - bytesPerPixel] +
						// [offset +- bytesPerPixel] przesunie wskaznik na kolejny bajt w lewo/prawo o 3 miejsca
						// zatem przesunie na odpowiadajacy mu ten sam kolor - na sasiada po lewej lub prawej

						matrix[1] * read[offset + stride] +
						matrix[2] * read[offset + stride + bytesPerPixel] +
						matrix[0] * read[offset + stride - bytesPerPixel] +
						// [offset +- stride] przesunie wskaznik na kolejny bajt tym razem w gore lub dol,
						// na odpowiadajacy ten sam kolor sasiada

						matrix[7] * read[offset - stride] +
						matrix[8] * read[offset - stride + bytesPerPixel] +
						matrix[6] * read[offset - stride - bytesPerPixel]
					) / sum;

					write[offset] = result > 255 ? Byte.MaxValue :
									result < 0 ? Byte.MinValue : (byte)result;
					// Jezeli result wyjdzie wiekszy niz 255 to ustawiamy 255 (Byte.MaxValue) 
					// w przeciwnym wypadku jesli wyjdzie mniejszy od 0 to ustawiamy 0 (Byte.MinValue)
					// w przeciwnym wypadku ustawiamy result jako bajt wynikowy
				}
			});

			bmp.UnlockBits(readData);
			newBitmap.UnlockBits(writeData);
			// Odblokowujemy z pamieci

			return newBitmap;
		}

		/// <summary>
		///		Pierwsza, najwolniejsza, wersja z Get/Set Pixel
		///		Warto zwrocic uwage ze modyfikuje bitmape podana jako parametr
		/// </summary>
		private Bitmap ApplyEffect_Slowest(Bitmap bmp)
		{
			for (int i = 0; i < bmp.Width; i++)
				for (int j = 0; j < bmp.Height; j++)
				{
					var color = bmp.GetPixel(i, j);

					var newColor = Color.FromArgb(color.R / 2, color.G / 2, color.B / 2);

					bmp.SetPixel(i, j, newColor);
				}
			return bmp;
		}

		/// <summary>
		///		Druga, szybsza metoda z <see cref="Marshal.Copy"/>
		/// </summary>
		private Bitmap ApplyEffect_Marshal(Bitmap bmp)
		{
			BitmapData data = bmp.LockBits(
				new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb
			);

			int stride = data.Stride;

			byte[] pixels = new byte[bmp.Width * bmp.Height * 3];
			Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

			// Dla kazdego i od 0 do wysokosci
			for (int i = 0; i < bmp.Height; i++)
				// Dla kazdego j od 0 do szerokosci (po bajtach)
				for (int j = 0; j < data.Stride; j++)
					// Ustaw dany bajt na 128 przez co da nam kolor szary
					// Poniewaz wszystkie piksela RGB ustawi nam na 128 128 128
					pixels[i * data.Stride + j] = 128;

			Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
			bmp.UnlockBits(data);
			return bmp;
		}

		private void TestButton_Click(object sender, Windows.RoutedEventArgs e)
		{
			Stopwatch stopwatch = new Stopwatch();

			stopwatch.Start();
			this.bitmap = ApplyEffect_Marshal(this.bitmap);

			//this.bitmap = ApplyEffect(this.bitmap, Matrix.Emboss);
			//this.bitmap = this.bitmap.ApplyEffect((r, g, b) => (r, r, r));
			//this.bitmap = this.bitmap.ApplyEffect(b => (byte)(b / 2));
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
