using System;
using System.Collections.Generic;
using System.Text;

namespace Imgo.MultiGet.ExTools
{
    /// <summary>
    /// FTP状态事件参数
    /// </summary>
    public sealed class FTPTaskStateEventArgs : EventArgs
    {
        private string _localFile = "";
        private string _remoteFile = "";
        private string _remoteIp = "";
        private int _remotePort = 0;
        private object _userToken = null;
        //private FTPState _state = FTPState.Pending;
        private DownloaderState _state = DownloaderState.Preparing;
        private string _statInfo = "";
        private double _progress = 0.0;
        private long _completeBytes = 0;
        private long _leftBytes = 0;
        private int _speedAvg = 0;
        private TimeSpan _usedTime = new TimeSpan(0);

        /// <summary>
        /// 本地文件
        /// </summary>
        public string LocalFile
        {
            get
            {
                return this._localFile;
            }
        }

        /// <summary>
        /// 远程Ip
        /// </summary>
        public string RemoteIp
        {
            get
            {
                return this._remoteIp;
            }
        }

        /// <summary>
        /// 远程端口
        /// </summary>
        public int RemotePort
        {
            get
            {
                return this._remotePort;
            }
        }

        /// <summary>
        /// 远程文件(格式为ftp://IP:Port/Filename/)
        /// </summary>
        public string RemoteFile
        {
            get
            {
                return this._remoteFile;
            }
        }

        /// <summary>
        /// 用户标识对象
        /// </summary>
        public object UserToken
        {
            get
            {
                return this._userToken;
            }
        }

        /// <summary>
        /// 状态
        /// </summary>
        public DownloaderState State
        {
            get
            {
                return this._state;
            }
            set
            {
                this._state = value;
            }
        }

        /// <summary>
        /// 状态信息
        /// </summary>
        public string StateInfo
        {
            get
            {
                return this._statInfo;
            }
            set
            {
                this._statInfo = value;
            }
        }

        /// <summary>
        /// 完成比例
        /// </summary>
        public double Progress
        {
            get
            {
                return this._progress;
            }
            set
            {
                this._progress = value;
            }
        }

        /// <summary>
        /// 已传输的字节数，单位B
        /// </summary>
        public long CompleteBytes
        {
            get
            {
                return this._completeBytes;
            }
            set
            {
                this._completeBytes = value;
            }
        }

        /// <summary>
        /// 剩余字节数，单位B
        /// </summary>
        public long LeftBytes
        {
            get
            {
                return this._leftBytes;
            }
            set
            {
                this._leftBytes = value;
            }
        }

        /// <summary>
        /// 平均传输速度，单位Bps
        /// </summary>
        public int SpeedAvg
        {
            get
            {
                return this._speedAvg;
            }
            set
            {
                this._speedAvg = value;
            }
        }

        /// <summary>
        /// 耗时
        /// </summary>
        public TimeSpan UsedTime
        {
            get
            {
                return this._usedTime;
            }
            set
            {
                this._usedTime = value;
            }
        }

        /// <summary>
        /// 构造逻辑
        /// </summary>
        /// <param name="localFile">本地文件</param>
        /// <param name="remoteIp">Ftp Ip</param>
        /// <param name="remotePort">Ftp 端口</param>
        /// <param name="remoteFile">远程文件</param>
        /// <param name="state">状态</param>
        /// <param name="stateInfo">状态信息</param>
        /// <param name="progress">完成进度</param>
        /// <param name="completeBytes">已传输字节数</param>
        /// <param name="leftBytes">剩余字节数</param>
        /// <param name="speedAvg">平均传输速度</param>
        /// <param name="usedTime">耗时</param>
        public FTPTaskStateEventArgs(string localFile, string remoteIp, int remotePort, string remoteFile,
            DownloaderState state, string stateInfo, double progress, long completeBytes, long leftBytes, int speedAvg,
            TimeSpan usedTime, object userToken)
        {
            this._localFile = localFile;
            this._remoteIp = remoteIp;
            this._remotePort = remotePort;
            this._remoteFile = string.Format("ftp://{0}:{1}/{2}", this._remoteIp, this._remotePort, remoteFile.Replace("\\", "/").Trim('/'));
            this._state = state;
            this._statInfo = stateInfo;
            this._progress = progress;
            this._completeBytes = completeBytes;
            this._leftBytes = leftBytes;
            this._speedAvg = speedAvg;
            this._userToken = userToken;
        }

        /// <summary>
        /// 重载ToString方法
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(string.Format("本地文件 = {0} \r\n", this.LocalFile));
            sb.AppendFormat(string.Format("远程文件 = {0} \r\n", this.RemoteFile));
            sb.AppendFormat(string.Format("当前状态 = {0} \r\n", this.State.ToString()));
            sb.AppendFormat(string.Format("状态描述 = {0} \r\n", this.StateInfo));
            sb.AppendFormat(string.Format("当前进度 = {0:0.00}% \r\n", this.Progress));
            sb.AppendFormat(string.Format("完成字节 = {0:0.00}KB \r\n", this.CompleteBytes / 1024.0));
            sb.AppendFormat(string.Format("剩余字节 = {0:0.00}KB \r\n", this.LeftBytes / 1024.0));
            sb.AppendFormat(string.Format("平均速度 = {0:0.00}KB/s \r\n", this.SpeedAvg / 1024.0));
            sb.AppendFormat(string.Format("完成耗时 = {0:0.00}s", this.UsedTime.TotalSeconds));
            return sb.ToString();
        }
    }
}
