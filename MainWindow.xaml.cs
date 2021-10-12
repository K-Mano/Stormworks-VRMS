using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Fluent;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Stormworks_VRMS
{
    public partial class MainWindow : Window
    {
        private Util util;
        private ConsoleWindow console;

        private SWHTTPListener listener;
        private HttpClient httpClient;

        private string logall = "";
        private List<VehicleListItem> vehicle;

        public struct VehicleListItem
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string UUID { get; set; }
        };
        public MainWindow()
        {
            InitializeComponent();

            util       = new Util();
            console    = new ConsoleWindow(util);

            // 通信APIの初期化
            httpClient = new HttpClient();
            listener   = new SWHTTPListener(httpClient, util);

            vehicle = new List<VehicleListItem>();
            VehicleListView.ItemsSource = vehicle;

            util.ConsoleStreamEvent += ConsoleStreamEventCallback;
            listener.RequestReceivedEvent += RequestReceivedEventCallback;
        }

        private void RequestReceivedEventCallback(object sender, SWHTTPListener.RequestReceivedEventArgs e)
        {
            NameValueCollection query = e.Request.QueryString;
            if (query["uuid"] != null && query["name"] != null)
            {
                util.ConsoleWriteDetails(string.Format("Waiting Request User Prompt [Thread ID: {0}]", Thread.CurrentThread.ManagedThreadId), Util.ConsoleEventLevel.INFO);
                Dispatcher.Invoke(() =>
                {
                    TaskDialog dialog = new TaskDialog();

                    dialog.OwnerWindowHandle = Handle;
                    dialog.Caption = "Stormworks VRMS™ アクセス要求";
                    dialog.InstructionText = "StormworksがVRMSへのアクセスを要求しています";
                    dialog.Text = "リクエストの発行元がわかっている場合を除き、許可しないことをおすすめします。";
                    dialog.Icon = TaskDialogStandardIcon.Shield;
                    dialog.DetailsCollapsedLabel = "詳細情報";
                    dialog.DetailsExpandedLabel = "詳細情報を非表示";
                    dialog.DetailsExpandedText = string.Format("機体名: {0}\n固有機体ID: {1}\nUUID: {2}", query["name"], query["id"], query["uuid"]);

                    dialog.ExpansionMode = TaskDialogExpandedDetailsLocation.ExpandContent;
                    dialog.HyperlinksEnabled = true;
                    dialog.FooterIcon = TaskDialogStandardIcon.Information;
                    dialog.FooterText = "Stormworksによるアクセスとアプリケーションの動作については、<a href=\"link2\">マニュアル</a>を参照してください。";

                    dialog.DetailsExpanded = true;


                    var deny = new TaskDialogCommandLink("deny", "キャンセル", "このリクエストの発行元が不明であり、リストへの追加を拒否します。");
                    deny.Click += (s1, e1) => {
                        util.ConsoleWriteDetails(string.Format("Access denied by User [Thread ID: {0}]", Thread.CurrentThread.ManagedThreadId), Util.ConsoleEventLevel.INFO);
                        dialog.Close();
                    };
                    deny.Default = true;
                    dialog.Controls.Add(deny);

                    var accept = new TaskDialogCommandLink("accept", "許可(&A)", "このリクエストを信用します。発行元がわかっているか、リクエストを自分で発行しました。");
                    accept.UseElevationIcon = true;
                    accept.Click += (s2, e2) => {
                        util.ConsoleWriteDetails(string.Format("Access accepted by User [Thread ID: {0}]", Thread.CurrentThread.ManagedThreadId), Util.ConsoleEventLevel.INFO);
                        vehicle.Add(new VehicleListItem { Id = query["id"], Name = query["name"], UUID = query["uuid"] });
                        VehicleListView.Items.Refresh();
                        dialog.Close();
                    };
                    dialog.Controls.Add(accept);

                    dialog.Show();
                });
            }
        }

        // MainWindowのウィンドウハンドル取得
        public IntPtr Handle
        {
            get
            {
               WindowInteropHelper helper = new WindowInteropHelper(this);
                return helper.Handle;
            }
        }
        private void ConsoleStreamEventCallback(object sender, Util.ConsoleStreamEventArgs e)
        {
            logall += e.Log + Environment.NewLine;
            Dispatcher.Invoke(() =>
            {
                ConsoleTextBox.Text = logall;
                ConsoleTextBox.ScrollToEnd();
            });
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            ButtonStart.IsEnabled = false;
            ButtonStop.IsEnabled  = true;

            string ip = "localhost";
            int port = 43000;

            Title = string.Format("Stormworks VRMS™ - 稼働中({0}:{1})", ip, port);

            listener.Start(ip, port);
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            ButtonStart.IsEnabled = true;
            ButtonStop.IsEnabled = false;

            Title = "Stormworks VRMS™ - 準備完了";

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

        private void ConsoleTopmost_Unchecked(object sender, RoutedEventArgs e)
        {
            console.Topmost = false;
        }

        private void ConsoleTopmost_Checked(object sender, RoutedEventArgs e)
        {
            console.Topmost = true;
        }
    }
}
