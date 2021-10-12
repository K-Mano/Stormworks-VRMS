using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Stormworks_VRMS
{
    public partial class ConsoleWindow : Window
    {
        private Util util;
        private string logall = "";
        delegate void DelegateProcess();

        public ConsoleWindow(Util util)
        {
            InitializeComponent();
            this.util = util;

            util.ConsoleStreamEvent += ConsoleStreamEventCallback;
            Init();
        }

        private void WindowConsole_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void Init()
        {
            foreach (string line in util.GetConsoleLogList())
            {
                logall += line + Environment.NewLine;
            }
            console.Text = logall;
        }

        private void ConsoleStreamEventCallback(object sender, Util.ConsoleStreamEventArgs e)
        {
            logall += e.Log + Environment.NewLine;
            Dispatcher.Invoke(() =>
            {
                console.Text = logall;
                console.ScrollToEnd();
            });
        }
    }
}
