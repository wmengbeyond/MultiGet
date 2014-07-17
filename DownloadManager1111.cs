using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections;
using System.Threading;

using Imgo.MultiGet.SpeedLimit;
using Imgo.MultiGet.XmlReader;
using Imgo.MultiGet.Protocols;
using Imgo.MultiGet.Concurrency;


using Imgo.MultiGet.AutoDownloads;

namespace Imgo.MultiGet
{
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

            LoadTimes();

            TimerCallback refreshCallBack = new TimerCallback(PersistList);
            TimeSpan refreshInterval = TimeSpan.FromMinutes(1);
            timer = new Timer(refreshCallBack, null, new TimeSpan(-1), refreshInterval);

            if (LoadXmlInfo.Instance.AutoStart)
            {
                this.Active = true;
            }
        }

        #endregion

        #region Fields

        DayHourMatrix matrix;
        private bool active;
        private bool needToRestore;
        private System.Threading.Timer timer;

        private List<Downloader> downloads = new List<Downloader>();
        private int addBatchCount;
        private ReaderWriterObjectLocker downloadListSync = new ReaderWriterObjectLocker();

        #endregion

        #region Properties

        public event EventHandler BeginAddBatchDownloads;

        public event EventHandler EndAddBatchDownloads;

        public event EventHandler<DownloaderEventArgs> DownloadEnded;

        public event EventHandler<DownloaderEventArgs> DownloadAdded;

        public event EventHandler<DownloaderEventArgs> DownloadRemoved;

        private void PersistList(object state)
        {
            StartJobsIfNeeded();
        }

        public bool Active
        {
            get { return active; }
            set
            {
                active = value;
                if (active)
                {
                    StartJobsIfNeeded();
                }
                else
                {
                    RestoreIfNecessary();
                }
            }
        }

        public void Dispose()
        {
            RestoreIfNecessary();

            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }

        private void LoadTimes()
        {
            matrix = new DayHourMatrix(/*parameters.TimesToWork*/);
        }

        private int GetActiveJobsCount()
        {
            int count = 0;

            for (int i = 0; i < DownloadManager.Instance.Downloads.Count; i++)
            {
                if (DownloadManager.Instance.Downloads[i].IsWorking() &&
                    DownloadManager.Instance.Downloads[i].State != DownloaderState.WaitingForReconnect)
                {
                    count++;
                }
            }

            return count;
        }

        private void StartJobsIfNeeded()
        {
            if (!Active || DownloadManager.Instance.AddBatchCount > 0)
            {
                //Debug.WriteLine("Leaving StartJobsIfNeeded");
                return;
            }

            DateTime now = DateTime.Now;

            EnableMode em = matrix[now.DayOfWeek, now.Hour];

            if (LoadXmlInfo.Instance.WorkOnlyOnSpecifiedTimes && em == EnableMode.Disabled)
            {
                RestoreIfNecessary();
                return;
            }

            if (em == EnableMode.ActiveWithLimit)
            {
                if (LoadXmlInfo.Instance.MaxRate != LoadXmlInfo.Instance.MaxRateOnTime)
                {
                    SpeedLimitExtension.Instance.SetMaxRateTemp(LoadXmlInfo.Instance.MaxRateOnTime);
                    needToRestore = true;
                }
            }
            else
            {
                RestoreIfNecessary();
            }

            int maxJobs = LoadXmlInfo.Instance.MaxJobs;

            using (DownloadManager.Instance.LockDownloadList(false))
            {
                int count = GetActiveJobsCount();

                if (count < maxJobs)
                {
                    for (int i = 0;
                        (count < maxJobs) && i < DownloadManager.Instance.Downloads.Count;
                        i++)
                    {
                        Downloader d = DownloadManager.Instance.Downloads[i];
                        if (d.State != DownloaderState.Ended && !d.IsWorking())
                        {
                            DownloadManager.Instance.Downloads[i].Start();
                            count++;
                        }
                    }
                }
            }
        }

        private void RestoreIfNecessary()
        {
            if (needToRestore)
            {
                SpeedLimitExtension.Instance.RestoreMaxRateFromParameters();
                needToRestore = false;
            }
        }

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

        void downloader_StateChanged(object sender, EventArgs e)
        {
            Downloader downloader = (Downloader)sender;

            if (downloader.State == DownloaderState.Ended ||
                downloader.State == DownloaderState.EndedWithError)
            {
                OnDownloadEnded((Downloader)sender);
            }
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

        public void RemoveDownload(Downloader downloader)
        {
            if (downloader.State != DownloaderState.NeedToPrepare ||
                downloader.State != DownloaderState.Ended ||
                downloader.State != DownloaderState.Paused)
            {
                downloader.Pause();
            }

            using (LockDownloadList(true))
            {
                downloads.Remove(downloader);
            }

            OnDownloadRemoved(downloader);
        }

        public void ClearEnded()
        {
            using (LockDownloadList(true))
            {
                for (int i = downloads.Count - 1; i >= 0; i--)
                {
                    if (downloads[i].State == DownloaderState.Ended)
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

        public Downloader Add(ResourceLocation rl, ResourceLocation[] mirrors, string localFile, int segments, bool autoStart)
        {
            Downloader d = new Downloader(rl, mirrors, localFile, segments);
            Add(d, autoStart);

            return d;
        }

        public Downloader Add(ResourceLocation rl, ResourceLocation[] mirrors, string localFile, List<Segment> segments, RemoteFileInfo remoteInfo, int requestedSegmentCount, bool autoStart, DateTime createdDateTime)
        {
            Downloader d = new Downloader(rl, mirrors, localFile, segments, remoteInfo, requestedSegmentCount, createdDateTime);
            Add(d, autoStart);

            return d;
        }

        public void Add(Downloader downloader, bool autoStart)
        {
            downloader.StateChanged += new EventHandler(downloader_StateChanged);

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
