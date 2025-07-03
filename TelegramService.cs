using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using WTelegram;
using TL;
using System.Threading;

namespace TelegramEAManager
{
    public class TelegramService
    {
        private Client? client;
        private int apiId;
        private string apiHash = "";
        private string phoneNumber = "";
        private User? me;
        private readonly Dictionary<long, int> lastMessageIds = new Dictionary<long, int>();
        private System.Threading.Timer? messagePollingTimer;
        private readonly List<long> monitoredChannels = new List<long>();
        private bool isMonitoring = false;

        // Events for real-time message processing
        public event EventHandler<(string message, long channelId, string channelName, DateTime messageTime)>? NewMessageReceived;

        public event EventHandler<string>? ErrorOccurred;
        public event EventHandler<string>? DebugMessage; // Add debug event


        public TelegramService()
        {
            LoadApiCredentials();
        }

        private void LoadApiCredentials()
        {
            // Try to load from settings file first
            if (TryLoadFromSettingsFile())
                return;

            // Try to load from App.config
            if (TryLoadFromAppConfig())
                return;

            // If neither works, show setup dialog
            ShowApiSetupDialog();
        }

        private bool TryLoadFromSettingsFile()
        {
            try
            {
                string settingsFile = "telegram_api.json";
                if (File.Exists(settingsFile))
                {
                    var json = File.ReadAllText(settingsFile);
                    var settings = JsonConvert.DeserializeObject<ApiSettings>(json);

                    if (settings != null && settings.ApiId > 0 && !string.IsNullOrEmpty(settings.ApiHash))
                    {
                        apiId = settings.ApiId;
                        apiHash = settings.ApiHash;
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore errors, try next method
            }
            return false;
        }

        private bool TryLoadFromAppConfig()
        {
            try
            {
                var apiIdStr = ConfigurationManager.AppSettings["TelegramApiId"];
                var apiHashStr = ConfigurationManager.AppSettings["TelegramApiHash"];

                if (!string.IsNullOrEmpty(apiIdStr) && !string.IsNullOrEmpty(apiHashStr))
                {
                    apiId = int.Parse(apiIdStr);
                    apiHash = apiHashStr;
                    return true;
                }
            }
            catch
            {
                // Ignore errors, show setup dialog
            }
            return false;
        }

        private void ShowApiSetupDialog()
        {
            using (var setupForm = new Form())
            {
                setupForm.Text = "🔑 Telegram API Setup - islamahmed9717";
                setupForm.Size = new Size(600, 500);
                setupForm.StartPosition = FormStartPosition.CenterScreen;
                setupForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                setupForm.MaximizeBox = false;
                setupForm.MinimizeBox = false;
                setupForm.BackColor = Color.White;

                // Title
                var lblTitle = new Label
                {
                    Text = "🔑 TELEGRAM API CREDENTIALS SETUP",
                    Location = new Point(20, 20),
                    Size = new Size(550, 30),
                    Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(37, 99, 235),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                setupForm.Controls.Add(lblTitle);

                // Instructions
                var lblInstructions = new Label
                {
                    Text = "📋 FOLLOW THESE STEPS TO GET YOUR API CREDENTIALS:",
                    Location = new Point(20, 60),
                    Size = new Size(550, 25),
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(249, 115, 22)
                };
                setupForm.Controls.Add(lblInstructions);

                // Step-by-step instructions
                var instructions = new TextBox
                {
                    Location = new Point(20, 90),
                    Size = new Size(550, 180),
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Font = new Font("Segoe UI", 10F),
                    Text = @"STEP 1: 🌐 Open your web browser and go to: https://my.telegram.org

STEP 2: 📱 Login with your phone number (same as you'll use in this app)

STEP 3: 🔐 Enter the verification code sent to your Telegram app

STEP 4: 🆕 Click ""API development tools""

STEP 5: 📝 Fill out the form:
   • App title: Telegram EA Manager
   • Short name: ea_manager_" + DateTime.Now.ToString("yyyyMMdd") + @"
   • Description: Trading signal manager for islamahmed9717
   • Platform: Desktop
   • URL: (leave empty)

STEP 6: ✅ Click ""Create application""

STEP 7: 📋 Copy the api_id (numbers) and api_hash (long string) below"
                };
                setupForm.Controls.Add(instructions);

                // API ID input
                var lblApiId = new Label
                {
                    Text = "📋 API ID (numbers only):",
                    Location = new Point(20, 290),
                    Size = new Size(200, 25),
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                setupForm.Controls.Add(lblApiId);

                var txtApiId = new TextBox
                {
                    Location = new Point(230, 290),
                    Size = new Size(200, 25),
                    Font = new Font("Segoe UI", 11F),
                    PlaceholderText = "e.g. 1234567"
                };
                txtApiId.KeyPress += (s, e) =>
                {
                    if (!char.IsDigit(e.KeyChar) && e.KeyChar != 8)
                        e.Handled = true;
                };
                setupForm.Controls.Add(txtApiId);

                // API Hash input
                var lblApiHash = new Label
                {
                    Text = "🔑 API Hash (long string):",
                    Location = new Point(20, 330),
                    Size = new Size(200, 25),
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                setupForm.Controls.Add(lblApiHash);

                var txtApiHash = new TextBox
                {
                    Location = new Point(230, 330),
                    Size = new Size(340, 25),
                    Font = new Font("Segoe UI", 11F),
                    PlaceholderText = "e.g. abcd1234efgh5678..."
                };
                setupForm.Controls.Add(txtApiHash);

                // Open website button
                var btnOpenWebsite = new Button
                {
                    Text = "🌐 OPEN TELEGRAM API WEBSITE",
                    Location = new Point(450, 290),
                    Size = new Size(120, 65),
                    BackColor = Color.FromArgb(34, 197, 94),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };
                btnOpenWebsite.Click += (s, e) =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://my.telegram.org") { UseShellExecute = true });
                    }
                    catch
                    {
                        MessageBox.Show("Please manually open: https://my.telegram.org", "Open Browser", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };
                setupForm.Controls.Add(btnOpenWebsite);

                // Save button
                var btnSave = new Button
                {
                    Text = "💾 SAVE & CONTINUE",
                    Location = new Point(200, 380),
                    Size = new Size(150, 40),
                    BackColor = Color.FromArgb(37, 99, 235),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    DialogResult = DialogResult.OK
                };
                setupForm.Controls.Add(btnSave);

                // Exit button
                var btnExit = new Button
                {
                    Text = "❌ EXIT APP",
                    Location = new Point(370, 380),
                    Size = new Size(120, 40),
                    BackColor = Color.FromArgb(220, 38, 38),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    DialogResult = DialogResult.Cancel
                };
                setupForm.Controls.Add(btnExit);

                // Validation
                btnSave.Click += (s, e) =>
                {
                    if (string.IsNullOrEmpty(txtApiId.Text) || string.IsNullOrEmpty(txtApiHash.Text))
                    {
                        MessageBox.Show("❌ Please enter both API ID and API Hash!", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (!int.TryParse(txtApiId.Text, out int testApiId) || testApiId <= 0)
                    {
                        MessageBox.Show("❌ API ID must be a valid number!", "Invalid API ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (txtApiHash.Text.Length < 10)
                    {
                        MessageBox.Show("❌ API Hash seems too short. Please check!", "Invalid API Hash", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Save credentials
                    apiId = testApiId;
                    apiHash = txtApiHash.Text.Trim();
                    SaveApiCredentials();
                    setupForm.DialogResult = DialogResult.OK;
                };

                setupForm.AcceptButton = btnSave;
                setupForm.CancelButton = btnExit;

                var result = setupForm.ShowDialog();
                if (result != DialogResult.OK)
                {
                    // User cancelled, exit application
                    Environment.Exit(0);
                }
            }
        }

        private void SaveApiCredentials()
        {
            try
            {
                var settings = new ApiSettings
                {
                    ApiId = apiId,
                    ApiHash = apiHash,
                    SavedDate = DateTime.UtcNow,
                    Username = "islamahmed9717"
                };

                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText("telegram_api.json", json);

                MessageBox.Show("✅ API credentials saved successfully!\n\n🔐 Your credentials are stored securely in telegram_api.json\n📱 You won't need to enter them again!",
                               "Credentials Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"⚠️ Warning: Could not save credentials to file.\n\n{ex.Message}\n\nYou may need to enter them again next time.",
                               "Save Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string? Config(string what)
        {
            switch (what)
            {
                case "api_id": return apiId.ToString();
                case "api_hash": return apiHash;
                case "phone_number": return phoneNumber;
                case "verification_code": return RequestVerificationCode();
                case "first_name": return "islamahmed9717";
                case "last_name": return "";
                case "password": return RequestPassword();
                case "session_pathname": return "session.dat";
                default: return null;
            }
        }

        private string RequestVerificationCode()
        {
            using (var codeForm = new Form())
            {
                codeForm.Text = "📱 Telegram Verification Code";
                codeForm.Size = new Size(400, 200);
                codeForm.StartPosition = FormStartPosition.CenterScreen;
                codeForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                codeForm.MaximizeBox = false;

                var lblMessage = new Label
                {
                    Text = "📱 Enter the verification code sent to your Telegram:",
                    Location = new Point(20, 20),
                    Size = new Size(350, 40),
                    Font = new Font("Segoe UI", 10F)
                };
                codeForm.Controls.Add(lblMessage);

                var txtCode = new TextBox
                {
                    Location = new Point(20, 70),
                    Size = new Size(200, 25),
                    Font = new Font("Segoe UI", 12F),
                    MaxLength = 6
                };
                codeForm.Controls.Add(txtCode);

                var btnOK = new Button
                {
                    Text = "✅ Confirm",
                    Location = new Point(240, 70),
                    Size = new Size(100, 25),
                    DialogResult = DialogResult.OK
                };
                codeForm.Controls.Add(btnOK);

                codeForm.AcceptButton = btnOK;
                txtCode.Focus();

                if (codeForm.ShowDialog() == DialogResult.OK)
                {
                    return txtCode.Text.Trim();
                }
                return "";
            }
        }

        private string RequestPassword()
        {
            using (var passwordForm = new Form())
            {
                passwordForm.Text = "🔐 Two-Factor Authentication";
                passwordForm.Size = new Size(400, 200);
                passwordForm.StartPosition = FormStartPosition.CenterScreen;
                passwordForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                passwordForm.MaximizeBox = false;

                var lblMessage = new Label
                {
                    Text = "🔐 Enter your 2FA password:",
                    Location = new Point(20, 20),
                    Size = new Size(350, 40),
                    Font = new Font("Segoe UI", 10F)
                };
                passwordForm.Controls.Add(lblMessage);

                var txtPassword = new TextBox
                {
                    Location = new Point(20, 70),
                    Size = new Size(200, 25),
                    Font = new Font("Segoe UI", 12F),
                    UseSystemPasswordChar = true
                };
                passwordForm.Controls.Add(txtPassword);

                var btnOK = new Button
                {
                    Text = "✅ Confirm",
                    Location = new Point(240, 70),
                    Size = new Size(100, 25),
                    DialogResult = DialogResult.OK
                };
                passwordForm.Controls.Add(btnOK);

                passwordForm.AcceptButton = btnOK;
                txtPassword.Focus();

                if (passwordForm.ShowDialog() == DialogResult.OK)
                {
                    return txtPassword.Text;
                }
                return "";
            }
        }

        public async Task<bool> ConnectAsync(string phone)
        {
            try
            {
                phoneNumber = phone;
                client = new Client(Config);
                me = await client.LoginUserIfNeeded();
                return me != null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Connection failed: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public bool IsUserAuthorized()
        {
            return client != null && me != null;
        }

        public async Task<List<ChannelInfo>> GetChannelsAsync()
        {
            var channels = new List<ChannelInfo>();

            try
            {
                if (client == null) return channels;

                var dialogs = await client.Messages_GetAllDialogs();

                foreach (var dialog in dialogs.dialogs)
                {
                    try
                    {
                        // Check for channels
                        if (dialogs.chats.TryGetValue(dialog.Peer.ID, out var chat))
                        {
                            if (chat is Channel channel)
                            {
                                channels.Add(new ChannelInfo
                                {
                                    Id = channel.ID,  // This is already long
                                    Title = channel.Title ?? "",
                                    Username = channel.username ?? "",
                                    Type = DetermineChannelType(channel),
                                    MembersCount = channel.participants_count,
                                    AccessHash = channel.access_hash,
                                    LastActivity = DateTime.UtcNow
                                });

                                // Debug logging
                                Console.WriteLine($"Found channel: {channel.Title} (ID: {channel.ID}, Username: {channel.username})");
                            }
                            else if (chat is Chat regularChat)
                            {
                                // Also include regular chats if they match signal patterns
                                if (IsSignalRelatedChat(regularChat.Title))
                                {
                                    channels.Add(new ChannelInfo
                                    {
                                        Id = regularChat.ID,  // This is already long
                                        Title = regularChat.Title ?? "",
                                        Username = "",
                                        Type = "Group",
                                        MembersCount = regularChat.participants_count,
                                        AccessHash = 0,
                                        LastActivity = DateTime.UtcNow
                                    });

                                    Console.WriteLine($"Found chat: {regularChat.Title} (ID: {regularChat.ID})");
                                }
                            }
                        }

                        // Also check for bot channels or users that might be signal providers
                        if (dialog.Peer is PeerUser && dialogs.users.TryGetValue(dialog.Peer.ID, out var user))
                        {
                            if (user.IsBot && IsSignalRelatedChat(user.first_name + " " + user.last_name))
                            {
                                channels.Add(new ChannelInfo
                                {
                                    Id = user.ID,  // This is already long
                                    Title = $"{user.first_name} {user.last_name}".Trim(),
                                    Username = user.username ?? "",
                                    Type = "Bot",
                                    MembersCount = 0,
                                    AccessHash = user.access_hash,
                                    LastActivity = DateTime.UtcNow
                                });

                                Console.WriteLine($"Found bot: {user.first_name} {user.last_name} (ID: {user.ID})");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing dialog: {ex.Message}");
                    }
                }

                // Sort channels by title for easier finding
                channels = channels.OrderBy(c => c.Title).ToList();

                Console.WriteLine($"Total channels/chats found: {channels.Count}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to get channels: {ex.Message}");
            }

            return channels;
        }

        private bool IsSignalRelatedChat(string? title)
        {
            if (string.IsNullOrEmpty(title))
                return false;

            var lowerTitle = title.ToLower();
            var signalKeywords = new[] { "signal", "indicator", "trading", "forex", "crypto", "vip", "premium", "gold" };

            return signalKeywords.Any(keyword => lowerTitle.Contains(keyword));
        }

        /// <summary>
        /// Start monitoring selected channels for new messages
        /// </summary>
        public void StartMonitoring(List<ChannelInfo> channels)
        {
            try
            {
                OnDebugMessage($"Starting monitoring for {channels.Count} channels");

                // Clear previous monitoring
                StopMonitoring();

                // Set up monitored channels
                monitoredChannels.Clear();

                // Initialize lastMessageIds asynchronously
                Task.Run(async () =>
                {
                    foreach (var channel in channels)
                    {
                        monitoredChannels.Add(channel.Id);

                        // Get the latest message ID for this channel
                        try
                        {
                            var latestMessageId = await GetLatestMessageId(channel.Id, channel.AccessHash);
                            lastMessageIds[channel.Id] = latestMessageId;
                            OnDebugMessage($"Initialized channel {channel.Title} with latest message ID: {latestMessageId}");
                        }
                        catch (Exception ex)
                        {
                            OnDebugMessage($"Failed to get latest message ID for {channel.Title}: {ex.Message}");
                            lastMessageIds[channel.Id] = 0;
                        }
                    }

                    isMonitoring = true;

                    // Start polling timer after initialization is complete
                    messagePollingTimer = new System.Threading.Timer(
                        async _ => await PollAllChannelsAsync(),
                        null,
                        TimeSpan.FromSeconds(5), // Start after 5 seconds to ensure proper initialization
                        TimeSpan.FromSeconds(2)
                    );

                    OnDebugMessage("Monitoring started successfully - will only process NEW messages from now");
                }).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        OnErrorOccurred($"Failed to start monitoring: {task.Exception?.GetBaseException().Message}");
                        OnDebugMessage($"Monitoring error: {task.Exception}");
                    }
                });
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to start monitoring: {ex.Message}");
                OnDebugMessage($"Monitoring error: {ex}");
            }
        }

        private async Task<int> GetLatestMessageId(long channelId, long accessHash)
        {
            if (client == null) return 0;

            try
            {
                var dialogs = await client.Messages_GetAllDialogs();
                var channel = dialogs.chats.Values.OfType<Channel>().FirstOrDefault(c => c.ID == channelId);

                if (channel != null)
                {
                    var inputChannel = new InputChannel(channelId, channel.access_hash);
                    var history = await client.Messages_GetHistory(inputChannel, limit: 1);

                    var latestMessage = history.Messages.OfType<TL.Message>().FirstOrDefault();
                    return latestMessage?.ID ?? 0;
                }
            }
            catch (Exception ex)
            {
                OnDebugMessage($"Error getting latest message ID: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Stop monitoring channels
        /// </summary>
        public void StopMonitoring()
        {
            messagePollingTimer?.Dispose();
            messagePollingTimer = null;
            monitoredChannels.Clear();
        }

        /// <summary>
        /// Poll all monitored channels for new messages
        /// </summary>
        private async Task PollAllChannelsAsync()
        {
            if (!isMonitoring || client == null || !IsUserAuthorized())
            {
                OnDebugMessage("Skipping poll - not monitoring or not authorized");
                return;
            }

            try
            {
                OnDebugMessage($"Polling {monitoredChannels.Count} channels...");

                // Use parallel processing with limited concurrency
                var semaphore = new SemaphoreSlim(3, 3); // Process max 3 channels at once
                var tasks = monitoredChannels.Select(async channelId =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        await PollChannelForNewMessages(channelId);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                // Wait for all with timeout
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30))) // 30 second total timeout
                {
                    try
                    {
                        await Task.WhenAll(tasks).WaitAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        OnDebugMessage("Polling cycle timeout - some channels may not have been checked");
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error polling channels: {ex.Message}");
                OnDebugMessage($"Poll error: {ex}");
            }
        }

        /// <summary>
        /// Poll specific channel for new messages - FIXED VERSION
        /// </summary>
        /// <summary>
        /// Poll specific channel for new messages - FIXED VERSION
        /// </summary>
        private async Task PollChannelForNewMessages(long channelId)
        {
            try
            {
                if (client == null) return;

                OnDebugMessage($"Polling channel ID: {channelId}");

                // Use cancellation token to prevent hanging
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10))) // 10 second timeout
                {
                    try
                    {
                        // Get channel access hash with timeout
                        var dialogs = await client.Messages_GetAllDialogs().WaitAsync(cts.Token);

                        // Try to find as Channel
                        var channel = dialogs.chats.Values.OfType<Channel>().FirstOrDefault(c => c.ID == channelId);

                        if (channel != null)
                        {
                            OnDebugMessage($"Found channel: {channel.Title}");

                            var inputChannel = new InputChannel(channelId, channel.access_hash);

                            // Get history with timeout
                            var history = await client.Messages_GetHistory(inputChannel, limit: 10).WaitAsync(cts.Token);

                            var lastKnownId = lastMessageIds.GetValueOrDefault(channelId, 0);
                            OnDebugMessage($"Last known message ID for {channel.Title}: {lastKnownId}");

                            var newMessages = 0;
                            var messages = history.Messages.OfType<TL.Message>()
                                .OrderBy(m => m.ID)
                                .ToList(); // Convert to list to avoid multiple enumerations

                            foreach (var message in messages)
                            {
                                if (message.ID > lastKnownId && !string.IsNullOrEmpty(message.message))
                                {
                                    newMessages++;
                                    OnDebugMessage($"New message in {channel.Title}: ID={message.ID}, Length={message.message.Length}");

                                    // Process in background to avoid blocking
                                    var msgCopy = message.message;
                                    var msgId = message.ID;
                                    _ = Task.Run(() =>
                                    {
                                        try
                                        {
                                            OnNewMessageReceived(msgCopy, channelId, channel.Title ?? $"Channel_{channelId}");
                                        }
                                        catch (Exception ex)
                                        {
                                            OnDebugMessage($"Error processing message: {ex.Message}");
                                        }
                                    });

                                    lastMessageIds[channelId] = msgId;
                                }
                            }

                            if (newMessages == 0)
                            {
                                OnDebugMessage($"No new messages in {channel.Title}");
                            }
                        }
                        else
                        {
                            // Try as regular chat or user
                            var chat = dialogs.chats.Values.FirstOrDefault(c => c.ID == channelId);
                            if (chat != null)
                            {
                                string chatName = "";
                                InputPeer? inputPeer = null;

                                if (chat is Chat regularChatObj)
                                {
                                    chatName = regularChatObj.Title;
                                    inputPeer = new InputPeerChat(regularChatObj.ID);
                                    OnDebugMessage($"Found chat: {chatName}");
                                }
                                else if (chat is Channel channelObj)
                                {
                                    chatName = channelObj.Title;
                                    inputPeer = new InputPeerChannel(channelObj.ID, channelObj.access_hash);
                                    OnDebugMessage($"Found channel (as chat): {chatName}");
                                }

                                // Check if it's a user instead
                                var user = dialogs.users.Values.FirstOrDefault(u => u.ID == channelId);
                                if (user != null)
                                {
                                    chatName = $"{user.first_name} {user.last_name}".Trim();
                                    inputPeer = new InputPeerUser(user.ID, user.access_hash);
                                    OnDebugMessage($"Found user: {chatName}");
                                }

                                if (inputPeer != null)
                                {
                                    var history = await client.Messages_GetHistory(inputPeer, limit: 10).WaitAsync(cts.Token);
                                    var lastKnownId = lastMessageIds.GetValueOrDefault(channelId, 0);

                                    var messages = history.Messages.OfType<TL.Message>()
                                        .OrderBy(m => m.ID)
                                        .ToList();

                                    foreach (var message in messages)
                                    {
                                        if (message.ID > lastKnownId && !string.IsNullOrEmpty(message.message))
                                        {
                                            var msgCopy = message.message;
                                            var msgId = message.ID;

                                            OnDebugMessage($"New message: {msgCopy.Substring(0, Math.Min(50, msgCopy.Length))}...");

                                            // Process in background
                                            _ = Task.Run(() =>
                                            {
                                                try
                                                {
                                                    OnNewMessageReceived(msgCopy, channelId, chatName);
                                                }
                                                catch (Exception ex)
                                                {
                                                    OnDebugMessage($"Error processing message: {ex.Message}");
                                                }
                                            });

                                            lastMessageIds[channelId] = msgId;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                OnDebugMessage($"Channel/Chat {channelId} not found in dialogs");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        OnDebugMessage($"Polling timeout for channel {channelId}");
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Error polling channel {channelId}: {ex.Message}");
                OnDebugMessage($"Channel poll error: {ex}");
            }
        }
        protected virtual void OnDebugMessage(string message)
        {
            DebugMessage?.Invoke(this, $"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        protected virtual void OnNewMessageReceived(string message, long channelId, string channelName, DateTime messageTime = default)
        {
            // Use actual message time if provided, otherwise use current time
            var actualTime = messageTime != default ? messageTime : DateTime.Now;

            OnDebugMessage($"Message received from {channelName} at {actualTime:yyyy-MM-dd HH:mm:ss} UTC");
            NewMessageReceived?.Invoke(this, (message, channelId, channelName, actualTime));
        }

        protected virtual void OnErrorOccurred(string error)
        {
            ErrorOccurred?.Invoke(this, error);
        }

        /// <summary>
        /// Determine channel type based on properties
        /// </summary>
        private string DetermineChannelType(Channel channel)
        {
            var title = channel.Title?.ToLower() ?? "";

            if (title.Contains("vip")) return "VIP";
            if (title.Contains("premium")) return "Premium";
            if (title.Contains("signals") || title.Contains("signal")) return "Signals";
            if (title.Contains("gold")) return "Gold";
            if (title.Contains("crypto") || title.Contains("bitcoin") || title.Contains("btc")) return "Crypto";
            if (channel.IsGroup) return "Groups";

            return "Channel";
        }

        /// <summary>
        /// Get recent messages from a specific channel (legacy method for compatibility)
        /// </summary>
        public async Task<List<string>> PollChannelMessagesAsync(int channelId, long accessHash, int limit = 10)
        {
            var result = new List<string>();

            try
            {
                if (client == null) return result;

                var inputChannel = new InputChannel(channelId, accessHash);
                var history = await client.Messages_GetHistory(inputChannel, limit: limit);

                foreach (var message in history.Messages.OfType<TL.Message>().OrderBy(m => m.ID))
                {
                    if (!string.IsNullOrEmpty(message.message))
                    {
                        result.Add(message.message);
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Failed to poll messages: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Event handlers
        /// </summary>




        public void Dispose()
        {
            StopMonitoring();
            client?.Dispose();
        }
    }
}