using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

using DPUruNet;

using Scanners.Exceptions;
using Scanners.Reactive;

namespace Scanners.Finger
{
    /// <summary>
    ///		A wrapper for <see cref="ReaderCollection"/> and other fingerprint classes from DPUruNet framework. <para/>
    ///		Contains methods for fingerprint capture using EikonTouch and U.are.U fingerprint scanner device.
    /// </summary>
    public class FingerprintScanner : IScanner
    {
        public event ErrorEventHandler OnError;
        public event SuccessEventHandler OnSuccess;

        /// <summary>
        ///		Initializes a static <see cref="ReaderCollection"/> of <see cref="FingerprintScanner"/> class.
        /// </summary>
        static FingerprintScanner() => Reset();

        /// <summary>
        /// Sets available readers.
        /// </summary>
        public static void Reset()
        {
            readerCollection = ReaderCollection.GetReaders();

            if (readerCollection.Count == 0)
                throw new Exceptions.NullReferenceException("No fingerprint scanner found - plug in the device.");
        }

        /// <summary>
        ///		Captures <see cref="Bitmap"/> from fingerprint scanner device. 
        /// </summary>
        /// <param name="index"> Index of <see cref="ReaderCollection"/> reader instance. </param>
        /// <param name="timeout"> Time after which scanner gives up, returns null if timed out. </param>
        /// <returns>
        ///		Captured instance of <see cref="Bitmap"/> class.
        /// </returns>
        /// <exception cref="IndexOutOfRangeException"> 
        ///		Occurs when <paramref name="index"/> is bigger than the count of fingerprint devices. 
        ///	</exception>
        /// <exception cref="NullReferenceException"> 
        ///		Occurs when device is unplugged. 
        ///	</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitmap CaptureBitmap(int index = 0, int timeout = -1) =>
            Catch(() => Capture(timeout, index)?.Data.Views?[0]?.ToBitmap());

        /// <summary>
        /// Captures <see cref="byte[]"/> from fingerprint scanner device.
        /// </summary>
        /// <param name="index"> Index of <see cref="ReaderCollection"/> reader instance. </param>
        /// <param name="timeout"> Time after which scanner gives up, returns null if timed out. </param>
        /// <exception cref="IndexOutOfRangeException"> 
        ///		Occurs when <paramref name="index"/> is bigger than the count of fingerprint devices. 
        ///	</exception>
        /// <exception cref="NullReferenceException"> 
        ///		Occurs when device is unplugged. 
        ///	</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte[] CaptureRaw(int index = 0, int timeout = -1) =>
            Catch(() => Capture(timeout, index)?.Data.Views?[0]?.Bytes);

        /// <summary>
        /// 	Captures the fingerprint data.
        /// </summary>
        /// <param name="timeout"> Time after which scanner gives up, returns null if timed out. </param>
        /// <param name="index"> Index of <see cref="ReaderCollection"/> reader instance. By default set to 0. </param>
        /// <returns>
        ///		Captured instance of <see cref="Bitmap"/> class.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Fingerprint CaptureFingerprint(int timeout = -1, int index = 0) => 
            Catch(() => Capture(index) is CaptureResult result ? new Fingerprint(result.Data) : null);

        /// <summary>
        ///		Gets the instance of <see cref="Reader"/> class.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Reader this[int key] => readerCollection[key];

        /// <summary>
        ///		Gets the count of available readers.
        /// </summary>
        public static int Length => readerCollection.Count;

        /// <summary>
        ///		Collection of fingerprint readers.
        /// </summary>
        private static ReaderCollection readerCollection;

        private CaptureResult Capture(int timeout = -1, int index = 0)
        { 
            if (index > readerCollection.Count)
                throw new IndexOutOfRangeException(nameof(index));

            var reader = readerCollection[index];

            if (reader?.Status?.Status != Constants.ReaderStatuses.DP_STATUS_READY)
            {
                reader.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE);
                var r = reader.Calibrate();
                var a = reader.GetStatus();
            }

            var result = reader.Capture(
                Constants.Formats.Fid.ISO,
                Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT,
                -1,
                reader.Capabilities.Resolutions[0]
            );

            if (result.Data == null)
                throw new Exceptions.NullReferenceException($"{nameof(result.Data)} was null");
            return result?.ResultCode != Constants.ResultCode.DP_SUCCESS ? null : result;
        }

        /// <summary>
        /// Streaming test
        /// </summary>
        public void Streaming()
        {
            var reader = readerCollection[0];

            if (reader?.Status?.Status != Constants.ReaderStatuses.DP_STATUS_READY)
            {
                reader.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE);
                var r = reader.Calibrate();
                var a = reader.GetStatus();
            }

            reader.StartStreaming();

            while (true)
            {
                var image = reader.GetStreamImage(Constants.Formats.Fid.ANSI, Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT, reader.Capabilities.Resolutions[0]);

                if (!(image?.Data is null) && image.ResultCode == Constants.ResultCode.DP_SUCCESS)
                {
                    image.Data.Views[0].ToBitmap().Save($"test{DateTime.Now.Millisecond}.png");
                }
            }
        }

        /// <summary>
        ///		Asynchronously captures <see cref="Bitmap"/> from fingerprint scanner device. 
        /// </summary>
        /// <param name="index"> Index of <see cref="ReaderCollection"/> reader instance. </param>
        /// <param name="timeout"> Time after which scanner gives up, returns null if timed out. </param>
        /// <returns>
        ///		Captured instance of <see cref="Bitmap"/> class.
        /// </returns>
        /// <exception cref="IndexOutOfRangeException"> 
        ///		Occurs when <paramref name="index"/> is bigger than the count of fingerprint devices. 
        ///	</exception>
        /// <exception cref="NullReferenceException"> 
        ///		Occurs when device is unplugged. 
        ///	</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<Bitmap> CaptureBitmapAsync(int index = 0, int timeout = -1) =>
            await CatchAsync(() => Task.Run(() => CaptureBitmap(index, timeout)));

        private T Catch<T>(Func<T> func)
            where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                T result = func();
                OnSuccess?.Invoke(this, new SuccessEventArgs(stopwatch.ElapsedMilliseconds));
                return result;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs(ex, stopwatch.ElapsedMilliseconds));
#if DEBUG
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                    Debug.WriteLineIf(Debugger.IsLogging(), ex.Message, nameof(Exception));
                }
                else ExceptionDispatchInfo.Capture(ex).Throw();
#endif
#if TRACE
                Trace.WriteLine(ex.Message, nameof(Exception));
                Trace.WriteLine(ex.Source, nameof(Exception));
                Trace.WriteLine(ex.StackTrace, nameof(Exception));
#endif
            }
#if TRACE && !DEBUG
            Trace.WriteLine("Null returned");
#endif
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<T> CatchAsync<T>(Func<Task<T>> func) => await Catch(func);
    }
}
