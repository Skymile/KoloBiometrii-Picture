using System;
using System.Collections.Generic;
using System.Linq;
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

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            var picture = new Picture("lena.bmp");

            var applied = Picture.Apply(picture,
                new[] {
                1, 1, 1,
                1, 1, 1,
                1, 1, 1
            }, 3);

            Image.Source = applied.Source;

            applied.Save("lena2.png");
        }
    }
}
