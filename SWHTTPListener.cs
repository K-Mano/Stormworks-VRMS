using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stormworks_VRMS
{
    class SWHTTPListener
    {
        private HttpClient httpClient;
        private Util util;

        CancellationTokenSource tokenSource = null;
        CancellationToken cancellation;

        private string ADDRESS;
        private int PORT;

        public SWHTTPListener(HttpClient httpClient, Util util)
        {
            this.httpClient = httpClient;
            this.util = util;
        }
        public bool isServerRunning()
        {
            if (tokenSource == null)
            {
                return false;
            }
            return true;
        }
        public void Stop()
        {
            util.ConsoleWriteDetails("The server will be shutdown.", Util.ConsoleEventLevel.INFO);

            // キャンセルトークンを使用
            tokenSource.Cancel();
            // ダミーリクエストを送信
            httpClient.GetAsync(string.Format(@"http://{0}:{1}/", ADDRESS, PORT));
            // トークンソースを解放
            tokenSource.Dispose();
        }

        public async void Start(string address, int port)
        {
            util.ConsoleWriteDetails(string.Format("Initialization Server Process [Thread ID: {0}]", Thread.CurrentThread.ManagedThreadId), Util.ConsoleEventLevel.INFO);

            ADDRESS = address;
            PORT = port;

            try
            {
                // HTTPリスナー作成
                HttpListener listener = new HttpListener();

                // リスナー設定
                listener.Prefixes.Clear();
                listener.Prefixes.Add(string.Format(@"http://{0}:{1}/", address, port));

                // リスナー開始
                util.ConsoleWriteDetails(string.Format("Starting Server on {0}:{1}.", address, port), Util.ConsoleEventLevel.INFO);
                listener.Start();

                // キャンセル時のトークンを取得
                tokenSource = new CancellationTokenSource();
                cancellation = tokenSource.Token;

                await Task.Run(() =>
                {
                    while (!cancellation.IsCancellationRequested)
                    {
                        try
                        {
                            // リクエスト取得
                            HttpListenerContext context = listener.GetContext();

                            // キャンセル要求時にダミーリクエストを処理して終了
                            if (cancellation.IsCancellationRequested)
                            {
                                HttpListenerResponse _response = context.Response;
                                _response.StatusCode = 404;
                                _response.Close();

                                break;
                            }

                            HttpListenerRequest request = context.Request;
                            util.ConsoleWriteDetails(string.Format("Request received ({0}) [Thread ID: {1}]", request.Url, Thread.CurrentThread.ManagedThreadId), Util.ConsoleEventLevel.INFO);

                            Task.Run(() => {
                                util.ConsoleWriteDetails(string.Format("Request analysing ({0}) [Thread ID: {1}]", request.RawUrl, Thread.CurrentThread.ManagedThreadId), Util.ConsoleEventLevel.INFO);

                                // レスポンス取得
                                HttpListenerResponse response = context.Response;

                                // HTMLを表示する
                                if (request != null)
                                {
                                    // レスポンス送信
                                    byte[] text = Encoding.UTF8.GetBytes("<html><head><meta charset='utf-8'/></head><body>Test Response</body></html>");
                                    response.OutputStream.Write(text, 0, text.Length);
                                    response.StatusCode = 200;
                                }
                                else
                                {
                                    // レスポンス送信
                                    response.StatusCode = 404;
                                }
                                response.Close();
                            });
                        }
                        catch (Exception e)
                        {
                            util.ConsoleWriteDetails(e.Message, Util.ConsoleEventLevel.ERROR);
                        }
                    }
                });

                // リスナーを閉じて終了
                listener.Abort();
            }
            catch (Exception e)
            {
                util.ConsoleWriteDetails(e.Message, Util.ConsoleEventLevel.ERROR);
            }
        }
    }
}
