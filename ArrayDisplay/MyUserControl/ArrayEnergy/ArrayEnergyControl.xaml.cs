using System;              
using System.Collections.Generic;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Timer = System.Timers.Timer;

namespace ArrayDisplay.MyUserControl.ArrayEnergy {
    using ArrayDisplay.Net;

    /// <summary>
    ///     ArrayEnergyControl.xaml 的交互逻辑
    /// </summary>
    public partial class ArrayEnergyControl:IDisposable {
        public static int array_Num = ConstUdpArg.ARRAY_NUM; //初始探头个数
        public static int max_PixLen ; //最大像素长度
        public static int min_PixLen ; //最小像素长度
        public static int deafult_PixLen ; // 默认像素长度


        readonly DispatcherTimer timer = new DispatcherTimer();
        readonly bool[] flag = new bool[array_Num];//探头情况
        readonly Rectangle[] rcts = new Rectangle[array_Num]; //探头能量对应矩形

        int currentListen;
        float[] data;//能量数据
        int lastListen;
        Rectangle singleClickObject;
        Timer singleOrDoubleClickTimer; //单双击定时器，显示或关闭rect
        public int[] flagstate;

        public float[] Data {
            set {
                try {
                    data = value;
                    UpdateEnergy();
                }
                catch(Exception e) {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }


        public Rectangle[] Rcts {
            get { return rcts; }
        }

        public ArrayEnergyControl() {
            InitializeComponent();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += TimerOnTick;
        }

        void TimerOnTick(object sender, EventArgs event_args) {
            DrawRect(Rcts);
        }

        #region 更新通道能量

//        void UpdateEnergy() {
//            int width = 8;
//            for(int i = 0; i < Valid_Array_Num; i++) {
//                try
//                {
//                    int x = i / 128; //显示窗口（0，1，2）
//                    int y = i % 128; //显示矩形（0-127)
//                    //                    float offset = 1840.0f / 128;
//                    int height = 0;
//                    if (flag[i])
//                        if (data[i] <= 0) height = MinEnergyValue;
//                        else if (data[i] > 1) height = MaxEnergyValue;
//                        else
//                            height = MinEnergyValue +
//                                     (MaxEnergyValue - MinEnergyValue) * (int)data[i];
//                    else height = 10; //关闭的通道固定一个值 
//                    int height1 = height;
////                    Rcts[i].Dispatcher.BeginInvoke(new Action(() =>
////                                                              {
////                                                                  Height = height1;
////                                                                  Width = width;
////                                                              }));
//                }
//                catch (Exception e)
//                {
//                    Console.WriteLine("Error_one");
//                    throw;
//                } 
//            }
//                
//        }
        void UpdateEnergy() {
            for(int i = 0; i < array_Num; i++)
                try {
                    int x = i / 128; //显示窗口（0，1，2）
                    int y = i % 128; //显示矩形（0-127)
                    int i1 = i;
                    Dispatcher.BeginInvoke(new Action(() => {
                                                          if (flag[i1])
                                                              if (data[i1] <= 0) Rcts[i1].Height = min_PixLen;
                                                              else if (data[i1] > 1)
                                                                  Rcts[i1].Height = max_PixLen;
                                                              else
                                                                  Rcts[i1].Height = min_PixLen + (max_PixLen - min_PixLen) * data[i1];
                                                          else Rcts[i1].Height = 10; //关闭的通道固定一个值 
                                                          Rcts[i1].Width = 8;
                                                      }));
                }
                catch(Exception) {
                    Console.WriteLine("Error_one");
                    throw;
                }
        }

        #endregion

        public void InitArrayEnergy() {
            var array_state = new int[256];
            for(int i = 0; i < array_Num; i++) {
                Rcts[i] = new Rectangle();
                Rcts[i].MouseLeftButtonDown += MouseLeftButton;
                Rcts[i].Name = "rct" + i;
                RegisterName(Rcts[i].Name, Rcts[i]); //从Xaml中通过名字访问对象
            }
            InitRcts(array_state);
            DrawRect(Rcts);

            #region    隐藏其他

            if (array_Num <= 128) {
                arrayLabelLeft2.Visibility = Visibility.Hidden;
                arrayLabelRight2.Visibility = Visibility.Hidden;

                arrayLabelLeft3.Visibility = Visibility.Hidden;
                arrayLabelRight3.Visibility = Visibility.Hidden;

                arrayLabelLeft4.Visibility = Visibility.Hidden;
                arrayLabelRight4.Visibility = Visibility.Hidden;
            }
            else if (array_Num <= 256) {
                arrayLabelLeft3.Visibility = Visibility.Hidden;
                arrayLabelRight3.Visibility = Visibility.Hidden;

                arrayLabelLeft4.Visibility = Visibility.Hidden;
                arrayLabelRight4.Visibility = Visibility.Hidden;
            }
            else if (array_Num <= 384) {
                arrayLabelLeft4.Visibility = Visibility.Hidden;
                arrayLabelRight4.Visibility = Visibility.Hidden;
            }

            #endregion
        }

        void InitRcts(IReadOnlyList<int> state_ints) {
            if (state_ints == null) throw new ArgumentNullException("state_ints");
            for(int i = 0; i < state_ints.Count; i++) {
                if (state_ints[i] == 0) {
                    Rcts[i].Fill = (SolidColorBrush) Resources["StrokeBrush"];
                    flag[i] = true;
                }
                else {
                    Rcts[i].Fill = (SolidColorBrush) Resources["DisableBrush"];
                    flag[i] = false;
                }
                Rcts[i].Width = 8;
                Rcts[i].Height = deafult_PixLen;
                Rcts[i].ToolTip = new TextBlock(new Run(i + 1 + ": " + Rcts[i].Height));
            }
        }

        void DrawRect(Rectangle[] rectangles) {
            for(int i = 0; i < rectangles.Length; i++) {
                int x = i / 128; //显示窗口（0，1，2）
                int y = i % 128; //显示矩形（0-127)
                float offset = 1840.0f / 128;
                switch(x) {
                    case 0: {
                        canvas0.Children.Add(rectangles[i]);
                        double left = y * offset + 3;
                        Canvas.SetLeft(rectangles[i], left);
                        break;
                    }
                    case 1: {
                        canvas1.Children.Add(rectangles[i]);
                        double left = y * offset + 3;
                        Canvas.SetLeft(rectangles[i], left);
                        break;
                    }
                    case 2: {
                        canvas2.Children.Add(rectangles[i]);
                        double left = y * offset + 3;
                        Canvas.SetLeft(rectangles[i], left);
                        break;
                    }
                    default: {
                        canvas3.Children.Add(rectangles[i]);
                        double left = y * offset + 3;
                        Canvas.SetLeft(rectangles[i], left);
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     鼠标左键双击用来关闭/打开通道
        /// </summary>
        void MouseLeftButton(object sender, MouseButtonEventArgs e) {
            Rectangle rt = sender as Rectangle;
            if (rt != null) {
                string s = rt.Name.Remove(0, 3);

                int i = int.Parse(s);

                if (e.ClickCount == 2) //双击，处理开启或关闭通道
                {
                    if (singleOrDoubleClickTimer != null) {
                        singleOrDoubleClickTimer.Enabled = false;
                        singleOrDoubleClickTimer.Close();
                    }

                    currentListen = 0;

                    EnableArrayEventArgs arg = new EnableArrayEventArgs(Enable_Array_Event, this);

                    if (flag[i]) {
                        flag[i] = false;
                        rt.Fill = (SolidColorBrush) Resources["disableBrush"];
                        arg.Flag = false;
                    }
                    else {
                        flag[i] = true;
                        rt.Fill = (SolidColorBrush) Resources["strokeBrush"];
                        arg.Flag = true;
                    }
                    arg.ArrayNum = i + 1;
                    RaiseEvent(arg);
                }
                else //单击，听音处理
                {
                    if (flag[i]) //通道有效
                    {
                        singleOrDoubleClickTimer = new Timer(500);
                        singleOrDoubleClickTimer.Elapsed += OnTimedEvent;
                        singleOrDoubleClickTimer.AutoReset = false;
                        singleOrDoubleClickTimer.Enabled = true;
                        singleClickObject = rt; //记下被单击的目标
                        currentListen = i + 1; //单击目标的编号
                    }
                }
            }
        }

        void EnableArrayListen() {
            ListenArrayEventArgs args = new ListenArrayEventArgs(Listen_Array_Event, singleClickObject);
            if (flag[currentListen - 1]) //通道有效
            {
                if (lastListen != currentListen) //通道变化了
                {
                    //关闭上次通道听音
                    if (lastListen != 0)
                        if (flag[lastListen - 1]) //若通道已打开
                            Rcts[lastListen - 1].Fill = (SolidColorBrush) Resources["strokeBrush"];
                        else //若通道已关闭
                            Rcts[lastListen - 1].Fill = (SolidColorBrush) Resources["disableBrush"];
                    //设置当前通道听音
                    Rcts[currentListen - 1].Fill = (SolidColorBrush) Resources["listenBrush"];
                    lastListen = currentListen;
                    args.Flag = true;
                }
                else //通道未发生变化
                {
                    if (Equals(Rcts[currentListen - 1].Fill, (SolidColorBrush) Resources["listenBrush"])) {
                        Rcts[currentListen - 1].Fill = (SolidColorBrush) Resources["strokeBrush"];
                        args.Flag = false;
                    }
                    else {
                        Rcts[currentListen - 1].Fill = (SolidColorBrush) Resources["listenBrush"];
                        args.Flag = true;
                    }
                }
                args.ArrayNum = currentListen;
                RaiseEvent(args);
            }
        }

        void OnTimedEvent(object source, ElapsedEventArgs e) {
            singleClickObject.Dispatcher.BeginInvoke(new DelegateEnableArrayListen(EnableArrayListen));
        }

        /// <summary>
        ///     鼠标左键单击击用来设置通道听音
        /// </summary>
        delegate void DelegateEnableArrayListen();

        #region 路由事件关闭/开启通道

        //声明路由事件
        public static readonly RoutedEvent Enable_Array_Event = EventManager.RegisterRoutedEvent("EnableArrayEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ArrayEnergyControl));

        //CLR事件包装
        public event RoutedEventHandler EnableArray {
            add { AddHandler(Enable_Array_Event, value); }
            remove { RemoveHandler(Enable_Array_Event, value); }
        }

        #endregion

        #region 路由事件通道听音

        //声明路由事件
        public static readonly RoutedEvent Listen_Array_Event = EventManager.RegisterRoutedEvent("ListenArrayEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ArrayEnergyControl));

        //CLR事件包装
        public event RoutedEventHandler ListenArray {
            add { AddHandler(Listen_Array_Event, value); }
            remove { RemoveHandler(Listen_Array_Event, value); }
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                if (singleOrDoubleClickTimer != null) singleOrDoubleClickTimer.Dispose();
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>
    ///     通道开关路由事件参数类
    /// </summary>
    public class EnableArrayEventArgs : RoutedEventArgs {
        public bool Flag {
            set;
            get;
        }

        public int ArrayNum {
            set;
            get;
        }

        public EnableArrayEventArgs(RoutedEvent routed_event, object source) : base(routed_event, source) { }
    }

    /// <summary>
    ///     通道听音路由事件参数类
    /// </summary>
    public class ListenArrayEventArgs : RoutedEventArgs {
        public bool Flag {
            set;
            get;
        }

        public int ArrayNum {
            set;
            get;
        }

        public ListenArrayEventArgs(RoutedEvent routed_event, object source) : base(routed_event, source) { }
    }
}
