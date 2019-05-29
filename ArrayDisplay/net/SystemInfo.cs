// 201812284:30 PM
namespace ArrayDisplay.Net {
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    using ArrayDisplay.Annotations;

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
            this.mcId = string.Empty;
            this.mcMac = string.Empty;
            this.mcType = string.Empty;
            this.pulseDelay = -1;
            this.pulsePeriod = -1;
            this.pulseWidth = -1;
            this.adcNum = 1;

            this.adcOffset = string.Empty;
            this.delayChannel = 1;
            this.delayTime = 1;

            this.dacLen = 3000;
            this.dacChannel = 2;
            this.origFrams = 200;
            this.origChannel = 1;
            this.origTdiv = 1;
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
                return this.mcType;
            }

            set {
                if (value == this.mcType) {
                    return;
                }
                this.mcType = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the mc id.
        /// 设备ID
        /// </summary>
        public string McId {
            get {
                return this.mcId;
            }

            set {
                if (value == this.mcId) {
                    return;
                }
                this.mcId = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the mc mac.
        /// 设备MAC
        /// </summary>
        public string McMac {
            get {
                return this.mcMac;
            }

            set {
                if (value == this.mcMac) {
                    return;
                }
                this.mcMac = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the adc offset.
        /// adc偏移
        /// </summary>
        public string AdcOffset {
            get {
                return this.adcOffset;
            }

            set {
                if (value == this.adcOffset) {
                    return;
                }
                this.adcOffset = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the adc num.
        /// adc偏移.adcID
        /// </summary>
        public int AdcNum {
            get {
                return this.adcNum;
            }

            set {
                if (value == this.adcNum) {
                    return;
                }
                this.adcNum = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the pulse period.
        /// 脉冲周期
        /// </summary>
        public int PulsePeriod {
            get {
                return this.pulsePeriod;
            }

            set {
                if (value == this.pulsePeriod) {
                    return;
                }
                this.pulsePeriod = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the pulse delay.
        /// 脉冲延时
        /// </summary>
        public int PulseDelay {
            get {
                return this.pulseDelay;
            }

            set {
                if (value == this.pulseDelay) {
                    return;
                }
                this.pulseDelay = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the pulse width.
        /// 脉冲宽度
        /// </summary>
        public int PulseWidth {
            get {
                return this.pulseWidth;
            }

            set {
                if (value == this.pulseWidth) {
                    return;
                }
                this.pulseWidth = value;
                this.OnPropertyChanged();
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
                return this.delayChannel;
            }

            set {
                this.delayChannel = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the delay time.
        /// 延时时间量
        /// </summary>
        public int DelayTime {
            get {
                return this.delayTime;
            }

            set {
                
                this.delayTime = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the work channel.
        /// 工作通道
        /// </summary>
        public int WorkChannel {
            get {
                return this.workChannel;
            }

            set {
               
                this.workChannel = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the dac lenth.
        /// DAC长度
        /// </summary>
        public int DacLenth {
            get {
                return this.dacLen;
            }

            set {
                
                this.dacLen = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the dac channel.
        /// Dac通道
        /// </summary>
        public int DacChannel {
            get {
                return this.dacChannel;
            }

            set {
                
                this.dacChannel = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the orig fram nums.
        /// 原始数据帧数
        /// </summary>
        public int OrigFramNums {
            get {
                return this.origFrams;
            }

            set {
                
                this.origFrams = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the orig channel.
        /// // 原始通道
        /// </summary>
        public int OrigChannel {
            get {
                return this.origChannel;
            }

            set {
                
                this.origChannel = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the orig tdiv.
        /// 原始时分
        /// </summary>
        public int OrigTdiv {
            get {
                return this.origTdiv;
            }

            set {
                
                this.origTdiv = value;
                this.OnPropertyChanged();
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
            PropertyChangedEventHandler onPropertyChanged = this.PropertyChanged;
            if (onPropertyChanged != null) {
                onPropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

}
