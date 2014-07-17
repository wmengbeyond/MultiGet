using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Net;
using System.IO;
using Imgo.Share;

namespace Imgo.MultiGet.ExTools
{
    internal sealed class Cfgger
    {        
        /// <summary>
        /// 配置文件内容发生变化的事件
        /// </summary>
        public static event EventHandler CfgFileChangedEvent = null;

        #region 私有成员变量
        private static readonly string SECTION = "appSettings";
        private static Cfg _cfg = null;
        #endregion

        static Cfgger()
        {
            string cfgFile = AssemInfo.GetAssemFile(Assembly.GetExecutingAssembly()) + ".config";
            _cfg = new Cfg(cfgFile);
            //加载所有的参数值
            LoadAllParamValues();
            _cfg.CfgFileChangedEvent += OnCfgFileChanged;
        }

        /// <summary>
        /// 捕获参数文件内容发生变化的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnCfgFileChanged(object sender, EventArgs e)
        {
            LoadAllParamValues();
            EventUtil.DispatchEventAsync(sender, CfgFileChangedEvent);
        }

        /// <summary>
        /// 加载所有的参数值
        /// </summary>
        private static void LoadAllParamValues()
        {
            Parallels = _cfg.GetValue<int>(SECTION, "Parallels", 1, 0);
            CheckAwaitXml = _cfg.GetValue<string>(SECTION, "CheckAwaitXml", "");
            CheckErrorXml = _cfg.GetValue<string>(SECTION, "CheckErrorXml", "");
            DeliveryProfileNos = _cfg.GetValue<string>(SECTION, "DeliveryProfileNos", "");
            IsExpectOwnerNode = _cfg.GetValue<bool>(SECTION, "IsExpectOwnerNode", false);
            ExpectOtherNodes = _cfg.GetValue<string>(SECTION, "ExpectOtherNodes", "");
            IsExpectCDL = _cfg.GetValue<bool>(SECTION, "IsExpectCDL", false);
            
            WorkType = _cfg.GetValue<int>(SECTION, "WorkType", 1);
            CheckModel = _cfg.GetValue<int>(SECTION, "CheckModel", 1);
            EnableSuccessIntoDb = _cfg.GetValue<bool>(SECTION, "EnableSuccessIntoDb", false);
            EnableSuccessIntoDbNotifyCdn3Proload = _cfg.GetValue<bool>(SECTION, "EnableSuccessIntoDbNotifyCdn3Proload", false);
            EnableSuccessIntoDbSendMasFeedback = _cfg.GetValue<bool>(SECTION, "EnableSuccessIntoDbSendMasFeedback", false);
            EnableFailedIntoDbSendMasFeedback = _cfg.GetValue<bool>(SECTION, "EnableFailedIntoDbSendMasFeedback", false);

            ComputeHashBufSize = _cfg.GetValue<int>(SECTION, "ComputeHashBufSize", 4096, 1, 104857600);

            SourceFtpIp = _cfg.GetValue<string>(SECTION, "SourceFtpIp", "");
            SourceFtpPort = _cfg.GetValue<int>(SECTION, "SourceFtpPort", 21);
            SourceFtpHomeDir = _cfg.GetValue<string>(SECTION, "SourceFtpHomeDir", "");
            SourceFtpUser = _cfg.GetValue<string>(SECTION, "SourceFtpUser", "");
            SourceFtpPwd = _cfg.GetValue<string>(SECTION, "SourceFtpPwd", "");

            FtpTransSpeed = _cfg.GetValue<int>(SECTION, "FtpTransSpeed", 0, 0, 1024);
            FtpSocketConnectTimeout = _cfg.GetValue<int>(SECTION, "FtpSocketConnectTimeout", 5, 1);
            FtpSocketSendTimeout = _cfg.GetValue<int>(SECTION, "FtpSocketSendTimeout", 5, -1);
            FtpSocketRecvTimeout = _cfg.GetValue<int>(SECTION, "FtpSocketRecvTimeout", 5, -1);
            FtpSocketSendBufSize = _cfg.GetValue<int>(SECTION, "FtpSocketSendBufSize", 131072, 1);
            FtpSocketRecvBufSize = _cfg.GetValue<int>(SECTION, "FtpSocketRecvBufSize", 131072, 1);
            FtpAppSendBufSize = _cfg.GetValue<int>(SECTION, "FtpAppSendBufSize", 16384, 1);
            FtpAppRecvBufSize = _cfg.GetValue<int>(SECTION, "FtpAppRecvBufSize", 16384, 1);
            FtpReadCmdResponseWaitMsec = _cfg.GetValue<int>(SECTION, "FtpReadCmdResponseWaitMsec", 100, 0);
            FtpReportStateInterval = _cfg.GetValue<int>(SECTION, "FtpReportStateInterval", 1, 3, 10);
            FtpEnableResume = _cfg.GetValue<bool>(SECTION, "FtpEnableResume", false);

            NdasUrl = _cfg.GetValue<string>(SECTION, "NdasUrl", "");
            NdasTimeoutMsec = _cfg.GetValue<int>(SECTION, "NdasTimeoutMsec", 60000, 0);
            NdasAuthKey = _cfg.GetValue<string>(SECTION, "NdasAuthKey", "");
            NdasAuthIv = _cfg.GetValue<string>(SECTION, "NdasAuthIv", "");

            LogType = _cfg.GetValue<LogType>(SECTION, "LogType", LogType.Default);
            LogLvl = _cfg.GetValue<LogLvl>(SECTION, "LogLvl", LogLvl.None);
            LogFile = _cfg.GetValue<string>(SECTION, "LogFile", @"work.log");
        }

        
        #region 属性

        #region 应用参数
        private static string _deliveryProfileNos = "";
        /// <summary>
        /// 需要取哪些分发策略号的文件，如果有多个则以,分隔分发策略号
        /// </summary>
        public static string DeliveryProfileNos
        {
            get { return _deliveryProfileNos; }
            private set { _deliveryProfileNos = value; }
        }

        private static bool _isExpectOwnerNode = false;
        /// <summary>
        /// 是否排除本节点已经入库的文件
        /// </summary>
        public static bool IsExpectOwnerNode
        {
            get { return _isExpectOwnerNode; }
            private set { _isExpectOwnerNode = value; }
        }

        private static string _expectOtherNodes = null;
        /// <summary>
        /// 要排除其他节点Id串，多个之间用,分隔
        /// </summary>
        public static string ExpectOtherNodes
        {
            get { return _expectOtherNodes; }
            private set { _expectOtherNodes = value; }
        }

        private static bool _isExpectCDL = false;
        /// <summary>
        /// 是否排除本节点和排除节点在分发队列中的文件
        /// </summary>
        public static bool IsExpectCDL
        {
            get { return _isExpectCDL; }
            private set { _isExpectCDL = value; }
        }

        private static int _parallels = 1;
        /// <summary>
        /// 并行执行个数
        /// </summary>
        public static int Parallels
        {
            get { return _parallels; }
            private set { _parallels = value; }
        }

        private static string _checkAwaitXml = "check-await.xml";
        /// <summary>
        /// 待检查文件清单(XML文件，如果本地无该文件则从数据库获取清单并写入该文件)
        /// </summary>
        public static string CheckAwaitXml
        {
            get { return _checkAwaitXml; }
            private set { _checkAwaitXml = value; }
        }

        private static string _checkErrorXml = "check-error.xml";
        /// <summary>
        /// 检查无效文件清单(XML文件)
        /// </summary>
        public static string CheckErrorXml
        {
            get { return _checkErrorXml; }
            private set { _checkErrorXml = value; }
        }        

        private static int _workType = 1;
        /// <summary>
        /// 工作方式（1检查待检查文件清单中的文件，2恢复检查无效文件清单中的文件）
        /// </summary>
        public static int WorkType
        {
            get { return _workType; }
            private set { _workType = value; }
        }

        private static int _checkModel = 1;
        /// <summary>
        /// 检查文件的方式（1检查文件是否存在；2比较本地与远程文件大小；3检查本地文件hash）
        /// </summary>
        public static int CheckModel
        {
            get { return _checkModel; }
            private set { _checkModel = value; }
        }

        private static bool _enableSuccessIntoDb = false;
        /// <summary>
        /// 文件检查有效或成功恢复后是否入库
        /// </summary>
        public static bool EnableSuccessIntoDb
        {
            get { return _enableSuccessIntoDb; }
            private set { _enableSuccessIntoDb = value; }
        }

        private static bool _enableSuccessIntoDbNotifyCdn3Proload = false;
        /// <summary>
        /// 文件检查有效或成功恢复后入库时，是否通知第三方CDN预分发
        /// </summary>
        public static bool EnableSuccessIntoDbNotifyCdn3Proload
        {
            get { return _enableSuccessIntoDbNotifyCdn3Proload; }
            private set { _enableSuccessIntoDbNotifyCdn3Proload = value; }
        }

        private static bool _enableSuccessIntoDbSendMasFeedback = false;
        /// <summary>
        /// 文件检查有效或成功恢复后入库时，是否向媒资发送成功反馈
        /// </summary>
        public static bool EnableSuccessIntoDbSendMasFeedback
        {
            get { return _enableSuccessIntoDbSendMasFeedback; }
            private set { _enableSuccessIntoDbSendMasFeedback = value; }
        }

        private static bool _enableFailedIntoDbSendMasFeedback = false;
        /// <summary>
        /// 文件检查无效或恢复失败后入库时，是否向媒资发送失败反馈（存储文件不存在、MD5校验失败）
        /// </summary>
        public static bool EnableFailedIntoDbSendMasFeedback
        {
            get { return _enableFailedIntoDbSendMasFeedback; }
            private set { _enableFailedIntoDbSendMasFeedback = value; }
        }

        private static int _computeHashBufSize = 4096;
        /// <summary>
        /// 计算文件hash时用到的读文件buffer大小，单位字节
        /// </summary>
        public static int ComputeHashBufSize
        {
            get { return _computeHashBufSize; }
            private set { _computeHashBufSize = value; }
        }
        #endregion

        #region 源FTP服务器参数
        private static string _sourceFtpIp = "";
        /// <summary>
        /// 源 Ftp Ip
        /// </summary>
        public static string SourceFtpIp
        {
            get { return _sourceFtpIp; }
            private set { _sourceFtpIp = value; }
        }

        private static int _sourceFtpPort = 21;
        /// <summary>
        /// 源 Ftp 端口
        /// </summary>
        public static int SourceFtpPort
        {
            get { return _sourceFtpPort; }
            private set { _sourceFtpPort = value; }
        }

        private static string _sourceFtpHomeDir = "";
        /// <summary>
        /// 源 Ftp 主目录
        /// </summary>
        public static string SourceFtpHomeDir
        {
            get { return _sourceFtpHomeDir; }
            private set { _sourceFtpHomeDir = value; }
        }

        private static string _sourceFtpUser = "";
        /// <summary>
        /// 源 Ftp 用户密码
        /// </summary>
        public static string SourceFtpUser
        {
            get { return _sourceFtpUser; }
            private set { _sourceFtpUser = value; }
        }

        private static string _sourceFtpPwd = "";
        /// <summary>
        /// 源 Ftp 用户密码
        /// </summary>
        public static string SourceFtpPwd
        {
            get { return _sourceFtpPwd; }
            private set { _sourceFtpPwd = value; }
        }
        #endregion

        #region FTP传输参数
        private static int _ftpTransSpeed = 25;
        /// <summary>
        /// Ftp传输速度，单位MB/s
        /// </summary>
        public static int FtpTransSpeed
        {
            get { return _ftpTransSpeed; }
            private set { _ftpTransSpeed = value; }
        }

        private static int _ftpSocketConnectTimeout = 5;
        /// <summary>
        /// Ftp套接字连接超时，单位秒。
        /// </summary>
        public static int FtpSocketConnectTimeout
        {
            get { return _ftpSocketConnectTimeout; }
            private set { _ftpSocketConnectTimeout = value; }
        }

        private static int _ftpSocketSendTimeout = 5;
        /// <summary>
        /// Ftp套接字发送超时。单位秒。
        /// </summary>
        public static int FtpSocketSendTimeout
        {
            get { return _ftpSocketSendTimeout; }
            private set { _ftpSocketSendTimeout = value; }
        }

        private static int _ftpSocketRecvTimeout = 5;
        /// <summary>
        /// Ftp套接字接收超时。单位秒。小于0为使用系统缺省值
        /// </summary>
        public static int FtpSocketRecvTimeout
        {
            get { return _ftpSocketRecvTimeout; }
            private set { _ftpSocketRecvTimeout = value; }
        }

        private static int _ftpSocketSendBufSize = 8192;
        /// <summary>
        /// ftp数据套接字发送缓冲区大小，单位字节。小于0为使用系统缺省值8K
        /// </summary>
        public static int FtpSocketSendBufSize
        {
            get { return _ftpSocketSendBufSize; }
            private set { _ftpSocketSendBufSize = value; }
        }

        private static int _ftpSocketRecvBufSize = 8192;
        /// <summary>
        /// Ftp数据套接字接收缓冲区大小。单位字节。小于0为使用系统缺省值8K
        /// </summary>
        public static int FtpSocketRecvBufSize
        {
            get { return _ftpSocketRecvBufSize; }
            private set { _ftpSocketRecvBufSize = value; }
        }

        private static int _ftpAppSendBufSize = 16384;
        /// <summary>
        /// Ftp应用程序发送缓冲区大小。单位字节。小于1为使用系统缺省值16K
        /// </summary>
        public static int FtpAppSendBufSize
        {
            get { return _ftpAppSendBufSize; }
            private set { _ftpAppSendBufSize = value; }
        }

        private static int _ftpAppRecvBufSize = 16384;
        /// <summary>
        /// Ftp应用程序接收缓冲区大小。单位字节。小于1为使用系统缺省值16K
        /// </summary>
        public static int FtpAppRecvBufSize
        {
            get { return _ftpAppRecvBufSize; }
            private set { _ftpAppRecvBufSize = value; }
        }


        private static int _ftpReadCmdResponseWaitMsec = 100;
        /// <summary>
        /// 读取FTP命令回复之前等待的时长。单位毫秒
        /// </summary>
        public static int FtpReadCmdResponseWaitMsec
        {
            get { return _ftpReadCmdResponseWaitMsec; }
            private set { _ftpReadCmdResponseWaitMsec = value; }
        }

        private static int _ftpReportStateInterval = 5;
        /// <summary>
        /// Ftp报告任务状态的间隔。单位秒。
        /// </summary>
        public static int FtpReportStateInterval
        {
            get { return _ftpReportStateInterval; }
            private set { _ftpReportStateInterval = value; }
        }

        private static bool _ftpEnableResume = false;
        /// <summary>
        /// Ftp是否开启断点续传
        /// </summary>
        public static bool FtpEnableResume
        {
            get { return _ftpEnableResume; }
            private set { _ftpEnableResume = value; }
        }
        #endregion

        #region 数据代理参数
        private static string _ndasUrl = "";
        /// <summary>
        ///数据库代理Url
        /// </summary>
        public static string NdasUrl
        {
            get { return _ndasUrl; }
            private set { _ndasUrl = value; }
        }

        private static int _ndasTimeoutMsec = 0;
        /// <summary>
        /// 数据代理超时。单位毫秒。
        /// </summary>
        public static int NdasTimeoutMsec
        {
            get { return _ndasTimeoutMsec; }
            private set { _ndasTimeoutMsec = value; }
        }

        private static string _ndasAuthKey = "";
        /// <summary>
        /// 数据库代理认证Key
        /// </summary>
        public static string NdasAuthKey
        {
            get { return _ndasAuthKey; }
            private set { _ndasAuthKey = value; }
        }

        private static string _ndasAuthIv = "";
        /// <summary>
        /// 数据库代理认证Iv
        /// </summary>
        public static string NdasAuthIv
        {
            get { return _ndasAuthIv; }
            private set { _ndasAuthIv = value; }
        }        
        #endregion

        #region 日志参数
        private static LogType _logType = LogType.Default;
        /// <summary>
        /// 日志类型
        /// </summary>
        public static LogType LogType
        {
            get { return _logType; }
            private set { _logType = value; }
        }

        private static LogLvl _logLvl = LogLvl.None;
        /// <summary>
        /// 日志级别
        /// </summary>
        public static LogLvl LogLvl
        {
            get { return _logLvl; }
            private set { _logLvl = value; }
        }

        private static string _logFile = "";
        /// <summary>
        /// 日志文件名
        /// </summary>
        public static string LogFile
        {
            get { return _logFile; }
            private set { _logFile = value; }
        }
        #endregion

        #endregion
    }
}
