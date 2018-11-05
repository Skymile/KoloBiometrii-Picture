using System;

namespace Scanners.Reactive
{
    public class SuccessEventArgs : EventArgs
    {
        public SuccessEventArgs(long miliseconds) => 
            this.Miliseconds = miliseconds;

        public long Miliseconds { get; private set; }
    }

    public delegate void SuccessEventHandler(object sender, SuccessEventArgs args);
}
