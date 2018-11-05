using System;

namespace Scanners.Reactive
{
    public class ErrorEventArgs : EventArgs
    {
        public ErrorEventArgs(Exception exception, long miliseconds)
        {
            this.Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            this.Miliseconds = miliseconds;
        }

        public Exception Exception { get; private set; }
        public long Miliseconds { get; private set; }
    }

    public delegate void ErrorEventHandler(object sender, ErrorEventArgs args);
}
