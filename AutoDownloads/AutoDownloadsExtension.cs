using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using Imgo.MultiGet.SpeedLimit;
using Imgo.MultiGet.XmlReader;
using Imgo.MultiGet.Extensions;

namespace Imgo.MultiGet.AutoDownloads
{
    public class AutoDownloadsExtension: /*IExtension, */IDisposable
    {
        DayHourMatrix matrix;
        private bool active;
        private bool needToRestore;
        private System.Threading.Timer timer;

        #region IExtension Members

        //public string Name
        //{
        //    get { return "Auto-Downloads"; }
        //}

        ////public IUIExtension UIExtension
        ////{
        ////    get { return new AutoDownloadsUIExtension(); }
        ////}

        //public IUIExtension UIExtension //wm
        //{
        //    get { return null; }
        //}

        #endregion

        #region Properties
        
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

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            RestoreIfNecessary();

            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }

        #endregion

        #region Methods

        private void PersistList(object state)
        {
            StartJobsIfNeeded();
        }

        private void LoadTimes()
        {
            matrix = new DayHourMatrix(/*parameters.TimesToWork*/);
        }

        void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TimesToWork")
            {
                LoadTimes();
            }
        }

        void Instance_DownloadRemoved(object sender, DownloaderEventArgs e)
        {
            StartJobsIfNeeded();
        }

        void Instance_DownloadEnded(object sender, DownloaderEventArgs e)
        {
            StartJobsIfNeeded();
        }

        void Instance_EndAddBatchDownloads(object sender, EventArgs e)
        {
            StartJobsIfNeeded();
        }

        void Instance_DownloadAdded(object sender, DownloaderEventArgs e)
        {
            if (!e.WillStart)
            {
                StartJobsIfNeeded();

                e.Downloader.StateChanged += delegate(object s, EventArgs ea)
                {
                    if (((Downloader)s).State == DownloaderState.WaitingForReconnect)
                    {
                        StartJobsIfNeeded();
                    }
                };
            }
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
                Debug.WriteLine("Leaving StartJobsIfNeeded");
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
                    //limit.Parameters.Enabled = true;
                    //limit.Parameters.MaxRate = parameters.MaxRateOnTime;
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
                        //if (d.State != DownloaderState.Ended && ! d.IsWorking())
                        if (d.State != DownloaderState.Ended && d.State != DownloaderState.EndedWithError  && !d.IsWorking())//wm alter
                        {
                            DownloadManager.Instance.Downloads[i].Start();
                            count ++;
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

        #endregion

        #region Constructor

        //public AutoDownloadsExtension():
        //    this(new AutoDownloadsParametersSettingsProxy())
        //{
        //}

        //public AutoDownloadsExtension(IAutoDownloadsParameters parameters)
        public AutoDownloadsExtension()
        {
            //if (parameters == null)
            //{
            //    throw new ArgumentNullException("parameters");
            //}

            DownloadManager.Instance.EndAddBatchDownloads += new EventHandler(Instance_EndAddBatchDownloads);
            DownloadManager.Instance.DownloadAdded += new EventHandler<DownloaderEventArgs>(Instance_DownloadAdded);
            DownloadManager.Instance.DownloadEnded += new EventHandler<DownloaderEventArgs>(Instance_DownloadEnded);
            DownloadManager.Instance.DownloadRemoved += new EventHandler<DownloaderEventArgs>(Instance_DownloadRemoved);

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
    }
}