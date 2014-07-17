using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections;
using System.Threading;

using Imgo.MultiGet.Common;
using Imgo.MultiGet.XmlReader;
using Imgo.MultiGet.Protocols;
using Imgo.MultiGet.Concurrency;
using Imgo.MultiGet.SpeedLimit;
using Imgo.MultiGet.Extensions;
using Imgo.MultiGet.PersistedList;
using Imgo.MultiGet.AutoDownloads;

namespace Imgo.MultiGet
{
    //public struct ResourceLocationInfo
    //{
    //    public string url;
    //    //public bool authenticate = true;
    //    public string login;
    //    public string password;
    //}

    public class DownloadManager
    {
        #region Singleton

        private static DownloadManager instance = new DownloadManager();

        public static DownloadManager Instance
        {
            get
            {
                return instance;
            }
        } 

        public DownloadManager()
        {
            new HttpFtpProtocolExtension();
            SpeedLimitExtension.Instance.newObject();
        }

        #endregion

        #region Fields
        
        private List<Downloader> downloads = new List<Downloader>();
        private int addBatchCount;
        private ReaderWriterObjectLocker downloadListSync = new ReaderWriterObjectLocker();

        private List<IDisposable> extensions = new List<IDisposable>();
        private bool disposed = false;

        #endregion

        #region Properties

        public event EventHandler BeginAddBatchDownloads;

        public event EventHandler EndAddBatchDownloads;

        public event EventHandler<DownloaderEventArgs> DownloadEnded;

        public event EventHandler<DownloaderEventArgs> DownloadAdded;

        public event EventHandler<DownloaderEventArgs> DownloadRemoved;

        public event EventHandler<DownloaderEventArgs> DownloadStateInfo;

        public ReadOnlyCollection<Downloader> Downloads
        {
            get
            {
                return downloads.AsReadOnly();
            }
        }

        public double TotalDownloadRate
        {
            get 
            {
                double total = 0;

                using (LockDownloadList(false))
                {
                    for (int i = 0; i < this.Downloads.Count; i++)
                    {
                        OnDownloadStateInfo(this.Downloads[i]);
                        if (this.Downloads[i].State == DownloaderState.Working)
                        {
                            total += this.Downloads[i].Rate;
                        }
                    }
                }

                return total; 
            }
        }

        public int AddBatchCount
        {
            get
            {
                return addBatchCount;
            }
        }

        #endregion 

        #region Methods

        public List<IDisposable> Extensions
        {
            get
            {
                return extensions;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void ExDispose()
        {
            if (!disposed)
            {
                disposed = true;
                for (int i = 0; i < Extensions.Count; i++)
                {
                    if (Extensions[i] is IDisposable)
                    {
                        try
                        {
                            ((IDisposable)Extensions[i]).Dispose();
                        }
                        catch (Exception ex)
                        {
                            //Debug.WriteLine(ex.ToString());
                        }
                    }
                }
            }
        }

        public void InitExtensions()
        {
            extensions.Add(new PersistedListExtension());
            extensions.Add(new AutoDownloadsExtension());
        }

        /// <summary>
        /// MultiParamInfo 只需要设置最基本的
        /// downloadFolder就可以下载 其他有个别动态调整参数 详见MultiParamInfo类
        /// </summary>
        /// <param name="info"></param>
        public void InitExtensions(ref MultiParamInfo info)
        {
            extensions.Add(new PersistedListExtension());
            extensions.Add(new AutoDownloadsExtension());
            info.AssignmentToXml();
        }

        void downloader_StateChanged(object sender, EventArgs e)
        {
            Downloader downloader = (Downloader)sender;

            if (downloader.State == DownloaderState.Ended ||
                downloader.State == DownloaderState.EndedWithError)
            {
                OnDownloadEnded((Downloader)sender);
            }

            //OnDownloadStateInfo(downloader);//wm
        }

        public IDisposable LockDownloadList(bool lockForWrite)
        {
            if (lockForWrite)
            {
                return downloadListSync.LockForWrite();
            }
            else
            {
                return downloadListSync.LockForRead();
            }
        }

        public void RemoveDownload(int index)
        {
            RemoveDownload(downloads[index]);
        }

        /// <summary>
        /// 通过fileId移除下载文件
        /// </summary>
        /// <param name="fileId"></param>
        public void RemoveDownload(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
                return;

            using (LockDownloadList(true))
            {
                for (int i = downloads.Count - 1; i >= 0; i--)
                {
                    if (downloads[i].FileID.Equals(fileId))
                    {
                        Downloader d = downloads[i];
                        downloads.RemoveAt(i);
                        OnDownloadRemoved(d);
                    }
                }
            }
        }

        public void RemoveDownload(Downloader downloader)
        {
            //if (downloader.State != DownloaderState.NeedToPrepare ||
            //    downloader.State != DownloaderState.Ended ||
            //    downloader.State != DownloaderState.Paused)

            if (downloader.State != DownloaderState.NeedToPrepare || 
                downloader.State != DownloaderState.Ended || 
                downloader.State != DownloaderState.EndedWithError || 
                downloader.State != DownloaderState.Paused)//wm alter
            {
                downloader.Pause();
            }

            using (LockDownloadList(true))
            {
                downloads.Remove(downloader);
            }

            OnDownloadRemoved(downloader);
        }

        /// <summary>
        /// 移除结束的下载任务
        /// </summary>
        public void ClearEnded()
        {
            using (LockDownloadList(true))
            {
                for (int i = downloads.Count - 1; i >= 0; i--)
                {
                    //if (downloads[i].State == DownloaderState.Ended)
                    if (downloads[i].State == DownloaderState.EndedWithError)
                    {
                        this.Downloads[i].Pause();
                    }

                    if (downloads[i].State == DownloaderState.Ended || downloads[i].State == DownloaderState.EndedWithError)//wm alter
                    {
                        Downloader d = downloads[i];
                        downloads.RemoveAt(i);
                        OnDownloadRemoved(d);
                    }
                }
            }
        }

        public void PauseAll()
        {
            using (LockDownloadList(false))
            {
                for (int i = 0; i < this.Downloads.Count; i++)
                {
                    this.Downloads[i].Pause();
                }
            }
        }

        //public string LocalFile(string url)
        //{
        //    return PathHelper.GetWithBackslash(LoadXmlInfo.Instance.DownloadFolder) + Filename(url);
        //}

        public string LocalFile(string subpath)
        {
            return PathHelper.GetWithBackslash(LoadXmlInfo.Instance.DownloadFolder) + subpath;
        }

        public string Filename(string url)
        {
            string filename;

            try
            {
                Uri u = new Uri(url);
                filename = Uri.UnescapeDataString(u.Segments[u.Segments.Length - 1]);
            }
            catch
            {
                filename = string.Empty;
            }

             return filename;
        }

        /// <summary>
        /// 下载文件只需要调用这个函数
        /// </summary>
        /// <param name="rlInfo">下载源地址列表 只需要添加url login password 后两项没有可不填</param>
        /// <param name="localFile">子路径</param>
        /// <param name="fileId">md5 code</param>
        /// <param name="autoStart">是否自动下载，一般设置false，系统会自动接管下载。设置true，立即下载</param>
        /// <returns> false 失败 true 成功</returns>
        public bool Add(List<ResourceLocation> rlInfo, string localFile, string fileId, bool autoStart=false)
        {
            if (rlInfo.Count <= 0)
                return false;

            try
            {
                ResourceLocation rl = rlInfo[0];
                //string localFile = LocalFile(rl.URL);

                rl.BindProtocolProviderType();

                if (rl.ProtocolProviderType == null)
                {
                    return false;
                }

                rlInfo.RemoveAt(0);

                ResourceLocation[] mirrors = new ResourceLocation[rlInfo.Count];
                for (int i = 0; i < rlInfo.Count; i++)
                {
                    mirrors[i] = ResourceLocation.FromURL(
                        rlInfo[i].URL,
                        BoolFormatter.FromString("Yes"),
                        rlInfo[i].Login,
                        rlInfo[i].Password);
                }

                if (mirrors != null && mirrors.Length > 0)
                {
                    foreach (ResourceLocation mirrorRl in mirrors)
                    {
                        mirrorRl.BindProtocolProviderType();

                        if (mirrorRl.ProtocolProviderType == null)
                        {
                            return false;
                        }
                    }
                }

                int segments = LoadXmlInfo.Instance.MaxSegments;

                Downloader download = DownloadManager.Instance.Add(
                        rl,
                        mirrors,
                        //LocalFile(localFile),
                        localFile,
                        segments,
                        fileId,
                        autoStart);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public Downloader Add(ResourceLocation rl, ResourceLocation[] mirrors, string localFile, int segments, string fileId, bool autoStart)
        {
            Downloader d = new Downloader(rl, mirrors, localFile, segments, fileId);
            Add(d, autoStart);

            return d;
        }

        public Downloader Add(ResourceLocation rl, ResourceLocation[] mirrors, string localFile, List<Segment> segments, RemoteFileInfo remoteInfo, int requestedSegmentCount, bool autoStart, DateTime createdDateTime, string fileId)
        {
            Downloader d = new Downloader(rl, mirrors, localFile, segments, remoteInfo, requestedSegmentCount, createdDateTime, fileId);
            Add(d, autoStart);

            return d;
        }

        public void Add(Downloader downloader, bool autoStart)
        {
            downloader.StateChanged += new EventHandler(downloader_StateChanged);

            downloader.InfoReceived += new EventHandler(downloader_StateChanged);//wm

            using (LockDownloadList(true))
            {
                downloads.Add(downloader);
            }

            OnDownloadAdded(downloader, autoStart);

            if (autoStart)
            {
                downloader.Start();
            }
        }

        public virtual void OnBeginAddBatchDownloads()
        {
            addBatchCount++;

            if (BeginAddBatchDownloads != null)
            {
                BeginAddBatchDownloads(this, EventArgs.Empty);
            }
        }

        public virtual void OnEndAddBatchDownloads()
        {
            addBatchCount--;

            if (EndAddBatchDownloads != null)
            {
                EndAddBatchDownloads(this, EventArgs.Empty);
            }
        }

        protected virtual void OnDownloadEnded(Downloader d)
        {
            if (DownloadEnded != null)
            {
                DownloadEnded(this, new DownloaderEventArgs(d));
            }
        }

        protected virtual void OnDownloadAdded(Downloader d, bool willStart)
        {
            if (DownloadAdded != null)
            {
                DownloadAdded(this, new DownloaderEventArgs(d, willStart));
            }
        }

        protected virtual void OnDownloadRemoved(Downloader d)
        {
            if (DownloadRemoved != null)
            {
                DownloadRemoved(this, new DownloaderEventArgs(d));
            }
        }

        protected virtual void OnDownloadStateInfo(Downloader d)
        {
            if (DownloadStateInfo != null)
            {
//                EventUtil.DispatchEventAsync<FTPTaskStateEventArgs>(this, this.FTPTaskStateEvent, new FTPTaskStateEventArgs(localFile, this.RemoteIP(), 21, /*myUri.Segments[1]*/ localFile,
//this.State, this.State.ToString(), this.Progress, this.Transfered, 0, Convert.ToInt32(this.Rate / 1024.0), this.Left, null));
                DownloadStateInfo(this, new DownloaderEventArgs(d));
            }
        }

        public void SwapDownloads(int idx, bool isThreadSafe)
        {
            if (isThreadSafe)
            {
                InternalSwap(idx);
            }
            else
            {
                using (LockDownloadList(true))
                {
                    InternalSwap(idx);
                }
            }
        }

        private void InternalSwap(int idx)
        {
            if (this.downloads.Count <= idx)
            {
                //return;
            }

            Downloader it1 = this.downloads[idx];
            Downloader it2 = this.downloads[idx - 1];

            this.downloads.RemoveAt(idx);
            this.downloads.RemoveAt(idx - 1);

            this.downloads.Insert(idx - 1, it1);
            this.downloads.Insert(idx, it2);
        }

        #endregion


    }
}
