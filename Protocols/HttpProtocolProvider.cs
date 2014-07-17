using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Reflection;

using Imgo.MultiGet.XmlReader;

namespace Imgo.MultiGet.Protocols
{
    public class HttpProtocolProvider : BaseProtocolProvider, IProtocolProvider
    {
        static HttpProtocolProvider()
        {
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(certificateCallBack);
        }

        static bool certificateCallBack(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private void FillCredentials(HttpWebRequest request, ResourceLocation rl)
        {
            if (rl.Authenticate)
            {
                string login = rl.Login;
                string domain = string.Empty;

                int slashIndex = login.IndexOf('\\');

                if (slashIndex >= 0)
                {
                    domain = login.Substring(0, slashIndex );
                    login = login.Substring(slashIndex + 1);
                }

                NetworkCredential myCred = new NetworkCredential(login, rl.Password);
                myCred.Domain = domain;

                request.Credentials = myCred;
            }
        }

        #region IProtocolProvider Members

        public virtual void Initialize(Downloader downloader)
        {
        }

        //#region HttpWebRequest.AddRange(long)
        //static MethodInfo httpWebRequestAddRangeHelper = typeof(WebHeaderCollection).GetMethod
        //                                        ("AddWithoutValidate", BindingFlags.Instance | BindingFlags.NonPublic);
        ///// <summary>
        ///// Adds a byte range header to a request for a specific range from the beginning or end of the requested data.
        ///// </summary>
        ///// <param name="request">The <see cref="System.Web.HttpWebRequest"/> to add the range specifier to.</param>
        ///// <param name="start">The starting or ending point of the range.</param>
        //public static void AddRange(this HttpWebRequest request, long start) { request.AddRange(start, -1L); }

        ///// <summary>Adds a byte range header to the request for a specified range.</summary>
        ///// <param name="request">The <see cref="System.Web.HttpWebRequest"/> to add the range specifier to.</param>
        ///// <param name="start">The position at which to start sending data.</param>
        ///// <param name="end">The position at which to stop sending data.</param>
        //public static void AddRange(this HttpWebRequest request, long start, long end)
        //{
        //    if (request == null) throw new ArgumentNullException("request");
        //    if (start < 0) throw new ArgumentOutOfRangeException("start", "Starting byte cannot be less than 0.");
        //    if (end < start) end = -1;

        //    string key = "Range";
        //    string val = string.Format("bytes={0}-{1}", start, end == -1 ? "" : end.ToString());

        //    httpWebRequestAddRangeHelper.Invoke(request.Headers, new object[] { key, val });
        //}
        //#endregion

        //wm

        public virtual Stream CreateStream(ResourceLocation rl, long initialPosition, long endPosition)
        {
            MethodInfo method = typeof(WebHeaderCollection).GetMethod
                        ("AddWithoutValidate", BindingFlags.Instance | BindingFlags.NonPublic);

            //HttpWebRequest request = (HttpWebRequest)GetRequest(rl);

            //FillCredentials(request, rl);

            ////===================================wm add============================
            //// Get the service point that handles the request's socket connection.
            //ServicePoint point = request.ServicePoint;
            //// Set the receive buffer size on the underlying socket.
            //point.ReceiveBufferSize = 8192;
            //// Set the connection lease timeout to infinite.
            ////===================================wm end============================

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(rl.URL));
            request.Credentials = new NetworkCredential(rl.Login, rl.Password);//设置用户名和密码

            ////wm Start 
            request.Timeout = LoadXmlInfo.Instance.ConnectTimeout;
            request.ReadWriteTimeout = LoadXmlInfo.Instance.ReadWriteTimeout;
            //request.KeepAlive = LoadXmlInfo.Instance.KeepLive;
            ////wm End

            //ServicePoint point = request.ServicePoint;
            //receiveBufSize = point.ReceiveBufferSize;//

            if (initialPosition != 0)
            {
                if (endPosition == 0)
                {
                    //wm alter beacuse momo Method Missing from Mono void HttpWebRequest.AddRange (Int64)
                    //request.AddRange(initialPosition);
                    string key = "Range";
                    string val = string.Format("bytes={0}", initialPosition);

                    method.Invoke(request.Headers, new object[] { key, val });
                }
                else
                {
                    //wm alter beacuse momo Method Missing from Mono void HttpWebRequest.AddRange (Int64)
                    //request.AddRange(initialPosition, endPosition);

                    string key = "Range";
                    string val = string.Format("bytes={0}-{1}", initialPosition, endPosition);
                    method.Invoke(request.Headers, new object[] { key, val });
                }
            }

            WebResponse response = request.GetResponse();

            return response.GetResponseStream();
        }

        public virtual RemoteFileInfo GetFileInfo(ResourceLocation rl, out Stream stream)
        {
            //HttpWebRequest request = (HttpWebRequest)GetRequest(rl);

            //FillCredentials(request, rl);

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(rl.URL));
            request.Credentials = new NetworkCredential(rl.Login, rl.Password);//设置用户名和密码

            //wm Start 
            request.Timeout = LoadXmlInfo.Instance.ConnectTimeout;
            request.ReadWriteTimeout = LoadXmlInfo.Instance.ReadWriteTimeout;
            //request.KeepAlive = LoadXmlInfo.Instance.KeepLive;
            //wm End

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            RemoteFileInfo result = new RemoteFileInfo();
            result.MimeType = response.ContentType;
            result.LastModified = response.LastModified;
            result.FileSize = response.ContentLength;
            result.AcceptRanges = String.Compare(response.Headers["Accept-Ranges"], "bytes", true) == 0;

            stream = response.GetResponseStream();

            return result;
        }

        #endregion
    }
}
