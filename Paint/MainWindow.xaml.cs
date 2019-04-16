using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.Win32;

using Models;
using Models.Algorithms.Binarization;
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

		private Bitmap picture = new Bitmap("thinning.png");

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
				try
				{
					(int X, int Y) = GetPosition((System.Windows.Controls.Image)sender, e);
					this.picture.SetPixel(
						X, Y, 
						System.Drawing.Color.FromArgb(R, G, B)
					);
				}
				catch
				{
				}
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

		private unsafe void BinarizationByThreshold_Click(object sender, RoutedEventArgs e) => 
			ApplyAlgorithm<Level>();

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
			NiblackGeneral((std, mean) => std * 0.5 + mean);

		private unsafe void NiblackGeneral(Compute computeResult)
		{
			BitmapData data = picture.LockBits(
				new System.Drawing.Rectangle(System.Drawing.Point.Empty, picture.Size),
				ImageLockMode.ReadWrite,
				System.Drawing.Imaging.PixelFormat.Format24bppRgb
			);

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

		private unsafe void Bernsen_Click(object sender, RoutedEventArgs e) => ApplyAlgorithm<Bernsen>();

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (Dialogs.TrySave(out string filename))
				picture.Save(filename);
		}

		private void Load_Click(object sender, RoutedEventArgs e)
		{
			if (Dialogs.TryLoad(out string filename))
				Load(filename);
		}

		private void Load(string filename)
		{
			picture = new Bitmap(filename);

			if (System.Drawing.Image.GetPixelFormatSize(picture.PixelFormat) != 24)
			{
				var bmp = new Bitmap(picture.Width, picture.Height, PixelFormat.Format24bppRgb);
				using (var g = Graphics.FromImage(bmp))
					g.DrawImage(picture, PointF.Empty);
				picture = bmp;
			}

			this.Filename = filename;
		}
		
		private string Filename = "thinning.png";

		private void KMM_Click(object sender, RoutedEventArgs e) => ApplyAlgorithm<KMM>();
		private void K3M_Click(object sender, RoutedEventArgs e) => ApplyAlgorithm<K3M>();
		private void ZhangSuen_Click(object sender, RoutedEventArgs e) => ApplyAlgorithm<ZhangSuen>();

		private void ApplyAlgorithm<T>()
			where T : IAlgorithm
		{
			var ctor = typeof(T).GetConstructors()[0];

			var parameters = ctor.GetParameters();

			T algorithm = default;
			if (parameters.Length == 0)
				algorithm = Activator.CreateInstance<T>();
			else
			{
				var window = new Window
				{
					Title = "Podaj parametry",
					MinWidth = 50
				};

				var grid = new Grid();
				window.Content = grid;

				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

				var labels = new Label[parameters.Length];
				var txt    = new TextBox[parameters.Length];

				for (int i = 0; i <= parameters.Length; i++)
					grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

				for (int i = 0; i < labels.Length; i++)
				{
					labels[i] = new Label { Content = parameters[i].Name };
					txt[i] = new TextBox { MinWidth = 20 };

					labels[i].Padding = labels[i].Margin = txt[i].Padding = txt[i].Margin = new Thickness(5);

					grid.Children.Add(labels[i]);
					grid.Children.Add(txt[i]);

					Grid.SetColumn(labels[i], 0);
					Grid.SetRow   (labels[i], i);

					Grid.SetColumn(txt[i], 1);
					Grid.SetRow   (txt[i], i);
				}

				var ok = new Button { Content = "Ok", IsDefault = true };
				var cancel = new Button { Content = "Cancel", IsCancel = true };

				ok.Padding = ok.Margin = cancel.Padding = cancel.Margin = new Thickness(5);

				grid.Children.Add(ok);
				grid.Children.Add(cancel);

				Grid.SetColumn(ok, 0);
				Grid.SetColumn(cancel, 1);

				Grid.SetRow(ok    , parameters.Length);
				Grid.SetRow(cancel, parameters.Length);

				ok.Click += Ok_Click;

				void Ok_Click(object sender, RoutedEventArgs e)
				{
					window.DialogResult = true;
					window.Close();
				};

				try
				{
					if (window.ShowDialog() == true)
					{
						object[] param = new object[parameters.Length];

						for (int i = 0; i < txt.Length; i++)
							param[i] = Convert.ChangeType(txt[i].Text, parameters[i].ParameterType);

						algorithm = (T)ctor.Invoke(param);
					}
				}
				finally
				{
					ok.Click -= Ok_Click;
				}
			}

			if (algorithm is null)
				return;

			this.picture = new Picture(this.Filename).Apply(algorithm).bitmap;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MainSource)));

		}

	}
}
