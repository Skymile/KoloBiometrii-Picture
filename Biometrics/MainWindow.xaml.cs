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

// tinyurl.com/knbiometrii
// https://github.com/Skymile/KoloBiometrii-Picture

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
            var picture = new Picture("lenna.png");

            var applied = picture.ApplySobel();

            applied.Save("lena2.png");

            Image.Source = applied.Source;
        }
    }
}
