using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using DPUruNet;
using static DPUruNet.Constants;

namespace Scanners.Finger
{
    /// <summary>
    ///		Represents a class which holds fingerprint data.
    /// </summary>
    public class Fingerprint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Fingerprint"/> class.
        /// </summary>
        public Fingerprint() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Fingerprint"/> class.
        /// </summary>
        /// <param name="fid">The Fingerprint Image Data.</param>
        internal Fingerprint(Fid fid) => this.Fid = fid;

        public Fingerprint(Bitmap bitmap)
        {
            //BitmapData data = bitmap.LockBits(ImageLockMode.ReadOnly);
            //
            //byte[] bytes = new byte[data.Stride * data.Height];
            //
            //Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            //
            //bitmap.UnlockBits(data);
            //
            //var fiv = new Fid.Fiv(bytes);
            //
            //Fid = Fingerrint
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Fingerprint"/> class.
        /// </summary>
        /// <param name="fingerprint">Fingerprint which stores Fid data</param>
        public Fingerprint(Fingerprint fingerprint) : this(fingerprint.Fid)
        { }

        /// <summary>
        ///		Checks whether instances are the same by reference.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) => obj == null || GetType() != obj.GetType() ? false : base.Equals(obj);

        /// <summary>
        ///		Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        ///		A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => base.GetHashCode();

        /// <summary>
        ///		Compares given fingerprints by their respective <see cref="Fmd"/>.
        /// </summary>
        /// <param name="other">The other.</param>
        public int CompareFmd(Fingerprint other) =>
            other == null
            ? Int32.MaxValue
            : Comparison.Compare(
                FeatureExtraction.CreateFmdFromFid(this.Fid, Formats.Fmd.ANSI).Data, 0,
                FeatureExtraction.CreateFmdFromFid(other.Fid, Formats.Fmd.ANSI).Data, 0
            ).Score;

        private Fid Fid;
    }
}

