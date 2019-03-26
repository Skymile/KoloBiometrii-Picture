using Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;

/// tinyurl.com/knbiometrii
/// https://github.com/Skymile/KoloBiometrii-Picture

namespace Biometrics
{
    using Minutiaes = Dictionary<MinutiaeType, List<(int X, int Y)>>;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            this.images = new Image[] {
                this.Image1, this.Image2, this.Image3, this.Image4
            };

            RefreshImages(this.pictures);
        }

        private Image[] images;

        private Picture[] pictures = new Picture[]
        {
            new Picture("fingerprint1.png"),
            new Picture("fingerprint2.png"),
            new Picture("fingerprint3.png"),
            new Picture("fingerprint4.png"),
        };

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            var kmm = new Picture[this.pictures.Length];
            var sw = Stopwatch.StartNew();
            kmm[0] = this.pictures[0].Thinning(new KMM());
            this.Status.Content = $"ms: {sw.ElapsedMilliseconds} ticks: {sw.ElapsedTicks}";
            for (int i = 1; i < kmm.Length; i++)
                kmm[i] = this.pictures[i].Thinning(new KMM());

            var cn = new Minutiaes[kmm.Length];
            for (int i = 0; i < kmm.Length; i++)
                cn[i] = kmm[i].CrossingNumber();

            var sb = new StringBuilder();
            for (int i = 0; i < cn.Length; i++)
            {
                for (int j = 0; j < cn.Length; j++)
                    sb.AppendLine($"{i + 1}/{j + 1}: {Picture.GetDifferences(cn[i], cn[j])}");
                sb.AppendLine();
            }
            this.Accuracy.Content = sb.ToString();

            RefreshImages(kmm);

            //picture.Save("fingerprint2.png");
        }

        private void RefreshImages(Picture[] pics)
        {
            for (int i = 0; i < pics.Length; ++i)
                this.images[i].Source = pics[i].Source;
        }
    }
}
