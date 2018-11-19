using Models;

using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Controls;

namespace Biometrics
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private Picture picture = new Picture("apple.png", ImageFormat.MemoryBmp);

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            var applied = picture.Apply(
                new[] {
                    1, 1, 1,
                    1, 1, 1,
                    1, 1, 1
                }, 3);

            Image.Source = applied.Source;

            picture = applied;

            picture.Save("apple2.png");
        }
    }
}
