using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Net;
using Imgo.MultiGet.Concurrency;
using Imgo.MultiGet.Common;
using Imgo.MultiGet.XmlReader;
using Imgo.MultiGet.SpeedLimit;
using Imgo.MultiGet.ExTools;

namespace Imgo.MultiGet
{
    public class Downloader
    {
        private string localFile;
        private int requestedSegmentCount;
        private ResourceLocation resourceLocation;
        private List<ResourceLocation> mirrors;
        private List<Segment> segments;
        private Thread mainThread;
        private List<Thread> threads;
        private RemoteFileInfo remoteFileInfo;
        private DownloaderState state;
        private DateTime createdDateTime;
        private Exception lastError;
        private Dictionary<string, object> extentedProperties = new Dictionary<string, object>();
        
        private IProtocolProvider defaultDownloadProvider;
        private ISegmentCalculator segmentCalculator;
        private IMirrorSelector mirrorSelector;

        private string statusMessage;

        private string FileId;
        private double currentWait;
        private const int BalancerUp = 50;
        private const int BalancerDown = -75;

        private Downloader(
            ResourceLocation rl,
            ResourceLocation[] mirrors, 
            string localFile)
        {
            this.threads = new List<Thread>();
            this.resourceLocation = rl;
            if (mirrors == null)
            {
                this.mirrors = new List<ResourceLocation>();
            }
            else
            {
                this.mirrors = new List<ResourceLocation>(mirrors);
            }
            this.localFile = localFile;

            extentedProperties = new Dictionary<string, object>();

            defaultDownloadProvider = rl.BindProtocolProviderInstance(this);

            segmentCalculator = new MinSizeSegmentCalculator();
            this.MirrorSelector = new SequentialMirrorSelector();
        }

        public Downloader(
            ResourceLocation rl,
            ResourceLocation[] mirrors, 
            string localFile, 
            int segmentCount,
            string fileId):
            this(rl, mirrors, localFile)
        {
            SetState(DownloaderState.NeedToPrepare);

            this.createdDateTime = DateTime.Now;
            this.requestedSegmentCount = segmentCount;
            this.segments = new List<Segment>();
            this.FileId = fileId;
        }

        public Downloader(
            ResourceLocation rl,
            ResourceLocation[] mirrors, 
            string localFile, 
            List<Segment> segments,
            RemoteFileInfo remoteInfo,
            int requestedSegmentCount,
            DateTime createdDateTime,
            string fileId) :
            this(rl, mirrors, localFile)
        {
            if (segments.Count > 0)
            {
                SetState(DownloaderState.Prepared);
            }
            else
            {
                SetState(DownloaderState.NeedToPrepare);
            }

            this.createdDateTime = createdDateTime;
            this.remoteFileInfo = remoteInfo;
            this.requestedSegmentCount = requestedSegmentCount;
            this.segments = segments;
            this.FileId = fileId;
        }

        #region Properties

        public event EventHandler Ending;

        public event EventHandler InfoReceived;

        public event EventHandler StateChanged;

        public event EventHandler<SegmentEventArgs> RestartingSegment;

        public event EventHandler<SegmentEventArgs> SegmentStoped;

        public event EventHandler<SegmentEventArgs> SegmentStarting;

        public event EventHandler<SegmentEventArgs> SegmentStarted;

        public event EventHandler<SegmentEventArgs> SegmentFailed;

        public Dictionary<string, object> ExtendedProperties
        {
            get { return extentedProperties; }
        }

        public ResourceLocation ResourceLocation
        {
            get
            {
                return this.resourceLocation;
            }
        }

        public List<ResourceLocation> Mirrors
        {
            get
            {
                return this.mirrors;
            }
        }

        public long FileSize
        {
            get
            {
                if (remoteFileInfo == null)
                {
                    return 0;
                }
                return remoteFileInfo.FileSize;
            }
        }

        public DateTime CreatedDateTime
        {
            get
            {
                return createdDateTime;
            }
        }

        public int RequestedSegments
        {
            get
            {
                return requestedSegmentCount;
            }
        }

        public string LocalFile
        {
            get
            {
                return this.localFile;
            }
        }

        public double Progress
        {
            get
            {
                int count = segments.Count;

                if (count > 0)
                {
                    double progress = 0;

                    for (int i = 0; i < count; i++)
                    {
                        progress += segments[i].Progress;
                    }

                    return progress / count;
                }
                else
                {
                    return 0;
                }
            }
        }

        public double Rate
        {
            get
            {
                double rate = 0;

                for (int i = 0; i < segments.Count; i++)
                {
                    rate += segments[i].Rate;
                }

                return rate;
            }
        }

        public long Transfered
        {
            get
            {
                long transfered = 0;

                for (int i = 0; i < segments.Count; i++)
                {
                    transfered += segments[i].Transfered;
                }

                return transfered;
            }
        }

        public TimeSpan Left
        {
            get
            {
                if (this.Rate == 0)
                {
                    return TimeSpan.MaxValue;
                }

                double missingTransfer = 0;

                for (int i = 0; i < segments.Count; i++)
                {
                    missingTransfer += segments[i].MissingTransfer;
                }

                return TimeSpan.FromSeconds(missingTransfer / this.Rate);
            }
        } 

        public List<Segment> Segments
        {
            get
            {
                return segments;
            }
        }

        public Exception LastError
        {
            get { return lastError; }
            set { lastError = value; }
        }

        public DownloaderState State
        {
            get { return state; }
        }

        public string FileID
        {
            get { return FileId; }
        }

        public string RemoteIP()
        {
            string ip;
             try
             {
                 Uri u = new Uri(this.ResourceLocation.URL);

                //ip = Uri.UnescapeDataString(u.Segments[u.Segments.Length - 1]);
                ip = u.Host;
             }
             catch
             {
                 ip = string.Empty;
             }

             return ip;
        }

        public bool IsWorking()
        {
            DownloaderState state = this.State;
            return (state == DownloaderState.Preparing ||
                state == DownloaderState.WaitingForReconnect ||
                state == DownloaderState.Working);
        }

        public RemoteFileInfo RemoteFileInfo
        {
            get { return remoteFileInfo; }
        }

        public string StatusMessage
        {
            get { return statusMessage; }
            set { statusMessage = value; }
        }

        public ISegmentCalculator SegmentCalculator
        {
            get { return segmentCalculator; }
            set 
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                segmentCalculator = value; 
            }
        }

        public IMirrorSelector MirrorSelector
        {
            get { return mirrorSelector; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                mirrorSelector = value;
                mirrorSelector.Init(this);                
            }
        }

        #endregion

        private void SetState(DownloaderState value)
        {
            state = value;

            OnStateChanged();
        }

        private void StartToPrepare()
        {
            mainThread = new Thread(new ParameterizedThreadStart(StartDownloadThreadProc));
            mainThread.Start(requestedSegmentCount);
        }

        private void StartPrepared()
        {
            mainThread = new Thread(new ThreadStart(RestartDownload));
            mainThread.Start();
        }

        protected virtual void OnRestartingSegment(Segment segment)
        {
            if (RestartingSegment != null)
            {
                RestartingSegment(this, new SegmentEventArgs(this, segment));
            }
        }

        protected virtual void OnSegmentStoped(Segment segment)
        {
            if (SegmentStoped != null)
            {
                SegmentStoped(this, new SegmentEventArgs(this, segment));
            }
        }

        protected virtual void OnSegmentFailed(Segment segment)
        {
            if (SegmentFailed != null)
            {
                SegmentFailed(this, new SegmentEventArgs(this, segment));
            }
        }

        protected virtual void OnSegmentStarting(Segment segment)
        {
            if (SegmentStarting != null)
            {
                SegmentStarting(this, new SegmentEventArgs(this, segment));
            }
        }

        protected virtual void OnSegmentStarted(Segment segment)
        {
            if (SegmentStarted != null)
            {
                SegmentStarted(this, new SegmentEventArgs(this, segment));
            }
        }

        protected virtual void OnStateChanged()
        {
            if (StateChanged != null)
            {
                StateChanged(this, EventArgs.Empty);
            }
        }

        protected virtual void OnEnding()
        {
            if (Ending != null)
            {
                Ending(this, EventArgs.Empty);
            }
        }

        protected virtual void OnInfoReceived()
        {
            if (InfoReceived != null)
            {
                InfoReceived(this, EventArgs.Empty);

                //EventUtil.DispatchEventAsync<FTPTaskStateEventArgs>(this, this.FTPTaskStateEvent, new FTPTaskStateEventArgs(localFile, this.RemoteIP(), 21, /*myUri.Segments[1]*/ localFile,
                //this.State, this.State.ToString(), this.Progress, this.Transfered, 0, Convert.ToInt32(this.Rate / 1024.0), this.Left, null));
            }
        }

        public IDisposable LockSegments()
        {
            return new ObjectLocker(this.segments);
        }

        public void WaitForConclusion()
        {
            if (! IsWorking())
            {
                if (mainThread != null && mainThread.IsAlive)
                {
                    mainThread.Join(TimeSpan.FromSeconds(1));
                }
            }

            while (IsWorking())
            {
                Thread.Sleep(100);
            }

            Debug.WriteLine(this.State.ToString());
        }

        public void Pause()
        {
            if (state == DownloaderState.Preparing || state == DownloaderState.WaitingForReconnect || state == DownloaderState.EndedWithError)
            {
                Segments.Clear();

                mainThread.Abort();
                mainThread = null;
                SetState(DownloaderState.NeedToPrepare);
                return;
            }

            if (state == DownloaderState.Working)
            {
                SetState(DownloaderState.Pausing);

                while (!AllWorkersStopped(5))
                    ;

                lock (threads)
                {
                    threads.Clear();
                }

                mainThread.Abort();
                mainThread = null;

                if (RemoteFileInfo != null && !RemoteFileInfo.AcceptRanges)
                {
                    // reset the segment
                    Segments[0].StartPosition = 0;
                }

                SetState(DownloaderState.Paused);
            }
        }

        public void Start()
        {
            if (state == DownloaderState.NeedToPrepare)
            {
                SetState(DownloaderState.Preparing);

                StartToPrepare();
            }
            else if (
                state != DownloaderState.Preparing &&
                state != DownloaderState.Pausing &&
                state != DownloaderState.Working &&
                state != DownloaderState.WaitingForReconnect)
            {
                SetState(DownloaderState.Preparing);

                StartPrepared();
            }
        }

        private void AllocLocalFile()
        {
            FileInfo fileInfo = new FileInfo(this.LocalFile);
            if (!Directory.Exists(fileInfo.DirectoryName))
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
            }

            if (fileInfo.Exists)
            {
                // auto rename the file...
                int count = 1;

                string fileExitWithoutExt = Path.GetFileNameWithoutExtension(this.LocalFile);
                string ext = Path.GetExtension(this.LocalFile);

                string newFileName;

                do
                {
                    newFileName = PathHelper.GetWithBackslash(fileInfo.DirectoryName)
                        + fileExitWithoutExt + String.Format("({0})", count++) + ext;
                }
                while (File.Exists(newFileName));
                
                this.localFile = newFileName;
            }

            using (FileStream fs = new FileStream(this.LocalFile, FileMode.Create, FileAccess.Write))
            {
                fs.SetLength(Math.Max(this.FileSize, 0));
            }
        }

        private void StartDownloadThreadProc(object objSegmentCount)
        {
            SetState(DownloaderState.Preparing);

            int segmentCount = Math.Min((int)objSegmentCount, LoadXmlInfo.Instance.MaxSegments);
            Stream inputStream = null;
            int currentTry = 0;

            do
            {
                lastError = null;

                if (state == DownloaderState.Pausing)
                {
                    SetState(DownloaderState.NeedToPrepare);
                    return;
                }

                SetState(DownloaderState.Preparing);

                currentTry++;

                try
                {
                    remoteFileInfo = defaultDownloadProvider.GetFileInfo(this.ResourceLocation, out inputStream);
                    break;
                }
                catch (ThreadAbortException)
                {
                    SetState(DownloaderState.NeedToPrepare);
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    if (currentTry < LoadXmlInfo.Instance.MaxRetries)
                    {
                        SetState(DownloaderState.WaitingForReconnect);
                        Logger.Info("DownloaderState.WaitingForReconnect:" + ex.Message);
                        Thread.Sleep(TimeSpan.FromSeconds(LoadXmlInfo.Instance.RetryDelay));
                    }
                    else
                    {
                        //SetState(DownloaderState.NeedToPrepare);
                        SetState(DownloaderState.EndedWithError);//wm alter
                        return;
                    }
                }
            }
            while (true);

            try
            {
                lastError = null;
                StartSegments(segmentCount, inputStream);
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastError = ex;
                SetState(DownloaderState.EndedWithError);
            }
        }

        //===========================wm add=============================
        /// <summary>
        /// 获取文件大小
        /// </summary>
        /// <param name="file">ip服务器下的相对路径</param>
        /// <returns>文件大小</returns>
        private int GetFileSize()
        {
            StringBuilder result = new StringBuilder();
            FtpWebRequest request;
            try
            {
                request = (FtpWebRequest)FtpWebRequest.Create(new Uri(this.ResourceLocation.URL));
                request.UseBinary = true;
                request.Credentials = new NetworkCredential(this.ResourceLocation.Login, this.ResourceLocation.Password);//设置用户名和密码
                request.Method = WebRequestMethods.Ftp.GetFileSize;

                int dataLength = (int)request.GetResponse().ContentLength;
                Logger.Info("文件大小={0}", dataLength);
                return dataLength;
            }
            catch (Exception ex)
            {
                Console.WriteLine("获取文件大小出错：" + ex.Message);
                return -1;
            }
        }
        //==============================================================

        private void StartSegments(int segmentCount, Stream inputStream)
        {
            Logger.Info("运行StartSegments()1");

            // notifies
            OnInfoReceived();

            // allocs the file on disk
            AllocLocalFile();

            long segmentSize;

            CalculatedSegment[] calculatedSegments;

            if (!remoteFileInfo.AcceptRanges)
            {
                calculatedSegments = new CalculatedSegment[] { new CalculatedSegment(0, remoteFileInfo.FileSize) };
            }
            else
            {
                calculatedSegments = this.SegmentCalculator.GetSegments(segmentCount, remoteFileInfo);
            }

            lock (threads) threads.Clear();
            lock (segments) segments.Clear();

            for (int i = 0; i < calculatedSegments.Length; i++)
            {
                Segment segment = new Segment();
                if (i == 0)
                {
                    segment.InputStream = inputStream;
                }

                segment.Index = i;
                segment.InitialStartPosition = calculatedSegments[i].StartPosition;
                segment.StartPosition = calculatedSegments[i].StartPosition;
                segment.EndPosition = calculatedSegments[i].EndPosition;

                segments.Add(segment);
            }

            RunSegments();
        }

        private void RestartDownload()
        {
            int currentTry = 0;
            Stream stream;
            RemoteFileInfo newInfo;

            try
            {
                do
                {
                    lastError = null;

                    SetState(DownloaderState.Preparing);

                    currentTry++;
                    try
                    {
                        newInfo = defaultDownloadProvider.GetFileInfo(this.ResourceLocation, out stream);

                        break;
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                        if (currentTry < LoadXmlInfo.Instance.MaxRetries)
                        {
                            SetState(DownloaderState.WaitingForReconnect);
                            Thread.Sleep(TimeSpan.FromSeconds(LoadXmlInfo.Instance.RetryDelay));
                        }
                        else
                        {
                            SetState(DownloaderState.EndedWithError);//wm alter
                            return;
                        }
                    }
                }
                while (true);
            }
            finally
            {
                SetState(DownloaderState.Prepared);
            }

            try
            {
                // check if the file changed on the server
                if (!newInfo.AcceptRanges ||
                    newInfo.LastModified > RemoteFileInfo.LastModified ||
                    newInfo.FileSize != RemoteFileInfo.FileSize)
                {
                    this.remoteFileInfo = newInfo;
                    StartSegments(this.RequestedSegments, stream);
                }
                else
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }

                    RunSegments();
                }
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastError = ex;
                SetState(DownloaderState.EndedWithError);
            }
        }

        private void RunSegments()
        {
            SetState(DownloaderState.Working);

            // TODO comparar o remote file Info se esta igual, se o download saiu de paused/prepared
            // e nao veio da thread que le o fileinfo

            using (FileStream fs = new FileStream(this.LocalFile, FileMode.Open, FileAccess.Write))
            {
                for (int i = 0; i < this.Segments.Count; i++)
                {
                    Segments[i].OutputStream = fs;
                    StartSegment(Segments[i]);
                }

                do
                {
                    while (!AllWorkersStopped(1000))
                        ;
                }
                while (RestartFailedSegments());
            }

            for (int i = 0; i < this.Segments.Count; i++)
            {
                if (Segments[i].State == SegmentState.Error)
                {
                    SetState(DownloaderState.EndedWithError);
                    return;
                }
            }

            if (this.State != DownloaderState.Pausing)
            {
                OnEnding();
            }

            SetState(DownloaderState.Ended);
        }

        private bool RestartFailedSegments()
        {
            bool hasErrors = false;
            double delay = 0;

            for (int i = 0; i < this.Segments.Count; i++)
            {
                if (Segments[i].State == SegmentState.Error &&
                    Segments[i].LastErrorDateTime != DateTime.MinValue &&
                    (LoadXmlInfo.Instance.MaxRetries == 0 ||
                    Segments[i].CurrentTry < LoadXmlInfo.Instance.MaxRetries))
                {
                    hasErrors = true;
                    TimeSpan ts =  DateTime.Now - Segments[i].LastErrorDateTime;

                    if (ts.TotalSeconds >= LoadXmlInfo.Instance.RetryDelay)
                    {
                        Segments[i].CurrentTry++;
                        StartSegment(Segments[i]);
                        OnRestartingSegment(Segments[i]);
                    }
                    else
                    {
                        delay = Math.Max(delay, LoadXmlInfo.Instance.RetryDelay * 1000 - ts.TotalMilliseconds);
                    }
                }
            }

            Thread.Sleep((int)delay);

            return hasErrors;
        }

        private void StartSegment(Segment newSegment)
        {
            Thread segmentThread = new Thread(new ParameterizedThreadStart(SegmentThreadProc));
            //segmentThread.Start(newSegment);

            lock (threads)
            {
                threads.Add(segmentThread);
            }

            segmentThread.Start(newSegment);
        }

        private bool AllWorkersStopped(int timeOut)
        {
            bool allFinished = true;

            Thread[] workers;

            lock (threads)
            {
                workers = threads.ToArray();
            }

            foreach (Thread t in workers)
            {
                Logger.Info("下载完成AllWorkersStoppedworkers={0}", t.ManagedThreadId);

                bool finished = t.Join(timeOut);
                allFinished = allFinished & finished;

                if (finished)
                {
                    Logger.Info("下载完成AllWorkersStoppedworkersthreads={0}", t.ManagedThreadId);
                    lock (threads)
                    {
                        threads.Remove(t);
                    }
                }
            }

            return allFinished;
        }

        //Unhandled Exception:
        //System.ObjectDisposedException: The object was used after being disposed.
        //at System.Net.Sockets.NetworkStream.CheckDisposed () [0x00000] in <filename unknown>:0 
        //at System.Net.Sockets.NetworkStream.Read (System.Byte[] buffer, Int32 offset, Int32 size) [0x00000] in <filename unknown>:0 
        //at System.IO.StreamReader.ReadBuffer () [0x00000] in <filename unknown>:0 
        //at System.IO.StreamReader.ReadLine () [0x00000] in <filename unknown>:0 
        //at System.Net.FtpWebRequest.GetResponseStatus () [0x00000] in <filename unknown>:0 
        //at System.Net.FtpWebRequest.SetTransferCompleted () [0x00000] in <filename unknown>:0 
        //at (wrapper remoting-invoke-with-check) System.Net.FtpWebRequest:SetTransferCompleted ()
        //at System.Net.FtpDataStream.Dispose (Boolean disposing) [0x00000] in <filename unknown>:0 
        //at System.Net.FtpDataStream.Finalize () [0x00000] in <filename unknown>:0 

        //Looks like Mono's TcpClient.ReceiveBufferSize is larger than on Windows.
        //According to the MSDN Docs, the default is 8192 bytes.
        //I just checked Mono's implementation and it's calling GetSocketOption() on the underlying socket, so it's using the operating system's default.
        //You have three options:

        //1Change the socket's receive buffer size by using client.ReceiveBufferSize = new_size.
        //2.Allocate a large enough buffer.
        //3.Don't read more data than the buffer can hold:
        //var max_size = Math.Min (client.ReceiveBufferSize, buffer.Length);
        //bytesRead = nStream.Read(inStream, 0, max_size);

        private void SegmentThreadProc(object objSegment)
        {
            Segment segment = (Segment)objSegment;

            segment.LastError = null;

            try
            {
                if (segment.EndPosition > 0 && segment.StartPosition >= segment.EndPosition)
                {
                    segment.State = SegmentState.Finished;

                    // raise the event
                    OnSegmentStoped(segment);

                    return;
                }

                //int buffSize = 8192;//wm alter
                int buffSize = LoadXmlInfo.Instance.MaxRecvBufSize; //86 *1024;
                //int receiveMaxBufSize = 0; //wm alter
                byte[] buffer = new byte[buffSize];

                segment.State = SegmentState.Connecting;

                // raise the event 
                OnSegmentStarting(segment);

                if (segment.InputStream == null)
                {
                    // get the next URL (It can the the main url or some mirror)
                    ResourceLocation location = this.MirrorSelector.GetNextResourceLocation();
                    // get the protocol provider for that mirror
                    IProtocolProvider provider = location.BindProtocolProviderInstance(this);

                    while (location != this.ResourceLocation)
                    {
                        Stream tempStream;

                        // get the remote file info on mirror
                        RemoteFileInfo tempRemoteInfo = provider.GetFileInfo(location, out tempStream);
                        if (tempStream != null) tempStream.Dispose();

                        // check if the file on mirror is the same
                        if (tempRemoteInfo.FileSize == remoteFileInfo.FileSize &&
                            tempRemoteInfo.AcceptRanges == remoteFileInfo.AcceptRanges)
                        {
                            // if yes, stop looking for the mirror
                            break;
                        }

                        lock (mirrors)
                        {
                            // the file on the mirror is not the same, so remove from the mirror list
                            mirrors.Remove(location);
                        }

                        // the file on the mirror is different
                        // so get other mirror to use in the segment
                        location = this.MirrorSelector.GetNextResourceLocation();
                        provider = location.BindProtocolProviderInstance(this);
                    }

                    // get the input stream from start position
                    segment.InputStream = provider.CreateStream(location, segment.StartPosition, segment.EndPosition);

                    // change the segment URL to the mirror URL
                    segment.CurrentURL = location.URL;
                }
                else
                {
                    //  change the segment URL to the main URL
                    segment.CurrentURL = this.resourceLocation.URL;
                }

                //FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(new Uri(segment.CurrentURL));
                //request.Credentials = new NetworkCredential(this.resourceLocation.Login, this.resourceLocation.Password);

                //request.Method = WebRequestMethods.Ftp.DownloadFile;
                //request.ContentOffset = segment.StartPosition;

                ////wm Start 
                //request.Timeout = LoadXmlInfo.Instance.ConnectTimeout;
                //request.ReadWriteTimeout = LoadXmlInfo.Instance.ReadWriteTimeout;
                ////request.KeepAlive = LoadXmlInfo.Instance.KeepLive;
                ////wm End

                //WebResponse response = request.GetResponse();

                //segment.InputStream = response.GetResponseStream();

                //    // raise the event
                //    OnSegmentStarted(segment);

                //    // change the segment state
                //    segment.State = SegmentState.Downloading;
                //    segment.CurrentTry = 0;
                //    //segment.InputStream.ReadTimeout = LoadXmlInfo.Instance.ReadWriteTimeout;//wm alter

                //    long readSize;

                //    do
                //    {
                //        // reads the buffer from input stream
                //        readSize = segment.InputStream.Read(buffer, 0, buffSize);

                //        // check if the segment has reached the end
                //        if (segment.EndPosition > 0 &&
                //            segment.StartPosition + readSize > segment.EndPosition)
                //        {
                //            // adjust the 'readSize' to write only necessary bytes
                //            readSize = (segment.EndPosition - segment.StartPosition);
                //            if (readSize <= 0)
                //            {
                //                segment.StartPosition = segment.EndPosition;
                //                break;
                //            }
                //        }

                //        // locks the stream to avoid that other threads changes
                //        // the position of stream while this thread is writing into the stream
                //        lock (segment.OutputStream)
                //        {
                //            segment.OutputStream.Position = segment.StartPosition;
                //            segment.OutputStream.Write(buffer, 0, (int)readSize);
                //        }

                //        // increse the start position of the segment and also calculates the rate
                //        segment.IncreaseStartPosition(readSize);

                //        // check if the stream has reached its end
                //        if (segment.EndPosition > 0 && segment.StartPosition >= segment.EndPosition)
                //        {
                //            segment.StartPosition = segment.EndPosition;
                //            break;
                //        }

                //        // check if the user have requested to pause the download
                //        if (state == DownloaderState.Pausing)
                //        {
                //            segment.State = SegmentState.Paused;
                //            break;
                //        }

                //        if (LoadXmlInfo.Instance.EnabledLimit)
                //        {
                //            if (this.Rate / XmlReader.LoadXmlInfo.Instance.MaxSegments > LoadXmlInfo.Instance.MaxRate)
                //            {
                //                currentWait += BalancerUp;
                //            }
                //            else
                //            {
                //                currentWait = Math.Max(currentWait + BalancerDown, 0);
                //            }

                //            Thread.Sleep(TimeSpan.FromMilliseconds(currentWait));
                //        }
                //    }
                //    while (readSize > 0);

                //    Thread.Sleep(100);
                //    segment.InputStream.Close();
                //    response.Close();

                    //segment.InputStream.Dispose();

                //using (segment.InputStream)
                //{
                //    Logger.Info("线程segment.InputStream={0}", segment.InputStream.GetHashCode());
                //    // raise the event
                //    OnSegmentStarted(segment);

                //    // change the segment state
                //    segment.State = SegmentState.Downloading;
                //    segment.CurrentTry = 0;
                //    //segment.InputStream.ReadTimeout = LoadXmlInfo.Instance.ReadWriteTimeout;//wm alter

                //    long readSize;

                //    do
                //    {
                //        // reads the buffer from input stream
                //        readSize = segment.InputStream.Read(buffer, 0, buffSize);

                //        //var max_size = Math.Min(receiveMaxBufSize, buffer.Length);//针对read错误的方法2 暂不使用
                //        //readSize = segment.InputStream.Read(buffer, 0, max_size);

                //        // check if the segment has reached the end
                //        if (segment.EndPosition > 0 &&
                //            segment.StartPosition + readSize > segment.EndPosition)
                //        {
                //            // adjust the 'readSize' to write only necessary bytes
                //            readSize = (segment.EndPosition - segment.StartPosition);
                //            if (readSize <= 0)
                //            {
                //                segment.StartPosition = segment.EndPosition;
                //                break;
                //            }
                //        }

                //        // locks the stream to avoid that other threads changes
                //        // the position of stream while this thread is writing into the stream
                //        lock (segment.OutputStream)
                //        {
                //            segment.OutputStream.Position = segment.StartPosition;
                //            segment.OutputStream.Write(buffer, 0, (int)readSize);
                //        }

                //        // increse the start position of the segment and also calculates the rate
                //        segment.IncreaseStartPosition(readSize);

                //        // check if the stream has reached its end
                //        if (segment.EndPosition > 0 && segment.StartPosition >= segment.EndPosition)
                //        {
                //            segment.StartPosition = segment.EndPosition;
                //            break;
                //        }

                //        // check if the user have requested to pause the download
                //        if (state == DownloaderState.Pausing)
                //        {
                //            segment.State = SegmentState.Paused;
                //            break;
                //        }

                //        //Thread.Sleep(1500);

                //        if (LoadXmlInfo.Instance.EnabledLimit)
                //        {
                //            if (this.Rate / XmlReader.LoadXmlInfo.Instance.MaxSegments > LoadXmlInfo.Instance.MaxRate)
                //            {
                //                currentWait += BalancerUp;
                //            }
                //            else
                //            {
                //                currentWait = Math.Max(currentWait + BalancerDown, 0);
                //            }

                //            Thread.Sleep(TimeSpan.FromMilliseconds(currentWait));
                //        }

                //        //SpeedLimitExtension.Instance.OnlyWaitFor(this);//wm alter
                //    }
                //    while (readSize > 0);


                    // raise the event
                    OnSegmentStarted(segment);

                    // change the segment state
                    segment.State = SegmentState.Downloading;
                    segment.CurrentTry = 0;
                    //segment.InputStream.ReadTimeout = LoadXmlInfo.Instance.ReadWriteTimeout;//wm alter

                    long readSize;

                    do
                    {
                        // reads the buffer from input stream
                        readSize = segment.InputStream.Read(buffer, 0, buffSize);

                        //var max_size = Math.Min(receiveMaxBufSize, buffer.Length);//针对read错误的方法2 暂不使用
                        //readSize = segment.InputStream.Read(buffer, 0, max_size);

                        // check if the segment has reached the end
                        if (segment.EndPosition > 0 &&
                            segment.StartPosition + readSize > segment.EndPosition)
                        {
                            // adjust the 'readSize' to write only necessary bytes
                            readSize = (segment.EndPosition - segment.StartPosition);
                            if (readSize <= 0)
                            {
                                segment.StartPosition = segment.EndPosition;
                                break;
                            }
                        }

                        // locks the stream to avoid that other threads changes
                        // the position of stream while this thread is writing into the stream
                        lock (segment.OutputStream)
                        {
                            segment.OutputStream.Position = segment.StartPosition;
                            segment.OutputStream.Write(buffer, 0, (int)readSize);
                        }

                        // increse the start position of the segment and also calculates the rate
                        segment.IncreaseStartPosition(readSize);

                        // check if the stream has reached its end
                        if (segment.EndPosition > 0 && segment.StartPosition >= segment.EndPosition)
                        {
                            segment.StartPosition = segment.EndPosition;
                            break;
                        }

                        // check if the user have requested to pause the download
                        if (state == DownloaderState.Pausing)
                        {
                            segment.State = SegmentState.Paused;
                            break;
                        }

                        //Thread.Sleep(1500);

                        if (LoadXmlInfo.Instance.EnabledLimit)
                        {
                            if (this.Rate / XmlReader.LoadXmlInfo.Instance.MaxSegments > LoadXmlInfo.Instance.MaxRate)
                            {
                                currentWait += BalancerUp;
                            }
                            else
                            {
                                currentWait = Math.Max(currentWait + BalancerDown, 0);
                            }

                            Thread.Sleep(TimeSpan.FromMilliseconds(currentWait));
                        }
                    }
                    while (readSize > 0);

                    Thread.Sleep(100);
                    segment.InputStream.Dispose();

                //    //修改异常问题
                //    //The network read timeout happens when downloading a file from slow or problematic network and 
                //    //timeout exception fires in a different layer, so readSize becomes zero and "while" loop ends.
                //    //if (segment.State == SegmentState.Downloading)
                //    //{
                //    //    segment.State = SegmentState.Finished;

                //    //    // try to create other segment, 
                //    //    // spliting the missing bytes from one existing segment
                //    //    AddNewSegmentIfNeeded();
                //    //}

                    if (segment.State == SegmentState.Downloading && segment.StartPosition == segment.EndPosition)
                    {
                        segment.State = SegmentState.Finished;

                        // try to create other segment, 
                        // spliting the missing bytes from one existing segment
                        AddNewSegmentIfNeeded();
                    }
                    else
                    {
                        segment.State = SegmentState.Error;
                        //SetState(DownloaderState.EndedWithError);
                        //throw new Exception("Probably stream read has been timeout.");
                    }
                //}
            }
            catch (Exception ex)
            {
                // store the error information
                segment.State = SegmentState.Error;
                segment.LastError = ex;

                Logger.Info("异常Exception={0}", Thread.CurrentThread.ManagedThreadId);

                // raise the event
                OnSegmentFailed(segment);
            }
            finally
            {
                // clean up the segment
                if (segment.InputStream != null)
                {
                    segment.InputStream = null;
                }
            }

            Logger.Info("下载完成中");
        }

        private void AddNewSegmentIfNeeded()
        {
            lock (segments)
            {
                for (int i = 0; i < this.segments.Count; i++)
                {
                    Segment oldSegment = this.segments[i];
                    if (oldSegment.State == SegmentState.Downloading &&
                        oldSegment.Left.TotalSeconds > LoadXmlInfo.Instance.MinSegmentLeftToStartNewSegment &&
                        oldSegment.MissingTransfer / 2 >= LoadXmlInfo.Instance.MinSegmentSize)
                    {
                        // get the half of missing size of oldSegment
                        long newSize = oldSegment.MissingTransfer / 2;

                        // create a new segment allocation the half old segment
                        Segment newSegment = new Segment();
                        newSegment.Index = this.segments.Count;
                        newSegment.StartPosition = oldSegment.StartPosition + newSize;
                        newSegment.InitialStartPosition = newSegment.StartPosition;
                        newSegment.EndPosition = oldSegment.EndPosition;
                        newSegment.OutputStream = oldSegment.OutputStream;

                        // removes bytes from old segments
                        oldSegment.EndPosition = oldSegment.EndPosition - newSize;

                        // add the new segment to the list
                        segments.Add(newSegment);
                        StartSegment(newSegment);
                        break;
                    }
                }
            }
        }
    }
}