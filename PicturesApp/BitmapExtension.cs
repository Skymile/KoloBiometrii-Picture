// Wersja na C# 7.2
//

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace PicturesApp
{
	public static class BitmapExtension
	{
		public static unsafe Bitmap ApplyEffect(
			this Bitmap Bitmap, Func<byte, byte> byteEffect
		)
		{
			const int bytesPerPixel = 3;

			int width = Bitmap.Width,
				height = Bitmap.Height,
				stride = Bitmap.Width * bytesPerPixel;

			BitmapData data = Bitmap.LockBits(
				new Rectangle(Point.Empty, Bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb
			);

			byte* ptr = (byte*)data.Scan0.ToPointer();

			Parallel.For(0, height, i =>
			{
				for (int j = 0, offset = i * stride; j < stride; ++j, ++offset)
					ptr[offset] = byteEffect(ptr[offset]);
			});

			Bitmap.UnlockBits(data);
			return Bitmap;
		}

		public static unsafe Bitmap ApplyEffect(
			this Bitmap Bitmap,
			Func<byte, byte, byte, (byte R, byte G, byte B)> pixelEffect
		)
		{
			const int bytesPerPixel = 3;

			int width = Bitmap.Width,
				height = Bitmap.Height,
				stride = Bitmap.Width * bytesPerPixel;

			BitmapData data = Bitmap.LockBits(
				new Rectangle(Point.Empty, Bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb
			);

			byte* ptr = (byte*)data.Scan0.ToPointer();

			Parallel.For(0, height, i =>
			{
				for (int j = 0, offset = i * stride; j < width; ++j)
				{
					var (R, G, B) = pixelEffect(ptr[offset], ptr[offset + 1], ptr[offset + 2]);

					ptr[offset] = R;
					ptr[offset + 1] = G;
					ptr[offset + 2] = B;

					offset += bytesPerPixel;
				}
			});

			Bitmap.UnlockBits(data);
			return Bitmap;
		}
	}
}
//*/
