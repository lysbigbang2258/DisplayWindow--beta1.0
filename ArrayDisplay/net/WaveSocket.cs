// 2019010710:15 AM

namespace ArrayDisplay.Net {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    using UI;

    /// <summary>
    /// 计算需连续发送命令的变量
    /// </summary>
    public class WaveSocket : IDisposable {
        /// <summary>
        /// 主Socket
        /// </summary>
        public Socket MSocket {
            get;
            set;
        }

        /// <summary>
        /// 接收线程
        /// </summary>
        public Thread RcvThread {
            get;
            set;
        }

        /// <summary>
        /// B值数组，通道*多帧数据
        /// </summary>
        public short[][] BvalueDatas {
            get;
            set;
        }

        /// <summary>
        /// 初始相位数组，通道*多帧数据
        /// </summary>
        public short[][] RcvPhaseData {
            get;
            set;
        }

        /// <summary>
        /// 计算得到的各通道初始相位
        /// </summary>
        public float[] PhaseFloats {
            get;
            set;
        }

        /// <summary>
        /// 接收数据通知开关
        /// </summary>
        public AutoResetEvent RcvResetEvent {
            get;
            set;
        }

        /// <summary>
        /// 发送指令变量，
        /// </summary>
        public UdpCommandSocket UCommandSocket {
            get;
            set;
        }

        /// <summary>
        /// 初始化一个Udp的Socket
        /// </summary>
        public WaveSocket() {
            MSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
                                    ProtocolType.Udp);
        }

        /// <summary>
        /// 开启计算B值流程
        /// </summary>
        /// <param name="ip">绑定本地节点</param>
        /// <param name="udpCommandSocket">接收发送命令socket</param>
        public void StartCaclBvalue(IPEndPoint ip, UdpCommandSocket udpCommandSocket) {
            try {
                UCommandSocket = udpCommandSocket;
                MSocket.ReceiveBufferSize = ConstUdpArg.ORIG_FRAME_LENGTH + 2;
                MSocket.Bind(ip);
                RcvThread = new Thread(RcvUdpdataToBvalue_ThreatStart) {IsBackground = true};
                RcvResetEvent = new AutoResetEvent(false);
                BvalueDatas = new short[64][];
                RcvThread.Start();
            }
            catch(Exception e) {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// 开启计算初始相位流程
        /// </summary>
        /// <param name="ip">绑定本地节点</param>
        /// <param name="udpCommandSocket">接收发送命令socket</param>
        public void StartCaclPhase(IPEndPoint ip, UdpCommandSocket udpCommandSocket)
        {
            try {
                UCommandSocket = udpCommandSocket;
                MSocket.ReceiveBufferSize = ConstUdpArg.ORIG_FRAME_LENGTH + 2;
                MSocket.Bind(ip);
                RcvThread = new Thread(RcvUdpdataToPhase_ThreatStart) { IsBackground = true };
                RcvResetEvent = new AutoResetEvent(false);
                RcvPhaseData = new short[8][];
                PhaseFloats = new float[8];
                RcvThread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// 接收网络流相位信号线程函数
        /// </summary>
        void RcvUdpdataToPhase_ThreatStart()
        {
            List<ArraySegment<byte>> buffer = new List<ArraySegment<byte>> {
                new ArraySegment<byte>(new byte[1282])
            };
            int ret;
            Stopwatch stopwatch = new Stopwatch();
            while (true)
            {
                RcvResetEvent.WaitOne();
                try
                {
                    stopwatch.Start();
                    ret = MSocket.Receive(buffer);
                    if (ret <= 0)
                    {
                        continue;
                    }

                    int offset = 0;
                    var bytedata = buffer[0].Array;
                    if (bytedata == null)
                    {
                        continue;
                    }

                    int chl = bytedata[0]; // 通道号
                    int tiv = bytedata[1]; // 时分

                    int index = tiv * 8 + chl; // 1时分表示8通道
                    offset += 2;
                    short[] sbdata = new short[(bytedata.Length - offset) / 2]; // 2byte转short数据
                    var purdata = new byte[bytedata.Length - offset];// 接收byte数组
                    Array.Copy(bytedata, offset, purdata, 0, purdata.Length);
                    for (int i = 0; i < sbdata.Length; i++) {
                        // 大端数据
                        var t = new byte[2];          // 大端转小端
                        t[0] = purdata[2 * i + 1];
                        t[1] = purdata[2 * i];
                        sbdata[i] = BitConverter.ToInt16(t, 0);
                    }

                    if (index<0 || index>31) {
                        Console.WriteLine("Index Error！");
                        return;
                    }

                    RcvPhaseData[index] = new short[sbdata.Length];
                    Console.WriteLine("通道： " + index);
                    Array.Copy(sbdata, RcvPhaseData[index], sbdata.Length);    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                RcvResetEvent.Reset(); // 线程继续等待
            }

        }

        /// <summary>
        /// 采集信号保存到Bvalue[]数组
        /// </summary>
        void RcvUdpdataToBvalue_ThreatStart() {
            List<ArraySegment<byte>> buffer = new List<ArraySegment<byte>> {
                new ArraySegment<byte>(new byte[1282]),
            };
            int ret;
            Stopwatch stopwatch = new Stopwatch();
            while(true) {
                RcvResetEvent.WaitOne();
                try {
                    stopwatch.Start();
                    ret = MSocket.Receive(buffer);
                    if (ret <= 0)
                    {
                        continue;
                    }

                    int offset = 0;
                    var bytedata = buffer[0].Array;
                    if (bytedata == null) {
                        continue;
                    }

                    int chl = bytedata[0];    // 通道
                    int tiv = bytedata[1];    // 时分                
                    int index = tiv * 8 + chl;  // 1时分8通道
                    offset += 2;
                    short[] sbdata = new short[(bytedata.Length - offset) / 2];
                    var purdata = new byte[bytedata.Length - offset];
                    
                    Array.Copy(bytedata, offset, purdata, 0, purdata.Length);
                    for (int i = 0; i < sbdata.Length; i++) {
                        // 大端数据转小端
                        var t = new byte[2];
                        t[0] = purdata[2 * i + 1];
                        t[1] = purdata[2 * i];
                        sbdata[i] = BitConverter.ToInt16(t, 0);
                    }

                    BvalueDatas[index] = new short[sbdata.Length];
                    double progressvalue = (index + 1) * 100.0 / 64; // 进度条
                    Console.WriteLine("通道： " + index);
                    Array.Copy(sbdata, BvalueDatas[index], sbdata.Length);
                    DisPlayWindow.HMainWindow.bvaulue_pgbar.Dispatcher.Invoke(() =>
                                                                              {
                                                                                  DisPlayWindow.HMainWindow.bvaulue_pgbar.Value = progressvalue;
                                                                              });// 进度条
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

                stopwatch.Stop();
                Console.WriteLine("RCVTime:"+stopwatch.ElapsedMilliseconds);

                RcvResetEvent.Reset();
            }
            
        }

        /// <summary>
        /// 循环发送命令
        /// </summary>
        /// <param name="chinum">通道个数</param>
        /// <param name="timenum">时分个数</param>
        public void SendOrigSwitchCommand(int chinum, int timenum) {
            UCommandSocket.SwitchWindow(ConstUdpArg.SwicthToOriginalWindow);

            // UCommand.WriteOrigTDiv(0);
            // Console.WriteLine("发送时分0");
            // UCommand.WriteOrigChannel(0);
            // Console.WriteLine("发送通道0");
            for(int i = 0; i < timenum; i++) {
                for(int j = 0; j < chinum; j++) {
                    UCommandSocket.WriteOrigTDiv(i);
                    Console.WriteLine("发送时分{0}", i);
                    UCommandSocket.WriteOrigChannel(j);
                    Console.WriteLine("发送通道{0}", j);
                    Thread.Sleep(15);
                    RcvResetEvent.Set();
                    Thread.Sleep(20);
                }
            }

            Thread.Sleep(100);
            UCommandSocket.SwitchWindow(ConstUdpArg.SwicthToOriginalWindow);
            UCommandSocket.WriteOrigChannel(0);
            UCommandSocket.WriteOrigTDiv(0);
            UCommandSocket.SwitchWindow(ConstUdpArg.SwicthToStateWindow);
        }

        /// <summary>
        /// 计算初始相位
        /// </summary>
        /// <returns>各个通道相位值</returns>
        public float[] CalToPhase() {
           return CalToPhase(RcvPhaseData);
        }

        /// <summary>
        /// 计算初始相位
        /// </summary>
        /// <param name="rcvPahseArray">采集原始数据值</param>
        /// <returns>初始相位float型</returns>
        float[] CalToPhase(short[][] rcvPahseArray) {
            int periodPoints = 16; // 一个周期点数
            int periodNum = rcvPahseArray[0].Length/ periodPoints; // 周期
            var calphase = new short[periodNum][];
            var calcos = new float[periodNum][];
            var calsin = new float[periodNum][];
            var meancos = new float[periodNum];
            var meansin = new float[periodNum];
            var caltin = new float[8];

            for (int i = 0; i < calphase.Length; i++)
            {
                calphase[i] = new short[periodPoints];
                calcos[i] = new float[periodPoints];
                calsin[i] = new float[periodPoints];

            }

            for (int index = 0; index < rcvPahseArray.Length; index++) {
                var array = rcvPahseArray[index];
                for (int i = 0; i < array.Length; i++) {
                    // 16个采样点表示1周期，
                    var frams = i / periodPoints;
                    var num = i % periodPoints;
                    calphase[frams][num] = array[i];
                }

                for (int i = 0; i < calphase.Length; i++)
                {
                    for (int j = 0; j < periodPoints; j++)
                    {
                        calcos[i][j] =(float)(calphase[i][j] * Math.Cos(Math.PI / 8 * j));
                        calsin[i][j] =(float)(calphase[i][j] * Math.Sin(Math.PI / 8 * j)); 
                    }
                   
                }

                for(int i = 0; i < calphase.Length; i++) {
                    meansin[i] = calsin[i].Average();
                    meancos[i] = calcos[i].Average();
                }

                var msin = meansin.Average();
                var mcos = meancos.Average();
                var art = (float)Math.Atan(msin / mcos) * -1; 
                caltin[index] =art;
            }

            PhaseFloats = caltin;
            return caltin;

        }

       /// <summary>
       /// 将计算得到的初始相位混频
       /// </summary>
       /// <param name="caltin"></param>
       /// <returns></returns>
       public byte[][] GetSendPhases(float[] caltin) {
            List<float> list1; 
           List<List<float>> oneLists = new List<List<float>>();
            List<float[]> doublelist = new List<float[]>();
            for(int index = 0; index < caltin.Length; index++) {
                list1 = new List<float>();
                for(int i = 0; i < 16; i++) {
                    var tmp1 = Math.Cos(Math.PI / 8 * i + caltin[index]); // 一倍频混
                    var tmp2 = Math.Cos(Math.PI / 4 * i + 2*caltin[index]);// 二倍频混
                    list1.Add((float)tmp1);
                    list1.Add((float)tmp2);
                }
                oneLists.Add(list1);
            }

            for (int i = 0; i < oneLists.Count; i++)
            {
                var tmplist = new List<float>();
                    tmplist.AddRange(oneLists[i]);
                doublelist.Add(tmplist.ToArray());
            }
            
            var shortlist = doublelist.ConvertAll(FloatsToshorts); 
            var bytelist = shortlist.ConvertAll(ShortTobytes);
            var sendbytes = bytelist.ToArray();
            return sendbytes;
        }
        
        static short[] FloatsToshorts(float[] input)
        {
            List<short> list = new List<short>();
            for (int i = 0; i < input.Length; i++)
            {
                short t = (short)(input[i] * Math.Pow(2, 12));
                list.Add(t);
            }

            return list.ToArray();
        }

        static byte[] ShortTobytes(short[] input)
        {
            List<byte> list = new List<byte>();
            var tmp =new byte[2];
            for (int i = input.Length-1; i >= 0; i--) {
                // 行列转换，这样数据才对
                var t = BitConverter.GetBytes(input[i]);  // 大小端转换
                tmp[0] = t[1];
                tmp[1] = t[0];
                list.AddRange(tmp);
            }

            return list.ToArray();
        }

        /// <summary>
        /// 通过转换得到待发送的B值数据包
        /// </summary>
        /// <param name="bfloats">各通道B值</param>
        /// <returns>一包bytes数据</returns>
        public byte[] GetSendBvalues(float[] bfloats)
        {
            List<List<byte>> blist = new List<List<byte>>();
            for (int i = 0; i < 8; i++)
            {
                blist.Add(new List<byte>());
            }

            for (int i = 0; i < bfloats.Length; i++)
            {
                short x;
                if (bfloats[i] < 0.271) {
                    // 实际计算值最小值
                    x = 32767;   // 最小值
                }
                else if (bfloats[i] > 17712) {
                    // 实际计算值最大值
                    x = 0;
                }
                else
                {
                    x = (short)(8856 / bfloats[i]); // bfloats为1时，x为8856
                }

                var tmp = BitConverter.GetBytes(x);

                blist[i % 8].AddRange(tmp);
            }

            var package = new List<byte>();
            short t = 32767; // 最小值
            for (int i = 0; i < 24; i++) {
                // 补充剩余的24个通道
                var tmp = BitConverter.GetBytes(t);
                package.AddRange(tmp);
            }

            for (int i = 0; i < 8; i++) {
                // 8个时分
                blist[i].AddRange(package);
            }

            List<byte> zList = new List<byte>();
            for (int i = 0; i < blist.Count; i++)
            {
                zList.AddRange(blist[i].ToArray());
            }

            return zList.ToArray();
        }

        #region IDisposable

        void ReleaseUnmanagedResources() {
            // TODO release unmanaged resources here
        }

        void Dispose(bool disposing) {
            ReleaseUnmanagedResources();
            if (disposing) {
                if (RcvResetEvent != null) RcvResetEvent.Dispose();
                if (MSocket != null) {
                    MSocket.Shutdown(SocketShutdown.Both);
                    MSocket.Close();
                    MSocket.Dispose();
                }
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        ~WaveSocket() {
            Dispose(false);
        }

        #endregion
    }

    
}
