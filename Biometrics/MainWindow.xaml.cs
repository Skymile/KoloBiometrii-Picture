﻿using Models;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

/// tinyurl.com/knbiometrii
/// https://github.com/Skymile/KoloBiometrii-Picture

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
            kmm[0] = this.pictures[0].KMM();
            this.Status.Content = $"ms: {sw.ElapsedMilliseconds} ticks: {sw.ElapsedTicks}";
            for (int i = 1; i < kmm.Length; i++)
                kmm[i] = this.pictures[i].KMM();

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
