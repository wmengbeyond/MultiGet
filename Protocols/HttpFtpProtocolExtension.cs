using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Imgo.MultiGet;
using Imgo.MultiGet.Extensions;

namespace Imgo.MultiGet.Protocols
{
    public class HttpFtpProtocolExtension
    {
        public HttpFtpProtocolExtension()
        {
            ProtocolProviderFactory.RegisterProtocolHandler("http", typeof(HttpProtocolProvider));
            ProtocolProviderFactory.RegisterProtocolHandler("https", typeof(HttpProtocolProvider));
            ProtocolProviderFactory.RegisterProtocolHandler("ftp", typeof(FtpProtocolProvider));
        }
    }
}
