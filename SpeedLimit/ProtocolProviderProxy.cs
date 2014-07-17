using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

using Imgo.MultiGet;

namespace Imgo.MultiGet.SpeedLimit
{
    public class ProtocolProviderProxy: IProtocolProvider
    {
        private IProtocolProvider proxy;

        #region IProtocolProvider Members

        public void Initialize(Downloader downloader)
        {
            proxy.Initialize(downloader);
        }

        public System.IO.Stream CreateStream(ResourceLocation rl, long initialPosition, long endPosition)
        {
            return new LimitedRateStreamProxy(proxy.CreateStream(rl, initialPosition, endPosition));
        }

        public RemoteFileInfo GetFileInfo(ResourceLocation rl, out System.IO.Stream stream)
        {
            RemoteFileInfo result = proxy.GetFileInfo(rl, out stream);

            if (stream != null)
            {
                stream = new LimitedRateStreamProxy(stream);
            }

            return result;
        }

        #endregion

        public ProtocolProviderProxy(IProtocolProvider proxy)
        {
            this.proxy = proxy;
        }
    }
}
