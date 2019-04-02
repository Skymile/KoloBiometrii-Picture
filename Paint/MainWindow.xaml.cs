using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Models;

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

		public byte R { get => r; set => SetChannel(value, ref r); }
		public byte G { get => g; set => SetChannel(value, ref g); }
		public byte B { get => b; set => SetChannel(value, ref b); }

		public byte Threshold { get => threshold; set => Set(value, ref threshold); }

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

		private Bitmap picture = new Bitmap("lenna.png");

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

				(int X, int Y) = GetPosition((System.Windows.Controls.Image)sender, e);

				int bpp = System.Drawing.Image.GetPixelFormatSize(picture.PixelFormat) / 8;
				int stride = picture.Width * bpp;
				int length = stride * picture.Height;

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
					{
						foreach (int o in offsets)
						{
							if (!visited.Contains(i + o) &&
								i + o > 0 && i + o + 2 < length &&

								p[i + o + 0] > value.R - threshold &&
								p[i + o + 0] < value.R + threshold &&
								p[i + o + 1] > value.G - threshold &&
								p[i + o + 1] < value.G + threshold &&
								p[i + o + 2] > value.B - threshold &&
								p[i + o + 2] < value.B + threshold
							)
							{
								newValues.Add(i);
								anyChange = true;
							}
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
		}
	}

	public enum ToolType
	{
		Pencil,
		LocalFill,
		GlobalFill
	}
}
