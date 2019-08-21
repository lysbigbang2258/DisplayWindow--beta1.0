// 201812284:30 PM
namespace ArrayDisplay.Net {
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    using Annotations;

    /// <summary>
    /// The system info.
    /// </summary>
    public sealed class SystemInfo : INotifyPropertyChanged {
        #region Field

        /// <summary>
        /// The mc id.
        /// </summary>
        string mcId;

        /// <summary>
        /// The mc mac.hun
        /// </summary>
        string mcMac;

        /// <summary>
        /// The mc type.
        /// </summary>
        string mcType;

        /// <summary>
        /// The pulse delay.
        /// </summary>
        int pulseDelay;

        /// <summary>
        /// The pulse period.
        /// </summary>
        int pulsePeriod;

        /// <summary>
        /// The pulse width.
        /// </summary>
        int pulseWidth;

        /// <summary>
        /// The adc num.
        /// </summary>
        int adcNum;

        /// <summary>
        /// The adc offset.
        /// </summary>
        string adcOffset;

        /// <summary>
        /// The delay channel.
        /// </summary>
        int delayChannel;

        /// <summary>
        /// The delay time.
        /// </summary>
        int delayTime;

        /// <summary>
        /// The dacLen.
        /// </summary>
        int dacLen;

        /// <summary>
        /// The dac channel.
        /// </summary>
        int dacChannel;

        /// <summary>
        /// The orig frams.
        /// </summary>
        int origFrams;

        /// <summary>
        /// The orig channel.
        /// </summary>
        int origChannel;

        /// <summary>
        /// The orig tdiv.
        /// </summary>
        int origTdiv;

        /// <summary>
        /// The work channel.
        /// </summary>
        int workChannel;

        int workSaveTime;

        int workFramNums;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemInfo"/> class.
        /// </summary>
        public SystemInfo() {
            mcId = string.Empty;
            mcMac = string.Empty;
            mcType = string.Empty;
            pulseDelay = -1;
            pulsePeriod = -1;
            pulseWidth = -1;
            adcNum = 1;

            adcOffset = string.Empty;
            delayChannel = 1;
            delayTime = 1;

            dacLen = 3000;
            dacChannel = 2;
            origFrams = 200;
            origChannel = 1;
            origTdiv = 1;
            workChannel = 1;
            workSaveTime = 3;

            workFramNums = 3125;
        }

        #region State Info

        /// <summary>
        /// Gets or sets the mc type.
        /// 设备类型
        /// </summary>
        public string McType {
            get {
                return mcType;
            }

            set {
                if (value == mcType) {
                    return;
                }
                mcType = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the mc id.
        /// 设备ID
        /// </summary>
        public string McId {
            get {
                return mcId;
            }

            set {
                if (value == mcId) {
                    return;
                }
                mcId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the mc mac.
        /// 设备MAC
        /// </summary>
        public string McMac {
            get {
                return mcMac;
            }

            set {
                if (value == mcMac) {
                    return;
                }
                mcMac = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the adc offset.
        /// adc偏移
        /// </summary>
        public string AdcOffset {
            get {
                return adcOffset;
            }

            set {
                if (value == adcOffset) {
                    return;
                }
                adcOffset = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the adc num.
        /// adc偏移.adcID
        /// </summary>
        public int AdcNum {
            get {
                return adcNum;
            }

            set {
                if (value == adcNum) {
                    return;
                }
                adcNum = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the pulse period.
        /// 脉冲周期
        /// </summary>
        public int PulsePeriod {
            get {
                return pulsePeriod;
            }

            set {
                if (value == pulsePeriod) {
                    return;
                }
                pulsePeriod = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the pulse delay.
        /// 脉冲延时
        /// </summary>
        public int PulseDelay {
            get {
                return pulseDelay;
            }

            set {
                if (value == pulseDelay) {
                    return;
                }
                pulseDelay = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the pulse width.
        /// 脉冲宽度
        /// </summary>
        public int PulseWidth {
            get {
                return pulseWidth;
            }

            set {
                if (value == pulseWidth) {
                    return;
                }
                pulseWidth = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Wave Info

        /// <summary>
        /// Gets or sets the delay channel.
        /// 延时通道
        /// </summary>
        public int DelayChannel {
            get {
                return delayChannel;
            }

            set {
                delayChannel = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the delay time.
        /// 延时时间量
        /// </summary>
        public int DelayTime {
            get {
                return delayTime;
            }

            set {
                
                delayTime = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the work channel.
        /// 工作通道
        /// </summary>
        public int WorkChannel {
            get {
                return workChannel;
            }

            set {
               
                workChannel = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the dac lenth.
        /// DAC长度
        /// </summary>
        public int DacLenth {
            get {
                return dacLen;
            }

            set {
                
                dacLen = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the dac channel.
        /// Dac通道
        /// </summary>
        public int DacChannel {
            get {
                return dacChannel;
            }

            set {
                
                dacChannel = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the orig fram nums.
        /// 原始数据帧数
        /// </summary>
        public int OrigFramNums {
            get {
                return origFrams;
            }

            set {
                
                origFrams = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the orig channel.
        /// // 原始通道
        /// </summary>
        public int OrigChannel {
            get {
                return origChannel;
            }

            set {
                
                origChannel = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the orig tdiv.
        /// 原始时分
        /// </summary>
        public int OrigTdiv {
            get {
                return origTdiv;
            }

            set {
                
                origTdiv = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the save packs.
        /// </summary>
        public int WorkSaveTime {
            get {
                return workSaveTime;
            }
            set {
                
                workSaveTime = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Get or Set The work fram nums.
        /// </summary>
        public int WorkFramNums {
            get {
                return workFramNums;
            }
            set {
                
                workFramNums = value;
                OnPropertyChanged();
            }
        }

        #endregion



        /// <summary>
        /// The property changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The on property changed.
        /// </summary>
        /// <param name="propertyName">
        /// The property name.
        /// </param>
        [NotifyPropertyChangedInvocator]
        void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler onPropertyChanged = PropertyChanged;
            if (onPropertyChanged != null) {
                onPropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

}
