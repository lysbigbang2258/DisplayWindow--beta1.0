// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UdpWaveData.cs" company="">
//
// </copyright>
// <summary>
//   Defines the UdpWaveData type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ArrayDisplay.BaseUtl;
using ArrayDisplay.DataUtl;
using ArrayDisplay.UI;

namespace ArrayDisplay.NetUtl
{
    /// <summary>
    ///     The udp wave data.
    /// </summary>
    public class UdpWaveData : IDisposable
    {
        /// <summary>
        /// The frame nums.
        /// 一帧数据长度
        /// </summary>
        public static int FrameNums;

        public static ConstUdpArg.WaveType waveType; // 波形数据类型

        private static readonly LinkedList<Array> linkbuffer = new LinkedList<Array>(); // 缓存数据buff

        private object lockobj = new object();

        public byte[][] Splikbytes;

        private SemaphoreSlim semaphore;

        private readonly int[] delayChannelOffsets = new int[8];

        private readonly int[] origChannelOffsets = new int[64];

        private readonly Socket waveSocket;

        private volatile bool exitFlag;

        private byte[] rcvBuf; // 接收数据缓存

        List<byte[]> recTmpBytes = new List<byte[]>();
        List<byte[]> recSendBytes = new List<byte[]>();
        List<byte> saveBytes = new List<byte>();

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpWaveData" /> class.
        /// </summary>
        public UdpWaveData()
        {
            try
            {
                waveSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                StartRcvEvent = new AutoResetEvent(false);
                IsStopRcved = false;
                semaphore = new SemaphoreSlim(0,1);
            }
            catch (Exception e)
            {
                LogHelper.LogError(e);
                throw;
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="UdpWaveData" /> class.
        /// </summary>
        ~UdpWaveData()
        {
            Dispose(false);
        }

        /// <summary>
        ///     原始数据保存方法
        /// </summary>
        public static EventHandler<byte[]> OrigSaveDataEventHandler { get; set; }

        /// <summary>
        ///     原始数据保存方法
        /// </summary>
        public static EventHandler<byte[][]> WorkDataEventHandler { get; set; }

        /// <summary>
        ///     正常工作数据保存方法
        /// </summary>
        public static EventHandler<byte[]> WorkSaveDataEventHandler { get; set; }

        /// <summary>
        ///     标志位
        /// </summary>
        public bool ExitFlag {
            get => exitFlag;
            set => exitFlag = value;
        }

        public IPEndPoint Ip { get; private set; }

        public bool IsBuilded { get; set; }

        public bool IsRcving { get; set; }

        public bool IsStopRcved { get; set; }

        public Thread RcvThread { get; private set; }

        public AutoResetEvent StartRcvEvent { get; set; }


        /// <summary>
        ///     数据处理线程
        /// </summary>
        public Dataproc WaveDataproc { get; private set; }

        /// <summary>
        ///     正在传输波形
        /// </summary>
        public ConstUdpArg.WaveType WaveType {
            get => waveType;

            set => waveType = value;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     The start receive data.
        /// </summary>
        /// <param name="ip">
        ///     The local ip.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        ///     succeeded return true
        /// </returns>
        public bool StartReceiveData(IPEndPoint ip)
        {
            Ip = ip;
            try
            {
                waveSocket.Bind(ip);
                IsBuilded = true;
                if (Equals(ip, ConstUdpArg.Src_NormWaveIp))
                {
                    WorkInit();
                    WaveType = ConstUdpArg.WaveType.Normal;
                }
                else if (Equals(ip, ConstUdpArg.Src_OrigWaveIp))
                {
                    OrigInit();
                    WaveType = ConstUdpArg.WaveType.Orig;
                }
                else if (Equals(ip, ConstUdpArg.Src_DelayWaveIp))
                {
                    DelayInit();
                    WaveType = ConstUdpArg.WaveType.Delay;
                }

                WaveDataproc = new Dataproc();
                WaveDataproc.Init(WaveType);
                return true;
            }
            catch (Exception e)
            {
                string str = @"创建UDP失败...错误为{0}"+ e;
                LogHelper.LogInfo(str);
                MessageBox.Show(@"创建UDP失败...");
            }

            return false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (waveSocket != null)
                {
                    try
                    {
                        waveSocket.Shutdown(SocketShutdown.Both);
                        Thread.Sleep(10);
                    }
                    catch (Exception e)
                    {
                    LogHelper.LogInfo(e.Message);
                    throw;
                    }

                    try
                    {

                        waveSocket.Close(100);
                    }
                    catch (Exception e)
                    {
                        LogHelper.LogInfo(e.Message);
                        throw;
                    }

                    try
                    {
                        waveSocket.Dispose();
                    }
                    catch (Exception e)
                    {
                        LogHelper.LogError(e);
                        throw;
                    }
                }

                if (WaveDataproc != null)
                {
                    WaveDataproc.Dispose();
                }

                if (StartRcvEvent != null)
                {
                    StartRcvEvent.Dispose();
                }

                if (RcvThread != null)
                {
                    RcvThread.Abort();
                    RcvThread = null;
                }

                IsBuilded = false;
                IsStopRcved = true;
                linkbuffer.Clear();
                LogHelper.LogInfo(@"关闭UDP线程...");
            }
        }

        /// <summary>
        ///     The delay init.
        /// </summary>
        private void DelayInit()
        {
            waveSocket.ReceiveBufferSize = ConstUdpArg.DELAY_FRAME_NUMS * ConstUdpArg.DELAY_FRAME_LENGTH * 2;
            FrameNums = ConstUdpArg.DELAY_FRAME_NUMS;
            rcvBuf = new byte[ConstUdpArg.DELAY_FRAME_LENGTH];
            RcvThread = new Thread(DelayThreadStart) { IsBackground = true, Priority = ThreadPriority.Highest, Name = "Delay" };
            RcvThread.Start();
        }

        /// <summary>
        ///     The delay thread start.
        /// </summary>
        private void DelayThreadStart()
        {
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            EndPoint senderRemote = remote;
            LogHelper.LogInfo(@"启动UDP线程...");

            while (true)
            {
                StartRcvEvent.WaitOne();
                IsRcving = true;
                if (waveSocket == null)
                {
                    IsRcving = false;
                    break;
                }


                try
                {
                    // 接收数据
                    if (waveSocket.Available!=0)
                    {
                         waveSocket.ReceiveFrom(
                            rcvBuf,
                            0,
                            rcvBuf.Length,
                            SocketFlags.None,
                            ref senderRemote);
                    }

                }
                catch (Exception e)
                {
                    LogHelper.LogError(e);
                    IsRcving = false;
                    break;
                }

                PutDelayData(rcvBuf);
                WaveDataproc.DelayBytesEvent.Set();
                StartRcvEvent.Set();
            }
        }

        /// <summary>
        ///     The normal thread start.
        /// </summary>
        private void NormalThreadStart()
        {
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            EndPoint senderRemote = remote;
            LogHelper.LogInfo(@"启动UDP线程...");

            while (true)
            {
                StartRcvEvent.WaitOne();

                if (IsStopRcved)
                {
                    return;
                }

                if (ExitFlag)
                {
                    ExitFlag = false;
                    return;
                }

                IsRcving = true;
                for (int index = 0; index < FrameNums; index++)
                {
                    if (waveSocket == null)
                    {
                        IsRcving = false;
                        break;
                    }

                    int ret = 0;

                    try
                    {
                        if (waveSocket.Available >= 0)
                        {
                            ret = waveSocket.ReceiveFrom(rcvBuf, ref senderRemote);
                        }

                        if (ret != 0)
                        {
                            var rcvBytes = new byte[256];
                            Array.Copy(rcvBuf, rcvBytes, rcvBytes.Length);
                            recTmpBytes.Add(rcvBytes);
                            saveBytes.AddRange(rcvBytes);
                        }
                    }
                    catch (Exception e)
                    {
                        LogHelper.LogError(e);
                        break;
                    }
                }

                Task.Run(
                    () =>
                    {
                        try
                        {
                            if (recTmpBytes.Count < FrameNums)
                            {
                                return;
                            }

                            if (semaphore.CurrentCount < 1)
                            {
                                semaphore.Release();
                            }
                            try
                            {
                                semaphore.Wait();
                                lock (lockobj)
                                {
                                    recSendBytes.AddRange(recTmpBytes.ToArray());
                                    recTmpBytes.Clear();
                                }

                                Splikbytes = PutWorkData(recSendBytes.ToArray());
                                var savetmp = saveBytes.ToArray();

                                WorkSaveDataEventHandler?.Invoke(null, savetmp);
                                if (Splikbytes != null)
                                {
                                    WorkDataEventHandler?.Invoke(null, Splikbytes);
                                }
                            }
                            catch (Exception e)
                            {
                                LogHelper.LogError(e);
                                throw;
                            }
                            finally
                            {
                                saveBytes.Clear();
                                recSendBytes.Clear();
                                Splikbytes = null;
                            }
                        }
                        catch (Exception e)
                        {
                            LogHelper.LogError(e);
                            throw;
                        }

                    });
                saveBytes.Clear();
                recTmpBytes.Clear();
                WaveDataproc.WorkBytesEvent.Set();
                StartRcvEvent.Set();
            }
        }

        /// <summary>
        ///  The orig init.
        /// </summary>
        private void OrigInit()
        {
            waveSocket.ReceiveBufferSize = (ConstUdpArg.ORIG_FRAME_LENGTH + 2) * ConstUdpArg.ORIG_FRAME_NUMS * 2;
            FrameNums = DisPlayWindow.Info.OrigFramNums;
            rcvBuf = new byte[ConstUdpArg.ORIG_FRAME_LENGTH + 2];
            RcvThread = new Thread(OrigThreadStart) { IsBackground = true, Priority = ThreadPriority.Highest, Name = "Orig" };
            RcvThread.Start();
        }

        /// <summary>
        /// The orig thread start.
        /// </summary>
        private void OrigThreadStart()
        {
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            EndPoint senderRemote = remote;
            LogHelper.LogInfo(@"启动UDP线程...");
            while (true)
            {
                StartRcvEvent.WaitOne();
                IsRcving = true;
                if (waveSocket == null)
                {
                    IsRcving = false;
                    break;
                }

                try
                {
                    if (waveSocket.Available!=0)
                    {
                        // 接收数据
                        int ret = waveSocket.ReceiveFrom(
                            rcvBuf,
                            0,
                            rcvBuf.Length,
                            SocketFlags.None,
                            ref senderRemote);
                    }
                }
                catch (Exception e)
                {
                    LogHelper.LogError(e);
                    IsRcving = false;
                    break;
                }

                PutOrigData(rcvBuf);
                WaveDataproc.OrigBytesEvent.Set();
                StartRcvEvent.Set();
            }
        }

        /// <summary>
        ///     The put delay data.
        /// </summary>
        /// <param name="buf">
        ///     The buf.
        /// </param>
        private void PutDelayData(byte[] buf)
        {
            var temp = new byte[400];
            var head = new byte[2];

            int offset = 0;
            if (!Equals(Ip, ConstUdpArg.Src_DelayWaveIp))
            {
                return;
            }

            Array.Copy(buf, 0, head, 0, head.Length);
            int channel = head[1];
            if (channel > 7 || channel < 0)
            {
                return;
            }

            offset += head.Length;
            Array.Copy(buf, offset, temp, 0, temp.Length);
            if (delayChannelOffsets[channel] >= WaveDataproc.DelayWaveBytes[0].Length)
            {
                delayChannelOffsets[channel] = 0;
            }

            Array.Copy(
                temp,
                0,
                WaveDataproc.DelayWaveBytes[channel],
                delayChannelOffsets[channel],
                temp.Length);
            delayChannelOffsets[channel] += temp.Length;
        }

        /// <summary>
        ///     将原始数据保存到对应通道
        ///     发送数据到保存线程
        /// </summary>
        /// <param name="buf"></param>
        private void PutOrigData(byte[] buf)
        {
            var head = new byte[2];
            int offset = 0;
            if (!Equals(Ip, ConstUdpArg.Src_OrigWaveIp))
            {
                return;
            }

            Array.Copy(buf, 0, head, 0, head.Length);
            int channel = head[0];
            int timdiv = head[1];
            if (channel < 0 && channel > ConstUdpArg.ORIG_CHANNEL_NUMS)
            {
                channel = 1;
            }

            if (timdiv < 0 && timdiv > ConstUdpArg.ORIG_TIME_NUMS)
            {
                timdiv = 1;
            }

            offset += head.Length;

            int len = origChannelOffsets[channel + timdiv * 8]; // 写入数据偏移长度
            if (len >= WaveDataproc.OrigWaveBytes[0].Length)
            {
                // 大于容量数据覆盖，从头再写入
                origChannelOffsets[channel + timdiv * 8] = 0;
                len = 0;
            }

            Array.Copy(
                buf,
                offset,
                WaveDataproc.OrigWaveBytes[channel + timdiv * 8],
                len,
                buf.Length - 2);

            var data = new byte[buf.Length - 2];
            Array.Copy(buf, offset, data, 0, buf.Length - 2);
            origChannelOffsets[channel * 8 + timdiv] += data.Length;
            if (OrigSaveDataEventHandler != null)
            {
                // 发送给保存线程
                OrigSaveDataEventHandler(null, data);
            }
        }

        /// <summary>
        ///     将WorkWave数据导入Detect_Bytes
        /// </summary>
        /// <param name="buf">一帧数据，长度为256，每4位表示1个探头数据</param>
        private byte[][] PutWorkData(byte[][] buf)
        {
            if (buf == null)
            {
                return null;
            }

            if (!Equals(Ip, ConstUdpArg.Src_NormWaveIp))
            {
                return null;
            }

            var resultBytes = new byte[ConstUdpArg.ARRAY_NUM][];
            for (int i = 0; i < resultBytes.Length; i++)
            {
                resultBytes[i] = new byte[DisPlayWindow.Info.WorkFramNums * 4];
            }

            try
            {
                for (int index = 0; index < buf.Length; index++)
                {
                    for (int i = 0; i < buf[index].Length / 4; i++)
                    {
                        Array.Copy(buf[index], i * 4, resultBytes[i], index * 4, 4);
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogError(e);
                throw;
            }

            return resultBytes;
        }

        /// <summary>
        ///     The work init.
        /// </summary>
        private void WorkInit()
        {
            waveSocket.ReceiveBufferSize = ConstUdpArg.WORK_FRAME_LENGTH * DisPlayWindow.Info.WorkFramNums * 2;
            FrameNums = DisPlayWindow.Info.WorkFramNums;
            rcvBuf = new byte[ConstUdpArg.WORK_FRAME_LENGTH * 2];
            RcvThread = new Thread(NormalThreadStart) { IsBackground = true, Priority = ThreadPriority.Highest, Name = "WorkWave" };
            RcvThread.Start();
        }
    }
}