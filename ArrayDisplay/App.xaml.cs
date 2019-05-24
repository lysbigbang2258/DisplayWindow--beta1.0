// --------------------------------------------------------------------------------------------------------------------
// <summary>
//   App.xaml 的交互逻辑
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ArrayDisplay {
    using System.Diagnostics;
    using System.Windows;

    /// <summary>
    ///     App.xaml 的交互逻辑
    /// </summary>
    public class App : Application {
        public App() {
            TotalStopWatch = new Stopwatch();
        }

       public static Stopwatch TotalStopWatch {
            get;
            set;
        }
    }
}
