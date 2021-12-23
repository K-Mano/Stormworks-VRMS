using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
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
using System.Windows.Threading;
using Fluent;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Stormworks_VRMS
{
    public partial class MainWindow : Window
    {
        // クラスのインスタンス作成
        private Util util;
        private ConsoleWindow console;
        private SWHTTPListener listener;
        private HttpClient httpClient;

        private string logall = "";
        private List<VehicleListItem> vehicle;

        private DispatcherTimer timer;

        private TaskDialog dialog;

        public struct VehicleListItem
        {
            /// <summary>
            /// 固有ID
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            /// 機体名称
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// 機体共通のUUID
            /// </summary>
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

            // 車両リストの初期化
            vehicle = new List<VehicleListItem>();
            VehicleListView.ItemsSource = vehicle;

            // イベントハンドラの登録
            util.ConsoleStreamEvent += ConsoleStreamEventCallback;
            listener.RequestReceivedEvent += RequestReceivedEventCallback;

            // ステータスをウィンドウタイトルに設定
            SetTitle(string.Format("準備完了"));
        }

        private void RequestReceivedEventCallback(object sender, SWHTTPListener.RequestReceivedEventArgs e)
        {
            // クエリを取得
            NameValueCollection query = e.Request.QueryString;

            // nameキーとuuidキーの存在を確認
            if (query["uuid"] != null && query["name"] != null)
            {
                util.ConsoleWriteDetails(string.Format("Waiting Request User Prompt [Thread ID: {0}]", Thread.CurrentThread.ManagedThreadId), Util.ConsoleEventLevel.INFO);
                Dispatcher.Invoke(() =>
                {
                    // タイマーの設定
                    timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(1.0);

                    // タイマを開始
                    timer.Start();

                    dialog = new TaskDialog();

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

                    var remain = new TaskDialogProgressBar();
                    remain.Value   = 0;
                    remain.Maximum = 5;

                    dialog.Controls.Add(remain);

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
                        // リストビューを更新
                        VehicleListView.Items.Refresh();
                        dialog.Close();
                    };
                    dialog.Controls.Add(accept);

                    dialog.Closing += (senderc, ec) => {
                        timer.Stop();
                    };

                    timer.Tick += (senders, es) =>
                    {
                        if (remain.Value < 5)
                        {
                            remain.Value += 1;
                        }
                        else 
                        {
                            dialog.Close();
                        };
                    };

                    dialog.Show();
                });
                dialog.Dispose();
            }
        }

        /// <summary>
        /// MainWindowのウィンドウハンドル
        /// </summary>
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
                // ログを更新, 最後尾までスクロール
                ConsoleTextBox.Text = logall;
                ConsoleTextBox.ScrollToEnd();
            });
        }

        private void SetTitle(string title)
        {
            // ウィンドウタイトルのフォーマット
            Title = string.Format("Stormworks VRMS™ Beta - {0}", title);
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            // ボタン表示の切り替え
            ButtonStart.IsEnabled = false;
            ButtonStop.IsEnabled  = true;

            // アドレス情報を登録
            string ip = "localhost";
            int port = 43000;

            // ウィンドウタイトルにステータスを設定
            SetTitle(string.Format("稼働中({0}:{1})", ip, port));

            listener.Start(ip, port);
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            // ボタン表示の切り替え
            ButtonStart.IsEnabled = true;
            ButtonStop.IsEnabled = false;

            // ウィンドウタイトルにステータスを設定
            SetTitle("準備完了");

            listener.Stop();

        }

        private void ShowConsole_Click(object sender, RoutedEventArgs e)
        {
            // コンソールウィンドウの表示
            console.Show();
        }

        private void WindowBase_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // アプリケーションの終了
            Application.Current.Shutdown();
        }

        private void ConsoleTopmost_Unchecked(object sender, RoutedEventArgs e)
        {
            // 通常の優先度モード
            console.Topmost = false;
        }

        private void ConsoleTopmost_Checked(object sender, RoutedEventArgs e)
        {
            // 最前面に表示
            console.Topmost = true;
        }

        private void VehicleListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // 選択オブジェクトの情報をUIに登録
                TargetName.Text = vehicle[VehicleListView.SelectedIndex].Name;
                TargetUUID.Text = vehicle[VehicleListView.SelectedIndex].UUID;
                TargetID.Text   = vehicle[VehicleListView.SelectedIndex].Id;
            }
            catch (ArgumentOutOfRangeException)
            {
                // 表示をリセット
                TargetName.Text = "-";
                TargetUUID.Text = "-";
                TargetID.Text   = "-";
            }
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 選択オブジェクトをリストから削除
                vehicle.RemoveAt(VehicleListView.SelectedIndex);
                // リストビューを更新
                VehicleListView.Items.Refresh();
            }
            catch (ArgumentOutOfRangeException)
            {
                // リストビューを更新
                VehicleListView.Items.Refresh();
            }
        }
    }
}
