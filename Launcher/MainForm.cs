using System.Diagnostics;
using System.Net.Http;
using System.ServiceProcess;
using System.Text.Json;

namespace AutoDealerSphere.Launcher
{
    public class MainForm : Form
    {
        private const string AppUrl = "http://localhost:5259";
        private const string ServiceName = "AutoDealerSphere";

        private Microsoft.Web.WebView2.WinForms.WebView2 _webView = null!;
        private System.Windows.Forms.Timer _retryTimer = null!;
        private int _retryCount = 0;
        private bool _navigated = false;
        private const int MaxRetries = 40;
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutoDealerSphere", "window.json");

        public MainForm()
        {
            InitializeComponent();
            StartService();
        }

        private void InitializeComponent()
        {
            Text = "AutoDealerSphere - 起動中...";
            MinimumSize = new Size(1024, 600);
            RestoreWindowSettings();

            _webView = new Microsoft.Web.WebView2.WinForms.WebView2
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(_webView);

            _retryTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _retryTimer.Tick += RetryTimer_Tick;

            FormClosing += (s, e) => SaveWindowSettings();
        }

        private void StartService()
        {
            try
            {
                using var sc = new ServiceController(ServiceName);
                if (sc.Status == ServiceControllerStatus.Stopped ||
                    sc.Status == ServiceControllerStatus.Paused)
                    sc.Start();
            }
            catch { }

            _retryTimer.Start();
        }

        private void RetryTimer_Tick(object? sender, EventArgs e)
        {
            if (_navigated) return;
            _retryCount++;

            if (_retryCount > MaxRetries)
            {
                _retryTimer.Stop();
                _navigated = true;
                ShowError("サーバーへの接続がタイムアウトしました。\nサービスが起動しているか確認してください。");
                return;
            }

            // HTTP チェックはバックグラウンドスレッドで行い、結果だけ UI スレッドに返す
            Thread thread = new Thread(() =>
            {
                bool ok = false;
                try
                {
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
                    var res = http.GetAsync(AppUrl).GetAwaiter().GetResult();
                    ok = res.IsSuccessStatusCode;
                }
                catch { }

                if (ok)
                {
                    Invoke(() =>
                    {
                        if (_navigated) return;
                        _retryTimer.Stop();
                        _navigated = true;
                        NavigateToApp();
                    });
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void NavigateToApp()
        {
            _webView.CoreWebView2InitializationCompleted += (s, e) =>
            {
                if (!e.IsSuccess)
                {
                    ShowError($"WebView2初期化失敗:\n{e.InitializationException}");
                    return;
                }
                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                _webView.CoreWebView2.NewWindowRequested += (s, args) =>
                {
                    args.Handled = true;
                    Process.Start(new ProcessStartInfo(args.Uri) { UseShellExecute = true });
                };
                _webView.CoreWebView2.NavigationCompleted += (s, args) =>
                {
                    var uri = _webView.Source?.ToString() ?? "";
                    if (!uri.StartsWith(AppUrl)) return;
                    Text = args.IsSuccess ? "AutoDealerSphere" : $"AutoDealerSphere - エラー (0x{args.WebErrorStatus:X})";
                };
                _webView.CoreWebView2.Navigate(AppUrl);
            };

            _webView.Source = new Uri(AppUrl);
        }

        private void RestoreWindowSettings()
        {
            WindowSettings? s = null;

            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                s = JsonSerializer.Deserialize<WindowSettings>(json);
            }

            if (s != null)
            {
                StartPosition = FormStartPosition.Manual;
                Bounds = new Rectangle(s.X, s.Y, s.Width, s.Height);
                if (s.Maximized) WindowState = FormWindowState.Maximized;
            }
            else
            {
                Width = 1280;
                Height = 800;
                StartPosition = FormStartPosition.CenterScreen;
            }
        }

        private void SaveWindowSettings()
        {
            try
            {
                var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                var s = new WindowSettings
                {
                    X = bounds.X,
                    Y = bounds.Y,
                    Width = bounds.Width,
                    Height = bounds.Height,
                    Maximized = WindowState == FormWindowState.Maximized
                };
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(s));
            }
            catch { }
        }

        private void ShowError(string message)
        {
            var textBox = new TextBox
            {
                Text = message,
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9)
            };
            Controls.Remove(_webView);
            Controls.Add(textBox);
            Text = "AutoDealerSphere - エラー";
        }
    }

    record WindowSettings
    {
        public int X { get; init; }
        public int Y { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public bool Maximized { get; init; }
    }
}
