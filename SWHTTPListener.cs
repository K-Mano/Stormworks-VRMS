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

        CancellationTokenSource tokenSource;
        CancellationToken cancellation;

        private string ADDRESS;
        private int PORT;

        public SWHTTPListener(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public void Stop()
        {
            Console.WriteLine("Stopping Server.");

            // ダミーリクエストを送信
            httpClient.GetAsync(string.Format(@"http://{0}:{1}/", ADDRESS, PORT));
            // キャンセルトークンを使用
            tokenSource.Cancel();
        }

        public async void Start(string address, int port)
        {
            Console.WriteLine("Initialization Server Process [Thread ID: {0}]", Thread.CurrentThread.ManagedThreadId);

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
                Console.WriteLine("Starting Server on {0}:{1}.", address, port);
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
                                _response.StatusCode = 200;

                                break;
                            }

                            HttpListenerRequest request = context.Request;
                            Console.WriteLine("Request received ({0}) [Thread ID: {1}]", request.Url, Thread.CurrentThread.ManagedThreadId);

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
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                });

                // リスナーを閉じて終了
                listener.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public enum EventLevel
        {
            INFO = 0,
            WARN = 1,
            ERROR = 2,
            CRITICAL = 3
        }
    }
}
