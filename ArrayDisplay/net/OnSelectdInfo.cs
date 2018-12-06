﻿// 2018071115:34

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ArrayDisplay.Annotations;

namespace ArrayDisplay.net {
    public class OnSelectdInfo:INotifyPropertyChanged,IDisposable
    {
        #region field

        int workWaveChannel = 0;
        bool isSaveData;

        #endregion

        #region Property 
        //工作波形
        public int WorkWaveChannel {
            get {
                return workWaveChannel;
            }
            set {
                workWaveChannel = value;
                OnPropertyChanged();
            }
        }

        public bool IsSaveData
        {
            get {
                
                return isSaveData;
            }
            set
            {
                isSaveData = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}