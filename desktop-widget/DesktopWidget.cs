using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace DesktopBottleFlip
{
    public class WidgetForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int HTCAPTION = 0x0002;
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9527;
        private const uint MOD_ALT = 0x0001;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        private WebView2 webView;
        private NotifyIcon trayIcon;
        private ToolStripMenuItem pinItem;
        private string htmlPath;
        private bool shownTrayTip = false;

        public WidgetForm(string targetHtmlPath)
        {
            this.htmlPath = targetHtmlPath;
            this.Text = "摸鱼立水瓶 — 桌面解压小挂件";
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.BackColor = Color.Black;

            // Scale default size cleanly for high-DPI screens
            float dpiScale = 1.0f;
            using (Graphics g = this.CreateGraphics())
            {
                dpiScale = g.DpiX / 96.0f;
            }
            int w = (int)Math.Round(370 * dpiScale);
            int h = (int)Math.Round(490 * dpiScale);
            this.Size = new Size(w, h);
            MoveToBottomRight();

            Icon appIcon = CreateBottleIcon();
            this.Icon = appIcon;
            BuildTrayMenu(appIcon);
            InitWebViewAsync();

            try { RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT, (uint)Keys.B); } catch { }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int pref = DWMWCP_ROUND;
                DwmSetWindowAttribute(this.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
                MARGINS margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
                DwmExtendFrameIntoClientArea(this.Handle, ref margins);
            }
            catch { }
        }

        private void MoveToBottomRight()
        {
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(wa.Right - this.Width - 18, wa.Bottom - this.Height - 14);
        }

        private async void InitWebViewAsync()
        {
            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            webView.DefaultBackgroundColor = Color.Transparent;
            this.Controls.Add(webView);

            string userDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DesktopBottleFlipWidget"
            );
            CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, userDataDir);
            await webView.EnsureCoreWebView2Async(env);

            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            webView.CoreWebView2.WebMessageReceived += OnWebMessage;

            string fileUri = new Uri(htmlPath).AbsoluteUri + "?widget=1";
            webView.CoreWebView2.Navigate(fileUri);
        }

        private void OnWebMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string msg = "";
            try { msg = e.TryGetWebMessageAsString(); } catch { return; }

            if (msg == "drag")
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
            else if (msg == "pin:1")
            {
                this.TopMost = true;
                if (pinItem != null) pinItem.Checked = true;
            }
            else if (msg == "pin:0")
            {
                this.TopMost = false;
                if (pinItem != null) pinItem.Checked = false;
            }
            else if (msg == "hide")
            {
                HideToTray();
            }
            else if (msg == "close")
            {
                ExitApp();
            }
        }

        private void HideToTray()
        {
            this.Hide();
            if (!shownTrayTip && trayIcon != null)
            {
                shownTrayTip = true;
                trayIcon.ShowBalloonTip(2000, "摸鱼立水瓶已最小化", "累了随时点托盘图标或按 Alt + B 呼出扔两把！", ToolTipIcon.Info);
            }
        }

        private void ToggleVisible()
        {
            if (this.Visible)
            {
                this.Hide();
            }
            else
            {
                this.Show();
                this.Activate();
            }
        }

        private void ResizePreset(int baseW, int baseH)
        {
            float dpiScale = 1.0f;
            using (Graphics g = this.CreateGraphics()) { dpiScale = g.DpiX / 96.0f; }
            this.Size = new Size((int)Math.Round(baseW * dpiScale), (int)Math.Round(baseH * dpiScale));
            MoveToBottomRight();
            if (!this.Visible) this.Show();
        }

        private void BuildTrayMenu(Icon appIcon)
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem showItem = new ToolStripMenuItem("🧴 显示 / 隐藏挂件 (Alt+B)");
            showItem.Click += delegate { ToggleVisible(); };
            menu.Items.Add(showItem);

            pinItem = new ToolStripMenuItem("📌 始终置顶");
            pinItem.Checked = true;
            pinItem.Click += delegate
            {
                this.TopMost = !this.TopMost;
                pinItem.Checked = this.TopMost;
            };
            menu.Items.Add(pinItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem szSmall = new ToolStripMenuItem("📐 迷你尺寸 (310×420)");
            szSmall.Click += delegate { ResizePreset(310, 420); };
            menu.Items.Add(szSmall);

            ToolStripMenuItem szNorm = new ToolStripMenuItem("📐 标准尺寸 (370×490)");
            szNorm.Click += delegate { ResizePreset(370, 490); };
            menu.Items.Add(szNorm);

            ToolStripMenuItem szLarge = new ToolStripMenuItem("📐 宽屏桌台 (480×600)");
            szLarge.Click += delegate { ResizePreset(480, 600); };
            menu.Items.Add(szLarge);

            ToolStripMenuItem resetPos = new ToolStripMenuItem("📍 归位屏幕右下角");
            resetPos.Click += delegate { MoveToBottomRight(); if (!this.Visible) this.Show(); };
            menu.Items.Add(resetPos);

            ToolStripMenuItem openFull = new ToolStripMenuItem("🌐 浏览器打开全屏闯关版");
            openFull.Click += delegate { try { Process.Start(new Uri(htmlPath).AbsoluteUri); } catch { } };
            menu.Items.Add(openFull);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("❌ 退出挂件");
            exitItem.Click += delegate { ExitApp(); };
            menu.Items.Add(exitItem);

            trayIcon = new NotifyIcon();
            trayIcon.Icon = appIcon;
            trayIcon.Text = "摸鱼立水瓶 (左键隐藏/呼出 · Alt+B)";
            trayIcon.ContextMenuStrip = menu;
            trayIcon.Visible = true;
            trayIcon.MouseClick += delegate(object s, MouseEventArgs me)
            {
                if (me.Button == MouseButtons.Left) ToggleVisible();
            };
        }

        private Icon CreateBottleIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                // Bottle body
                using (Brush bodyBr = new SolidBrush(Color.FromArgb(235, 225, 245, 255)))
                {
                    g.FillRectangle(bodyBr, 10, 9, 12, 20);
                }
                // Blue water in lower half
                using (Brush waterBr = new SolidBrush(Color.FromArgb(230, 56, 189, 248)))
                {
                    g.FillRectangle(waterBr, 11, 18, 10, 10);
                }
                // Red Nongfu-style label & cap
                using (Brush redBr = new SolidBrush(Color.FromArgb(255, 220, 38, 38)))
                {
                    g.FillRectangle(redBr, 10, 12, 12, 5);
                    g.FillRectangle(redBr, 12, 3, 8, 5);
                }
                using (Pen borderPen = new Pen(Color.FromArgb(220, 30, 41, 59), 1.5f))
                {
                    g.DrawRectangle(borderPen, 10, 9, 12, 20);
                }
            }
            return Icon.FromHandle(bmp.GetHicon());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                ToggleVisible();
                return;
            }
            base.WndProc(ref m);
        }

        private void ExitApp()
        {
            try { UnregisterHotKey(this.Handle, HOTKEY_ID); } catch { }
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            Application.Exit();
        }

        [STAThread]
        public static void Main(string[] args)
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string candidate1 = Path.Combine(baseDir, "index.html");
            string candidate2 = Path.GetFullPath(Path.Combine(baseDir, "..\\index.html"));
            string target = File.Exists(candidate1) ? candidate1 : candidate2;

            if (!File.Exists(target))
            {
                MessageBox.Show("未找到 index.html: " + candidate1, "摸鱼立水瓶", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new WidgetForm(target));
        }
    }
}
