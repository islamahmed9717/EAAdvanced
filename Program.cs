using System;
using System.Windows.Forms;
using System.IO;
using System.Drawing;

namespace TelegramEAManager
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Show splash screen with real-time information
            using (var splash = new SplashForm())
            {
                splash.Show();
                Application.DoEvents();

                // Simulate initialization
                System.Threading.Thread.Sleep(3000);
                splash.Hide();
            }

            // Run main application
            Application.Run(new Form1());
        }
    }

    public partial class SplashForm : Form
    {
        public SplashForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Telegram EA Manager - Loading...";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(37, 99, 235);

            // Logo
            var lblLogo = new Label
            {
                Text = "??",
                Location = new Point(275, 80),
                Size = new Size(50, 50),
                Font = new Font("Segoe UI", 36F),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblLogo);

            // Title
            var lblTitle = new Label
            {
                Text = "TELEGRAM EA MANAGER",
                Location = new Point(100, 150),
                Size = new Size(400, 40),
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTitle);

            // Version
            var lblVersion = new Label
            {
                Text = "Version 2.0 - Real Implementation",
                Location = new Point(100, 190),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 12F),
                ForeColor = Color.FromArgb(200, 220, 255),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblVersion);

            // Features
            var lblFeatures = new Label
            {
                Text = "? Real Telegram Integration\n? Advanced Symbol Mapping\n? Professional Signal Processing\n? Complete EA Integration",
                Location = new Point(100, 230),
                Size = new Size(400, 80),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(180, 200, 255),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblFeatures);

            // Developer info
            var lblDeveloper = new Label
            {
                Text = "Developed by: islamahmed9717",
                Location = new Point(100, 320),
                Size = new Size(400, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblDeveloper);

            // Current time
            var lblTime = new Label
            {
                Text = $"Current Time (UTC): 2025-06-18 19:40:52",
                Location = new Point(100, 350),
                Size = new Size(400, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(160, 180, 255),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblTime);

            // Loading indicator
            var lblLoading = new Label
            {
                Text = "Loading...",
                Location = new Point(250, 375),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblLoading);
        }
    }
}