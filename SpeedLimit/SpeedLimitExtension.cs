using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Microsoft.Win32;
using System.ComponentModel;

using Imgo.MultiGet;
using Imgo.MultiGet.ExTools;
using Imgo.MultiGet.XmlReader;

namespace Imgo.MultiGet.SpeedLimit
{
    public class SpeedLimitExtension
    {
        #region Singleton

        private static SpeedLimitExtension instance = new SpeedLimitExtension();

        public static SpeedLimitExtension Instance
        {
            get
            {
                return instance;
            }
        }

        public void newObject()
        {
     
        }

        #endregion

        private const int BalancerUp = 50;
        private const int BalancerDown = -75;

        private double currentWait;
        private bool enabled = false;
        private double maxLimit;

        #region Properties

        public bool CurrentEnabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        public double CurrentMaxRate
        {
            get { return maxLimit; }
        }

        #endregion

        #region Methods

        public void SetMaxRateTemp(double max)
        {
            this.enabled = true;
            this.maxLimit = max;
        }

        public void RestoreMaxRateFromParameters()
        {
            ReadSettings();
        }

        private void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ReadSettings();
        }

        private void ReadSettings()
        {
            currentWait = 0;
            maxLimit = LoadXmlInfo.Instance.MaxRate;
            enabled = LoadXmlInfo.Instance.EnabledLimit;
        }

        private void ProtocolProviderFactory_ResolvingProtocolProvider(object sender, ResolvingProtocolProviderEventArgs e)
        {
            e.ProtocolProvider = new ProtocolProviderProxy(e.ProtocolProvider);
        }

        internal void SendDownloadStateInfo()
        {
            double totalRate = DownloadManager.Instance.TotalDownloadRate;
        }

        internal void WaitFor()
        {
            if (enabled)
            {
                double totalRate = DownloadManager.Instance.TotalDownloadRate;

                if (totalRate > maxLimit)
                {
                    currentWait += BalancerUp;
                }
                else
                {
                    currentWait = Math.Max(currentWait + BalancerDown, 0);
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(currentWait));

                Debug.WriteLine("TotalDownloadRate = " + totalRate);
                Debug.WriteLine("maxLimit = " + maxLimit);
                Debug.WriteLine("currentWait = " + currentWait);
            }
        }

        internal void OnlyWaitFor(Downloader downloader)
        {
            if (enabled)
            {
                if (downloader.Rate / XmlReader.LoadXmlInfo.Instance.MaxSegments > maxLimit)
                {
                    currentWait += BalancerUp;
                }
                else
                {
                    currentWait = Math.Max(currentWait + BalancerDown, 0);
                }

                Logger.Info("ÐÝÃßÊ±¼ä={0}", currentWait);
                Thread.Sleep(TimeSpan.FromMilliseconds(currentWait));
            }
        }

        #endregion

        #region Constructor

        public SpeedLimitExtension()
        {
            ReadSettings();
            ProtocolProviderFactory.ResolvingProtocolProvider += new EventHandler<ResolvingProtocolProviderEventArgs>(ProtocolProviderFactory_ResolvingProtocolProvider);
        }

        #endregion
    }
}