// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   处理与分发网络数据
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ArrayDisplay.Net {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Windows;

    using ArrayDisplay.UI;

    /// <inheritdoc />
    /// <summary>
    ///     处理与分发网络数据
    /// </summary>
    public sealed class Dataproc : IDisposable {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Dataproc" /> class.
        /// </summary>
        public Dataproc() {
            TransFormFft = new FFT_TransForm();
            ArrayNums = ConstUdpArg.ARRAY_NUM; // 探头个数

            FreqWaveEvent = new AutoResetEvent(false);
            WorkEnergyEvent = new AutoResetEvent(false);
            DelayBytesEvent = new AutoResetEvent(false);
            OrigBytesEvent = new AutoResetEvent(false);
            WorkBytesEvent = new AutoResetEvent(false);
        }

        /// <summary>
        ///     Gets or sets the sound event handler.
        /// </summary>
        public static EventHandler<byte[]> SoundEventHandler {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the pre graph event handler.
        /// </summary>
        public static EventHandler<float[]> PreGraphEventHandler {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the bck graph event handler.
        /// </summary>
        public static EventHandler<float[]> BckGraphEventHandler {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the energy array event handler.
        /// </summary>
        public static EventHandler<float[]> EnergyArrayEventHandler {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the orig graph event handler.
        /// </summary>
        public static EventHandler<float[]> OrigGraphEventHandler {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the frap graph event handler.
        /// </summary>
        public static EventHandler<float[]> FrapGraphEventHandler {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the frap point graph event handler.
        /// </summary>
        public static EventHandler<Point[]> FrapPointGraphEventHandler {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the delay graph event handler.
        /// </summary>
        public static EventHandler<float[]> DelayGraphEventHandler {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the energy thread.
        /// </summary>
        public Thread EnergyThread {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the work thread.
        /// </summary>
        public Thread WorkThread {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the delay thread.
        /// </summary>
        public Thread DelayThread {
            get;

            set;
        }

        /// <summary>
        ///     Gets or sets the orig thread.
        /// </summary>
        public Thread OrigThread {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the freq thread.
        /// </summary>
        public Thread FreqThread {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the array nums.
        /// </summary>
        public int ArrayNums {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the work bytes event.
        /// </summary>
        public AutoResetEvent WorkBytesEvent {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the orig bytes event.
        /// </summary>
        public AutoResetEvent OrigBytesEvent {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the delay bytes event.
        /// </summary>
        public AutoResetEvent DelayBytesEvent {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the work energy event.
        /// </summary>
        public AutoResetEvent WorkEnergyEvent {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the freq wave event.
        /// </summary>
        public AutoResetEvent FreqWaveEvent {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the delay wave bytes.
        /// </summary>
        public byte[][] DelayWaveBytes {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the orig wave bytes.
        /// </summary>
        public byte[][] OrigWaveBytes {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the work wave bytes.
        /// </summary>
        public byte[][] WorkWaveBytes {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the work wavefdatas.
        /// </summary>
        float[] WorkWavefdatas {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the freq wave one.
        /// </summary>
        float[] FreqWavefData {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the delay wave floats.
        /// </summary>
        float[][] DelayWaveFloats {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the trans form fft.
        /// </summary>
        FFT_TransForm TransFormFft {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the origdata.
        /// </summary>
        byte[] Origdata {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the work wave floats.
        /// </summary>
        float[][] WorkWaveFloats {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the energy floats.
        /// </summary>
        float[] EnergyFloats {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the orig wave floats.
        ///     原始数据解调数据，横坐标表示探头序列，纵坐标表示数据组成
        /// </summary>
        float[][] OrigWaveFloats {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the play wave bytes.
        /// </summary>
        byte[][] PlayWaveBytes {
            get;
            set;
        }

        /// <summary>
        ///     Gets or sets the listen coefficent.
        /// </summary>
        float ListenCoefficent {
            get;
            set;
        }

        #region IDisposable

        /// <summary>
        ///     The release unmanaged resources.
        /// </summary>
        void ReleaseUnmanagedResources() {
            // TODO release unmanaged resources here
        }

        /// <summary>
        ///     The dispose.
        /// </summary>
        /// <param name="disposing">
        ///     The disposing.
        /// </param>
        void Dispose(bool disposing) {
            ReleaseUnmanagedResources();
            if (disposing) {
                if (OrigThread != null) {
                    OrigThread.Abort();
                    if (OrigThread.ThreadState != ThreadState.Aborted) {
                        Thread.Sleep(100);
                    }
                }

                if (DelayThread != null) {
                    DelayThread.Abort();
                }

                if (WorkThread != null) {
                    WorkThread.Abort();
                    if (WorkThread.ThreadState != ThreadState.Aborted) {
                        Thread.Sleep(100);
                    }
                }

                if (EnergyThread != null) {
                    EnergyThread.Abort();
                }

                if (FreqThread != null) {
                    FreqThread.Abort();
                }

                if (WorkBytesEvent != null) {
                    WorkBytesEvent.Dispose();
                }

                if (OrigBytesEvent != null) {
                    OrigBytesEvent.Dispose();
                }

                if (DelayBytesEvent != null) {
                    DelayBytesEvent.Dispose();
                }

                if (WorkEnergyEvent != null) {
                    WorkEnergyEvent.Dispose();
                }

                if (FreqWaveEvent != null) {
                    FreqWaveEvent.Dispose();
                }
            }
        }

        /// <inheritdoc />
        ~Dataproc() {
            Dispose(false);
        }

        /// <inheritdoc />
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        ///     The init depend on Network type .
        /// </summary>
        /// <param name="type">
        ///     The type is WaveType.
        /// </param>
        public void Init(ConstUdpArg.WaveType type) {
            switch(type) {
                case ConstUdpArg.WaveType.Delay:
                    DelayInit();
                    break;
                case ConstUdpArg.WaveType.Normal:
                    NormalInit();
                    break;
                case ConstUdpArg.WaveType.Orig:
                    OrigInit();
                    break;
                default:
                    Console.WriteLine("初始化Dataproc出错");
                    break;
            }
        }

        /// <summary>
        ///     The origdata init.
        /// </summary>
        void OrigInit() {
            // 时分长度
            OrigWaveBytes = new byte[ConstUdpArg.ORIG_DETECT_LENGTH][];
            OrigWaveFloats = new float[ConstUdpArg.ORIG_DETECT_LENGTH][];

            // 原始数据帧数
            Origdata = new byte[DisPlayWindow.Info.OrigFramNums * ArrayNums];

            // 每帧长度
            for(int i = 0; i < ConstUdpArg.ORIG_DETECT_LENGTH; i++) {
                OrigWaveBytes[i] = new byte[(ConstUdpArg.ORIG_FRAME_LENGTH - 2) * DisPlayWindow.Info.OrigFramNums]; // 2位数据位
                OrigWaveFloats[i] = new float[(ConstUdpArg.ORIG_FRAME_LENGTH - 2) / 2 * DisPlayWindow.Info.OrigFramNums];
            }

            OrigThread = new Thread(ThreadOrigWaveStart) { IsBackground = true };
            OrigThread.Start();
        }

        /// <summary>
        ///     The normalData init.
        /// </summary>
        void NormalInit() {
            TransFormFft = new FFT_TransForm();
            WorkWaveBytes = new byte[ArrayNums][];
            WorkWaveFloats = new float[ArrayNums][];
            WorkWaveBytes = new byte[ArrayNums][];
            PlayWaveBytes = new byte[ArrayNums][];

            int frameNum = ConstUdpArg.WORK_FRAME_NUMS * 4; // 工作数据帧数

            // int frameNum = 2000 * ConstUdpArg.WORK_FRAME_LENGTH; //工作数据帧数
            for(int i = 0; i < ArrayNums; i++) {
                WorkWaveBytes[i] = new byte[frameNum * 4]; // 避免数据为null
                WorkWaveFloats[i] = new float[frameNum]; // 避免数据为null 
                PlayWaveBytes[i] = new byte[frameNum * 2]; // 避免数据为null 
            }

            WorkWavefdatas = new float[frameNum]; // 工作波形
            EnergyFloats = new float[ArrayNums];
            ListenCoefficent = (float)Math.Pow(10, 50 / 20.0F) * 100; // 31622.78; //听音强度

            WorkThread = new Thread(ThreadWorkWaveStart) { IsBackground = true };
            WorkThread.Start();
            EnergyThread = new Thread(ThreadEnergyStart) { IsBackground = true };
            EnergyThread.Start();
            FreqThread = new Thread(ThreadFreqWaveStart) { IsBackground = true };
            FreqThread.Start();
        }

        /// <summary>
        ///     The delaydata init.
        /// </summary>
        void DelayInit() {
            DelayWaveBytes = new byte[ConstUdpArg.DELAY_FRAME_CHANNELS][];
            DelayWaveFloats = new float[ConstUdpArg.DELAY_FRAME_CHANNELS][];
            for(int i = 0; i < ConstUdpArg.DELAY_FRAME_CHANNELS; i++) {
                DelayWaveBytes[i] = new byte[(ConstUdpArg.DELAY_FRAME_LENGTH - 2) * ConstUdpArg.DELAY_FRAME_NUMS];
                DelayWaveFloats[i] = new float[(ConstUdpArg.DELAY_FRAME_LENGTH - 2) * ConstUdpArg.DELAY_FRAME_NUMS / 2];
            }

            DelayThread = new Thread(ThreadDelayWaveStart) { IsBackground = true };
            DelayThread.Start();
        }

        /// <summary>
        ///     The thread delay wave start.
        /// </summary>
        void ThreadDelayWaveStart() {
            while(true) {
                DelayBytesEvent.WaitOne();
                var r = new byte[2];

                for(int i = 0; i < DelayWaveBytes.Length; i++) {
                    for(int j = 0; j < DelayWaveBytes[0].Length / 2; j++) {
                        r[0] = DelayWaveBytes[i][(j * 2) + 1];
                        r[1] = DelayWaveBytes[i][(j * 2) + 0];
                        short a = BitConverter.ToInt16(r, 0);
                        DelayWaveFloats[i][j] = a / 8192.0f;
                    }
                }

                int channel = DisPlayWindow.Info.DelayChannel - 1;
                DelayGraphEventHandler(null, DelayWaveFloats[channel]);
            }
        }

        /// <summary>
        ///     The thread orig wave start.
        /// </summary>
        void ThreadOrigWaveStart() {
            while(true) {
                OrigBytesEvent.WaitOne();
                var r = new byte[2];
                for(int i = 0; i < ConstUdpArg.ORIG_DETECT_LENGTH; i++) {
                    for(int j = 0; j < (OrigWaveBytes[0].Length / 2) - 1; j++) {
                        r[0] = OrigWaveBytes[i][(j * 2) + 1];
                        r[1] = OrigWaveBytes[i][j * 2];
                        short a = BitConverter.ToInt16(r, 0);

                        OrigWaveFloats[i][j] = a / 8192.0f;
                    }
                }

                if (OrigGraphEventHandler != null) {
                    string sender = "OrigNet";
                    int index = DisPlayWindow.Info.OrigChannel - 1 + ((DisPlayWindow.Info.OrigTdiv - 1) * 8);
                    OrigGraphEventHandler.Invoke(sender, OrigWaveFloats[index]);
                }
            }
        }

        /// <summary>
        ///     线程处理函数：能量数据处理
        /// </summary>
        void ThreadEnergyStart() {
            while(true) {
                WorkEnergyEvent.WaitOne();
                double ftemp = 0;
                for(int i = 0; i < WorkWaveFloats.Length; i++) {
                    float f = WorkWaveFloats[i].Max();
                    ftemp = Math.Abs(f);
                    EnergyFloats[i] = (float)ftemp;
                }

                float max = EnergyFloats.Max();
                for(int i = 0; i < EnergyFloats.Length; i++) {
                    if (Math.Abs(max) > float.Epsilon) {
                        EnergyFloats[i] = EnergyFloats[i] / max;
                    }
                    else {
                        EnergyFloats[i] = 0;
                    }
                }

                var rlist = new List<float>();
                for(int i = 0; i < EnergyFloats.Length; i++) {
                    rlist.Add(EnergyFloats[i]);
                }

                if (EnergyArrayEventHandler != null) {
                    EnergyArrayEventHandler.Invoke(null, rlist.ToArray());
                }
            }
        }

        /// <summary>
        ///     The thread work wave start.
        /// </summary>
        void ThreadWorkWaveStart() {
            while(true) {
                WorkBytesEvent.WaitOne();
                var r = new byte[4];
                for(int i = 0; i < ConstUdpArg.ARRAY_NUM; i++) {
                    for(int j = 0; j < ConstUdpArg.WORK_FRAME_NUMS; j++) {
                        r[0] = WorkWaveBytes[i][(j * 4) + 3];
                        r[1] = WorkWaveBytes[i][(j * 4) + 2];
                        r[2] = WorkWaveBytes[i][(j * 4) + 1];
                        r[3] = WorkWaveBytes[i][j * 4];
                        int a = BitConverter.ToInt32(r, 0);
                        float tmp = a / 1048576.0f;

                        // float tmp = a / 2.0f;
                        WorkWaveFloats[i][j] = tmp;

                        // 听音数据处理
                        float f = WorkWaveFloats[i][j] * ListenCoefficent;
                        short sh;
                        if (f > 32767) {
                            sh = 32767;
                        }
                        else if (f <= -32767) {
                            sh = -32767;
                        }
                        else {
                            sh = (short)f;
                        }

                        var x = BitConverter.GetBytes(sh);
                        Array.Copy(x, 0, PlayWaveBytes[i], j * 2, 2);
                    }
                }

                int offset = ConstUdpArg.offsetArrayTwo[DisPlayWindow.Info.WorkChannel - 1];
                WorkWavefdatas = WorkWaveFloats[offset - 1];
                WorkEnergyEvent.Set();
                FreqWaveEvent.Set();

                PreGraphEventHandler.Invoke(null, WorkWavefdatas);

                // if (BckGraphEventHandler != null) BckGraphEventHandler.Invoke(null, WorkWaveTwo);
                if (SoundEventHandler != null) {
                    int channel = DisPlayWindow.Info.WorkChannel;
                    if (channel > 0) {
                        SoundEventHandler.Invoke(null, PlayWaveBytes[channel]);
                    }
                }
            }
        }

        /// <summary>
        ///     The thread freq wave start.
        /// </summary>
        void ThreadFreqWaveStart() {
            while(true) {
                FreqWaveEvent.WaitOne();
                FreqWavefData = TransFormFft.FFT(WorkWavefdatas);
                var dataPoints = NewFFT.Start(WorkWavefdatas, 1024 * 16); // 用前1024 * 16个点

                FrapPointGraphEventHandler.Invoke(null, dataPoints);
            }
        }
    }
}
