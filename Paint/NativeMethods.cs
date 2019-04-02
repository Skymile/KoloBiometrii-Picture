using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Models
{
    internal static class NativeMethods
    {
        internal static BitmapSource GetBitmapSource(this Bitmap bitmap)
        {
            IntPtr ptr = bitmap.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()
                );
            }
            finally
            {
                DeleteObject(ptr);
            }
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr ptr);
    }
}
