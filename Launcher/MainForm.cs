using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.Net.Http;
using System.ServiceProcess;

namespace AutoDealerSphere.Launcher
{
    public class MainForm : Form
    {
        private const string AppUrl = "http://localhost:5259";
        private const string ServiceName = "AutoDealerSphere";

        private Microsoft.Web.WebView2.WinForms.WebView2 _webView = null!;
        private System.Windows.Forms.Timer _retryTimer = null!;
        private int _retryCount = 0;
        private const int MaxRetries = 40; // 40回 × 500ms = 20秒

        public MainForm()
        {
            InitializeComponent();
            StartService();
        }

        private void InitializeComponent()
        {
            Text = "AutoDealerSphere";
            Width = 1280;
            Height = 800;
            MinimumSize = new Size(1024, 600);
            StartPosition = FormStartPosition.CenterScreen;

            _webView = new Microsoft.Web.WebView2.WinForms.WebView2
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(_webView);

            _retryTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _retryTimer.Tick += RetryTimer_Tick;

            FormClosing += MainForm_FormClosing;
        }

        private void StartService()
        {
            try
            {
                using var sc = new ServiceController(ServiceName);
                var status = sc.Status;
                Debug.WriteLine($"[LAUNCHER] サービス状態: {status}");

                if (status == ServiceControllerStatus.Stopped ||
                    status == ServiceControllerStatus.Paused)
                {
                    sc.Start();
                    Debug.WriteLine("[LAUNCHER] サービス開始命令を送信");
                }
                // Running / StartPending の場合はそのまま待機
            }
            catch (InvalidOperationException)
            {
                // サービスが未登録（開発時など）→ 既に起動済みのServerに直接接続を試みる
                Debug.WriteLine("[LAUNCHER] サービス未登録。直接接続を試みます。");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LAUNCHER] サービス起動エラー: {ex.Message}");
            }

            Text = "AutoDealerSphere - 起動中...";
            _retryTimer.Start();
        }

        private async void RetryTimer_Tick(object? sender, EventArgs e)
        {
            _retryCount++;
            Debug.WriteLine($"[LAUNCHER] 接続試行 {_retryCount}/{MaxRetries}");

            if (_retryCount > MaxRetries)
            {
                _retryTimer.Stop();
                Debug.WriteLine("[LAUNCHER ERROR] タイムアウト");
                // タイムアウト時はエラーページを表示
                await InitializeWebViewAsync(showError: true);
                return;
            }

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                await http.GetAsync(AppUrl);
                _retryTimer.Stop();
                Debug.WriteLine("[LAUNCHER] サーバー応答確認 → WebView2初期化");
                await InitializeWebViewAsync();
            }
            catch
            {
                // まだ起動中
            }
        }

        private async Task InitializeWebViewAsync(bool showError = false)
        {
            try
            {
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AutoDealerSphere");

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await _webView.EnsureCoreWebView2Async(env);

                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;

                _webView.CoreWebView2.NewWindowRequested += (s, args) =>
                {
                    args.Handled = true;
                    Process.Start(new ProcessStartInfo(args.Uri) { UseShellExecute = true });
                };

                Text = "AutoDealerSphere";

                if (showError)
                {
                    _webView.CoreWebView2.NavigateToString(@"
                        <html><body style='font-family:sans-serif;padding:2rem;'>
                        <h2>起動できませんでした</h2>
                        <p>サービスが起動しているか確認してください。</p>
                        <p>Windowsのサービス管理画面で「AutoDealerSphere」が実行中になっているか確認してください。</p>
                        </body></html>");
                }
                else
                {
                    _webView.Source = new Uri(AppUrl);
                }

                Debug.WriteLine("[LAUNCHER] WebView2初期化完了");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LAUNCHER ERROR] WebView2初期化失敗: {ex}");
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // サービスはWindowsが管理するため、Launcher終了時は何もしない
            // （サービスはPC起動中ずっと動き続ける）
        }
    }
}
