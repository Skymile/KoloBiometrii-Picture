using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using DPUruNet;

using DrawImg = System.Drawing.Imaging;

namespace Scanners.Finger
{
    /// <summary>
    ///		Extension methods for <see cref="Bitmap"/> class
    /// </summary>
    public static partial class BitmapExtensions
    {
        /// <summary>
        ///		Converts <see cref="Fid.Fiv"/> instance into new instance of <see cref="Bitmap"/> class. 
        /// </summary>
        /// <param name="fingerprintData"> 
        ///		<see cref="Fid.Fiv"/> data to be converted into <see cref="Bitmap"/> instance. 
        ///	</param>
        /// <returns> 
        ///		Instance of <see cref="Bitmap"/> class converted from <see cref="Fid.Fiv"/>; returns null if an error has occured. 
        /// </returns>
        /// <exception cref="NullReferenceException">
        ///		<paramref name="fingerprintData"> cannot be null. </paramref>
        /// </exception>
        public static Bitmap ToBitmap(this Fid.Fiv fingerprintData)
        {
            // temporary from SDK
            var bmp = new Bitmap(fingerprintData.Width, fingerprintData.Height, DrawImg.PixelFormat.Format24bppRgb);
            DrawImg.BitmapData data = bmp.LockBits(DrawImg.ImageLockMode.WriteOnly);

            try
            {
                byte[] inBytes = fingerprintData.RawImage,
                       rgbBytes = new byte[fingerprintData.RawImage.Length * 3];

                for (int i = 0; i < inBytes.Length; ++i)
                {
                    rgbBytes[i * 3 + 0] = inBytes[i];
                    rgbBytes[i * 3 + 1] = inBytes[i];
                    rgbBytes[i * 3 + 2] = inBytes[i];
                }

                for (int i = 0; i < bmp.Height; ++i)
                    Marshal.Copy(rgbBytes, i * bmp.Width * 3, new IntPtr(data.Scan0.ToInt64() + data.Stride * i), bmp.Width * 3);
            }
            catch
            {
                return null;
            }
            finally
            {
                bmp.UnlockBits(data);
            }

            return bmp;
        }

        /// <summary>
        ///		Locks the bits of given <see cref="Bitmap"/> class instance into <see cref="DrawImg.BitmapData"/> class. 
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="lockMode"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">
        ///		<see cref="Bitmap"/> <paramref name="bitmap"/> parameter cannot be null. 
        /// </exception>
        public static DrawImg.BitmapData LockBits(this Bitmap bitmap, DrawImg.ImageLockMode lockMode) =>
            bitmap.LockBits(bitmap.GetRectangle(), lockMode, bitmap.PixelFormat);

        /// <summary>
        ///		Gets the whole area of given <see cref="Bitmap"/> class as a <see cref="Rectangle"/> struct.  
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">
        ///		<see cref="Bitmap"/> <paramref name="bitmap"/> instance cannot be null.
        /// </exception>
        public static Rectangle GetRectangle(this Bitmap bitmap) =>
            new Rectangle(0, 0, bitmap.Width, bitmap.Height);

        /// <summary>
        ///		Gets the <see cref="Bitmap"/> <paramref name="bitmap"/> new instance of <see cref="ImageSource"/> class. <para/>
        ///		Useful for WPF controls.	
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException">
        ///		<see cref="Bitmap"/> <paramref name="bitmap"/> cannot be null.
        /// </exception>
        public static ImageSource GetSource(this Bitmap bitmap) =>
            Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()
            );

        public static Bitmap ApplyEffect(this Bitmap bitmap, int[] matrix)
        {
            DrawImg.BitmapData data = bitmap.LockBits(DrawImg.ImageLockMode.ReadWrite);

            int kHeight = (int)Math.Floor(Math.Pow(matrix.Length, 0.5));

            byte[] input = new byte[data.Stride * data.Height],
                   output = new byte[data.Stride * data.Height];

            Marshal.Copy(data.Scan0, input, 0, data.Stride * data.Height);

            int bytesPerPixel = data.Stride / data.Width;

            for (int i = 1; i < data.Width - 1; i++)
                for (int j = 1; j < data.Height - 1; j++)
                {
                    int sum = 0, offset = j * data.Stride + i * bytesPerPixel;

                    for (int k = 0; k < matrix.Length; k++)
                        sum += input[
                              (i - 1 + k % kHeight) * bytesPerPixel
                            + (j - 1 + k / kHeight) * data.Stride
                        ] * matrix[k];

                    sum = sum > 128 ? 255 : 0;

                    output[offset] =
                        output[offset + 1] =
                        output[offset + 2] = (byte)sum;
                }

            Marshal.Copy(output, 0, data.Scan0, input.Length);
            return bitmap;
        }
    }
}
