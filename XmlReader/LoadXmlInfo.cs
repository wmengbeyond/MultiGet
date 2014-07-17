using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Imgo.MultiGet.XmlReader
{
    public class LoadXmlInfo
    {
        #region Singleton

        private static LoadXmlInfo instance = new LoadXmlInfo();

        public static LoadXmlInfo Instance
        {
            get
            {
                return instance;
            }
        }

        public LoadXmlInfo()
        {
            ReaderXml();
        }

        #endregion

        #region Fields

        private long minSegmentSize = 200000;
        private int minSegmentLeftToStartNewSegment = 30;
        private int retryDelay = 5;
        private int maxRetries = 10;
        private int maxSegments = 5;
        private string downloadFolder;

        private int maxJobs = 5;
        private bool workOnlyOnSpecifiedTimes = false;
        private string timesToWork;
        private double maxRateOnTime;
        private bool autoStart = true;

        private bool enabledLimit = false;
        private double maxRate = 1024;

        private int readWriteTimeout = 1000 * 60;
        private int connectTimeout = 1000 * 30;
        private int maxRecvBufSize = 86 * 1024;
        private bool keepLive = true;

        private string proxyAddress;
        private string proxyUserName;
        private string proxyPassword;
        private string proxyDomain;
        private bool useProxy = false;
        private string proxyByPassOnLocal;
        private int proxyPort = 80;

        #endregion


        #region Methods

        public long MinSegmentSize {
            get {
                return minSegmentSize;
            }
            set {
                minSegmentSize = value;
            }
        }

        public int MinSegmentLeftToStartNewSegment
        {
            get
            {
                return minSegmentLeftToStartNewSegment;
            }
            set
            {
                minSegmentLeftToStartNewSegment = value;
            }
        }

        public int RetryDelay
        {
            get
            {
                return retryDelay;
            }
            set
            {
                retryDelay = value;
            }
        }

        public int MaxSegments
        {
            get
            {
                return maxSegments;
            }
            set
            {
                maxSegments = value;
            }
        }

        public string DownloadFolder
        {
            get
            {
                return downloadFolder;
            }
            set
            {
                downloadFolder = value;
            }
        }

        public int MaxRetries
        {
            get
            {
                return maxRetries;
            }
            set
            {
                maxRetries = value;
            }
        }

        public int MaxJobs {
            get {
                return maxJobs;
            }
            set {
                maxJobs = value;
            }
        }

        public bool WorkOnlyOnSpecifiedTimes {
            get {
                return workOnlyOnSpecifiedTimes;
            }
            set {
                workOnlyOnSpecifiedTimes = value;
            }
        }

        public string TimesToWork {
            get {
                return timesToWork;
            }
            set {
                timesToWork = value;
            }
        }

        public double MaxRateOnTime {
            get {
                //return maxRateOnTime;
                return (maxRateOnTime * 1024);
            }
            set {
                maxRateOnTime = value;
            }
        }

        public bool AutoStart {
            get {
                return autoStart;
            }
            set {
                autoStart = value;
            }
        }

        public bool EnabledLimit {
            get {
                return enabledLimit;
            }
            set {
                enabledLimit = value;
            }
        }

        public double MaxRate {
            get {
                return maxRate;
            }
            set {
                maxRate = value;
            }
        }
 
        /// <summary>
        /// 获取或设置写入或读取流时的超时（以毫秒为单位）
        /// </summary>
       public int ReadWriteTimeout {
            get {
                return readWriteTimeout;
            }
            set {
                readWriteTimeout = value;
            }
        }

        /// <summary>
        /// 连接的超时（以毫秒为单位）
        /// </summary>
       public int ConnectTimeout {
            get {
                return connectTimeout;
            }
            set {
                connectTimeout = value;
            }
        }

        /// <summary>
        /// 接受缓冲区大小
        /// </summary>
       public int MaxRecvBufSize {
            get {
                return maxRecvBufSize;
            }
            set {
                maxRecvBufSize = value;
            }
        }

        /// <summary>
        /// 获取或设置一个 Boolean 值，该值指定在请求完成之后是否关闭到 FTP 服务器的控制连接
        /// </summary>
       public bool KeepLive
       {
           get
           {
               return keepLive;
           }
           set
           {
               keepLive = value;
           }
       }

       public string ProxyAddress {
            get {
                return proxyAddress;
            }
            set {
                proxyAddress = value;
            }
        }

        public string ProxyUserName {
            get {
                return proxyUserName;
            }
            set {
                proxyUserName = value;
            }
        }

        public string ProxyPassword {
            get {
                return proxyPassword;
            }
            set {
                proxyPassword = value;
            }
        }

        public string ProxyDomain {
            get {
                return proxyDomain;
            }
            set {
                proxyDomain = value;
            }
        }

        public bool UseProxy {
            get {
                return useProxy;
            }
            set {
                useProxy = value;
            }
        }

        public string ProxyByPassOnLocal {
            get {
                return proxyByPassOnLocal;
            }
            set {
                proxyByPassOnLocal = value;
            }
        }

       public int ProxyPort {
            get {
                return proxyPort;
            }
            set {
                proxyPort = value;
            }
        }

        public void ReaderXml()
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load("MultiGetConfig.xml");

                XmlNode node = doc.SelectSingleNode("root/Core/MinSegmentSize");
                minSegmentSize = Convert.ToInt64(node.InnerText);
                node = doc.SelectSingleNode("root/Core/MinSegmentLeftToStartNewSegment");
                MinSegmentLeftToStartNewSegment = Convert.ToInt32(node.InnerText);
                node = doc.SelectSingleNode("root/Core/RetryDelay");
                retryDelay = Convert.ToInt32(node.InnerText);
                node = doc.SelectSingleNode("root/Core/MaxRetries");
                maxRetries = Convert.ToInt32(node.InnerText);
                node = doc.SelectSingleNode("root/Core/MaxSegments");
                maxSegments = Convert.ToInt32(node.InnerText);
                node = doc.SelectSingleNode("root/Core/DownloadFolder");
                downloadFolder = node.InnerText;

                node = doc.SelectSingleNode("root/Extension/Scheduler/MaxJobs");
                maxJobs = Convert.ToInt32(node.InnerText);
                node = doc.SelectSingleNode("root/Extension/Scheduler/WorkOnlyOnSpecifiedTimes");
                workOnlyOnSpecifiedTimes = Convert.ToBoolean(node.InnerText);
                node = doc.SelectSingleNode("root/Extension/Scheduler/TimesToWork");
                timesToWork = node.InnerText;
                node = doc.SelectSingleNode("root/Extension/Scheduler/MaxRateOnTime");
                //maxRateOnTime = Convert.ToDouble(node.InnerText);
                maxRateOnTime = (Math.Max(Convert.ToDouble(node.InnerText), 1024) / 1024.0);
                node = doc.SelectSingleNode("root/Extension/Scheduler/AutoStart");
                autoStart = Convert.ToBoolean(node.InnerText);

                node = doc.SelectSingleNode("root/Extension/SpeedLimit/EnabledLimit");
                enabledLimit = Convert.ToBoolean(node.InnerText);
                node = doc.SelectSingleNode("root/Extension/SpeedLimit/MaxRate");
                maxRate = Convert.ToDouble(node.InnerText);

                node = doc.SelectSingleNode("root/Extension/Protocols/ProxyAddress");
                proxyAddress = node.InnerText;
                node = doc.SelectSingleNode("root/Extension/Protocols/ProxyUserName");
                proxyUserName = node.InnerText;
                node = doc.SelectSingleNode("root/Extension/Protocols/ProxyPassword");
                proxyPassword = node.InnerText;
                node = doc.SelectSingleNode("root/Extension/Protocols/ProxyDomain");
                proxyDomain = node.InnerText;
                node = doc.SelectSingleNode("root/Extension/Protocols/UseProxy");
                useProxy = Convert.ToBoolean(node.InnerText);
                node = doc.SelectSingleNode("root/Extension/Protocols/ProxyByPassOnLocal");
                proxyByPassOnLocal = node.InnerText;
                node = doc.SelectSingleNode("root/Extension/Protocols/ProxyPort");
                proxyPort = Convert.ToInt32(node.InnerText);
            }
            catch
            {
                //doc.Close();
            }

            //doc.Close();
        }

        #endregion
    }
}
