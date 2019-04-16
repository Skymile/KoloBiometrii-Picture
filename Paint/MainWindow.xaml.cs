using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.Win32;

using Models;

using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Paint
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public MainWindow()
		{
			InitializeComponent();
			this.DataContext = this;
		}

		public byte R { get => this.r; set => SetChannel(value, ref this.r); }
		public byte G { get => this.g; set => SetChannel(value, ref this.g); }
		public byte B { get => this.b; set => SetChannel(value, ref this.b); }

		public byte Threshold { get => this.threshold; set => Set(value, ref this.threshold); }

		public System.Windows.Media.Brush Fill => new SolidColorBrush(System.Windows.Media.Color.FromRgb(R, G, B));

		public event PropertyChangedEventHandler PropertyChanged;

		private void SetChannel(byte value, ref byte field, [CallerMemberName] string name = "")
		{
			Set(value, ref field, name);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Fill)));
		}

		private void Set<T>(T value, ref T field, [CallerMemberName] string name = "")
		{
			field = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		private ToolType currentTool = ToolType.Pencil;

		public ToolType CurrentTool
		{
			get => currentTool;
			set => Set(value, ref currentTool);
		}

		private Bitmap picture = new Bitmap("apple.png");

		public ImageSource MainSource => picture.GetBitmapSource();

		public ToolType[] Tools => (ToolType[])Enum.GetValues(typeof(ToolType));

		private byte r;
		private byte g;
		private byte b;
		private byte threshold;

		private void Image_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed &&
				CurrentTool == ToolType.Pencil)
			{
				(int X, int Y) = GetPosition((System.Windows.Controls.Image)sender, e);
				this.picture.SetPixel(
					X, Y, 
					System.Drawing.Color.FromArgb(R, G, B)
				);
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MainSource)));
			}
		}

		private (int X, int Y) GetPosition(System.Windows.Controls.Image img, MouseEventArgs e)
		{
			System.Windows.Point p = e.GetPosition(img);

			int x = (int)(p.X / img.ActualWidth * picture.Width  );
			int y = (int)(p.Y / img.ActualHeight * picture.Height);

			return (x, y);
		}

		private unsafe void Image_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (this.CurrentTool == ToolType.LocalFill)
			{
				BitmapData data = picture.LockBits(
					new System.Drawing.Rectangle(System.Drawing.Point.Empty, picture.Size), 
					ImageLockMode.ReadWrite,
					picture.PixelFormat
				);

				byte* p = (byte*)data.Scan0.ToPointer();

				int bpp = System.Drawing.Image.GetPixelFormatSize(picture.PixelFormat) / 8;
				int stride = picture.Width * bpp;
				int length = stride * picture.Height;

				(int X, int Y) = GetPosition((System.Windows.Controls.Image)sender, e);
				int current = X * bpp + Y * stride;

				(byte R, byte G, byte B) value = (p[current], p[current + 1], p[current + 2]);

				var visited = new HashSet<int>();
				visited.Add(current);

				int[] offsets =
				{
					stride, -stride, bpp, -bpp, 
					stride - bpp, stride + bpp, -stride - bpp, -stride + bpp
				};

				bool anyChange = true;
				while (anyChange)
				{
					anyChange = false;

					var newValues = new List<int>();
					foreach (int i in visited)
						foreach (int o in offsets)
						{
							if (!visited.Contains(i + o) &&
								i + o > 0 && i + o + 2 < length &&

								p[i + o + 0] > value.R - threshold && p[i + o + 0] < value.R + threshold &&
								p[i + o + 1] > value.G - threshold && p[i + o + 1] < value.G + threshold &&
								p[i + o + 2] > value.B - threshold && p[i + o + 2] < value.B + threshold
							)
							{
								newValues.Add(i + o);
								anyChange = true;
							}
						}

					foreach (int i in newValues)
						visited.Add(i);
				}

				foreach (int i in visited)
				{
					p[i + 0] = R;
					p[i + 1] = G;
					p[i + 2] = B;
				}

				picture.UnlockBits(data);
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MainSource)));
			}
			else if (CurrentTool == ToolType.GlobalFill)
			{
				BitmapData data = picture.LockBits(
					new System.Drawing.Rectangle(System.Drawing.Point.Empty, picture.Size),
					ImageLockMode.ReadWrite,
					picture.PixelFormat
				);

				byte* p = (byte*)data.Scan0.ToPointer();

				int bpp = System.Drawing.Image.GetPixelFormatSize(picture.PixelFormat) / 8;
				int stride = picture.Width * bpp;
				int length = stride * picture.Height;

				(int X, int Y) = GetPosition((System.Windows.Controls.Image)sender, e);
				int current = X * bpp + Y * stride;

				byte value = p[current];

				for (int i = 0; i < length; i++)
					p[i] = p[i] > value - this.Threshold && p[i] < value + this.Threshold ? byte.MaxValue : byte.MinValue;

				picture.UnlockBits(data);
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MainSource)));
			}
		}

		private unsafe void BinarizationByThreshold_Click(object sender, RoutedEventArgs e)
		{
			BitmapData data = picture.LockBits(
				new System.Drawing.Rectangle(System.Drawing.Point.Empty, picture.Size),
				ImageLockMode.ReadWrite,
				picture.PixelFormat
			);

			byte* p = (byte*)data.Scan0.ToPointer();

			int bpp = System.Drawing.Image.GetPixelFormatSize(picture.PixelFormat) / 8;
			int stride = picture.Width * bpp;
			int length = stride * picture.Height;

			for (int i = 0; i < length; i++)
				p[i] = p[i] > this.Threshold ? byte.MaxValue : byte.MinValue;

			picture.UnlockBits(data);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MainSource)));
		}

		private unsafe void Otsu_Click(object sender, RoutedEventArgs e)
		{
			BitmapData data = picture.LockBits(
				new System.Drawing.Rectangle(System.Drawing.Point.Empty, picture.Size),
				ImageLockMode.ReadWrite,
				System.Drawing.Imaging.PixelFormat.Format24bppRgb
			);

			byte* p = (byte*)data.Scan0.ToPointer();

			int bpp = 3;
			int stride = picture.Width * bpp;
			int length = stride * picture.Height;

			var histData = new int[256];
			float sum = 0;
			for (int i = 0; i < length; ++i)
				histData[p[i]]++;
			for (int i = 0; i < 256; i++)
				sum += i * histData[i];

			float sumB = 0;
			int back = 0;
			int threshold = 0;
			float varMax = 0;

			for (int i = 0; i < 256; i++)
			{
				back += histData[i];
				if (back == 0)
					continue;

				int fore = length - back;
				if (fore == 0)
					break;

				sumB += i * histData[i];

				float backMean = sumB / back;
				float foreMean = (sum - sumB) / fore;

				float varBetween = (float)back * fore * (backMean - foreMean) * (backMean - foreMean);

				if (varBetween > varMax)
				{
					varMax = varBetween;
					threshold = i;
				}
			}

			this.Threshold = (byte)threshold;

			for (int i = 0; i < length; i++)
				p[i] = p[i] > threshold ? byte.MaxValue : byte.MinValue;

			picture.UnlockBits(data);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MainSource)));
		}

		private void Niblack_Click(object sender, RoutedEventArgs e) => 
			NiblackGeneral((std, mean) => std * 0.2 + mean);

		public delegate double Compute(double std, double mean);

		private unsafe void NiblackGeneral(Compute computeResult)
		{
			BitmapData data = picture.LockBits(
				new System.Drawing.Rectangle(System.Drawing.Point.Empty, picture.Size),
				ImageLockMode.ReadWrite,
				System.Drawing.Imaging.PixelFormat.Format24bppRgb
			);

			byte* p = (byte*)data.Scan0.ToPointer();

			int bpp = 3;
			int stride = picture.Width * bpp;
			int length = stride * picture.Height;

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

			picture.UnlockBits(data);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MainSource)));
		}

		private void Reset_Click(object sender, RoutedEventArgs e)
		{
			Load(this.Filename);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MainSource)));
		}

		private void Sauvola_Click(object sender, RoutedEventArgs e)
		{
			double div = 2;
			double threshold = 0.5;

			NiblackGeneral((std, mean) => mean * (1 + threshold * (std / div - 1)));
		}

		private void Phansalkar_Click(object sender, RoutedEventArgs e)
		{
			double div = 2;
			double threshold = 0.5;

			double p = 2;
			double q = 10;

			NiblackGeneral((std, mean) => mean * (1 + p * Math.Exp(-q * mean) + threshold * (std / div - 1)));
		}

		private unsafe void Bernsen_Click(object sender, RoutedEventArgs e)
		{
			int mainThreshold = 15;
			int windowSize = 3;

			BitmapData data = picture.LockBits(
				new System.Drawing.Rectangle(System.Drawing.Point.Empty, picture.Size),
				ImageLockMode.ReadWrite,
				System.Drawing.Imaging.PixelFormat.Format24bppRgb
			);

			byte* p = (byte*)data.Scan0.ToPointer();

			int bpp = 3;
			int stride = picture.Width * bpp;
			int length = picture.Height * stride;

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

			for (int i = offset; i < write.Length - offset; ++i)
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

				byte threshold = contrast < mainThreshold ? (byte)128 : mean;

				write[i] = p[i] > threshold ? byte.MaxValue : byte.MinValue;
			}

			for (int i = 0; i < write.Length; ++i)
				p[i] = write[i];

			picture.UnlockBits(data);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MainSource)));
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (save.ShowDialog() == true)
				picture.Save(save.FileName);
		}

		private void Load_Click(object sender, RoutedEventArgs e)
		{
			if (open.ShowDialog() == true)
				Load(open.FileName);
		}

		private void Load(string filename)
		{
			picture = new Bitmap(filename);

			if (Image.GetPixelFormatSize(picture.PixelFormat) != 24)
			{
				var bmp = new Bitmap(picture.Width, picture.Height, PixelFormat.Format24bppRgb);
				using (var g = Graphics.FromImage(bmp))
					g.DrawImage(picture, PointF.Empty);
				picture = bmp;
			}

			this.Filename = filename;
		}

		private string Filename = "apple.png";

		private static readonly OpenFileDialog open = new OpenFileDialog
		{
			Title = "Wybierz obraz",
			InitialDirectory = Directory.GetCurrentDirectory()
		};

		private static readonly SaveFileDialog save = new SaveFileDialog
		{
			Title = "Wybierz obraz",
			InitialDirectory = Directory.GetCurrentDirectory()
		};
	}
}
