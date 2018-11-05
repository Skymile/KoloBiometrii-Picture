using System.Drawing;

using Scanners.Reactive;

namespace Scanners.Finger
{
    public interface IScanner
    {
        event ErrorEventHandler OnError;
        event SuccessEventHandler OnSuccess;

        Bitmap CaptureBitmap(int index = 0, int timeout = -1);
        byte[] CaptureRaw(int index = 0, int timeout = -1);
    }
}