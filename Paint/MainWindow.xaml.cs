using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
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
	}

	public enum ToolType
	{
		Pencil,
		LocalFill,
		GlobalFill
	}
}
