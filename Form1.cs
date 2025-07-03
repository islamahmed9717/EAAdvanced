using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TelegramEAManager
{
    public partial class Form1 : Form
    {
        #region Private Fields
        private TelegramService telegramService = null!;
        private SignalProcessingService signalProcessor = null!;
        private List<ChannelInfo> allChannels = new List<ChannelInfo>();
        private List<ChannelInfo> selectedChannels = new List<ChannelInfo>();
        private bool isMonitoring = false;
        private System.Windows.Forms.Timer uiUpdateTimer = null!;
        private List<ProcessedSignal> allSignals = new List<ProcessedSignal>();
        private FileSystemWatcher? signalFileWatcher;
        private System.Windows.Forms.Timer? cleanupTimer;
        private readonly HashSet<string> liveFeedIds = new HashSet<string>();

        private Form? debugForm = null;
        private bool isDebugFormOpen = false;
        private TextBox? debugConsole = null;
        private readonly object debugLock = new object();


        private readonly ConcurrentDictionary<string, DateTime> recentSignalsInUI = new ConcurrentDictionary<string, DateTime>();
        private readonly SemaphoreSlim uiUpdateSemaphore = new SemaphoreSlim(1, 1);
        private System.Threading.Timer? uiCleanupTimer;

        #endregion

        public Form1()
        {
            InitializeComponent();
            InitializeServices();
            SetupUI();
            LoadApplicationSettings();
            SetupTimers();
        }
        private void StartAutoCleanup()
        {
            // Stop existing timer if any
            cleanupTimer?.Stop();
            cleanupTimer?.Dispose();

            // Create new cleanup timer (runs every 5 minutes)
            cleanupTimer = new System.Windows.Forms.Timer
            {
                Interval = 300000 // 5 minutes
            };

            cleanupTimer.Tick += (s, e) =>
            {
                try
                {
                    signalProcessor.CleanupProcessedSignals();
                    LogMessage("🧹 Auto-cleanup completed - removed old/processed signals");
                }
                catch (Exception ex)
                {
                    LogMessage($"❌ Auto-cleanup error: {ex.Message}");
                }
            };

            cleanupTimer.Start();
            LogMessage("🧹 Auto-cleanup started (runs every 5 minutes)");
        }
        private void CreateDebugConsole()
        {
            try
            {
                // Close existing debug form if open
                if (debugForm != null && !debugForm.IsDisposed)
                {
                    debugForm.Close();
                    debugForm.Dispose();
                    debugForm = null;
                }

                // Create a new debug window
                debugForm = new Form
                {
                    Text = "🐛 Debug Console - Telegram EA Manager - islamahmed9717",
                    Size = new Size(1000, 600),
                    StartPosition = FormStartPosition.Manual,
                    FormBorderStyle = FormBorderStyle.Sizable,
                    MinimizeBox = true,
                    MaximizeBox = true,
                    ShowInTaskbar = true,
                    BackColor = Color.Black,
                    Icon = this.Icon // Use main form's icon if available
                };

                // Position next to main form
                debugForm.Location = new Point(this.Location.X + this.Width + 10, this.Location.Y);

                // Create main panel
                var mainPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(5)
                };
                debugForm.Controls.Add(mainPanel);

                // Create header panel
                var headerPanel = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 40,
                    BackColor = Color.FromArgb(37, 99, 235)
                };
                mainPanel.Controls.Add(headerPanel);

                var lblHeader = new Label
                {
                    Text = $"🐛 REAL-TIME DEBUG CONSOLE | Started: {DateTime.Now:HH:mm:ss} | User: islamahmed9717",
                    Dock = DockStyle.Fill,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(10, 0, 0, 0)
                };
                headerPanel.Controls.Add(lblHeader);

                // Create debug text area
                debugConsole = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ScrollBars = ScrollBars.Both,
                    BackColor = Color.Black,
                    ForeColor = Color.Lime,
                    Font = new Font("Consolas", 9F),
                    ReadOnly = true,
                    WordWrap = false,
                    Margin = new Padding(5)
                };
                mainPanel.Controls.Add(debugConsole);

                // Create button panel
                var buttonPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    BackColor = Color.FromArgb(30, 30, 30)
                };
                mainPanel.Controls.Add(buttonPanel);

                // Clear button
                var btnClear = new Button
                {
                    Text = "🗑️ Clear",
                    Location = new Point(10, 10),
                    Size = new Size(80, 30),
                    BackColor = Color.FromArgb(220, 38, 38),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F)
                };
                btnClear.Click += (s, e) => {
                    if (debugConsole != null && !debugConsole.IsDisposed)
                    {
                        debugConsole.Clear();
                        LogDebugMessage("🗑️ Debug console cleared");
                    }
                };
                buttonPanel.Controls.Add(btnClear);

                // Save log button
                var btnSave = new Button
                {
                    Text = "💾 Save Log",
                    Location = new Point(100, 10),
                    Size = new Size(90, 30),
                    BackColor = Color.FromArgb(34, 197, 94),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F)
                };
                btnSave.Click += (s, e) => SaveDebugLog();
                buttonPanel.Controls.Add(btnSave);

                // Auto-scroll checkbox
                var chkAutoScroll = new CheckBox
                {
                    Text = "Auto-scroll",
                    Location = new Point(200, 15),
                    Size = new Size(100, 20),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9F),
                    Checked = true
                };
                buttonPanel.Controls.Add(chkAutoScroll);

                // Status label
                var lblStatus = new Label
                {
                    Text = "Debug console ready...",
                    Location = new Point(320, 15),
                    Size = new Size(400, 20),
                    ForeColor = Color.Gray,
                    Font = new Font("Segoe UI", 8F)
                };
                buttonPanel.Controls.Add(lblStatus);

                // Handle form closing
                debugForm.FormClosing += (s, e) => {
                    isDebugFormOpen = false;
                    debugConsole = null;
                    debugForm = null;
                };

                // Show the form
                debugForm.Show();
                isDebugFormOpen = true;

                // Initial message
                LogDebugMessage("🚀 DEBUG CONSOLE STARTED");
                LogDebugMessage($"📅 Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                LogDebugMessage($"👤 User: islamahmed9717");
                LogDebugMessage($"🔗 Connected to Telegram: {(telegramService?.IsUserAuthorized() ?? false)}");
                LogDebugMessage($"📊 Monitoring: {(isMonitoring ? "ACTIVE" : "STOPPED")}");
                LogDebugMessage("═══════════════════════════════════════");

                LogMessage("✅ Debug console opened successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Failed to open debug console:\n\n{ex.Message}",
                               "Debug Console Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Enhanced debug message logging with better error handling
        private void LogDebugMessage(string message)
        {
            try
            {
                // Always log to console as backup
                Console.WriteLine($"[DEBUG] {message}");

                if (debugConsole != null && !debugConsole.IsDisposed && isDebugFormOpen)
                {
                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    var formattedMessage = $"[{timestamp}] {message}";

                    if (debugConsole.InvokeRequired)
                    {
                        try
                        {
                            debugConsole.Invoke(new Action(() => {
                                AppendToDebugConsole(formattedMessage);
                            }));
                        }
                        catch (InvalidOperationException)
                        {
                            // Control is being disposed, ignore
                        }
                    }
                    else
                    {
                        AppendToDebugConsole(formattedMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                // Fail silently but log to console
                Console.WriteLine($"Debug console error: {ex.Message}");
            }
        }
        private void AppendToDebugConsole(string message)
        {
            try
            {
                if (debugConsole == null || debugConsole.IsDisposed) return;

                debugConsole.AppendText(message + Environment.NewLine);

                // Auto-scroll to bottom
                debugConsole.SelectionStart = debugConsole.Text.Length;
                debugConsole.ScrollToCaret();

                // Limit text length to prevent memory issues
                if (debugConsole.Text.Length > 100000)
                {
                    var lines = debugConsole.Lines;
                    if (lines.Length > 1000)
                    {
                        var keepLines = lines.Skip(lines.Length - 800).ToArray();
                        debugConsole.Text = string.Join(Environment.NewLine, keepLines);
                        debugConsole.AppendText(Environment.NewLine + "... (older messages truncated) ..." + Environment.NewLine);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Append error: {ex.Message}");
            }
        }

        private void SaveDebugLog()
        {
            try
            {
                if (debugConsole == null || debugConsole.IsDisposed || string.IsNullOrEmpty(debugConsole.Text))
                {
                    MessageBox.Show("❌ No debug data to save!", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log|All files (*.*)|*.*",
                    FileName = $"TelegramEA_Debug_islamahmed9717_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                    Title = "Save Debug Log"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    var logContent = $"# Telegram EA Manager Debug Log\r\n" +
                                   $"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n" +
                                   $"# User: islamahmed9717\r\n" +
                                   $"# System: {Environment.OSVersion}\r\n" +
                                   $"# .NET Version: {Environment.Version}\r\n" +
                                   $"#" + new string('=', 50) + "\r\n\r\n" +
                                   debugConsole.Text;

                    File.WriteAllText(saveDialog.FileName, logContent);

                    MessageBox.Show($"✅ Debug log saved successfully!\n\n📁 File: {saveDialog.FileName}\n📊 Size: {new FileInfo(saveDialog.FileName).Length} bytes",
                                   "Log Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Failed to save debug log:\n\n{ex.Message}",
                               "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void SaveDebugLog(object? sender, EventArgs e)
        {
            if (debugConsole == null || string.IsNullOrEmpty(debugConsole.Text)) return;

            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FileName = $"TelegramEA_Debug_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveDialog.FileName, debugConsole.Text);
                    MessageBox.Show("Debug log saved successfully!", "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save debug log: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AppendDebugMessage(string message, Color? color = null)
        {
            if (debugConsole == null || debugConsole.IsDisposed) return;

            if (debugConsole.InvokeRequired)
            {
                debugConsole.BeginInvoke(new Action(() => AppendDebugMessage(message, color)));
                return;
            }

            try
            {
                // Limit console size to prevent memory issues
                if (debugConsole.Text.Length > 100000)
                {
                    debugConsole.Text = debugConsole.Text.Substring(50000);
                }

                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                var formattedMessage = $"[{timestamp}] {message}{Environment.NewLine}";

                debugConsole.AppendText(formattedMessage);

                // Auto-scroll if enabled
                var chkAutoScroll = debugForm?.Controls.Find("chkAutoScroll", true).FirstOrDefault() as CheckBox;
                if (chkAutoScroll?.Checked == true)
                {
                    debugConsole.SelectionStart = debugConsole.Text.Length;
                    debugConsole.ScrollToCaret();
                }
            }
            catch (Exception ex)
            {
                // Prevent debug console errors from crashing the app
                Console.WriteLine($"Debug console error: {ex.Message}");
            }
        }

        private void InitializeServices()
        {
            telegramService = new TelegramService();
            signalProcessor = new SignalProcessingService();

            // Subscribe to real-time message events
            telegramService.NewMessageReceived += TelegramService_NewMessageReceived;
            telegramService.ErrorOccurred += TelegramService_ErrorOccurred;
            telegramService.DebugMessage += TelegramService_DebugMessage;

            // Subscribe to signal processing events
            signalProcessor.SignalProcessed += SignalProcessor_SignalProcessed;
            signalProcessor.ErrorOccurred += SignalProcessor_ErrorOccurred;

            // Start UI cleanup timer
            uiCleanupTimer = new System.Threading.Timer(
                _ => CleanupRecentSignalsTracker(),
                null,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1)
            );
        }
        private void TelegramService_DebugMessage(object? sender, string message)
        {
            LogDebugMessage($"📡 TELEGRAM: {message}");

            // Also log to console for backup
            Console.WriteLine($"[TELEGRAM] {message}");
        }
        private void AddDebugButton(Panel parent)
        {
            var btnDebug = new Button
            {
                Name = "btnDebug",
                Text = isDebugFormOpen ? "🐛 HIDE DEBUG" : "🐛 SHOW DEBUG",
                Location = new Point(440, 295),
                Size = new Size(120, 35),
                BackColor = isDebugFormOpen ? Color.FromArgb(220, 38, 38) : Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            btnDebug.Click += (s, e) => {
                if (isDebugFormOpen && debugForm != null && !debugForm.IsDisposed)
                {
                    debugForm.Close();
                    btnDebug.Text = "🐛 SHOW DEBUG";
                    btnDebug.BackColor = Color.FromArgb(239, 68, 68);
                }
                else
                {
                    CreateDebugConsole();
                    btnDebug.Text = "🐛 HIDE DEBUG";
                    btnDebug.BackColor = Color.FromArgb(220, 38, 38);
                }
            };

            parent.Controls.Add(btnDebug);
        }
        private void StartSignalFileMonitoring(string mt4Path)
        {
            try
            {
                var signalFilePath = Path.Combine(mt4Path, "telegram_signals.txt");
                var directory = Path.GetDirectoryName(signalFilePath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Stop existing watcher if any
                StopSignalFileMonitoring();

                // Create new file watcher with buffering
                signalFileWatcher = new FileSystemWatcher
                {
                    Path = directory,
                    Filter = "telegram_signals.txt",
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    // Enable internal buffer to prevent missing events
                    InternalBufferSize = 65536 // 64KB buffer
                };

                // Use async event handler to prevent blocking
                signalFileWatcher.Changed += async (sender, e) => await OnSignalFileChangedAsync(sender, e);
                signalFileWatcher.Created += async (sender, e) => await OnSignalFileChangedAsync(sender, e);
                signalFileWatcher.EnableRaisingEvents = true;

                LogMessage($"📁 Started monitoring signal file: {signalFilePath}");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Failed to start file monitoring: {ex.Message}");
            }
        }
        private async Task OnSignalFileChangedAsync(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Debounce - wait for file write to complete
                await Task.Delay(200);

                // Process file change in background
                await Task.Run(() =>
                {
                    try
                    {
                        if (this.InvokeRequired)
                        {
                            this.BeginInvoke(new Action(() => {
                                LogMessage($"📝 Signal file updated: {e.ChangeType} at {DateTime.Now:HH:mm:ss}");
                                UpdateFileStatus(e.FullPath);
                            }));
                        }
                        else
                        {
                            LogMessage($"📝 Signal file updated: {e.ChangeType} at {DateTime.Now:HH:mm:ss}");
                            UpdateFileStatus(e.FullPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"❌ Error processing file change: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Error monitoring file: {ex.Message}");
            }
        }
        private void StopSignalFileMonitoring()
        {
            if (signalFileWatcher != null)
            {
                signalFileWatcher.EnableRaisingEvents = false;
                signalFileWatcher.Dispose();
                signalFileWatcher = null;
            }
        }

        private void OnSignalFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Debounce - wait a bit for write to complete
                Thread.Sleep(100);

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => {
                        LogMessage($"📝 Signal file updated: {e.ChangeType} at {DateTime.Now:HH:mm:ss}");
                        UpdateFileStatus(e.FullPath);
                    }));
                }
                else
                {
                    LogMessage($"📝 Signal file updated: {e.ChangeType} at {DateTime.Now:HH:mm:ss}");
                    UpdateFileStatus(e.FullPath);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Error monitoring file: {ex.Message}");
            }
        }

        private void UpdateFileStatus(string filePath)
        {
            Task.Run(async () =>
            {
                try
                {
                    // Wait a bit for file to be fully written
                    await Task.Delay(100);

                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Exists)
                    {
                        string lastSignalInfo = "";

                        // Read file with retry mechanism
                        for (int i = 0; i < 3; i++)
                        {
                            try
                            {
                                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                using (var reader = new StreamReader(fs))
                                {
                                    var lines = new List<string>();
                                    string line;
                                    while ((line = await reader.ReadLineAsync()) != null)
                                    {
                                        lines.Add(line);
                                    }

                                    var lastSignalLine = lines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#"));
                                    if (!string.IsNullOrEmpty(lastSignalLine))
                                    {
                                        var parts = lastSignalLine.Split('|');
                                        if (parts.Length >= 11)
                                        {
                                            var timestamp = parts[0];
                                            var channel = parts[2];
                                            var symbol = parts[4];
                                            var direction = parts[3];
                                            var status = parts[10];

                                            lastSignalInfo = $"📊 Last signal: {symbol} {direction} from {channel} at {timestamp} - Status: {status}";
                                        }
                                    }
                                }
                                break; // Success, exit retry loop
                            }
                            catch (IOException)
                            {
                                if (i < 2) await Task.Delay(100); // Wait before retry
                                else throw; // Last attempt failed
                            }
                        }

                        // Update UI
                        if (this.InvokeRequired)
                        {
                            this.BeginInvoke(new Action(() => {
                                if (!string.IsNullOrEmpty(lastSignalInfo))
                                    LogMessage(lastSignalInfo);

                                var lblStats = this.Controls.Find("lblStats", true)[0] as Label;
                                if (lblStats != null)
                                {
                                    lblStats.Text = $"📊 Live System | Signals: {allSignals.Count} | File: {fileInfo.Length:N0} bytes | Last update: {fileInfo.LastWriteTime:HH:mm:ss}";
                                }
                            }));
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"❌ Error reading file status: {ex.Message}");
                }
            });
        }

        // Update the event handler method to match the delegate signature
        private async void TelegramService_NewMessageReceived(object? sender, (string message, long channelId, string channelName, DateTime messageTime) e)
        {
            try
            {
                // Process the message in a background task to avoid blocking
                await Task.Run(() =>
                {
                    var processedSignal = signalProcessor.ProcessTelegramMessage(e.message, e.channelId, e.channelName);

                    // Only add non-duplicate signals
                    if (!processedSignal.Status.Contains("Duplicate"))
                    {
                        // Add to signals list
                        lock (allSignals)
                        {
                            allSignals.Add(processedSignal);
                            // Keep only last 1000 signals
                            if (allSignals.Count > 1000)
                            {
                                allSignals.RemoveRange(0, allSignals.Count - 1000);
                            }
                        }

                        // Update UI
                        AddToLiveSignals(processedSignal);

                        // Log the signal
                        LogMessage($"📨 New signal from {e.channelName}: {processedSignal.ParsedData?.Symbol} {processedSignal.ParsedData?.Direction} - {processedSignal.Status}");
                    }
                });
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Error processing message: {ex.Message}");
            }
        }
        private void UpdateAfterNewSignal(ProcessedSignal processedSignal, long channelId)
        {
            // Add to live signals display
            AddToLiveSignals(processedSignal);

            // Add to signals history
            allSignals.Add(processedSignal);

            // Update the selected channels table
            UpdateSelectedChannelSignalCount(channelId);

            // Update statistics
            UpdateSignalsCount();

            // Log the message
            LogMessage($"📨 New signal from {processedSignal.ChannelName}: {processedSignal.Status}");

            // Show notification for processed signals
            if (processedSignal.Status.Contains("Processed"))
            {
                ShowNotification($"📊 New Signal: {processedSignal.ParsedData?.Symbol} {processedSignal.ParsedData?.Direction}");
            }
        }
        private void UpdateSelectedChannelSignalCount(long channelId)
        {
            var lvSelected = this.Controls.Find("lvSelected", true).FirstOrDefault() as ListView;
            if (lvSelected == null) return;

            // Update UI thread-safe
            if (lvSelected.InvokeRequired)
            {
                lvSelected.Invoke(new Action(() => UpdateChannelCountInternal(lvSelected, channelId)));
            }
            else
            {
                UpdateChannelCountInternal(lvSelected, channelId);
            }
        }
        private void UpdateChannelCountInternal(ListView lvSelected, long channelId)
        {
            foreach (ListViewItem item in lvSelected.Items)
            {
                var channel = item.Tag as ChannelInfo;
                if (channel != null && channel.Id == channelId)
                {
                    // Count signals from this channel
                    var signalsFromChannel = allSignals.Count(s => s.ChannelId == channelId);

                    // Update the Signals column (index 2)
                    if (item.SubItems.Count > 2)
                    {
                        item.SubItems[2].Text = signalsFromChannel.ToString();
                    }

                    // Update visual feedback
                    item.BackColor = Color.FromArgb(200, 255, 200); // Light green flash

                    // Reset color after a moment
                    var timer = new System.Windows.Forms.Timer { Interval = 1000 };
                    timer.Tick += (s, e) => {
                        item.BackColor = Color.FromArgb(220, 255, 220);
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Start();

                    break;
                }
            }
        }



        private void TelegramService_ErrorOccurred(object? sender, string e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => LogMessage($"🔴 Telegram Error: {e}")));
            }
            else
            {
                LogMessage($"🔴 Telegram Error: {e}");
            }
        }

        private void SignalProcessor_SignalProcessed(object? sender, ProcessedSignal e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => {
                    LogMessage($"✅ Signal processed: {e.ParsedData?.Symbol} {e.ParsedData?.Direction} - {e.Status}");
                    ShowNotification($"📊 New Signal: {e.ParsedData?.Symbol} {e.ParsedData?.Direction}");
                }));
            }
            else
            {
                LogMessage($"✅ Signal processed: {e.ParsedData?.Symbol} {e.ParsedData?.Direction} - {e.Status}");
                ShowNotification($"📊 New Signal: {e.ParsedData?.Symbol} {e.ParsedData?.Direction}");
            }
        }

        private void SignalProcessor_ErrorOccurred(object? sender, string e)
        {
            AppendDebugMessage($"[ERROR] {e}", Color.Red);
            LogMessage($"🔴 Signal Processing Error: {e}");
        }

        private void SetupTimers()
        {
            uiUpdateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            uiUpdateTimer.Tick += UiUpdateTimer_Tick;
            uiUpdateTimer.Start();
        }

        #region UI Setup
        private void SetupUI()
        {
            // Main Form Setup
            this.Text = "📊 Telegram EA Manager - islamahmed9717 | Real Implementation";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 245, 245);
            this.WindowState = FormWindowState.Normal;

            CreateHeaderPanel();
            CreateMainContent();
            CreateBottomPanel();
            CreateStatusBar();
        }

        private void CreateHeaderPanel()
        {
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(37, 99, 235)
            };
            this.Controls.Add(headerPanel);

            // Title
            var lblTitle = new Label
            {
                Text = "📊 REAL TELEGRAM EA MANAGER",
                Location = new Point(20, 5),
                Size = new Size(400, 25),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold)
            };
            headerPanel.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Name = "lblSubtitle",
                Text = $"🕒 Current Now (UTC): 2025-06-21 15:23:07 | User: islamahmed9717",
                Location = new Point(20, 28),
                Size = new Size(500, 15),
                ForeColor = Color.FromArgb(200, 220, 255),
                Font = new Font("Segoe UI", 8F)
            };
            headerPanel.Controls.Add(lblSubtitle);

            // Phone section
            var lblPhone = new Label
            {
                Text = "📱 Phone:",
                Location = new Point(550, 5),
                Size = new Size(70, 15),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            headerPanel.Controls.Add(lblPhone);

            var cmbPhone = new ComboBox
            {
                Name = "cmbPhone",
                Location = new Point(620, 3),
                Size = new Size(180, 20),
                Font = new Font("Segoe UI", 9F),
                DropDownStyle = ComboBoxStyle.DropDown
            };
            headerPanel.Controls.Add(cmbPhone);

            var btnConnect = new Button
            {
                Name = "btnConnect",
                Text = "🔗 CONNECT",
                Location = new Point(810, 3),
                Size = new Size(100, 20),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btnConnect.Click += BtnConnect_Click;
            headerPanel.Controls.Add(btnConnect);

            // MT4 Path
            var lblMT4 = new Label
            {
                Text = "📁 MT4/MT5:",
                Location = new Point(550, 28),
                Size = new Size(70, 15),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8F)
            };
            headerPanel.Controls.Add(lblMT4);

            var txtMT4Path = new TextBox
            {
                Name = "txtMT4Path",
                Location = new Point(620, 26),
                Size = new Size(260, 18),
                Font = new Font("Segoe UI", 7F),
                Text = AutoDetectMT4Path()
            };
            headerPanel.Controls.Add(txtMT4Path);

            var btnBrowse = new Button
            {
                Name = "btnBrowse",
                Text = "📂",
                Location = new Point(885, 26),
                Size = new Size(25, 18),
                BackColor = Color.FromArgb(249, 115, 22),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7F)
            };
            btnBrowse.Click += BtnBrowse_Click;
            headerPanel.Controls.Add(btnBrowse);

            // Status Panel
            CreateStatusPanel(headerPanel);
        }

        private void CreateStatusPanel(Panel parent)
        {
            var statusPanel = new Panel
            {
                Name = "statusPanel",
                Location = new Point(920, 3),
                Size = new Size(300, 60),
                BackColor = Color.FromArgb(249, 115, 22),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblConnectionStatus = new Label
            {
                Name = "lblConnectionStatus",
                Text = "✅ CONNECTED & AUTHORIZED",
                Location = new Point(8, 3),
                Size = new Size(284, 15),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            statusPanel.Controls.Add(lblConnectionStatus);

            var lblChannelsCount = new Label
            {
                Name = "lblChannelsCount",
                Text = "📢 Channels: 31",
                Location = new Point(8, 20),
                Size = new Size(90, 12),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 7F)
            };
            statusPanel.Controls.Add(lblChannelsCount);

            var lblSelectedCount = new Label
            {
                Name = "lblSelectedCount",
                Text = "✅ Selected: 2",
                Location = new Point(100, 20),
                Size = new Size(85, 12),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 7F)
            };
            statusPanel.Controls.Add(lblSelectedCount);

            var lblSignalsCount = new Label
            {
                Name = "lblSignalsCount",
                Text = "📊 Today: 0",
                Location = new Point(188, 20),
                Size = new Size(100, 12),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 7F)
            };
            statusPanel.Controls.Add(lblSignalsCount);

            var lblMonitoringStatus = new Label
            {
                Name = "lblMonitoringStatus",
                Text = "⏯️ Ready to monitor",
                Location = new Point(8, 35),
                Size = new Size(284, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 7F),
                TextAlign = ContentAlignment.MiddleCenter
            };
            statusPanel.Controls.Add(lblMonitoringStatus);

            parent.Controls.Add(statusPanel);
        }

        private void CreateMainContent()
        {
            // COMPLETELY REDESIGNED MAIN CONTENT STRUCTURE
            var mainContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(20, 15, 20, 10),
                BackColor = Color.FromArgb(245, 245, 245)
            };

            // Set column widths: 55% for channels, 45% for controls
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            mainContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            mainContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // LEFT PANEL - Channels
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(5)
            };
            mainContainer.Controls.Add(leftPanel, 0, 0);

            // RIGHT PANEL - Controls  
            var rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 249, 250),
                Padding = new Padding(10),
                BorderStyle = BorderStyle.FixedSingle
            };
            mainContainer.Controls.Add(rightPanel, 1, 0);

            this.Controls.Add(mainContainer);

            // Create content for both panels
            CreateChannelsSection(leftPanel);
            CreateControlsSection(rightPanel);
        }

        private void CreateChannelsSection(Panel parent)
        {
            var lblAllChannels = new Label
            {
                Text = "📢 ALL YOUR TELEGRAM CHANNELS",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 5, 0, 0)
            };
            parent.Controls.Add(lblAllChannels);

            // Search and filter panel
            var searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5)
            };

            var txtSearch = new TextBox
            {
                Name = "txtSearch",
                Location = new Point(5, 8),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "Search channels..."
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;
            searchPanel.Controls.Add(txtSearch);

            var cmbFilter = new ComboBox
            {
                Name = "cmbFilter",
                Location = new Point(210, 8),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbFilter.Items.AddRange(new[] { "All Types", "VIP", "Premium", "Signals", "Gold", "Crypto", "Groups" });
            cmbFilter.SelectedIndex = 0;
            cmbFilter.SelectedIndexChanged += CmbFilter_SelectedIndexChanged;
            searchPanel.Controls.Add(cmbFilter);

            var btnRefresh = new Button
            {
                Name = "btnRefreshChannels",
                Text = "🔄",
                Location = new Point(315, 8),
                Size = new Size(30, 25),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRefresh.Click += BtnRefreshChannels_Click;
            searchPanel.Controls.Add(btnRefresh);

            // ADD THE QUICK SEARCH BUTTON HERE
            var btnFindIndicator = new Button
            {
                Text = "🔍 Find Indicator",
                Location = new Point(350, 8),
                Size = new Size(110, 25),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
            };
            btnFindIndicator.Click += (s, e) =>
            {
                txtSearch.Text = "indicator";
                SearchAndHighlightChannel("indicator");
            };
            searchPanel.Controls.Add(btnFindIndicator);

            parent.Controls.Add(searchPanel);

            var lvChannels = new ListView
            {
                Name = "lvChannels",
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                CheckBoxes = true,
                Font = new Font("Segoe UI", 9F)
            };

            lvChannels.Columns.Add("Channel Name", 280);
            lvChannels.Columns.Add("ID", 100);
            lvChannels.Columns.Add("Type", 80);
            lvChannels.Columns.Add("Members", 80);
            lvChannels.Columns.Add("Activity", 50);

            lvChannels.ItemChecked += LvChannels_ItemChecked;
            parent.Controls.Add(lvChannels);
        }

        private void CreateControlsSection(Panel parent)
        {
            // FORCE CREATE ALL CONTROLS WITH EXPLICIT POSITIONING

            // SELECTED CHANNELS SECTION
            var lblSelected = new Label
            {
                Text = "✅ SELECTED CHANNELS",
                Location = new Point(10, 10),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(34, 197, 94),
                BackColor = Color.Transparent
            };
            parent.Controls.Add(lblSelected);

            var lvSelected = new ListView
            {
                Name = "lvSelected",
                Location = new Point(10, 40),
                Size = new Size(480, 150),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.White
            };

            lvSelected.Columns.Add("Channel", 200);
            lvSelected.Columns.Add("ID", 100);
            lvSelected.Columns.Add("Signals", 60);
            lvSelected.Columns.Add("Status", 120);

            parent.Controls.Add(lvSelected);

            // CONTROL BUTTONS SECTION
            var lblControls = new Label
            {
                Text = "🎮 MONITORING CONTROLS",
                Location = new Point(10, 200),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                BackColor = Color.Transparent
            };
            parent.Controls.Add(lblControls);

            // MAIN MONITORING BUTTONS - LARGE AND PROMINENT
            var btnStartMonitoring = new Button
            {
                Name = "btnStartMonitoring",
                Text = "▶️ START MONITORING",
                Location = new Point(10, 235),
                Size = new Size(230, 50),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Enabled = true,
                UseVisualStyleBackColor = false
            };
            btnStartMonitoring.FlatAppearance.BorderSize = 0;
            btnStartMonitoring.Click += BtnStartMonitoring_Click;
            parent.Controls.Add(btnStartMonitoring);

            var btnStopMonitoring = new Button
            {
                Name = "btnStopMonitoring",
                Text = "⏹️ STOP MONITORING",
                Location = new Point(250, 235),
                Size = new Size(200, 50),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Enabled = false,
                UseVisualStyleBackColor = false
            };
            btnStopMonitoring.FlatAppearance.BorderSize = 0;
            btnStopMonitoring.Click += BtnStopMonitoring_Click;
            parent.Controls.Add(btnStopMonitoring);

            // UTILITY BUTTONS - FIRST ROW
            var btnCopyChannelIDs = new Button
            {
                Name = "btnCopyChannelIDs",
                Text = "📋 COPY IDs",
                Location = new Point(10, 295),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(168, 85, 247),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };
            btnCopyChannelIDs.FlatAppearance.BorderSize = 0;
            btnCopyChannelIDs.Click += BtnCopyChannelIDs_Click;
            parent.Controls.Add(btnCopyChannelIDs);

            var btnTestSignal = new Button
            {
                Name = "btnTestSignal",
                Text = "🧪 TEST",
                Location = new Point(105, 295),
                Size = new Size(70, 35),
                BackColor = Color.FromArgb(249, 115, 22),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };
            btnTestSignal.FlatAppearance.BorderSize = 0;
            btnTestSignal.Click += BtnTestSignal_Click;
            parent.Controls.Add(btnTestSignal);

            var btnGenerateEAConfig = new Button
            {
                Name = "btnGenerateEAConfig",
                Text = "⚙️ CONFIG",
                Location = new Point(180, 295),
                Size = new Size(85, 35),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };
            btnGenerateEAConfig.FlatAppearance.BorderSize = 0;
            btnGenerateEAConfig.Click += BtnGenerateEAConfig_Click;
            parent.Controls.Add(btnGenerateEAConfig);

            var btnClearOldSignals = new Button
            {
                Name = "btnClearOldSignals",
                Text = "🧹 CLEAR",
                Location = new Point(270, 295),
                Size = new Size(80, 35),
                BackColor = Color.FromArgb(107, 114, 128),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };
            btnClearOldSignals.FlatAppearance.BorderSize = 0;
            btnClearOldSignals.Click += (s, e) =>
            {
                ClearOldSignalsFromFile();
                MessageBox.Show("✅ Old signals cleared from file!\n\nOnly signals from the last hour are kept.",
                               "File Cleaned", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            parent.Controls.Add(btnClearOldSignals);

            var btnCheckFile = new Button
            {
                Name = "btnCheckFile",
                Text = "📂 CHECK",
                Location = new Point(355, 295),
                Size = new Size(80, 35),
                BackColor = Color.FromArgb(75, 85, 99),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };
            btnCheckFile.FlatAppearance.BorderSize = 0;
            btnCheckFile.Click += (s, e) => CheckSignalFile();
            parent.Controls.Add(btnCheckFile);

            // FIXED: Properly create the debug button using the corrected method
            var btnDebug = new Button
            {
                Name = "btnDebug",
                Text = isDebugFormOpen ? "🐛 HIDE DEBUG" : "🐛 SHOW DEBUG",
                Location = new Point(440, 295),
                Size = new Size(110, 35),
                BackColor = isDebugFormOpen ? Color.FromArgb(220, 38, 38) : Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };
            btnDebug.FlatAppearance.BorderSize = 0;

            // FIXED: Proper debug button click handler
            btnDebug.Click += (s, e) => {
                try
                {
                    if (isDebugFormOpen && debugForm != null && !debugForm.IsDisposed)
                    {
                        debugForm.Close();
                        btnDebug.Text = "🐛 SHOW DEBUG";
                        btnDebug.BackColor = Color.FromArgb(239, 68, 68);
                        LogMessage("Debug console closed");
                    }
                    else
                    {
                        CreateDebugConsole();
                        btnDebug.Text = "🐛 HIDE DEBUG";
                        btnDebug.BackColor = Color.FromArgb(220, 38, 38);
                        LogMessage("Debug console opened");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"❌ Debug button error: {ex.Message}");
                    MessageBox.Show($"❌ Debug console error:\n\n{ex.Message}",
                                   "Debug Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            parent.Controls.Add(btnDebug);

            // LIVE SIGNALS SECTION
            var lblLiveSignals = new Label
            {
                Text = "📊 LIVE SIGNALS FEED",
                Location = new Point(10, 340),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(249, 115, 22),
                BackColor = Color.Transparent
            };
            parent.Controls.Add(lblLiveSignals);

            var lvLiveSignals = new ListView
            {
                Name = "lvLiveSignals",
                Location = new Point(10, 370),
                Size = new Size(480, 200),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 8F),
                BackColor = Color.White
            };

            lvLiveSignals.Columns.Add("Time", 60);
            lvLiveSignals.Columns.Add("Channel", 100);
            lvLiveSignals.Columns.Add("Symbol", 60);
            lvLiveSignals.Columns.Add("Direction", 50);
            lvLiveSignals.Columns.Add("SL", 50);
            lvLiveSignals.Columns.Add("TP", 50);
            lvLiveSignals.Columns.Add("Status", 110);

            parent.Controls.Add(lvLiveSignals);
        }
        private void CreateBottomPanel()
        {
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.FromArgb(37, 99, 235)
            };
            this.Controls.Add(bottomPanel);

            var lblUser = new Label
            {
                Name = "lblUser",
                Text = "👤 islamahmed9717",
                Location = new Point(20, 25),
                Size = new Size(200, 25),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold)
            };
            bottomPanel.Controls.Add(lblUser);

            CreateBottomButtons(bottomPanel);

            var lblStats = new Label
            {
                Name = "lblStats",
                Text = "📊 System ready - Connect to Telegram to start",
                Location = new Point(800, 25),
                Size = new Size(500, 25),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleRight
            };
            bottomPanel.Controls.Add(lblStats);
        }

        private void CreateBottomButtons(Panel parent)
        {
            var btnHistory = new Button
            {
                Text = "📈 SIGNALS HISTORY",
                Location = new Point(250, 20),
                Size = new Size(160, 40),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnHistory.Click += BtnHistory_Click;
            parent.Controls.Add(btnHistory);

            var btnEASettings = new Button
            {
                Text = "⚙️ EA SETTINGS",
                Location = new Point(420, 20),
                Size = new Size(130, 40),
                BackColor = Color.FromArgb(249, 115, 22),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnEASettings.Click += BtnEASettings_Click;
            parent.Controls.Add(btnEASettings);

            var btnSymbolMapping = new Button
            {
                Text = "🗺️ SYMBOL MAPPING",
                Location = new Point(560, 20),
                Size = new Size(170, 40),
                BackColor = Color.FromArgb(168, 85, 247),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnSymbolMapping.Click += BtnSymbolMapping_Click;
            parent.Controls.Add(btnSymbolMapping);
        }


        private void CreateStatusBar()
        {
            var statusStrip = new StatusStrip
            {
                BackColor = Color.FromArgb(250, 250, 250)
            };

            var statusLabel = new ToolStripStatusLabel
            {
                Name = "statusLabel",
                Text = $"Ready - Current UTC Now: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                Font = new Font("Segoe UI", 9F),
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var versionLabel = new ToolStripStatusLabel
            {
                Name = "versionLabel",
                Text = "v2.0.0 - Real Implementation",
                Font = new Font("Segoe UI", 9F)
            };

            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(versionLabel);
            this.Controls.Add(statusStrip);
        }
        #endregion

        private void TestSignalIDMatching()
        {
            try
            {
                // Make sure MT4 path is set
                var txtMT4Path = this.Controls.Find("txtMT4Path", true)[0] as TextBox;
                var mt4Path = txtMT4Path?.Text?.Trim() ?? "";

                if (string.IsNullOrEmpty(mt4Path) || !Directory.Exists(mt4Path))
                {
                    MessageBox.Show("❌ Please set a valid MT4/MT5 Files folder path first!", "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Update EA settings with current MT4 path
                var currentSettings = signalProcessor.GetEASettings();
                currentSettings.MT4FilesPath = mt4Path;
                currentSettings.SignalFilePath = "telegram_signals.txt";
                signalProcessor.UpdateEASettings(currentSettings);

                // Create a test signal with known values
                var testTime = DateTime.UtcNow; // Use current UTC time
                var testMessage = @"🧪 TEST SIGNAL 🧪
BUY EURUSD NOW
SL: 1.0800
TP1: 1.0900
TP2: 1.0950";

                // Process the test message through the signal processor
                var processedSignal = signalProcessor.ProcessTelegramMessage(
                    testMessage,
                    123456789,  // Test channel ID
                    "TEST_CHANNEL"
                );

                // Show results
                string resultMessage = $"🧪 TEST SIGNAL RESULTS:\n\n" +
                                     $"✅ Signal ID: {processedSignal.Id}\n" +
                                     $"📊 Symbol: {processedSignal.ParsedData?.Symbol} → {processedSignal.ParsedData?.FinalSymbol}\n" +
                                     $"📈 Direction: {processedSignal.ParsedData?.Direction}\n" +
                                     $"📝 Status: {processedSignal.Status}\n" +
                                     $"🕒 Time: {processedSignal.DateTime:yyyy-MM-dd HH:mm:ss} UTC\n\n";

                // Check if file was written
                var signalFilePath = Path.Combine(mt4Path, "telegram_signals.txt");
                if (File.Exists(signalFilePath))
                {
                    var lines = File.ReadAllLines(signalFilePath);
                    var testLine = lines.LastOrDefault(l => l.Contains(processedSignal.Id));

                    if (!string.IsNullOrEmpty(testLine))
                    {
                        resultMessage += $"✅ Signal written to file successfully!\n\n" +
                                       $"📄 File line:\n{testLine}\n\n" +
                                       $"💡 Check if your EA processes this signal with ID: {processedSignal.Id}";
                    }
                    else
                    {
                        resultMessage += "❌ Signal not found in file!";
                    }
                }
                else
                {
                    resultMessage += "❌ Signal file not found!";
                }

                MessageBox.Show(resultMessage, "Signal ID Test Results", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Also add to live signals display
                AddToLiveSignals(processedSignal);
                allSignals.Add(processedSignal);

                LogMessage($"🧪 Test completed - Signal ID: {processedSignal.Id}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Test failed:\n\n{ex.Message}", "Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Add this button click handler to trigger the test
        private void BtnTestIDMatching_Click(object sender, EventArgs e)
        {
            TestSignalIDMatching();
        }

        #region Event Handlers
        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            var cmbPhone = this.Controls.Find("cmbPhone", true)[0] as ComboBox;
            var phoneNumber = cmbPhone?.Text?.Trim() ?? "";

            // Check if already connected
            bool isAlreadyConnected = telegramService.IsUserAuthorized();

            if (isAlreadyConnected && !string.IsNullOrEmpty(phoneNumber))
            {
                var result = MessageBox.Show("✅ Already connected to Telegram!\n\n🔄 Do you want to reload channels?\n\n" +
                                           "Click YES to refresh channel list\n" +
                                           "Click NO to reconnect with different account",
                                           "Already Connected",
                                           MessageBoxButtons.YesNoCancel,
                                           MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    await ReloadChannels();
                    return;
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            if (string.IsNullOrEmpty(phoneNumber))
            {
                ShowMessage("❌ Please enter your phone number", "Phone Required", MessageBoxIcon.Warning);
                return;
            }

            if (!IsValidPhoneNumber(phoneNumber))
            {
                ShowMessage("❌ Please enter a valid phone number with country code\nExample: +1234567890",
                           "Invalid Phone Number", MessageBoxIcon.Warning);
                return;
            }

            var btnConnect = sender as Button;
            var originalText = btnConnect?.Text ?? "";
            if (btnConnect != null)
            {
                btnConnect.Text = "🔄 CONNECTING...";
                btnConnect.Enabled = false;
            }

            try
            {
                bool connected = await telegramService.ConnectAsync(phoneNumber);

                if (connected)
                {
                    await LoadChannelsAfterAuth(phoneNumber);

                    if (!isAlreadyConnected)
                    {
                        ShowMessage("✅ Successfully connected to Telegram!\n\n📱 Phone: " + phoneNumber +
                                   "\n🕒 Time: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC" +
                                   "\n👤 User: islamahmed9717",
                                   "Connection Successful", MessageBoxIcon.Information);
                    }
                }
                else
                {
                    throw new Exception("Authentication failed or was cancelled");
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"❌ Connection failed:\n\n{ex.Message}\n\n🕒 Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n👤 User: islamahmed9717",
                           "Connection Error", MessageBoxIcon.Error);
                UpdateStatus(false, false);
            }
            finally
            {
                if (btnConnect != null)
                {
                    btnConnect.Text = originalText;
                    btnConnect.Enabled = true;
                }
            }
        }

        // Add this new method to handle channel reloading
        private async Task ReloadChannels()
        {
            var btnConnect = this.Controls.Find("btnConnect", true)[0] as Button;
            var btnRefresh = this.Controls.Find("btnRefreshChannels", true)[0] as Button;

            if (btnConnect != null)
            {
                btnConnect.Text = "🔄 RELOADING...";
                btnConnect.Enabled = false;
            }

            if (btnRefresh != null)
            {
                btnRefresh.Enabled = false;
                btnRefresh.Text = "⏳";
            }

            try
            {
                LogMessage("🔄 Reloading channels from Telegram...");

                // Clear current channel lists
                allChannels.Clear();
                var lvChannels = this.Controls.Find("lvChannels", true)[0] as ListView;
                if (lvChannels != null)
                {
                    lvChannels.Items.Clear();
                }

                // Get fresh channel list
                var channels = await telegramService.GetChannelsAsync();
                allChannels = channels;

                // Update the UI
                UpdateChannelsList(channels);

                LogMessage($"✅ Channels reloaded successfully - Found {channels.Count} channels");

                ShowMessage($"✅ Channels reloaded successfully!\n\n" +
                           $"📢 Found {channels.Count} channels\n" +
                           $"🕒 Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                           $"👤 User: islamahmed9717\n\n" +
                           $"💡 Look for 'Indicator Signals' in the list",
                           "Channels Reloaded", MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowMessage($"❌ Failed to reload channels:\n\n{ex.Message}",
                           "Reload Error", MessageBoxIcon.Error);
                LogMessage($"❌ Channel reload failed: {ex.Message}");
            }
            finally
            {
                if (btnConnect != null)
                {
                    btnConnect.Text = "🔗 CONNECT";
                    btnConnect.Enabled = true;
                }

                if (btnRefresh != null)
                {
                    btnRefresh.Enabled = true;
                    btnRefresh.Text = "🔄";
                }
            }
        }

      
        private async Task LoadChannelsAfterAuth(string phoneNumber)
        {
            try
            {
                // Update UI to show connected status
                UpdateStatus(true, true);

                // Load channels using the new service
                var channels = await telegramService.GetChannelsAsync();

                // Update your channels list UI
                UpdateChannelsList(channels);

                // Save phone number for future use
                SavePhoneNumber(phoneNumber);

                // Log the successful connection
                LogMessage($"✅ Connected successfully - Phone: {phoneNumber}, Channels: {channels.Count}, Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC, User: islamahmed9717");

                ShowMessage($"✅ Successfully connected to Telegram!\n\n" +
                           $"📱 Phone: {phoneNumber}\n" +
                           $"📢 Found {channels.Count} channels\n" +
                           $"🕒 Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                           $"👤 User: islamahmed9717\n\n" +
                           $"🎯 Select channels and start monitoring!",
                           "Connection Successful", MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowMessage($"❌ Failed to load channels:\n\n{ex.Message}", "Channel Loading Error", MessageBoxIcon.Error);
            }
        }

        private void ShowNotification(string message)
        {
            try
            {
                // Create a simple notification (you can enhance this)
                var notificationForm = new Form
                {
                    Size = new Size(300, 100),
                    StartPosition = FormStartPosition.Manual,
                    FormBorderStyle = FormBorderStyle.None,
                    BackColor = Color.FromArgb(34, 197, 94),
                    TopMost = true,
                    ShowInTaskbar = false
                };

                var lblMessage = new Label
                {
                    Text = message,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                notificationForm.Controls.Add(lblMessage);

                notificationForm.Show();

                // Auto-close after 3 seconds
                var timer = new System.Windows.Forms.Timer { Interval = 3000 };
                timer.Tick += (s, e) => {
                    timer.Stop();
                    notificationForm.Close();
                };
                timer.Start();
            }
            catch
            {
                // Ignore notification errors
            }
        }

        private void UpdateStatus(bool isConnected, bool isAuthorized)
        {
            var statusPanel = this.Controls.Find("statusPanel", true)[0] as Panel;
            var lblConnectionStatus = statusPanel?.Controls.Find("lblConnectionStatus", true)[0] as Label;
            var lblMonitoringStatus = statusPanel?.Controls.Find("lblMonitoringStatus", true)[0] as Label;

            if (lblConnectionStatus != null && lblMonitoringStatus != null && statusPanel != null)
            {
                if (isMonitoring)
                {
                    statusPanel.BackColor = Color.FromArgb(34, 197, 94); // Green
                    lblConnectionStatus.Text = "✅ LIVE MONITORING";
                    lblMonitoringStatus.Text = $"📊 Active on {selectedChannels.Count} channels";
                }
                else if (isConnected && isAuthorized)
                {
                    statusPanel.BackColor = Color.FromArgb(249, 115, 22); // Orange
                    lblConnectionStatus.Text = "🔗 CONNECTED";
                    lblMonitoringStatus.Text = "⏯️ Ready to monitor";
                }
                else
                {
                    statusPanel.BackColor = Color.FromArgb(220, 38, 38); // Red
                    lblConnectionStatus.Text = "❌ DISCONNECTED";
                    lblMonitoringStatus.Text = "⏸️ Not connected";
                }
            }
        }

        private void LvChannels_ItemChecked(object? sender, ItemCheckedEventArgs e)
        {
            var channel = e.Item.Tag as ChannelInfo;
            if (channel == null) return;

            var lvSelected = this.Controls.Find("lvSelected", true).FirstOrDefault() as ListView;
            if (lvSelected == null) return;

            if (e.Item.Checked)
            {
                // Add to selected channels
                if (!selectedChannels.Any(c => c.Id == channel.Id))
                {
                    selectedChannels.Add(channel);

                    var item = new ListViewItem(channel.Title);
                    item.SubItems.Add(channel.Id.ToString());
                    item.SubItems.Add("0"); // Signals count
                    item.SubItems.Add("✅ Ready"); // Status
                    item.Tag = channel;
                    item.BackColor = Color.FromArgb(255, 255, 220); // Light yellow initially

                    lvSelected.Items.Add(item);
                }
            }
            else
            {
                // Remove from selected channels
                selectedChannels.RemoveAll(c => c.Id == channel.Id);

                for (int i = lvSelected.Items.Count - 1; i >= 0; i--)
                {
                    var selectedChannel = lvSelected.Items[i].Tag as ChannelInfo;
                    if (selectedChannel?.Id == channel.Id)
                    {
                        lvSelected.Items.RemoveAt(i);
                        break;
                    }
                }
            }

            UpdateSelectedCount();

            // Force refresh
            lvSelected.Refresh();
        }


        private async void BtnStartMonitoring_Click(object? sender, EventArgs e)
        {
            if (selectedChannels.Count == 0)
            {
                ShowMessage("⚠️ Please select at least one channel to monitor!",
                           "No Channels Selected", MessageBoxIcon.Warning);
                return;
            }

            var txtMT4Path = this.Controls.Find("txtMT4Path", true)[0] as TextBox;
            var mt4Path = txtMT4Path?.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(mt4Path) || !Directory.Exists(mt4Path))
            {
                ShowMessage("❌ Please set a valid MT4/MT5 Files folder path!",
                           "Invalid Path", MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Clean up old signals first
                signalProcessor.CleanupProcessedSignals();

                // Update EA settings
                var currentSettings = signalProcessor.GetEASettings();
                currentSettings.MT4FilesPath = mt4Path;
                currentSettings.SignalFilePath = "telegram_signals.txt";
                signalProcessor.UpdateEASettings(currentSettings);

                // Start auto-cleanup
                StartAutoCleanup();

                // Start file monitoring
                StartSignalFileMonitoring(mt4Path);

                // Start real Telegram monitoring
                telegramService.StartMonitoring(selectedChannels);

                // Update state
                isMonitoring = true;

                // Update UI
                var btnStart = sender as Button;
                var btnStop = this.Controls.Find("btnStopMonitoring", true)[0] as Button;
                if (btnStart != null) btnStart.Enabled = false;
                if (btnStop != null) btnStop.Enabled = true;

                UpdateStatus(true, true);

                // Update selected channels to show "Live" status
                UpdateSelectedChannelsStatus("📊 Live");
                UpdateSelectedChannelsSignalCounts(); // Add this new method

                ShowMessage($"✅ Monitoring started successfully!\n\n" +
                           $"📊 Monitoring {selectedChannels.Count} channels\n" +
                           $"📁 Signals saved to: {mt4Path}\\telegram_signals.txt\n" +
                           $"🔄 Real-time message processing active\n" +
                           $"🧹 Auto-cleanup enabled (every 5 minutes)\n\n" +
                           $"⚠️ Keep this application running!",
                           "Monitoring Started", MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowMessage($"❌ Failed to start monitoring:\n\n{ex.Message}", "Monitoring Error", MessageBoxIcon.Error);
            }
        }
        private void UpdateSelectedChannelsSignalCounts()
        {
            var lvSelected = this.Controls.Find("lvSelected", true).FirstOrDefault() as ListView;
            if (lvSelected == null) return;

            if (lvSelected.InvokeRequired)
            {
                lvSelected.Invoke(new Action(() => UpdateSelectedChannelsSignalCounts()));
                return;
            }

            foreach (ListViewItem item in lvSelected.Items)
            {
                var channel = item.Tag as ChannelInfo;
                if (channel != null && item.SubItems.Count > 2)
                {
                    // Count signals from this channel
                    var signalCount = allSignals.Count(s => s.ChannelId == channel.Id);
                    item.SubItems[2].Text = signalCount.ToString();
                }
            }

            lvSelected.Refresh();
        }

        private void InitializeSelectedChannelsDisplay()
        {
            var lvSelected = this.Controls.Find("lvSelected", true).FirstOrDefault() as ListView;
            if (lvSelected == null) return;

            foreach (ListViewItem item in lvSelected.Items)
            {
                var channel = item.Tag as ChannelInfo;
                if (channel != null)
                {
                    // Initialize signal count
                    var signalsFromChannel = allSignals.Count(s => s.ChannelId == channel.Id);
                    if (item.SubItems.Count > 2)
                    {
                        item.SubItems[2].Text = signalsFromChannel.ToString();
                    }
                }
            }
        }

        private void SignalProcessor_DebugMessage(object? sender, string message)
        {
            AppendDebugMessage($"[PROCESSOR] {message}", Color.Yellow);
            Console.WriteLine($"[PROCESSOR] {message}");
        }

        private void BtnStopMonitoring_Click(object? sender, EventArgs e)
        {
            try
            {
                // Stop cleanup timer
                cleanupTimer?.Stop();
                cleanupTimer?.Dispose();
                cleanupTimer = null;

                // Stop file monitoring
                StopSignalFileMonitoring();

                // Stop Telegram monitoring
                telegramService.StopMonitoring();

                isMonitoring = false;

                // Update UI
                var btnStart = this.Controls.Find("btnStartMonitoring", true)[0] as Button;
                var btnStop = sender as Button;
                if (btnStart != null) btnStart.Enabled = true;
                if (btnStop != null) btnStop.Enabled = false;

                UpdateStatus(telegramService.IsUserAuthorized(), false);

                // Update selected channels to show "Ready" status
                UpdateSelectedChannelsStatus("✅ Ready");

                ShowMessage("⏹️ Monitoring stopped successfully!", "Monitoring Stopped", MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowMessage($"❌ Error stopping monitoring:\n\n{ex.Message}", "Error", MessageBoxIcon.Error);
            }
        }

        private void BtnCopyChannelIDs_Click(object? sender, EventArgs e)
        {
            if (selectedChannels.Count == 0)
            {
                ShowMessage("⚠️ Please select channels first!", "No Channels Selected", MessageBoxIcon.Warning);
                return;
            }

            var channelIds = string.Join(",", selectedChannels.Select(c => c.Id.ToString()));
            try
            {
                Clipboard.SetText(channelIds);
            }
            catch (ExternalException)
            {
                ShowMessage("⚠️ Unable to copy to clipboard. Please try again.", "Clipboard Error", MessageBoxIcon.Error);
            }

            var channelList = string.Join("\n", selectedChannels.Select(c => $"• {c.Title} ({c.Id}) - {c.Type}"));

            ShowMessage($"📋 Channel IDs copied to clipboard!\n\n📝 PASTE THIS IN YOUR EA:\n{channelIds}\n\n📢 SELECTED CHANNELS:\n{channelList}\n\n⚙️ Configure your EA with these Channel IDs!",
                       "Channel IDs Copied", MessageBoxIcon.Information);
        }

        private void BtnTestSignal_Click(object? sender, EventArgs e)
        {
            var txtMT4Path = this.Controls.Find("txtMT4Path", true)[0] as TextBox;
            var mt4Path = txtMT4Path?.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(mt4Path) || !Directory.Exists(mt4Path))
            {
                ShowMessage("❌ Please set a valid MT4/MT5 path first!", "Invalid Path", MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Update EA settings with current MT4 path
                var currentSettings = signalProcessor.GetEASettings();
                currentSettings.MT4FilesPath = mt4Path;
                currentSettings.SignalFilePath = "telegram_signals.txt";
                signalProcessor.UpdateEASettings(currentSettings);

                // Create different test signals each time
                var testScenarios = new[]
                {
            // EURUSD Buy
            @"🚀 FOREX SIGNAL 🚀
BUY EURUSD @ 1.0890
SL: 1.0860
TP1: 1.0920
TP2: 1.0950
TP3: 1.0980",

            // GBPUSD Sell
            @"📊 TRADING ALERT 📊
SELL GBPUSD NOW
Stop Loss: 1.2650
Take Profit 1: 1.2600
Take Profit 2: 1.2550",

            // GOLD Buy
            @"🏆 GOLD SIGNAL 🏆
BUY GOLD (XAUUSD)
Entry: Market Price
SL: 1945.00
TP: 1965.00",

            // USDJPY Buy
            @"💹 SIGNAL TIME 💹
USDJPY BUY NOW
SL 148.50
TP 149.50"
        };

                // Rotate through different test signals
                var random = new Random();
                var testMessage = testScenarios[random.Next(testScenarios.Length)];

                // FIX: Process the test message with current timestamp
                var processedSignal = signalProcessor.ProcessTelegramMessage(
                    testMessage,
                    999999,
                    "TEST_CHANNEL"
                   
                );

                // Add to signals history and UI
                allSignals.Add(processedSignal);
                AddToLiveSignals(processedSignal);

                // Verify file was written
                var signalFilePath = Path.Combine(mt4Path, "telegram_signals.txt");
                var fileWritten = File.Exists(signalFilePath);
                var fileSize = fileWritten ? new FileInfo(signalFilePath).Length : 0;

                // Read last line from file to verify
                string lastLine = "";
                if (fileWritten)
                {
                    var lines = File.ReadAllLines(signalFilePath);
                    lastLine = lines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#")) ?? "";
                }

                ShowMessage($"🧪 TEST SIGNAL RESULTS:\n\n" +
                            $"✅ Signal Type: {processedSignal.ParsedData?.Symbol} {processedSignal.ParsedData?.Direction}\n" +
                            $"📝 Status: {processedSignal.Status}\n" +
                            $"📁 File Written: {(fileWritten ? "YES ✅" : "NO ❌")}\n" +
                            $"📏 File Size: {fileSize} bytes\n" +
                            $"🕒 Timestamp: {DateTime.UtcNow:yyyy.MM.dd HH:mm:ss} UTC\n\n" +
                            $"📄 Last Line in File:\n{lastLine}\n\n" +
                            $"💡 Now check your EA - it should process this signal immediately!",
                            "Test Signal Complete",
                            MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowMessage($"❌ Failed to process test signal:\n\n{ex.Message}", "Test Failed", MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // Stop all timers
                uiUpdateTimer?.Stop();
                uiUpdateTimer?.Dispose();

                cleanupTimer?.Stop();
                cleanupTimer?.Dispose();

                uiCleanupTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                uiCleanupTimer?.Dispose();

                // Stop file monitoring
                StopSignalFileMonitoring();

                // Stop telegram monitoring
                telegramService?.StopMonitoring();
                telegramService?.Dispose();

                // Dispose semaphores
                uiUpdateSemaphore?.Dispose();

                // Clear collections
                recentSignalsInUI?.Clear();
                allSignals?.Clear();
                selectedChannels?.Clear();

                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                // Log error but don't throw
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }

            base.OnFormClosing(e);
        }
        private void BtnGenerateEAConfig_Click(object? sender, EventArgs e)
        {
            if (selectedChannels.Count == 0)
            {
                ShowMessage("⚠️ Please select channels first!", "No Channels Selected", MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var config = GenerateEAConfiguration();

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FileName = $"TelegramEA_Config_islamahmed9717_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
                    Title = "Save EA Configuration"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveDialog.FileName, config);
                    Clipboard.SetText(config);

                    ShowMessage($"⚙️ EA configuration generated successfully!\n\n📁 Saved to: {saveDialog.FileName}\n📋 Configuration also copied to clipboard!\n\n⚙️ Import this configuration into your EA settings.",
                               "Configuration Generated", MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"❌ Failed to generate configuration:\n\n{ex.Message}", "Generation Error", MessageBoxIcon.Error);
            }
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select your MT4/MT5 Files folder (usually contains MQL4 or MQL5 subfolder)";
                folderDialog.ShowNewFolderButton = false;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    var txtMT4Path = this.Controls.Find("txtMT4Path", true)[0] as TextBox;
                    if (txtMT4Path != null)
                    {
                        txtMT4Path.Text = folderDialog.SelectedPath;
                        SaveMT4Path(folderDialog.SelectedPath);
                    }
                }
            }
        }

        private async void BtnRefreshChannels_Click(object? sender, EventArgs e)
        {
            if (!telegramService.IsUserAuthorized())
            {
                ShowMessage("❌ Please connect to Telegram first!", "Not Connected", MessageBoxIcon.Warning);
                return;
            }

            await ReloadChannels();
        }

        // Add this method to Form1.cs
        private void SearchAndHighlightChannel(string searchTerm)
        {
            var lvChannels = this.Controls.Find("lvChannels", true)[0] as ListView;
            if (lvChannels == null) return;

            searchTerm = searchTerm.ToLower();
            bool found = false;

            foreach (ListViewItem item in lvChannels.Items)
            {
                var channel = item.Tag as ChannelInfo;
                if (channel == null) continue;

                string channelTitle = channel.Title.ToLower();
                string channelUsername = channel.Username?.ToLower() ?? "";

                if (channelTitle.Contains(searchTerm) || channelUsername.Contains(searchTerm))
                {
                    // Highlight the found channel
                    item.BackColor = Color.FromArgb(255, 255, 100); // Bright yellow
                    item.Font = new Font(item.Font, FontStyle.Bold);

                    if (!found)
                    {
                        // Ensure it's visible and select it
                        item.EnsureVisible();
                        item.Selected = true;
                        found = true;
                    }
                }
                else
                {
                    // Reset highlighting for non-matching items
                    item.Font = new Font("Segoe UI", 9F);
                    // Restore original color based on type
                    RestoreChannelItemColor(item, channel);
                }
            }

            if (!found)
            {
                LogMessage($"⚠️ Channel containing '{searchTerm}' not found. Try reloading channels.");
            }
            else
            {
                LogMessage($"✅ Found and highlighted channels containing '{searchTerm}'");
            }
        }

        private void RestoreChannelItemColor(ListViewItem item, ChannelInfo channel)
        {
            // Restore original color coding based on type
            switch (channel.Type.ToLower())
            {
                case "vip":
                    item.BackColor = Color.FromArgb(255, 235, 59); // Yellow
                    break;
                case "premium":
                    item.BackColor = Color.FromArgb(156, 39, 176); // Purple
                    item.ForeColor = Color.White;
                    break;
                case "signals":
                    item.BackColor = Color.FromArgb(76, 175, 80); // Green
                    item.ForeColor = Color.White;
                    break;
                case "gold":
                    item.BackColor = Color.FromArgb(255, 193, 7); // Gold
                    break;
                case "crypto":
                    item.BackColor = Color.FromArgb(255, 87, 34); // Orange
                    item.ForeColor = Color.White;
                    break;
                default:
                    item.BackColor = Color.FromArgb(200, 230, 255); // Light blue
                    item.ForeColor = Color.Black;
                    break;
            }
        }

        // Update the TxtSearch_TextChanged event handler
        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            var txtSearch = sender as TextBox;
            if (txtSearch == null) return;

            string searchText = txtSearch.Text.Trim();

            if (!string.IsNullOrEmpty(searchText))
            {
                // Highlight matching channels
                SearchAndHighlightChannel(searchText);
            }
            else
            {
                // Clear highlighting when search is empty
                var lvChannels = this.Controls.Find("lvChannels", true)[0] as ListView;
                if (lvChannels != null)
                {
                    foreach (ListViewItem item in lvChannels.Items)
                    {
                        var channel = item.Tag as ChannelInfo;
                        if (channel != null)
                        {
                            RestoreChannelItemColor(item, channel);
                            item.Font = new Font("Segoe UI", 9F);
                        }
                    }
                }
            }

            ApplyChannelFilters();
        }

        // Add a quick search button specifically for "Indicator Signals"
        private void AddQuickSearchButton(Panel parent)
        {
            var btnFindIndicator = new Button
            {
                Text = "🔍 Find Indicator Signals",
                Location = new Point(435, 8),
                Size = new Size(150, 25),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            btnFindIndicator.Click += (s, e) =>
            {
                var txtSearch = this.Controls.Find("txtSearch", true)[0] as TextBox;
                if (txtSearch != null)
                {
                    txtSearch.Text = "indicator";
                    SearchAndHighlightChannel("indicator");
                }
            };
            parent.Controls.Add(btnFindIndicator);
        }

        private void BtnHistory_Click(object? sender, EventArgs e)
        {
            var historyForm = new SignalsHistoryForm(allSignals);
            historyForm.ShowDialog();
        }

        private void BtnEASettings_Click(object? sender, EventArgs e)
        {
            ShowMessage("⚙️ EA Settings feature will be implemented!\n\nThis will allow you to configure:\n• Risk management\n• Lot sizes\n• Trading hours\n• Symbol mappings\n\n🚀 Coming soon!", "EA Settings", MessageBoxIcon.Information);
        }

        private void BtnSymbolMapping_Click(object? sender, EventArgs e)
        {
            ShowMessage("🗺️ Symbol Mapping feature will be implemented!\n\nThis will allow you to:\n• Map Telegram symbols to MT4/MT5\n• Set symbol prefixes/suffixes\n• Configure exclusions\n\n🚀 Coming soon!", "Symbol Mapping", MessageBoxIcon.Information);
        }

        private void CmbFilter_SelectedIndexChanged(object? sender, EventArgs e)
        {
            ApplyChannelFilters();
        }

        private void UiUpdateTimer_Tick(object? sender, EventArgs e)
        {
            // Update time in subtitle showing both UTC and local
            var lblSubtitle = this.Controls.Find("lblSubtitle", true).FirstOrDefault() as Label;
            if (lblSubtitle != null)
            {
                var utcTime = DateTime.UtcNow;
                var localTime = DateTime.Now;
                lblSubtitle.Text = $"🕒 UTC: {utcTime:yyyy-MM-dd HH:mm:ss} | Local: {localTime:HH:mm:ss} | User: islamahmed9717";
            }

            // Update time in status bar
            foreach (Control control in this.Controls)
            {
                if (control is StatusStrip statusStrip)
                {
                    foreach (ToolStripItem item in statusStrip.Items)
                    {
                        if (item.Name == "statusLabel")
                        {
                            item.Text = $"Real-time System Active | UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} | Local: {DateTime.Now:HH:mm:ss}";
                        }
                    }
                }
            }

            // Update signals count
            UpdateSignalsCount();

            // Update selected channels if monitoring
            if (isMonitoring)
            {
                UpdateSelectedChannelsSignalCounts();
            }
        }
        private void ClearOldSignalsFromFile()
        {
            var txtMT4Path = this.Controls.Find("txtMT4Path", true)[0] as TextBox;
            var mt4Path = txtMT4Path?.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(mt4Path) || !Directory.Exists(mt4Path))
                return;

            try
            {
                var signalFilePath = Path.Combine(mt4Path, "telegram_signals.txt");

                if (File.Exists(signalFilePath))
                {
                    // Read all lines
                    var lines = File.ReadAllLines(signalFilePath).ToList();
                    var newLines = new List<string>();
                    var utcNow = DateTime.UtcNow;

                    // Keep header lines (those starting with #)
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                        {
                            newLines.Add(line);
                        }
                        else
                        {
                            // Parse signal line to check age
                            var parts = line.Split('|');
                            if (parts.Length >= 11)
                            {
                                var timestampStr = parts[0];
                                if (DateTime.TryParse(timestampStr, out DateTime signalTime))
                                {
                                    // Keep only signals from last hour
                                    var ageMinutes = (utcNow - signalTime).TotalMinutes;
                                    if (ageMinutes <= 10) // Keep signals less than 60 minutes old
                                    {
                                        newLines.Add(line);
                                    }
                                    else
                                    {
                                        LogMessage($"🧹 Removing old signal: {parts[4]} {parts[3]} - Age: {ageMinutes:F1} minutes");
                                    }
                                }
                            }
                        }
                    }

                    // Write back cleaned file
                    File.WriteAllLines(signalFilePath, newLines);

                    LogMessage($"🧹 Cleaned signal file - removed old signals, kept {newLines.Count(l => !l.StartsWith("#") && !string.IsNullOrWhiteSpace(l))} recent signals");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Error cleaning signal file: {ex.Message}");
            }
        }

        // Add a button to clear old signals
        private void AddClearSignalsButton(Panel parent)
        {
            var btnClearOldSignals = new Button
            {
                Name = "btnClearOldSignals",
                Text = "🧹 CLEAR OLD",
                Location = new Point(330, 295),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(107, 114, 128),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnClearOldSignals.Click += (s, e) =>
            {
                ClearOldSignalsFromFile();
                MessageBox.Show("✅ Old signals cleared from file!\n\nOnly signals from the last hour are kept.",
                               "File Cleaned", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            parent.Controls.Add(btnClearOldSignals);
        }

        private void AddCheckFileButton(Panel parent)
        {
            var btnCheckFile = new Button
            {
                Name = "btnCheckFile",
                Text = "📂 CHECK FILE",
                Location = new Point(440, 295),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(75, 85, 99),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnCheckFile.Click += (s, e) =>
            {
                CheckSignalFile();
            };
            parent.Controls.Add(btnCheckFile);
        }

        private void CheckSignalFile()
        {
            var txtMT4Path = this.Controls.Find("txtMT4Path", true)[0] as TextBox;
            var mt4Path = txtMT4Path?.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(mt4Path))
            {
                ShowMessage("❌ Please set MT4/MT5 path first!", "No Path", MessageBoxIcon.Warning);
                return;
            }

            var signalFilePath = Path.Combine(mt4Path, "telegram_signals.txt");

            if (!File.Exists(signalFilePath))
            {
                ShowMessage($"❌ Signal file not found!\n\n{signalFilePath}", "File Not Found", MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var lines = File.ReadAllLines(signalFilePath);
                var signalLines = lines.Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#")).ToList();

                var fileInfo = new FileInfo(signalFilePath);

                var report = $"📁 SIGNAL FILE REPORT:\n\n" +
                            $"📍 Path: {signalFilePath}\n" +
                            $"📏 Size: {fileInfo.Length} bytes\n" +
                            $"🕒 Last Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}\n" +
                            $"📊 Total Lines: {lines.Length}\n" +
                            $"📈 Signal Lines: {signalLines.Count}\n\n";

                if (signalLines.Count > 0)
                {
                    report += "📋 LAST 5 SIGNALS:\n\n";
                    var lastSignals = signalLines.TakeLast(5).Reverse();

                    foreach (var line in lastSignals)
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 5)
                        {
                            report += $"• {parts[0]} - {parts[4]} {parts[3]} from {parts[2]}\n";
                        }
                    }
                }
                else
                {
                    report += "⚠️ No signals found in file!";
                }

                ShowMessage(report, "Signal File Status", MessageBoxIcon.Information);

                // Also open the file in notepad for direct viewing
                try
                {
                    System.Diagnostics.Process.Start("notepad.exe", signalFilePath);
                }
                catch { }
            }
            catch (Exception ex)
            {
                ShowMessage($"❌ Error reading file:\n\n{ex.Message}", "Error", MessageBoxIcon.Error);
            }
        }
        #endregion

        #region Helper Methods
        private void RefreshChannelsList()
        {
            var lvChannels = this.Controls.Find("lvChannels", true)[0] as ListView;
            if (lvChannels == null) return;

            lvChannels.Items.Clear();

            foreach (var channel in allChannels)
            {
                var item = new ListViewItem(channel.Title);
                item.SubItems.Add(channel.Id.ToString());
                item.SubItems.Add(channel.Type);
                item.SubItems.Add(channel.MembersCount.ToString());
                item.SubItems.Add(channel.LastActivity.ToString("HH:mm"));
                item.Tag = channel;

                // Color coding based on type
                switch (channel.Type.ToLower())
                {
                    case "vip":
                        item.BackColor = Color.FromArgb(255, 235, 59); // Yellow
                        break;
                    case "premium":
                        item.BackColor = Color.FromArgb(156, 39, 176); // Purple
                        item.ForeColor = Color.White;
                        break;
                    case "signals":
                        item.BackColor = Color.FromArgb(76, 175, 80); // Green
                        item.ForeColor = Color.White;
                        break;
                    case "gold":
                        item.BackColor = Color.FromArgb(255, 193, 7); // Gold
                        break;
                    case "crypto":
                        item.BackColor = Color.FromArgb(255, 87, 34); // Orange
                        item.ForeColor = Color.White;
                        break;
                    default:
                        item.BackColor = Color.FromArgb(200, 230, 255); // Light blue
                        break;
                }

                lvChannels.Items.Add(item);
            }

            ApplyChannelFilters();
            UpdateChannelsCount();
        }

        private void ApplyChannelFilters()
        {
            var lvChannels = this.Controls.Find("lvChannels", true)[0] as ListView;
            var txtSearch = this.Controls.Find("txtSearch", true)[0] as TextBox;
            var cmbFilter = this.Controls.Find("cmbFilter", true)[0] as ComboBox;

            if (lvChannels == null) return;

            var searchText = txtSearch?.Text?.ToLower() ?? "";
            var filterType = cmbFilter?.SelectedItem?.ToString() ?? "All Types";

            foreach (ListViewItem item in lvChannels.Items)
            {
                var channel = item.Tag as ChannelInfo;
                if (channel == null) continue;

                bool visible = true;

                // Apply search filter
                if (!string.IsNullOrEmpty(searchText))
                {
                    visible = channel.Title.ToLower().Contains(searchText) ||
                             channel.Id.ToString().Contains(searchText) ||
                             channel.Type.ToLower().Contains(searchText);
                }

                // Apply type filter
                if (visible && filterType != "All Types")
                {
                    visible = channel.Type.Equals(filterType, StringComparison.OrdinalIgnoreCase);
                }

                item.Font = visible ? new Font("Segoe UI", 9F) : new Font("Segoe UI", 9F, FontStyle.Strikeout);
                item.ForeColor = visible ? item.ForeColor : Color.Gray;
            }
        }

        // Replace the existing AddToLiveSignals method with this:
        private async void AddToLiveSignals(ProcessedSignal signal)
        {
            // Skip duplicate signals
            if (signal.Status.Contains("Duplicate"))
                return;

            // Create a unique key for this signal
            var signalKey = $"{signal.ChannelId}_{signal.ParsedData?.Symbol}_{signal.ParsedData?.Direction}_{signal.DateTime:HHmmss}";

            // Check if we've already added this signal recently
            if (recentSignalsInUI.TryGetValue(signalKey, out DateTime addedTime))
            {
                if ((DateTime.Now - addedTime).TotalSeconds < 30) // Within 30 seconds
                    return; // Skip duplicate
            }

            // Add to recent signals tracker
            recentSignalsInUI[signalKey] = DateTime.Now;

            // Use semaphore to prevent concurrent UI updates
            await uiUpdateSemaphore.WaitAsync();
            try
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() => AddSignalToListView(signal)));
                }
                else
                {
                    AddSignalToListView(signal);
                }
            }
            finally
            {
                uiUpdateSemaphore.Release();
            }
        }
        private void AddSignalToListView(ProcessedSignal signal)
        {
            var lvLiveSignals = this.Controls.Find("lvLiveSignals", true)[0] as ListView;
            if (lvLiveSignals == null) return;

            // Suspend layout to improve performance
            lvLiveSignals.BeginUpdate();
            try
            {
                // Convert UTC to local time for display
                var localTime = signal.DateTime.ToLocalTime();

                var item = new ListViewItem(localTime.ToString("HH:mm:ss"));
                item.SubItems.Add(signal.ChannelName);
                item.SubItems.Add(signal.ParsedData?.Symbol ?? "N/A");
                item.SubItems.Add(signal.ParsedData?.Direction ?? "N/A");
                item.SubItems.Add(signal.ParsedData?.StopLoss > 0 ? signal.ParsedData.StopLoss.ToString("F5") : "N/A");
                item.SubItems.Add(signal.ParsedData?.TakeProfit1 > 0 ? signal.ParsedData.TakeProfit1.ToString("F5") : "N/A");
                item.SubItems.Add(signal.Status);

                // Color coding based on status
                if (signal.Status.Contains("Processed"))
                    item.BackColor = Color.FromArgb(220, 255, 220); // Light green
                else if (signal.Status.Contains("Error") || signal.Status.Contains("Invalid"))
                    item.BackColor = Color.FromArgb(255, 220, 220); // Light red
                else if (signal.Status.Contains("Ignored"))
                    item.BackColor = Color.FromArgb(255, 255, 220); // Light yellow
                else if (signal.Status.Contains("Test"))
                    item.BackColor = Color.FromArgb(220, 220, 255); // Light blue

                lvLiveSignals.Items.Insert(0, item);

                // Keep only last 50 signals
                while (lvLiveSignals.Items.Count > 50)
                {
                    lvLiveSignals.Items.RemoveAt(lvLiveSignals.Items.Count - 1);
                }
            }
            finally
            {
                lvLiveSignals.EndUpdate();
            }
        }

        private void CleanupRecentSignalsTracker()
        {
            var cutoffTime = DateTime.Now.AddMinutes(-5);
            var keysToRemove = recentSignalsInUI
                .Where(kvp => kvp.Value < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                recentSignalsInUI.TryRemove(key, out _);
            }
        }

        private void UpdateSelectedChannelsStatus(string status)
        {
            var lvSelected = this.Controls.Find("lvSelected", true).FirstOrDefault() as ListView;
            if (lvSelected == null) return;

            // Update the list on the UI thread if necessary
            if (lvSelected.InvokeRequired)
            {
                lvSelected.Invoke(new Action(() => UpdateSelectedChannelsStatus(status)));
                return;
            }

            foreach (ListViewItem item in lvSelected.Items)
            {
                // Update the status column (index 3)
                if (item.SubItems.Count > 3)
                {
                    item.SubItems[3].Text = status;
                }

                // Update background color based on status
                if (status.Contains("Live") || status.Contains("📊"))
                {
                    item.BackColor = Color.FromArgb(200, 255, 200); // Light green for active
                    item.ForeColor = Color.Black;
                }
                else if (status.Contains("Ready") || status.Contains("✅"))
                {
                    item.BackColor = Color.FromArgb(255, 255, 220); // Light yellow for ready
                    item.ForeColor = Color.Black;
                }
                else
                {
                    item.BackColor = Color.White;
                    item.ForeColor = Color.Black;
                }
            }

            // Force refresh the ListView
            lvSelected.Refresh();
        }

        private void UpdateChannelsCount()
        {
            var lblChannelsCount = this.Controls.Find("lblChannelsCount", true)[0] as Label;
            if (lblChannelsCount != null)
            {
                lblChannelsCount.Text = $"📢 Channels: {allChannels.Count}";
            }
        }

        private void UpdateSelectedCount()
        {
            var lblSelectedCount = this.Controls.Find("lblSelectedCount", true)[0] as Label;
            if (lblSelectedCount != null)
            {
                lblSelectedCount.Text = $"✅ Selected: {selectedChannels.Count}";
            }

            // SIMPLIFIED - Button enabled when channels selected and not monitoring
            var btnStartMonitoring = this.Controls.Find("btnStartMonitoring", true)[0] as Button;
            if (btnStartMonitoring != null)
            {
                btnStartMonitoring.Enabled = selectedChannels.Count > 0 && !isMonitoring;
            }
        }

        private void UpdateSignalsCount()
        {
            var todaySignals = allSignals.Count(s => s.DateTime.Date == DateTime.Now.Date);

            var lblSignalsCount = this.Controls.Find("lblSignalsCount", true)[0] as Label;
            if (lblSignalsCount != null)
            {
                lblSignalsCount.Text = $"📊 Today: {todaySignals}";
            }

            var lblStats = this.Controls.Find("lblStats", true)[0] as Label;
            if (lblStats != null)
            {
                lblStats.Text = $"📊 Live System | Today: {todaySignals} signals | Total: {allSignals.Count} | Monitoring: {selectedChannels.Count} channels | Status: {(isMonitoring ? "ACTIVE" : "READY")}";
            }
        }

        private string GenerateEAConfiguration()
        {
            var channelIds = string.Join(",", selectedChannels.Select(c => c.Id.ToString()));

            return $@"//+------------------------------------------------------------------+
//|                    Telegram EA Configuration                     |
//|                Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} UTC               |
//|                User: islamahmed9717                              |
//+------------------------------------------------------------------+

//--- Telegram Channel Settings ---
ChannelIDs = ""{channelIds}""
SignalFilePath = ""TelegramSignals.txt""

//--- Risk Management Settings ---
RiskMode = ""Fixed""
FixedLotSize = 0.01
RiskPercent = 2.0
RiskAmount = 100

//--- Symbol Mapping Settings ---
SymbolsMapping = ""EURUSD:EURUSD,GBPUSD:GBPUSD,USDJPY:USDJPY,GOLD:XAUUSD,SILVER:XAGUSD,BITCOIN:BTCUSD""
SymbolPrefix = """"
SymbolSuffix = """"

//--- Advanced Settings ---
UseTrailingStop = false
TrailingStartPips = 10
TrailingStepPips = 5
MoveSLToBreakeven = true
BreakevenAfterPips = 10
SendNotifications = true
MaxSpreadPips = 5
SignalCheckInterval = 5
ForceMarketExecution = true
MaxRetriesOrderSend = 3

//--- Selected Channels ---
/*
{string.Join("\n", selectedChannels.Select(c => $"Channel: {c.Title} (ID: {c.Id}) - Type: {c.Type} - Members: {c.MembersCount}"))}
*/

//--- Configuration Instructions ---
/*
1. Copy the above settings into your Telegram EA input parameters
2. Make sure the MT4/MT5 Files path is set correctly in this app
3. Ensure this Windows application is running and monitoring channels
4. The EA will automatically read signals from: TelegramSignals.txt
5. Start monitoring in this app before running the EA

Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss} UTC
By: islamahmed9717 - Telegram EA Manager v2.0 (Real Implementation)
System: Windows Forms .NET 9.0 with WTelegramClient
*/";
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return false;

            var cleanPhone = phoneNumber.Replace(" ", "").Replace("-", "");
            if (!cleanPhone.StartsWith("+"))
                return false;

            var digits = cleanPhone.Substring(1);
            return digits.Length >= 10 && digits.Length <= 15 && digits.All(char.IsDigit);
        }

        private string AutoDetectMT4Path()
        {
            try
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var possiblePaths = new[]
                {
                    Path.Combine(userProfile, "AppData", "Roaming", "MetaQuotes", "Terminal"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "MetaTrader 4", "MQL4", "Files"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "MetaTrader 4", "MQL4", "Files"),
                    Path.Combine(userProfile, "Documents", "MT4", "Files"),
                    Path.Combine(userProfile, "Documents", "MT5", "Files")
                };

                foreach (var basePath in possiblePaths)
                {
                    if (Directory.Exists(basePath))
                    {
                        var directories = Directory.GetDirectories(basePath);
                        foreach (var dir in directories)
                        {
                            var mql4Files = Path.Combine(dir, "MQL4", "Files");
                            var mql5Files = Path.Combine(dir, "MQL5", "Files");

                            if (Directory.Exists(mql4Files))
                                return mql4Files;
                            if (Directory.Exists(mql5Files))
                                return mql5Files;
                        }

                        return basePath;
                    }
                }

                return Path.Combine(userProfile, "Documents", "MT4", "Files");
            }
            catch
            {
                return "";
            }
        }

        private void ShowMessage(string message, string title, MessageBoxIcon icon)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
        }
        private void DebugSignalFile()
        {
            var txtMT4Path = this.Controls.Find("txtMT4Path", true)[0] as TextBox;
            var mt4Path = txtMT4Path?.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(mt4Path))
            {
                LogMessage("❌ MT4 path not set!");
                return;
            }

            var signalFilePath = Path.Combine(mt4Path, "telegram_signals.txt");

            try
            {
                if (File.Exists(signalFilePath))
                {
                    // Read file content
                    var lines = File.ReadAllLines(signalFilePath);
                    LogMessage($"📁 Signal file contains {lines.Length} lines");

                    // Show last 5 signals
                    var signalLines = lines.Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#")).TakeLast(5);

                    foreach (var line in signalLines)
                    {
                        LogMessage($"📊 Signal: {line}");

                        // Parse and show status
                        var parts = line.Split('|');
                        if (parts.Length >= 11)
                        {
                            var timestamp = parts[0];
                            var status = parts[10];
                            LogMessage($"   ⏰ Time: {timestamp}, Status: {status}");
                        }
                    }

                    // Check file permissions
                    var fileInfo = new FileInfo(signalFilePath);
                    LogMessage($"📋 File info: Size={fileInfo.Length} bytes, LastWrite={fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                }
                else
                {
                    LogMessage($"❌ Signal file not found: {signalFilePath}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Error reading signal file: {ex.Message}");
            }

        }
        private void AddDebugFileButton(Panel parent)
        {
            var btnDebugFile = new Button
            {
                Name = "btnDebugFile",
                Text = "🔍 DEBUG FILE",
                Location = new Point(550, 295),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(139, 92, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnDebugFile.Click += (s, e) => DebugSignalFile();
            parent.Controls.Add(btnDebugFile);
        }

        private void LoadApplicationSettings()
        {
            // Load saved phone numbers, MT4 path, etc.
            try
            {
                if (File.Exists("app_settings.json"))
                {
                    var json = File.ReadAllText("app_settings.json");
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);

                    if (settings != null)
                    {
                        var cmbPhone = this.Controls.Find("cmbPhone", true)[0] as ComboBox;
                        var txtMT4Path = this.Controls.Find("txtMT4Path", true)[0] as TextBox;

                        if (settings.SavedAccounts?.Count > 0 && cmbPhone != null)
                        {
                            cmbPhone.Items.AddRange(settings.SavedAccounts.ToArray());
                            cmbPhone.Text = settings.LastPhoneNumber;
                        }

                        if (!string.IsNullOrEmpty(settings.MT4Path) && txtMT4Path != null)
                        {
                            txtMT4Path.Text = settings.MT4Path;
                        }
                    }
                }
            }
            catch
            {
                // Ignore loading errors
            }
        }

        private void UpdateChannelsList(List<ChannelInfo> channels)
        {
            allChannels = channels;
            RefreshChannelsList();
        }

        private void LogMessage(string message)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() => LogMessageInternal(message)));
                }
                else
                {
                    LogMessageInternal(message);
                }
            }
            catch
            {
                // Ignore logging errors to prevent crashes
            }
        }
        private void LogMessageInternal(string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");

            // Optionally update a log textbox if you have one
            // var txtLog = this.Controls.Find("txtLog", true).FirstOrDefault() as TextBox;
            // if (txtLog != null)
            // {
            //     txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            // }
        }

        private void SavePhoneNumber(string phoneNumber)
        {
            try
            {
                var settings = LoadAppSettings();
                if (!settings.SavedAccounts.Contains(phoneNumber))
                {
                    settings.SavedAccounts.Add(phoneNumber);
                }
                settings.LastPhoneNumber = phoneNumber;
                SaveAppSettings(settings);

                var cmbPhone = this.Controls.Find("cmbPhone", true)[0] as ComboBox;
                if (cmbPhone != null && !cmbPhone.Items.Contains(phoneNumber))
                {
                    cmbPhone.Items.Add(phoneNumber);
                }
            }
            catch
            {
                // Ignore save errors
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Ensure all controls are properly initialized
            EnsureControlsInitialized();

            // Subscribe to events
            if (signalProcessor != null)
            {
                signalProcessor.DebugMessage += SignalProcessor_DebugMessage;
            }

            if (telegramService != null)
            {
                telegramService.DebugMessage += TelegramService_DebugMessage;
            }
        }


        private void EnsureControlsInitialized()
        {
            // Find and verify critical controls exist
            var controlsToCheck = new[] { "lvLiveSignals", "lvSelected", "lvChannels" };

            foreach (var controlName in controlsToCheck)
            {
                var controls = this.Controls.Find(controlName, true);
                if (controls.Length == 0)
                {
                    MessageBox.Show($"Critical control '{controlName}' not found! UI may not function properly.",
                                   "Initialization Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        private void SaveMT4Path(string path)
        {
            try
            {
                var settings = LoadAppSettings();
                settings.MT4Path = path;
                SaveAppSettings(settings);
            }
            catch
            {
                // Ignore save errors
            }
        }

        private AppSettings LoadAppSettings()
        {
            try
            {
                if (File.Exists("app_settings.json"))
                {
                    var json = File.ReadAllText("app_settings.json");
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
                // Return default settings on error
            }
            return new AppSettings();
        }

        private void SaveAppSettings(AppSettings settings)
        {
            try
            {
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText("app_settings.json", json);
            }
            catch
            {
                // Ignore save errors
            }
        }

        #endregion
    }
}