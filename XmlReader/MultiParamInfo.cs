using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Imgo.MultiGet.XmlReader
{
    public class MultiParamInfo
    {
        #region Fields

        private long minSegmentSize = 200000;//最小分片的大小
        private int minSegmentLeftToStartNewSegment = 30;//最小片时间
        private int retryDelay = 5;//重试间隔
        private int maxRetries = 10;//重试次数
        private int maxSegments = 5;//一个下载文件分多少个源地址来同时下载 最大分片数
        private string downloadFolder;//必须设置下载存放文件 /data

        private int maxJobs = 5;//同时下载的文件数
        private bool workOnlyOnSpecifiedTimes = false;//无需处理
        private string timesToWork;//无需处理
        private double maxRateOnTime;//最大下载速率
        private bool autoStart = true;//手动下载时改成false，自动状态无需赋值

        private bool enabledLimit = false;//限速时设置true，不限速无需赋值
        private double maxRate = 1024;//enabledLimit = false时，maxRate才有效

        private int readWriteTimeout = 1000 * 60;
        private int connectTimeout = 1000 * 30;
        private int maxRecvBufSize = 86 * 1024;
        private bool keepLive = false;

        //private string proxyAddress;
        //private string proxyUserName;
        //private string proxyPassword;
        //private string proxyDomain;
        //private bool useProxy = false;
        //private string proxyByPassOnLocal;
        //private int proxyPort = 80;

        #endregion

        #region Methods

        /// <summary>
        /// 最小分片大小
        /// </summary>
        public long MinSegmentSize
        {
            get
            {
                return minSegmentSize;
            }
            set
            {
                minSegmentSize = value;
            }
        }

        /// <summary>
        /// 新分片需要下载时间 暂时不需要
        /// </summary>
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

        /// <summary>
        /// 下载异常时，重试间隔时间
        /// </summary>
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

        /// <summary>
        /// 一个下载文件最多同时分片数
        /// </summary>
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

        /// <summary>
        /// 下载文件存放路径
        /// </summary>
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

        /// <summary>
        /// 下载异常时，最大重试次数
        /// </summary>
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

        /// <summary>
        /// 程序可同时下载的文件数
        /// </summary>
        public int MaxJobs
        {
            get
            {
                return maxJobs;
            }
            set
            {
                maxJobs = value;
            }
        }

        /// <summary>
        /// 是否开启下载时间段 暂无使用
        /// </summary>
        public bool WorkOnlyOnSpecifiedTimes
        {
            get
            {
                return workOnlyOnSpecifiedTimes;
            }
            set
            {
                workOnlyOnSpecifiedTimes = value;
            }
        }

        /// <summary>
        /// 下载时间段 暂无使用
        /// </summary>
        public string TimesToWork
        {
            get
            {
                return timesToWork;
            }
            set
            {
                timesToWork = value;
            }
        }

        /// <summary>
        /// 最大下载速度
        /// </summary>
        public double MaxRateOnTime
        {
            get
            {
                //return maxRateOnTime;
                return (maxRateOnTime * 1024);
            }
            set
            {
                maxRateOnTime = value;
            }
        }

        /// <summary>
        /// 是否开启自动下载
        /// </summary>
        public bool AutoStart
        {
            get
            {
                return autoStart;
            }
            set
            {
                autoStart = value;
            }
        }

        /// <summary>
        /// 是否进行速度限制
        /// </summary>
        public bool EnabledLimit
        {
            get
            {
                return enabledLimit;
            }
            set
            {
                enabledLimit = value;
            }
        }

        /// <summary>
        /// 开启限速后的下载速率
        /// </summary>
        public double MaxRate
        {
            get
            {
                return maxRate;
            }
            set
            {
                maxRate = value;
            }
        }

        /// <summary>
        /// 获取或设置写入或读取流时的超时（以毫秒为单位）
        /// </summary>
        public int ReadWriteTimeout
        {
            get
            {
                return readWriteTimeout;
            }
            set
            {
                readWriteTimeout = value;
            }
        }

        /// <summary>
        /// 连接的超时（以毫秒为单位）
        /// </summary>
        public int ConnectTimeout
        {
            get
            {
                return connectTimeout;
            }
            set
            {
                connectTimeout = value;
            }
        }

        /// <summary>
        /// 接受缓冲区大小
        /// </summary>
        public int MaxRecvBufSize
        {
            get
            {
                return maxRecvBufSize;
            }
            set
            {
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

        //public string ProxyAddress
        //{
        //    get
        //    {
        //        return proxyAddress;
        //    }
        //    set
        //    {
        //        proxyAddress = value;
        //    }
        //}

        //public string ProxyUserName
        //{
        //    get
        //    {
        //        return proxyUserName;
        //    }
        //    set
        //    {
        //        proxyUserName = value;
        //    }
        //}

        //public string ProxyPassword
        //{
        //    get
        //    {
        //        return proxyPassword;
        //    }
        //    set
        //    {
        //        proxyPassword = value;
        //    }
        //}

        //public string ProxyDomain
        //{
        //    get
        //    {
        //        return proxyDomain;
        //    }
        //    set
        //    {
        //        proxyDomain = value;
        //    }
        //}

        //public bool UseProxy
        //{
        //    get
        //    {
        //        return useProxy;
        //    }
        //    set
        //    {
        //        useProxy = value;
        //    }
        //}

        //public string ProxyByPassOnLocal
        //{
        //    get
        //    {
        //        return proxyByPassOnLocal;
        //    }
        //    set
        //    {
        //        proxyByPassOnLocal = value;
        //    }
        //}

        //public int ProxyPort
        //{
        //    get
        //    {
        //        return proxyPort;
        //    }
        //    set
        //    {
        //        proxyPort = value;
        //    }
        //}

        public void AssignmentToXml()
        {
           LoadXmlInfo.Instance.MinSegmentSize = minSegmentSize;
           LoadXmlInfo.Instance.MinSegmentLeftToStartNewSegment = minSegmentLeftToStartNewSegment;
           LoadXmlInfo.Instance.RetryDelay = retryDelay;
           LoadXmlInfo.Instance.MaxRetries = maxRetries;
           LoadXmlInfo.Instance.MaxSegments = maxSegments;
           LoadXmlInfo.Instance.DownloadFolder = downloadFolder;
           LoadXmlInfo.Instance.MaxJobs = maxJobs;
           LoadXmlInfo.Instance.WorkOnlyOnSpecifiedTimes = workOnlyOnSpecifiedTimes;
           //LoadXmlInfo.Instance.TimesToWork = timesToWork;
           //LoadXmlInfo.Instance.MaxRateOnTime = maxRateOnTime;
           LoadXmlInfo.Instance.MaxRateOnTime = (Math.Max(maxRateOnTime, 1024) / 1024.0);
           LoadXmlInfo.Instance.AutoStart = autoStart;
           LoadXmlInfo.Instance.EnabledLimit = enabledLimit;
           LoadXmlInfo.Instance.MaxRate = maxRate;
        }

        #endregion
    }
}
