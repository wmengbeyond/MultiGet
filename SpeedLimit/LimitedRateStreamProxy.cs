using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;

namespace Imgo.MultiGet.SpeedLimit
{
    public class LimitedRateStreamProxy: Stream
    {
        private Stream proxy;

        #region Stream

        public override bool CanRead
        {
            get { return proxy.CanRead; }
        }

        public override bool CanSeek
        {
            get { return proxy.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return proxy.CanWrite; }
        }

        public override void Flush()
        {
            proxy.Flush();
        }

        public override long Length
        {
            get { return proxy.Length; }
        }

        public override long Position
        {
            get
            {
                return proxy.Position;
            }
            set
            {
                proxy.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            //SpeedLimitExtension.Instance.WaitFor();//wm alter
            SpeedLimitExtension.Instance.SendDownloadStateInfo();

            return proxy.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return proxy.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            proxy.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            proxy.Write(buffer, offset, count);
        }

        //BS Start wm alter
        public override void Close()
        {
            proxy.Close();
        }

        protected override void Dispose(bool disposing)
        {
            proxy.Dispose();
        }
        //BS End

        #endregion

        public LimitedRateStreamProxy(Stream proxy)
        {
            this.proxy = proxy;
        }
    }
}
