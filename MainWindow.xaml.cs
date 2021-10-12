using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Fluent;

namespace Stormworks_VRMS
{
    public partial class MainWindow : Window
    {
        private Util util;
        private ConsoleWindow console;

        private SWHTTPListener listener;
        private HttpClient httpClient;

        public MainWindow()
        {
            InitializeComponent();

            util = new Util();
            console = new ConsoleWindow(util);

            // 通信APIの初期化
            httpClient = new HttpClient();
            listener   = new SWHTTPListener(httpClient, util);

            util.ConsoleStreamEvent += ConsoleStreamEventCallback;
        }

        // MainWindowのウィンドウハンドル取得
        public IntPtr Handle
        {
            get
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(this);
                return helper.Handle;
            }
        }
        private void ConsoleStreamEventCallback(object sender, Util.ConsoleStreamEventArgs e)
        {
            //logall += (e.Log + Environment.NewLine);
            //console.Text = logall;
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            ButtonStart.IsEnabled = false;
            ButtonStop.IsEnabled  = true;

            listener.Start("localhost", 43000);
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            ButtonStart.IsEnabled = true;
            ButtonStop.IsEnabled = false;

            listener.Stop();

        }

        private void ShowConsole_Click(object sender, RoutedEventArgs e)
        {
            console.Show();
        }

        private void WindowBase_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
