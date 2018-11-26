using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private Picture picture = new Picture("fingerprint.png");

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            var applied = picture.KMM();
            this.Status.Content = $"ms: {sw.ElapsedMilliseconds} ticks: {sw.ElapsedTicks}";

            Image.Source = applied.Source;

            picture = applied;

            picture.Save("fingerprint2.png");
        }
    }
}
