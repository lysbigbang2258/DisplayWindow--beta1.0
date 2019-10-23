// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DisPlayWindow.xaml.cs" company="">
//   
// </copyright>
// <summary>
//   DisPlayWindow.xaml 的交互逻辑
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using ArrayDisplay.DataUtl;
using ArrayDisplay.DiscFile;
using ArrayDisplay.NetUtl;
using ArrayDisplay.sound;
using NationalInstruments.Restricted;
using Binding = System.Windows.Data.Binding;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace ArrayDisplay.UI
{
    /// <summary>
    ///     DisPlayWindow.xaml 的交互逻辑
    /// </summary>
    public sealed partial class DisPlayWindow : IDisposable
    {
        #region Field

        public static DisPlayWindow HMainWindow; // 当前窗口句柄

        public static int sndCoefficent = 50;

        private static DxPlaySound dxplaysnd; // 播放声音对象

        // Udp_Data _capudp;
        // string _ip;
        // int _port;
        private DataUtl.DiscFile discFile;

        private Dataproc dataproc;

        private ObservableCollection<UIBValue> observableCollection;

        private int origChannel;

        private Stopwatch stopwatch;

        private UdpCommandSocket udpCommandSocket;

        #endregion

        #region Property

        public static SystemInfo Info { get; set; }

        public bool IsorigSaveFlag { get; private set; }

        public bool IsworkSaveFlag { get; private set; }

        private int DelayChannel { get; }

        private int OrigChannel { get; set; }

        public int OrigTiv { get; set; }

        public float[] Bvalues { get; set; }

        public float[] Phases { get; set; }

        public bool IsGetBvalue { get; set; }

        public UdpWaveData NormWaveData { get; set; }

        public UdpWaveData OrigWaveData { get; set; }

        public UdpWaveData DelayWaveData { get; set; }

        public bool IsGraphPause { get; set; }

        public bool IsWorkWaveStart { get; set; }

        public Stopwatch MainStopwatch { get; set; }

        // 标识原始数据回放
        public bool IsOrigReplay { get; set; }

        // 标识正常数据回放
        public bool IsWorkReplay { get; set; }

        public CancellationTokenSource Cancellation { get; set; }

        #endregion

        // 界面初始化

        #region 听音功能

        /// <summary>
        ///     听音相关
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSound_OnClick(object sender, RoutedEventArgs e)
        {
            if (dxplaysnd == null)
            {
                dxplaysnd = new DxPlaySound(31250);
                var buffer = new byte[31250 * 2];
                dxplaysnd.WriteOneTimData(buf: buffer);
                dxplaysnd.WriteOneTimData(buf: buffer);
                dxplaysnd.WriteOneTimData(buf: buffer);
                Dataproc.SoundEventHandler += OnSndData;

                // btnlisten.Content = "停止听音";
            }
            else
            {
                if (Dataproc.SoundEventHandler != null)
                {
                    Dataproc.SoundEventHandler -= OnSndData;
                }

                dxplaysnd.Close();
                dxplaysnd = null;

                // btnlisten.Content = "开始听音";
            }
        }

        // 传输声音数据用于播放
        private void OnSndData(object sender, byte[] data)
        {
            if (dxplaysnd != null)
            {
                dxplaysnd.WriteOneTimData(buf: data);
            }
        }

        private void SoundValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
        }

        #endregion

        #region  基础功能

        public DisPlayWindow()
        {
            InitializeComponent();
            OrigChannel = 0;
            DelayChannel = 0;
            OrigTiv = 0;
            IsGraphPause = false;
            IsWorkWaveStart = false;
            MainStopwatch = new Stopwatch();
            IsOrigReplay = false;
            IsWorkReplay = false;
            Cancellation = new CancellationTokenSource();
        }

        #region IDisposable

        /// <inheritdoc />
        public void Dispose()
        {
            if (discFile != null)
            {
                discFile.Dispose();
            }

            if (udpCommandSocket != null)
            {
                udpCommandSocket.Dispose();
            }

            if (dataproc != null)
            {
                dataproc.Dispose();
            }

            if (NormWaveData != null)
            {
                NormWaveData.Dispose();
            }

            if (OrigWaveData != null)
            {
                OrigWaveData.Dispose();
            }

            if (DelayWaveData != null)
            {
                DelayWaveData.Dispose();
            }
        }

        #endregion

        /// <summary>
        ///     界面载入事件响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisPlayWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            Dataproc.PreGraphEventHandler += OnWorkGraph; // Work时域波形事件处理方法连接到事件
            Dataproc.PreGraphEventHandler += OnMaxWorkValue; // Work时域波形最大值
            Dataproc.OrigGraphEventHandler += OnOrigGraph; // Orig波形事件处理方法连接到事件
            Dataproc.DelayGraphEventHandler += OnDelayWaveGraph; // Delay波形事件处理方法连接到事件

            // Dataproc.FrapGraphEventHandler += OnFrapGraph; //Work频域波形事件处理方法
            Dataproc.EnergyArrayEventHandler += OnEnergyArrayGraph; // 能量图事件处理方法
            Dataproc.FrapPointGraphEventHandler += OnFrapPointGraph; // 使用新FFT频域事件处理
            Dataproc.FrapPointGraphEventHandler += OnMaxFrapPoint; // 频率最大值
            HMainWindow = this;

            Info = new SystemInfo();

            udpCommandSocket = new UdpCommandSocket();

            if (!udpCommandSocket.Init())
            {
                return;
            }

            if (!udpCommandSocket.TestConnect())
            {
                return;
            }

            LoadDeviceState();

            observableCollection = new ObservableCollection<UIBValue>();
            blistview.ItemsSource = observableCollection;

            ControlsSetBinding();
        }

        /// <summary>
        ///     The load device state.
        /// </summary>
        private void LoadDeviceState()
        {
            Task.Run(() =>
            {
                udpCommandSocket.SwitchWindow(cmdBytes: ConstUdpArg.SwicthToStateWindow);
                try
                {
                    udpCommandSocket.GetDeviceState();
                }
                catch (Exception exception)
                {
                    Console.WriteLine(value: exception);
                    throw;
                }
            });
        }

        /// <summary>
        ///     给控件添加绑定
        /// </summary>
        private void ControlsSetBinding()
        {
            // 系统信息
            tb_state_mc_type.SetBinding(dp: TextBox.TextProperty, binding: new Binding("McType") {Source = Info});
            tb_state_mc_id.SetBinding(dp: TextBox.TextProperty, binding: new Binding("McId") {Source = Info});
            tb_state_mc_mac.SetBinding(dp: TextBox.TextProperty, binding: new Binding("McMac") {Source = Info});

            // 系统参数
            tb_setting_pulse_period.SetBinding(dp: TextBox.TextProperty, binding: new Binding("PulsePeriod") {Source = Info});
            tb_setting_pulse_delay.SetBinding(dp: TextBox.TextProperty, binding: new Binding("PulseDelay") {Source = Info});
            tb_setting_pulse_width.SetBinding(dp: TextBox.TextProperty, binding: new Binding("PulseWidth") {Source = Info});
            tb_setting_adc_offset.SetBinding(dp: TextBox.TextProperty, binding: new Binding("AdcOffset") {Source = Info});
            tb_setting_adc_num.SetBinding(dp: TextBox.TextProperty, binding: new Binding("AdcNum") {Source = Info});

            // 延时波形
            tb_deleyTime.SetBinding(dp: TextBox.TextProperty, binding: new Binding("DelayTime") {Source = Info});
            tb_deleyChannel.SetBinding(dp: TextBox.TextProperty, binding: new Binding("DelayChannel") {Source = Info});

            // 原始数据
            tb_origFram.SetBinding(dp: TextBox.TextProperty, binding: new Binding("OrigFramNums") {Source = Info});
            tb_dacLen.SetBinding(dp: TextBox.TextProperty, binding: new Binding("DacLenth") {Source = Info});
            tb_dacChannel.SetBinding(dp: TextBox.TextProperty, binding: new Binding("DacChannel") {Source = Info});
            tb_origChannel.SetBinding(dp: TextBox.TextProperty, binding: new Binding("OrigChannel") {Source = Info});
            tb_origTdiv.SetBinding(dp: TextBox.TextProperty, binding: new Binding("OrigTdiv") {Source = Info, Mode = BindingMode.TwoWay});

            // 正常工作
            tb_workChannel.SetBinding(dp: TextBox.TextProperty, binding: new Binding("WorkChannel") {Source = Info, Mode = BindingMode.TwoWay});
            tb_workSaveTime.SetBinding(dp: TextBox.TextProperty, binding: new Binding("WorkSaveTime") {Source = Info, Mode = BindingMode.TwoWay});
            tb_workFram.SetBinding(dp: TextBox.TextProperty, binding: new Binding("WorkFramNums") {Source = Info, Mode = BindingMode.TwoWay});
        }

        /// <summary>
        ///     The init graph state.
        /// </summary>
        private void InitGraphState()
        {
            IsGraphPause = false;

            if (OrigWaveData != null)
            {
                udpCommandSocket.SwitchWindow(ConstUdpArg.SwicthToStateWindow);
                OrigWaveData.ExitFlag = true;
                OrigWaveData.Dispose();
                OrigWaveData = null;
                tabControl.Dispatcher.InvokeAsync(() =>
                {
                    orige_graph.DataSource = 0;
                    orige_graph.Refresh();
                    btn_origstart.Content = "启动";
                });
            }
            if (NormWaveData != null)
            {
                udpCommandSocket.SwitchWindow(ConstUdpArg.SwicthToStateWindow);
                NormWaveData.ExitFlag = true;
                NormWaveData.Dispose();
                NormWaveData = null;

                tabControl.Dispatcher.InvokeAsync(() =>
                {
                    graph_normalFrequency.DataSource = 0;
                    graph_normalFrequency.Refresh();
                    graph_energyFirst.DataSource = 0;
                    graph_energyFirst.Refresh();
                    graph_normalTime.DataSource = 0;
                    graph_normalTime.Refresh();
                    btn_workstart.Content = "启动";
                });
            }
            if (DelayWaveData != null)
            {
                udpCommandSocket.SwitchWindow(ConstUdpArg.SwicthToStateWindow);
                DelayWaveData.ExitFlag = true;
                DelayWaveData.Dispose();
                DelayWaveData = null;
                tabControl.Dispatcher.InvokeAsync(() =>
                {
                    delay_graph.DataSource = 0;
                    delay_graph.Refresh();
                    btn_delaystart.Content = "启动";
                });
               
            }

        }

        /// <summary>
        ///     The tab control_ on selection changed.
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
        private void TabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChangedEventArgs slcArg = e;
            if (slcArg.RemovedItems.IsEmpty())
            {
                return;
            }

            HeaderedContentControl arg = slcArg.RemovedItems[0] as HeaderedContentControl;
            if (arg == null)
            {
                return;
            }

            IsGraphPause = false;
            switch (arg.Name)
            {
                case "delayItem":
                {
                    Task.Run(() =>
                    {
                        udpCommandSocket.SwitchWindow(cmdBytes: ConstUdpArg.SwicthToDeleyWindow);
                        InitGraphState();
                        Console.WriteLine("关闭延时波形");
                    });
                }

                    break;
                case "origItem":
                {
                    Task.Run(() =>
                    {
                        udpCommandSocket.SwitchWindow(cmdBytes: ConstUdpArg.SwicthToOriginalWindow);
                        InitGraphState();
                        Console.WriteLine("关闭原始波形");
                    });
                }

                    break;
                case "normalItem":
                {
                    Task.Run(() =>
                    {
                        udpCommandSocket.SwitchWindow(cmdBytes: ConstUdpArg.SwicthToNormalWindow);
                        InitGraphState();
                        Console.WriteLine("关闭正常工作波形");
                    });
                }

                    break;
                case "configItem":
                    break;
                case "settingItem":
                    break;
                default:
                {
                    Console.WriteLine("切换窗口错误");
                }

                    break;
            }
        }

        /// <summary>
        ///     按钮事件响应：调试窗口最小化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBtnWindowMinClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        ///     按钮事件响应：调试窗口关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBtnWindowShutDownClick(object sender, RoutedEventArgs e)
        {
            udpCommandSocket.SwitchWindow(cmdBytes: ConstUdpArg.SwicthToStateWindow);

            // Dispose();
            Close();
        }

        #endregion

        #region 延时数据功能

        /// <summary>
        ///     The delay data start_ on click.
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
        private void DelayDataStart_OnClick(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                // try
                // {
                udpCommandSocket.SwitchWindow(cmdBytes: ConstUdpArg.SwicthToDeleyWindow);
                if (DelayWaveData == null)
                {
                    DelayWaveData = new UdpWaveData();
                    if (!DelayWaveData.StartReceiveData(ip: ConstUdpArg.Src_DelayWaveIp))
                    {
                        return;
                    }

                    btn_delaystart.Dispatcher.InvokeAsync(() => { btn_delaystart.Content = "停止"; });
                    DelayWaveData.StartRcvEvent.Set();
                }
                else if (DelayWaveData != null || DelayWaveData.IsBuilded)
                {
                    try
                    {
                        InitGraphState();
                        btn_delaystart.Dispatcher.InvokeAsync(() => { btn_delaystart.Content = "启动"; });
                        delay_graph.Dispatcher.InvokeAsync(() =>
                        {
                            delay_graph.DataSource = 0;
                            delay_graph.Refresh();
                        });
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(value: exception);
                        throw;
                    }
                }
                else if (DelayWaveData.IsBuilded && !DelayWaveData.IsRcving)
                {
                    DelayWaveData.StartRcvEvent.Set();
                    btn_delaystart.Dispatcher.InvokeAsync(() => { btn_delaystart.Content = "停止"; });
                }
                // catch (Exception exception)
                // {
                //     Console.WriteLine(@"错误代码{0}", exception);
                //     Console.WriteLine(@"网络地址错误...");
                //     MessageBox.Show(@"网络地址错误...");
                // }
            });
        }

        /// <summary>
        ///     Tb_deleyChannel的按键更改响应
        ///     回车：数值发送
        ///     上下方向键：更改数值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tb_deleyChannel_OnKeyUp(object sender, KeyEventArgs e)
        {
            TextBox tb = sender as TextBox;
            int num = 1;
            if (!int.TryParse(s: tb.Text, result: out num))
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                {
                    // 回车更改
                    try
                    {
                        if (num < 1 || num > ConstUdpArg.DELAY_FRAME_CHANNELS)
                        {
                            num = 1;
                            tb.Text = "1";
                        }

                        Info.DelayChannel = num;
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(value: exception);
                        throw;
                    }

                    break;
                }

                case Key.Up:
                {
                    // 上方向键增加数值
                    if (num + 1 > ConstUdpArg.DELAY_FRAME_CHANNELS)
                    {
                        return;
                    }

                    tb.Text = (num + 1).ToString();

                    break;
                }

                case Key.Down:
                {
                    // 下方向键减少数值
                    if (num - 1 < 1)
                    {
                        return;
                    }

                    tb.Text = (num - 1).ToString();

                    break;
                }
            }
        }

        /// <summary>
        ///     Tb_deleyTime的按键更改响应
        ///     回车：数值发送
        ///     上下方向键：更改数值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tb_deleyTime_OnKeyUp(object sender, KeyEventArgs e)
        {
            TextBox tb = sender as TextBox;
            int num = 1;
            if (!int.TryParse(s: tb.Text, result: out num))
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                {
                    // 回车更改
                    try
                    {
                        if (num < 1 || num > 1000)
                        {
                            num = 1;
                            tb.Text = "1";
                        }

                        OnDelayTimeWrite_OnClick(null, null); // 发送通道延时值 
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(value: exception);
                        throw;
                    }

                    break;
                }

                case Key.Up:
                {
                    // 上方向键增加数值
                    if (num + 1 > 1000)
                    {
                        return;
                    }

                    tb.Text = (num + 1).ToString();

                    break;
                }

                case Key.Down:
                {
                    // 下方向键减少数值
                    if (num - 1 < 1)
                    {
                        return;
                    }

                    tb.Text = (num - 1).ToString();

                    break;
                }
            }
        }

        #region 延时信息.(读/写/存).按钮响应

        /// <summary>///延时信息.读.按钮响应 /// </summary>
        private void OnDelayTimeRead_OnClick(object sender, RoutedEventArgs e)
        {
            udpCommandSocket.ReadDelyTime();
        }

        /// <summary>///延时信息.写.按钮响应 /// </summary>
        private void OnDelayTimeWrite_OnClick(object sender, RoutedEventArgs e)
        {
            string strT = tb_deleyTime.Text;
            if (string.IsNullOrEmpty(value: strT))
            {
                return;
            }

            uint t = uint.Parse(s: strT);
            byte[] temp = BitConverter.GetBytes(value: t);
            var data = new byte[2];
            for (int i = 0; i < 2; i++)
            {
                data[0] = temp[1];
                data[1] = temp[0];
            }

            udpCommandSocket.WriteDelayTime(data: data);
        }

        /// <summary>///延时信息.存.按钮响应 /// </summary>
        private void OnDelayTimeSave_OnClick(object sender, RoutedEventArgs e)
        {
            string strT = tb_deleyTime.Text;
            if (string.IsNullOrEmpty(value: strT))
            {
                return;
            }

            int intT = int.Parse(s: strT);
            int a = intT / 256;
            int b = intT - a * 256;

            var data = new byte[2];
            data.SetValue((byte) a, 0);
            data.SetValue((byte) b, 1);

            udpCommandSocket.SaveDelayTime(data: data);
        }

        #endregion

        #region Dac信息.(读/写/存).按钮响应

        private void DacLenRead_OnClick(object sender, RoutedEventArgs e)
        {
            udpCommandSocket.ReadCanChannelLen();
        }

        private void DacLenWrite_OnClick(object sender, RoutedEventArgs e)
        {
            string strT = tb_dacLen.Text;
            if (string.IsNullOrEmpty(value: strT))
            {
                return;
            }

            int t = int.Parse(s: strT);

            udpCommandSocket.WriteDacChannel(num: t);
        }

        private void DacLenSave_OnClick(object sender, RoutedEventArgs e)
        {
            string strT = tb_dacLen.Text;
            if (string.IsNullOrEmpty(value: strT))
            {
                return;
            }

            int intT = int.Parse(s: strT);
            int a = intT / 256;
            int b = intT - a * 256;

            var data = new byte[2];
            data.SetValue((byte) a, 0);
            data.SetValue((byte) b, 1);

            udpCommandSocket.SaveDacLen(data: data);
        }

        #endregion

        #endregion

        #region 原始数据与自动定标功能

        /// <summary>
        ///     原始数据图像
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OrigDataStart_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                udpCommandSocket.SwitchWindow(cmdBytes: ConstUdpArg.SwicthToOriginalWindow);
                if (OrigWaveData == null)
                {
                    OrigWaveData = new UdpWaveData {ExitFlag = false};
                    OrigWaveData.StartReceiveData(ip: ConstUdpArg.Src_OrigWaveIp);
                    udpCommandSocket.WriteOrigChannel(num: OrigChannel);
                    udpCommandSocket.WriteOrigTDiv(num: OrigTiv);
                    btn_origstart.Content = "停止";
                    OrigWaveData.StartRcvEvent.Set();
                }
                else if (OrigWaveData != null || OrigWaveData.IsBuilded)
                {
                    InitGraphState();
                    btn_origstart.Content = "启动";

                    orige_graph.Dispatcher.InvokeAsync(() =>
                    {
                        orige_graph.DataSource = 0;
                        orige_graph.Refresh();
                    });
                    orige_graph.Refresh();
                }
            }
            catch
            {
                Console.WriteLine(@"网络地址错误...");
            }
        }

        private void SaveOrigData_OnClick(object sender, RoutedEventArgs e)
        {
            if (OrigWaveData == null || !OrigWaveData.IsRcving)
            {
                MessageBox.Show("请采集原始波形数据");
                return;
            }

            IsorigSaveFlag = !IsorigSaveFlag;
            if (IsorigSaveFlag)
            {
                discFile = new DataUtl.DiscFile();
                btn_origsave.Content = "正在保存";
                discFile.IsStartFlag = true;
                discFile.EnableOrigSaveFile();
            }
            else
            {
                btn_origsave.Content = "保存数据";
                discFile.IsStartFlag = false;
                discFile.DisableSaveFile();
                discFile.Dispose();
            }
        }

        /// <summary>
        ///     更改原始通道的时分/通道参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeOrigConfig_OnClick(object sender, RoutedEventArgs e)
        {
            int ch = 1;
            int tdiv = 1;
            int.TryParse(s: tb_origChannel.Text, result: out ch);
            int.TryParse(s: tb_origTdiv.Text, result: out tdiv);

            try
            {
                if (ch < 1 || ch > ConstUdpArg.ORIG_CHANNEL_NUMS)
                {
                    MessageBox.Show("请输入正确的通道值");
                    ch = 1;
                }

                if (tdiv < 1 || tdiv > ConstUdpArg.ORIG_TIME_NUMS)
                {
                    MessageBox.Show("请输入正确的时分值");
                    ch = 1;
                }

                OrigChannel = ch - 1;
                OrigTiv = tdiv - 1;
            }
            catch (Exception)
            {
                // ignored
            }

            udpCommandSocket.WriteOrigChannel(num: OrigChannel);
            udpCommandSocket.WriteOrigTDiv(num: OrigTiv);
        }

        /// <summary>
        ///     计算C值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalCanCvalue_OnClick(object sender, RoutedEventArgs e)
        {
            int daclen = 0;
            int.TryParse(s: tb_dacLen.Text, result: out daclen);
            if (daclen != 0)
            {
                double tmp = daclen * 0.754;
                double result = Math.Round(value: tmp, mode: MidpointRounding.AwayFromZero); // 实现四舍五入
                tb_dacCvalue.Text = result.ToString();
            }
        }

        /// <summary>
        ///     Tb_origTdiv的按键更改响应
        ///     回车：数值发送
        ///     上下方向键：更改数值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tb_origTdiv_KeyUp(object sender, KeyEventArgs e)
        {
            TextBox tb = sender as TextBox;
            int num = 1;
            if (!int.TryParse(s: tb.Text, result: out num))
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                {
                    // 回车更改
                    try
                    {
                        if (num < 1 || num > ConstUdpArg.ORIG_TIME_NUMS)
                        {
                            num = 1;
                        }

                        OrigTiv = num - 1;
                        udpCommandSocket.WriteOrigTDiv(num: OrigTiv); // 0起始方式
                        OrigDataStart_OnClick(null, null); // 关闭波形
                        OrigDataStart_OnClick(null, null); // 开启波形
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(value: exception);
                        throw;
                    }

                    break;
                }

                case Key.Up:
                {
                    // 上方向键增加数值
                    if (num + 1 > ConstUdpArg.ORIG_TIME_NUMS)
                    {
                        return;
                    }

                    tb.Text = (num + 1).ToString();

                    break;
                }

                case Key.Down:
                {
                    // 下方向键减少数值
                    if (num - 1 < 1)
                    {
                        return;
                    }

                    tb.Text = (num - 1).ToString();
                    break;
                }
            }
        }

        /// <summary>
        ///     Tb_origChannel的按键更改响应
        ///     回车：数值发送
        ///     上下方向键：更改数值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tb_origChannel_OnKeyUp(object sender, KeyEventArgs e)
        {
            TextBox tb = sender as TextBox;
            int num = 1;
            if (!int.TryParse(s: tb.Text, result: out num))
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                {
                    // 回车更改
                    try
                    {
                        if (num < 1 || num > ConstUdpArg.ORIG_CHANNEL_NUMS)
                        {
                            num = 1;
                            tb.Text = "1";
                        }

                        OrigChannel = num - 1;
                        udpCommandSocket.WriteOrigChannel(num - 1); // 0起始方式
                        OrigDataStart_OnClick(null, null); // 关闭波形
                        OrigDataStart_OnClick(null, null); // 开启波形
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(value: exception);
                        throw;
                    }

                    break;
                }

                case Key.Up:
                {
                    // 上方向键增加数值
                    if (num + 1 > ConstUdpArg.ORIG_CHANNEL_NUMS)
                    {
                        return;
                    }

                    tb.Text = (num + 1).ToString();

                    break;
                }

                case Key.Down:
                {
                    // 下方向键减少数值
                    if (num - 1 < 1)
                    {
                        return;
                    }

                    tb.Text = (num - 1).ToString();

                    break;
                }
            }
        }

        /// <summary>
        ///     Tb_origFram的按键更改响应
        ///     回车：数值发送
        ///     上下方向键：更改数值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tb_origFram_OnKeyUp(object sender, KeyEventArgs e)
        {
            TextBox tb = sender as TextBox;
            int num = 1;
            if (!int.TryParse(s: tb.Text, result: out num))
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                {
                    // 回车更改
                    try
                    {
                        if (num < 0 || num > ConstUdpArg.ORIG_FRAME_NUMS * 10)
                        {
                            num = 200;
                            tb.Text = "200";
                        }

                        OrigDataStart_OnClick(null, null); // 关闭波形
                        Info.OrigFramNums = num;
                        OrigDataStart_OnClick(null, null); // 开启波形
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(value: exception);
                    }

                    break;
                }

                case Key.Up:
                {
                    // 上方向键增加数值
                    if (num * 10 > ConstUdpArg.ORIG_FRAME_NUMS * 10)
                    {
                        return;
                    }

                    tb.Text = (num * 10).ToString();

                    break;
                }

                case Key.Down:
                {
                    // 下方向键减少数值
                    if (num / 10 < 1)
                    {
                        return;
                    }

                    tb.Text = (num / 10).ToString();

                    break;
                }
            }
        }

        /// <summary>
        ///     读取文本数据值
        /// </summary>
        /// <param name="filepath">文本路径</param>
        /// <param name="valuedex"></param>
        /// <returns></returns>
        private float[] ReadDiscValue(string filepath, int valuedex)
        {
            var rerList = new List<float>();
            using (FileStream fs = new FileStream(path: filepath, mode: FileMode.Open, access: FileAccess.Read))
            {
                using (StreamReader sr = new StreamReader(stream: fs, encoding: Encoding.GetEncoding("gb2312")))
                {
                    string line;
                    string pattern = @"[+-]?\d+[\.]?\d*"; // 包括带小数点的数字和整数
                    float decvalue;
                    while ((line = sr.ReadLine()) != null)
                    {
                        Console.WriteLine(value: line);
                        MatchCollection matchCollection = Regex.Matches(input: line, pattern: pattern);
                        Match newMatch = matchCollection[i: valuedex];

                        if (float.TryParse(s: newMatch.Value, result: out decvalue))
                        {
                            rerList.Add(item: decvalue);
                        }
                        else
                        {
                            rerList.Add(0f);
                        }
                    }
                }
            }

            return rerList.ToArray();
        }

        /// <summary>
        ///     The send config_ on click.
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
        private void SendConfig_OnClick(object sender, RoutedEventArgs e)
        {
            SendBvalue();
            SendPhase();

            MessageBox.Show("发送成功");
        }

        /// <summary>
        ///     The send bvalue.
        /// </summary>
        private void SendBvalue()
        {
            if (!udpCommandSocket.IsSocketConnect)
            {
                return;
            }

            string dirpath;
            string inipath = Environment.CurrentDirectory + "\\data";
            dirpath = inipath;

            string filename = "B值.txt";
            string filepath = dirpath + "\\" + filename;
            if (!File.Exists(path: filepath))
            {
                MessageBox.Show("请先计算B值并保存");
                return;
            }

            try
            {
                float[] bfloats = ReadDiscValue(filepath: filepath, valuedex: 2);
                WaveSocket socket = new WaveSocket();
                byte[] result = socket.GetSendBvalues(bfloats: bfloats);
                udpCommandSocket.WriteBvalue(dataBytes: result);
            }
            catch (Exception e)
            {
                Console.WriteLine(value: e);
                throw;
            }
        }

        /// <summary>
        ///     The send phase.
        /// </summary>
        private void SendPhase()
        {
            if (!udpCommandSocket.IsSocketConnect)
            {
                return;
            }

            string dirpath;
            string inipath = Environment.CurrentDirectory + "\\data";
            dirpath = inipath;

            string filename = "初始相位.txt";
            string filepath = dirpath + "\\" + filename;

            if (!File.Exists(path: filepath))
            {
                MessageBox.Show("请先计算初始相位并保存");
                return;
            }
            try
            {
                float[] bfloats = ReadDiscValue(filepath: filepath, valuedex: 1);
                WaveSocket socket = new WaveSocket();
                byte[][] result = socket.GetSendPhases(caltin: bfloats);
                udpCommandSocket.WritePhase(datalistBytes: result);
            }
            catch (Exception e)
            {
                Console.WriteLine(value: e);
                throw;
            }
        }

        /// <summary>
        ///     保存参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveConfig_OnClick(object sender, RoutedEventArgs e)
        {
            if (!udpCommandSocket.IsSocketConnect)
            {
                return;
            }

            NormalBValueSave(bFloats: Bvalues);
            PhaseSave(bPhases: Phases);
        }

        /// <summary>
        ///     相位保存
        /// </summary>
        /// <param name="bPhases"></param>
        private void PhaseSave(float[] bPhases)
        {
            if (bPhases == null)
            {
                return;
            }

            string dirpath;
            string inipath = Environment.CurrentDirectory + "\\data";
            dirpath = inipath;
            string filename = "初始相位.txt";
            string filepath = dirpath + "\\" + filename;
            using (FileStream fs = new FileStream(path: filepath, mode: FileMode.Create, access: FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(stream: fs, encoding: Encoding.GetEncoding("gb2312")))
                {
                    for (int i = 0; i < bPhases.Length; i++)
                    {
                        string tmp = string.Format("通道{0} Phase={1}", i + 1, bPhases[i]);
                        sw.WriteLine(value: tmp);
                    }
                }
            }
        }

        /// <summary>
        ///     保存B值
        /// </summary>
        /// <param name="bFloats"></param>
        private void NormalBValueSave(float[] bFloats)
        {
            if (bFloats == null)
            {
                return;
            }

            string inipath = Environment.CurrentDirectory + "\\data";
            if (!Directory.Exists(path: inipath))
            {
                Directory.CreateDirectory(path: inipath);
            }

            string filename = "B值.txt";
            string filepath = inipath + "\\" + filename;
            using (FileStream fs = new FileStream(path: filepath, mode: FileMode.Create, access: FileAccess.Write))
            {
                using (StreamWriter sw = new StreamWriter(stream: fs, encoding: Encoding.GetEncoding("gb2312")))
                {
                    for (int i = 0; i < bFloats.Length; i++)
                    {
                        int tiv = i / 8;
                        int chl = i % 8;
                        string tmp = string.Format("时分:{0}B通道{1}:{2}", tiv + 1, chl + 1, bFloats[i]);
                        sw.WriteLine(value: tmp);
                    }
                }
            }
        }

        /// <summary>
        ///     计算初始相位点击响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalPhase_OnClick(object sender, RoutedEventArgs e)
        {
            if (btn_calPhase.IsEnabled)
            {
                if (!udpCommandSocket.IsSocketConnect)
                {
                    return;
                }
                btn_calPhase.IsEnabled = false;
                Phases = new float[ConstUdpArg.ORIG_TIME_NUMS];

                Task.Run(() =>
                {
                    WaveSocket waveSocket = new WaveSocket();
                    try
                    {
                        
                        waveSocket.StartCaclPhase(ip: ConstUdpArg.Src_OrigWaveIp, udpCommandSocket: udpCommandSocket);
                        waveSocket.SendOrigSwitchCommand(8, 1); // 八个通道一个时分
                        waveSocket.RcvResetEvent.Set();
                        Phases = waveSocket.CalToPhase();
                        MessageBox.Show("初始相位计算成功");
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                        throw;
                    }
                   

                    finally
                    {
                        btn_calPhase.Dispatcher.Invoke(() => { btn_calPhase.IsEnabled = true; });
                    }
                    btn_calPhase.Dispatcher.Invoke(() => { btn_calPhase.IsEnabled = true; });
                    waveSocket.Dispose();
                });
     
            }
        }

        /// <summary>
        ///     计算B值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalBvalue_OnClick(object sender, RoutedEventArgs e)
        {
            if (!udpCommandSocket.IsSocketConnect)
            {
                return;
            }

            btn_calBvalue.IsEnabled = false;
            Bvalues = new float[ConstUdpArg.ORIG_TIME_NUMS * ConstUdpArg.ORIG_CHANNEL_NUMS];
            Task.Run(() =>
            {
                WaveSocket waveSocket = new WaveSocket();
                try
                {
                    waveSocket.StartCaclBvalue(ip: ConstUdpArg.Src_OrigWaveIp, udpCommandSocket: udpCommandSocket);
                    waveSocket.SendOrigSwitchCommand(8, 8);
                    waveSocket.RcvResetEvent.Set();

                    var Bresults = new float[64];

                    for (int i = 0; i < waveSocket.BvalueDatas.Length; i++)
                    {
                        short[] query = waveSocket.BvalueDatas[i];
                        Bresults[i] = (query.Max() - query.Min()) / (8192.0f * 2.0f); // short转换为float  （最大值-最小值）/2
                    }

                    Array.Copy(sourceArray: Bresults, destinationArray: Bvalues, length: Bresults.Length);

                    
                    blistview.Dispatcher.Invoke(() =>
                    {
                        observableCollection.Clear();
                        foreach (float bvalue in Bresults)
                        {
                            observableCollection.Add(new UIBValue(value: bvalue));
                        }
                        IsGetBvalue = true;
                    });

                    MessageBox.Show("B值计算成功");
                }
                catch (Exception exception)
                {
                    Console.WriteLine(value: exception);
                    MessageBox.Show("B值计算失败");
                }

                finally
                {
                    btn_calBvalue.Dispatcher.Invoke(() => { btn_calBvalue.IsEnabled = true; });
                    waveSocket.Dispose();
                }
            });
        }

        // 回放原始数据
        private void ReplayOrigData_OnClick(object sender, RoutedEventArgs e)
        {
            if (!IsOrigReplay)
            {
                btn_origreplay.Content = "正在回放";
                byte[] buffBytes;

                List<string> fileList = GetFileDialogList(); // 选择
                if (fileList == null)
                {
                    btn_origreplay.Content = "回放";
                    return;
                }

                IsOrigReplay = true;
                if (!discFile.ReplayData(fileList: fileList, cancellationToken: Cancellation))
                {
                    MessageBox.Show("回放数据失败");
                }
            }
            else
            {
                btn_origreplay.Content = "回放";
                Cancellation.Cancel();
                Cancellation = new CancellationTokenSource();
                IsOrigReplay = false;
                orige_graph.DataSource = 0;
                orige_graph.Refresh();
            }
        }

        /// <summary>
        ///     从文件选择框选择文件并获取文件名列表
        /// </summary>
        /// <returns></returns>
        private static List<string> GetFileDialogList()
        {
            OpenFileDialog open = new OpenFileDialog {Title = "打开文件", Filter = "波形文件（.bin）|*.bin|所有文件|*.*"};

            RelativeDirectory rd = new RelativeDirectory();
            open.InitialDirectory = rd.Path + "//wavedata";

            var fileList = new List<string>(); // 文件列表

            byte[] buffBytes;
            if (open.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // 打开
                fileList.AddRange(collection: open.FileNames);
                return fileList;
            }

            return null;
        }

        #endregion

        #region 正常波形功能

        /// <summary>
        ///     保存正常工作数据按钮处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveWorkData_OnClick(object sender, RoutedEventArgs e)
        {
            if (NormWaveData == null)
            {
                if ((string) btn_workSave.Content == "保存数据")
                {
                    MessageBox.Show("请采集原始波形数据");
                }
                btn_workSave.Content = "保存数据";
                return;
            }

            IsworkSaveFlag = !IsworkSaveFlag;
            if (IsworkSaveFlag)
            {
                discFile = new DataUtl.DiscFile();
                btn_workSave.Content = "关闭保存";
                discFile.EnableWorkSaveFile();
                discFile.IsStartFlag = true;
            }
            else
            {
                btn_workSave.Content = "保存数据";
                discFile.DisableSaveFile();
                discFile.IsStartFlag = true;
                discFile.Dispose();
            }
        }

        /// <summary>
        ///     连接正常工作数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WorkDataStart_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                udpCommandSocket.SwitchWindow(cmdBytes: ConstUdpArg.SwicthToNormalWindow);
                if (NormWaveData == null)
                {
                    NormWaveData = new UdpWaveData();
                    NormWaveData.StartReceiveData(ip: ConstUdpArg.Src_NormWaveIp);
                    btn_workstart.Content = "停止";
                    NormWaveData.StartRcvEvent.Set();
                }
                else if (NormWaveData != null || NormWaveData.IsBuilded)
                {
                    InitGraphState();
                    graph_normalTime.Dispatcher.InvokeAsync(() => { graph_normalTime.DataSource = 0; });
                    graph_normalTime.Refresh();
                    graph_normalFrequency.Dispatcher.InvokeAsync(() => { graph_normalFrequency.DataSource = 0; });
                    graph_normalFrequency.Refresh();
                    graph_energyFirst.Dispatcher.InvokeAsync(() =>
                    {
                        graph_energyFirst.DataSource = 0;
                        graph_energyFirst.Refresh();
                    });

                    btn_workstart.Content = "启动";
                }
            }
            catch
            {
                Console.WriteLine(@"正常工作波形采集失败...");
                MessageBox.Show(@"正常工作波形采集失败...");
            }
        }

        /// <summary>
        ///     暂停工作波形
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnPauseWorkGraph_OnClick(object sender, RoutedEventArgs e)
        {
            IsGraphPause = !IsGraphPause;
        }

        /// <summary>
        ///     回放正常工作数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReplayWorkData_OnClick(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        ///     Tb_origTdiv的按键更改响应
        ///     回车：数值发送
        ///     上下方向键：更改数值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Tb_workChNum_OnKeyUp(object sender, KeyEventArgs e)
        {
            TextBox tb = sender as TextBox;
            int num = 1;
            if (!int.TryParse(s: tb.Text, result: out num))
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                {
                    // 回车更改
                    try
                    {
                        if (num < 1 || num > ConstUdpArg.ORIG_TIME_NUMS * ConstUdpArg.ORIG_CHANNEL_NUMS)
                        {
                            num = 1;
                        }

                        Info.WorkChannel = num;
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(value: exception);
                        throw;
                    }

                    break;
                }

                case Key.Up:
                {
                    // 上方向键增加数值
                    if (num + 1 > ConstUdpArg.ORIG_TIME_NUMS * ConstUdpArg.ORIG_CHANNEL_NUMS)
                    {
                        return;
                    }

                    tb.Text = (num + 1).ToString();

                    break;
                }

                case Key.Down:
                {
                    // 下方向键减少数值
                    if (num - 1 < 1)
                    {
                        return;
                    }

                    tb.Text = (num - 1).ToString();

                    break;
                }
            }
        }

        private void Tb_workFram_OnKeyUp(object sender, KeyEventArgs e)
        {
            TextBox tb = sender as TextBox;
            int num = 3125;
            if (!int.TryParse(s: tb.Text, result: out num))
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                {
                    // 回车更改
                    try
                    {
                        if (num < 0 || num > ConstUdpArg.WORK_FRAME_NUMS * 5)
                        {
                            num = 3125;
                            tb.Text = "3125";
                        }

                        WorkDataStart_OnClick(null, null); // 关闭波形
                        Info.WorkFramNums = num;
                        WorkDataStart_OnClick(null, null); // 开启波形
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(value: exception);
                        throw;
                    }

                    break;
                }

                case Key.Up:
                {
                    // 上方向键增加数值
                    if (num * 10 > ConstUdpArg.ORIG_FRAME_NUMS * 10)
                    {
                        return;
                    }

                    tb.Text = (num * 10).ToString();

                    break;
                }

                case Key.Down:
                {
                    // 下方向键减少数值
                    if (num / 10 < 1)
                    {
                        return;
                    }

                    tb.Text = (num / 10).ToString();

                    break;
                }
            }
        }

        #endregion

        #region 读取波形数据

        /// <summary>///控件读入能量图波形/// </summary>
        private void OnEnergyArrayGraph(object sender, float[] e)
        {
            if (e == null)
            {
                return;
            }

            if (IsGraphPause)
            {
                Console.WriteLine("能量波形数据已暂停");
                return;
            }

            int len = ConstUdpArg.ARRAY_USED;
            var graphdata = new float[len];

            Array.Copy(sourceArray: e, sourceIndex: 0, destinationArray: graphdata, destinationIndex: 0, length: len);

            var xnums = new int[len];
            var dataPoints1 = new Point[len];

            for (int i = 0; i < xnums.Length; i++)
            {
                xnums[i] = i + 1;
            }

            for (int i = 0; i < 64; i++)
            {
                dataPoints1[i] = new Point(xnums[i], graphdata[i]);
            }

            graph_energyFirst.Dispatcher.Invoke(() =>
            {
                graph_energyFirst.Refresh();
                graph_energyFirst.DataSource = dataPoints1;
            });
        }

        /// <summary>///控件读入延迟波形/// </summary>
        private void OnDelayWaveGraph(object sender, float[] e)
        {
            if (e == null)
            {
                return;
            }

            if (IsGraphPause)
            {
                Console.WriteLine("原始波形数据已暂停");
                return;
            }

            delay_graph.Dispatcher.Invoke(() =>
            {
                delay_graph.Refresh();
                delay_graph.DataSource = e;
            });
        }

        /// <summary>///控件读入原始波形/// </summary>
        private void OnOrigGraph(object sender, float[] e)
        {
            if (e == null)
            {
                return;
            }

            if (IsGraphPause)
            {
                Console.WriteLine("工作时域波形数据已暂停");
                return;
            }

            orige_graph.Dispatcher.Invoke(() =>
            {
                orige_graph.Refresh();
                orige_graph.DataSource = e;
            });
        }

        /// <summary>///控件读入工作时域波形/// </summary>
        private void OnWorkGraph(object sender, float[] e)
        {
            if (e == null)
            {
                return;
            }

            if (IsGraphPause)
            {
                Console.WriteLine("工作时域波形数据已暂停");
                return;
            }

            graph_normalTime.Dispatcher.Invoke(() =>
            {
                graph_normalTime.Refresh();
                graph_normalTime.DataSource = e;
            });
        }

        /// <summary>
        ///     控件读入时域最大值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMaxWorkValue(object sender, float[] e)
        {
            if (e == null)
            {
                return;
            }

            if (IsGraphPause)
            {
                // Console.WriteLine("工作时域波形数据已暂停");
                return;
            }

            float maxtmp = e.Max();
            tb_ampValue.Dispatcher.Invoke(() => { tb_ampValue.Text = maxtmp.ToString(); });
        }

        /// <summary>///控件读入工作频率波形/// </summary>
        private void OnFrapPointGraph(object sender, Point[] points)
        {
            if (points == null)
            {
                return;
            }

            if (IsGraphPause)
            {
                Console.WriteLine("工作频率波形数据已暂停");
                return;
            }

            graph_normalFrequency.Dispatcher.Invoke(() =>
            {
                graph_normalFrequency.Refresh();
                graph_normalFrequency.DataSource = points;
            });
        }

        /// <summary>
        ///     读入频率求出最大值，发送给控件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMaxFrapPoint(object sender, Point[] e)
        {
            if (e == null)
            {
                return;
            }

            if (IsGraphPause)
            {
                Console.WriteLine("工作频率波形数据已暂停");
                return;
            }

            Point maxPoint = new Point(-100, -100);
            foreach (Point dPoint in e)
            {
                if (maxPoint.Y < dPoint.Y)
                {
                    maxPoint = dPoint;
                }
            }

            tb_frqValue.Dispatcher.Invoke(() => { tb_frqValue.Text = maxPoint.X.ToString(); });
        }

        #endregion

        #region 系统设置与参数

        #region 脉冲周期.(读/写/存).按钮响应

        /// <summary>脉冲周期.读.按钮响应</summary>
        private void OnPulsePeriodRead_OnClick(object sender, RoutedEventArgs e)
        {
            udpCommandSocket.ReadPulsePeriod();
            Task.Run(() =>
            {
                btn_readpluseperiod.Dispatcher.Invoke(() => { btn_readpluseperiod.IsEnabled = false; });

                Thread.Sleep(1000);
                btn_readpluseperiod.Dispatcher.Invoke(() => { btn_readpluseperiod.IsEnabled = true; });
            });
        }

        /// <summary>脉冲周期.写.按钮响应</summary>
        private void OnPulsePeriodWrite_OnClick(object sender, RoutedEventArgs e)
        {
            string strT = tb_setting_pulse_period.Text;
            if (string.IsNullOrEmpty(value: strT))
            {
                return;
            }

            int intT = int.Parse(s: strT);
            intT = intT / 5;
            int a = intT / 256;
            int b = intT - a * 256;

            var data = new byte[2];
            data.SetValue((byte) a, 0);
            data.SetValue((byte) b, 1);
            udpCommandSocket.WritePulsePeriod(data: data);
        }

        /// <summary>脉冲周期.存.按钮响应</summary>
        private void OnPulsePeriodSave_OnClick(object sender, RoutedEventArgs e)
        {
            string strT = tb_setting_pulse_period.Text;
            if (string.IsNullOrEmpty(value: strT))
            {
                return;
            }

            int intT = int.Parse(s: strT);
            intT = intT / 5;
            int a = intT / 256;
            int b = intT - a * 256;

            var data = new byte[2];
            data.SetValue((byte) a, 0);
            data.SetValue((byte) b, 1);
            udpCommandSocket.SavePulsePeriod(data: data);
        }

        #endregion

        #region 脉冲延时.(读/写/存).按钮响应

        /// <summary>
        ///     可调整值的TextBox上滚轮动作响应(小数)
        ///     ADC偏移
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextboxOnMouseWheel2(object sender, MouseWheelEventArgs e)
        {
            // e.Delta > 0,向上滚动滚轮,文本框数字+0.01,最大1
            // e.Delta < 0,向下滚动滚轮,文本框数字-0.01,最小0
            ChangeMouseWheel(sender: sender, isInt: false, isUp: e.Delta > 0);
        }

        /// <summary>脉冲延时.读.按钮响应</summary>
        private void OnPulseDelayRead_OnClick(object sender, RoutedEventArgs e)
        {
            udpCommandSocket.ReadPulseDelay();
        }

        /// <summary>脉冲延时.写.按钮响应</summary>
        private void OnPulseDelayWrite_OnClick(object sender, RoutedEventArgs e)
        {
            string strT = tb_setting_pulse_delay.Text;
            if (string.IsNullOrEmpty(value: strT))
            {
                return;
            }

            int intT = int.Parse(s: strT);
            intT = intT / 5;
            int a = intT / 256;
            int b = intT - a * 256;

            var data = new byte[2];
            data.SetValue((byte) a, 0);
            data.SetValue((byte) b, 1);
            udpCommandSocket.WritePulseDelay(data: data);
        }

        /// <summary>脉冲延时.存.按钮响应</summary>
        private void OnPulseDelaySave_OnClick(object sender, RoutedEventArgs e)
        {
            string strT = tb_setting_pulse_delay.Text;
            if (string.IsNullOrEmpty(value: strT))
            {
                return;
            }

            int intT = int.Parse(s: strT);
            intT = intT / 5;
            int a = intT / 256;
            int b = intT - a * 256;

            var data = new byte[2];
            data.SetValue((byte) a, 0);
            data.SetValue((byte) b, 1);
            udpCommandSocket.SavePulseDelay(data: data);
        }

        #endregion

        #region 脉冲宽度.(读/写/存).按钮响应

        /// <summary>脉冲宽度.读.按钮响应</summary>
        private void OnPulseWidthRead_OnClick(object sender, RoutedEventArgs e)
        {
            udpCommandSocket.ReadPulseWidth();
        }

        /// <summary>脉冲宽度.写.按钮响应</summary>
        private void OnPulseWidthWrite_OnClick(object sender, RoutedEventArgs e)
        {
            string strT = tb_setting_pulse_width.Text;
            if (string.IsNullOrEmpty(value: strT))
            {
                return;
            }

            int intT = int.Parse(s: strT);
            intT = intT / 5;
            int a = intT / 256;
            int b = intT - a * 256;

            var data = new byte[2];
            data.SetValue((byte) a, 0);
            data.SetValue((byte) b, 1);
            udpCommandSocket.WritePulseWidth(data: data);
        }

        /// <summary>脉冲宽度.存.按钮响应</summary>
        private void OnPulseWidthSave_OnClick(object sender, RoutedEventArgs e)
        {
            string strT = tb_setting_pulse_width.Text;
            if (string.IsNullOrEmpty(value: strT))
            {
                return;
            }

            int intT = int.Parse(s: strT);
            intT = intT / 5;
            int a = intT / 256;
            int b = intT - a * 256;

            var data = new byte[2];
            data.SetValue((byte) a, 0);
            data.SetValue((byte) b, 1);
            udpCommandSocket.SavePulseWidth(data: data);
        }

        #endregion

        #region ADC偏移.(读/写/存).按钮响应

        /// <summary>ADC偏移.读.按钮响应</summary>
        private void OnAdcOffsetRead_OnClick(object sender, RoutedEventArgs e)
        {
            udpCommandSocket.ReadAdcOffset();
        }

        /// <summary>ADC偏移.写.按钮响应</summary>
        private void OnAdcOffsetWrite_OnClick(object sender, RoutedEventArgs e)
        {
            string strT = tb_setting_adc_offset.Text;
            if (string.IsNullOrEmpty(value: strT))
            {
                return;
            }

            float floatT = float.Parse(s: strT);
            if (floatT > 0.5)
            {
                floatT = 0.5f;
            }

            int tmpT = (int) ((floatT + 1.2) * 128 / 1.2);

            udpCommandSocket.WriteAdcOffset((byte) tmpT);
        }

        /// <summary>ADC偏移.存.按钮响应</summary>
        private void OnAdcOffsetSave_OnClick(object sender, RoutedEventArgs e)
        {
            string strT = tb_setting_adc_offset.Text;
            if (string.IsNullOrEmpty(value: strT))
            {
                return;
            }

            float floatT = float.Parse(s: strT);
            int tmpT = (int) ((floatT + 1.2) * 128 / 1.2);
            udpCommandSocket.SaveAdcOffset((byte) tmpT);
        }

        #endregion

        /// <summary>
        ///     可调整值的TextBox上滚轮动作响应(整数)
        ///     脉冲周期/脉冲延时/脉冲宽度
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextboxOnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // e.Delta > 0,向上滚动滚轮,文本框数字+1,最大65535
            // e.Delta < 0,向下滚动滚轮,文本框数字-1,最小0
            ChangeMouseWheel(sender: sender, isInt: true, isUp: e.Delta > 0);
        }

        /// <summary>
        ///     TextBox值发生变化
        /// </summary>
        /// <param name="sender">TextBox</param>
        /// <param name="isInt">文本框中是否是整数</param>
        /// <param name="isUp">是否向上变化</param>
        private void ChangeMouseWheel(object sender, bool isInt, bool isUp)
        {
            TextBox tb = sender as TextBox;
            string str = tb.Text;
            if (string.IsNullOrEmpty(value: str))
            {
                return;
            }

            if (isInt)
            {
                decimal d = decimal.Parse(s: str);
                if (isUp)
                {
                    if (d + 1 > 65535)
                    {
                        return;
                    }

                    tb.Text = (d + 1).ToString();
                }
                else
                {
                    if (d - 1 < 0)
                    {
                        return;
                    }

                    tb.Text = (d - 1).ToString();
                }
            }
            else
            {
                float d = float.Parse(s: str);
                if (isUp)
                {
                    if (d + 0.01 > 1)
                    {
                        return;
                    }

                    string s = tb.Text = (d + 0.01).ToString("G2");
                }
                else
                {
                    if (d - 0.01 < 0)
                    {
                        return;
                    }

                    tb.Text = (d - 0.01).ToString("G2");
                }
            }
        }

        #endregion
    }
}