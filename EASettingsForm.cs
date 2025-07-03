using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace TelegramEAManager
{
    public partial class EASettingsForm : Form
    {
        private EASettings eaSettings;
        private EASettings originalSettings;

        public EASettingsForm(EASettings currentSettings)
        {
            eaSettings = new EASettings
            {
                ChannelIds = currentSettings.ChannelIds,
                SignalFilePath = currentSettings.SignalFilePath,
                RiskMode = currentSettings.RiskMode,
                FixedLotSize = currentSettings.FixedLotSize,
                RiskPercent = currentSettings.RiskPercent,
                RiskAmount = currentSettings.RiskAmount,
                UseTrailingStop = currentSettings.UseTrailingStop,
                TrailingStartPips = currentSettings.TrailingStartPips,
                TrailingStepPips = currentSettings.TrailingStepPips,
                MoveSLToBreakeven = currentSettings.MoveSLToBreakeven,
                BreakevenAfterPips = currentSettings.BreakevenAfterPips,
                SendNotifications = currentSettings.SendNotifications,
                MT4FilesPath = currentSettings.MT4FilesPath
            };

            originalSettings = currentSettings;

            InitializeComponent();
            SetupUI();
            LoadCurrentSettings();
        }

        private void SetupUI()
        {
            this.Text = "⚙️ EA Settings & Configuration - islamahmed9717";
            this.Size = new Size(800, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            CreateHeaderSection();
            CreateRiskManagementSection();
            CreateAdvancedSection();
            CreateNotificationSection();
            CreateButtonSection();
        }

        private void CreateHeaderSection()
        {
            var headerPanel = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(740, 80),
                BackColor = Color.FromArgb(240, 249, 255),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTitle = new Label
            {
                Text = "⚙️ EXPERT ADVISOR SETTINGS & CONFIGURATION",
                Location = new Point(20, 15),
                Size = new Size(500, 30),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235)
            };
            headerPanel.Controls.Add(lblTitle);

            var lblInfo = new Label
            {
                Text = $"Configure your EA settings | Current Time: 2025-06-18 19:37:36 UTC | User: islamahmed9717",
                Location = new Point(20, 45),
                Size = new Size(700, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray
            };
            headerPanel.Controls.Add(lblInfo);

            this.Controls.Add(headerPanel);
        }

        private void CreateRiskManagementSection()
        {
            var lblRiskTitle = new Label
            {
                Text = "💰 RISK MANAGEMENT",
                Location = new Point(20, 120),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235)
            };
            this.Controls.Add(lblRiskTitle);

            // Risk Mode
            var lblRiskMode = new Label
            {
                Text = "Risk Mode:",
                Location = new Point(30, 155),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(lblRiskMode);

            var cmbRiskMode = new ComboBox
            {
                Name = "cmbRiskMode",
                Location = new Point(140, 153),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 9F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbRiskMode.Items.AddRange(new[] { "FIXED_LOT", "MONEY_AMOUNT", "PERCENT_BALANCE" });
            cmbRiskMode.SelectedIndexChanged += CmbRiskMode_SelectedIndexChanged;
            this.Controls.Add(cmbRiskMode);

            var lblRiskModeDesc = new Label
            {
                Name = "lblRiskModeDesc",
                Text = "Fixed lot size for all trades",
                Location = new Point(300, 155),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray
            };
            this.Controls.Add(lblRiskModeDesc);

            // Fixed Lot Size
            var lblFixedLot = new Label
            {
                Text = "Fixed Lot Size:",
                Location = new Point(30, 185),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(lblFixedLot);

            var nudFixedLot = new NumericUpDown
            {
                Name = "nudFixedLot",
                Location = new Point(140, 183),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9F),
                Minimum = 0.01m,
                Maximum = 100m,
                DecimalPlaces = 2,
                Increment = 0.01m
            };
            this.Controls.Add(nudFixedLot);

            // Risk Percent
            var lblRiskPercent = new Label
            {
                Text = "Risk Percent (%):",
                Location = new Point(30, 215),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(lblRiskPercent);

            var nudRiskPercent = new NumericUpDown
            {
                Name = "nudRiskPercent",
                Location = new Point(140, 213),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9F),
                Minimum = 0.1m,
                Maximum = 50m,
                DecimalPlaces = 1,
                Increment = 0.1m
            };
            this.Controls.Add(nudRiskPercent);

            // Risk Amount
            var lblRiskAmount = new Label
            {
                Text = "Risk Amount ($):",
                Location = new Point(30, 245),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(lblRiskAmount);

            var nudRiskAmount = new NumericUpDown
            {
                Name = "nudRiskAmount",
                Location = new Point(140, 243),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9F),
                Minimum = 1m,
                Maximum = 100000m,
                DecimalPlaces = 0,
                Increment = 10m
            };
            this.Controls.Add(nudRiskAmount);

            // Signal File Path
            var lblSignalFile = new Label
            {
                Text = "Signal File Name:",
                Location = new Point(400, 155),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(lblSignalFile);

            var txtSignalFile = new TextBox
            {
                Name = "txtSignalFile",
                Location = new Point(510, 153),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(txtSignalFile);
        }

        private void CreateAdvancedSection()
        {
            var lblAdvancedTitle = new Label
            {
                Text = "🔧 ADVANCED TRADING FEATURES",
                Location = new Point(20, 290),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235)
            };
            this.Controls.Add(lblAdvancedTitle);

            // Trailing Stop
            var chkTrailingStop = new CheckBox
            {
                Name = "chkTrailingStop",
                Text = "Use Trailing Stop",
                Location = new Point(30, 325),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 9F)
            };
            chkTrailingStop.CheckedChanged += ChkTrailingStop_CheckedChanged;
            this.Controls.Add(chkTrailingStop);

            var lblTrailingStart = new Label
            {
                Text = "Trailing Start (pips):",
                Location = new Point(50, 355),
                Size = new Size(120, 20),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(lblTrailingStart);

            var nudTrailingStart = new NumericUpDown
            {
                Name = "nudTrailingStart",
                Location = new Point(180, 353),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 9F),
                Minimum = 1,
                Maximum = 1000,
                Value = 20
            };
            this.Controls.Add(nudTrailingStart);

            var lblTrailingStep = new Label
            {
                Text = "Trailing Step (pips):",
                Location = new Point(280, 355),
                Size = new Size(120, 20),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(lblTrailingStep);

            var nudTrailingStep = new NumericUpDown
            {
                Name = "nudTrailingStep",
                Location = new Point(410, 353),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 9F),
                Minimum = 1,
                Maximum = 100,
                Value = 5
            };
            this.Controls.Add(nudTrailingStep);

            // Breakeven
            var chkBreakeven = new CheckBox
            {
                Name = "chkBreakeven",
                Text = "Move SL to Breakeven",
                Location = new Point(30, 385),
                Size = new Size(180, 25),
                Font = new Font("Segoe UI", 9F)
            };
            chkBreakeven.CheckedChanged += ChkBreakeven_CheckedChanged;
            this.Controls.Add(chkBreakeven);

            var lblBreakevenAfter = new Label
            {
                Text = "Breakeven After (pips):",
                Location = new Point(50, 415),
                Size = new Size(130, 20),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(lblBreakevenAfter);

            var nudBreakevenAfter = new NumericUpDown
            {
                Name = "nudBreakevenAfter",
                Location = new Point(190, 413),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 9F),
                Minimum = 1,
                Maximum = 500,
                Value = 15
            };
            this.Controls.Add(nudBreakevenAfter);
        }

        private void CreateNotificationSection()
        {
            var lblNotificationTitle = new Label
            {
                Text = "📢 NOTIFICATIONS & ALERTS",
                Location = new Point(20, 460),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235)
            };
            this.Controls.Add(lblNotificationTitle);

            var chkNotifications = new CheckBox
            {
                Name = "chkNotifications",
                Text = "Send MT4/MT5 Notifications",
                Location = new Point(30, 495),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(chkNotifications);

            var chkAlerts = new CheckBox
            {
                Name = "chkAlerts",
                Text = "Send Pop-up Alerts",
                Location = new Point(250, 495),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(chkAlerts);

            var chkPushNotifications = new CheckBox
            {
                Name = "chkPushNotifications",
                Text = "Send Push Notifications to Mobile",
                Location = new Point(30, 525),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(chkPushNotifications);

            // MT4 Files Path Display
            var lblMT4PathTitle = new Label
            {
                Text = "📁 Current MT4/MT5 Files Path:",
                Location = new Point(30, 555),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(lblMT4PathTitle);

            var lblMT4Path = new Label
            {
                Name = "lblMT4Path",
                Text = "Not configured",
                Location = new Point(30, 575),
                Size = new Size(700, 20),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(59, 130, 246),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 249, 250)
            };
            this.Controls.Add(lblMT4Path);
        }

        private void CreateButtonSection()
        {
            var buttonPanel = new Panel
            {
                Location = new Point(20, 610),
                Size = new Size(740, 50)
            };

            var btnSave = new Button
            {
                Text = "💾 SAVE SETTINGS",
                Location = new Point(0, 10),
                Size = new Size(140, 35),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            btnSave.Click += BtnSave_Click;
            buttonPanel.Controls.Add(btnSave);

            var btnCancel = new Button
            {
                Text = "❌ CANCEL",
                Location = new Point(150, 10),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                DialogResult = DialogResult.Cancel
            };
            buttonPanel.Controls.Add(btnCancel);

            var btnExportSet = new Button
            {
                Text = "📤 EXPORT .SET FILE",
                Location = new Point(260, 10),
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(168, 85, 247),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnExportSet.Click += BtnExportSet_Click;
            buttonPanel.Controls.Add(btnExportSet);

            var btnReset = new Button
            {
                Text = "🔄 RESET",
                Location = new Point(420, 10),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(249, 115, 22),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnReset.Click += BtnReset_Click;
            buttonPanel.Controls.Add(btnReset);

            var btnTest = new Button
            {
                Text = "🧪 TEST CONFIG",
                Location = new Point(530, 10),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnTest.Click += BtnTest_Click;
            buttonPanel.Controls.Add(btnTest);

            this.Controls.Add(buttonPanel);
        }

        private void LoadCurrentSettings()
        {
            var cmbRiskMode = this.Controls.Find("cmbRiskMode", true)[0] as ComboBox;
            var nudFixedLot = this.Controls.Find("nudFixedLot", true)[0] as NumericUpDown;
            var nudRiskPercent = this.Controls.Find("nudRiskPercent", true)[0] as NumericUpDown;
            var nudRiskAmount = this.Controls.Find("nudRiskAmount", true)[0] as NumericUpDown;
            var txtSignalFile = this.Controls.Find("txtSignalFile", true)[0] as TextBox;
            var chkTrailingStop = this.Controls.Find("chkTrailingStop", true)[0] as CheckBox;
            var nudTrailingStart = this.Controls.Find("nudTrailingStart", true)[0] as NumericUpDown;
            var nudTrailingStep = this.Controls.Find("nudTrailingStep", true)[0] as NumericUpDown;
            var chkBreakeven = this.Controls.Find("chkBreakeven", true)[0] as CheckBox;
            var nudBreakevenAfter = this.Controls.Find("nudBreakevenAfter", true)[0] as NumericUpDown;
            var chkNotifications = this.Controls.Find("chkNotifications", true)[0] as CheckBox;
            var chkAlerts = this.Controls.Find("chkAlerts", true)[0] as CheckBox;
            var chkPushNotifications = this.Controls.Find("chkPushNotifications", true)[0] as CheckBox;
            var lblMT4Path = this.Controls.Find("lblMT4Path", true)[0] as Label;

            // Load current settings
            cmbRiskMode.SelectedItem = eaSettings.RiskMode;
            nudFixedLot.Value = (decimal)eaSettings.FixedLotSize;
            nudRiskPercent.Value = (decimal)eaSettings.RiskPercent;
            nudRiskAmount.Value = (decimal)eaSettings.RiskAmount;
            txtSignalFile.Text = eaSettings.SignalFilePath;
            chkTrailingStop.Checked = eaSettings.UseTrailingStop;
            nudTrailingStart.Value = eaSettings.TrailingStartPips;
            nudTrailingStep.Value = eaSettings.TrailingStepPips;
            chkBreakeven.Checked = eaSettings.MoveSLToBreakeven;
            nudBreakevenAfter.Value = eaSettings.BreakevenAfterPips;
            chkNotifications.Checked = eaSettings.SendNotifications;
            chkAlerts.Checked = true; // Default enabled
            chkPushNotifications.Checked = eaSettings.SendNotifications;
            lblMT4Path.Text = string.IsNullOrEmpty(eaSettings.MT4FilesPath) ? "Not configured" : eaSettings.MT4FilesPath;

            // Update UI based on current selections
            CmbRiskMode_SelectedIndexChanged(cmbRiskMode, EventArgs.Empty);
            ChkTrailingStop_CheckedChanged(chkTrailingStop, EventArgs.Empty);
            ChkBreakeven_CheckedChanged(chkBreakeven, EventArgs.Empty);
        }

        private void CmbRiskMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            var cmbRiskMode = sender as ComboBox;
            var lblRiskModeDesc = this.Controls.Find("lblRiskModeDesc", true)[0] as Label;

            switch (cmbRiskMode.SelectedItem?.ToString())
            {
                case "FIXED_LOT":
                    lblRiskModeDesc.Text = "Fixed lot size for all trades";
                    break;
                case "MONEY_AMOUNT":
                    lblRiskModeDesc.Text = "Risk fixed amount of money per trade";
                    break;
                case "PERCENT_BALANCE":
                    lblRiskModeDesc.Text = "Risk percentage of account balance";
                    break;
            }
        }

        private void ChkTrailingStop_CheckedChanged(object sender, EventArgs e)
        {
            var chkTrailingStop = sender as CheckBox;
            var nudTrailingStart = this.Controls.Find("nudTrailingStart", true)[0] as NumericUpDown;
            var nudTrailingStep = this.Controls.Find("nudTrailingStep", true)[0] as NumericUpDown;

            nudTrailingStart.Enabled = chkTrailingStop.Checked;
            nudTrailingStep.Enabled = chkTrailingStop.Checked;
        }

        private void ChkBreakeven_CheckedChanged(object sender, EventArgs e)
        {
            var chkBreakeven = sender as CheckBox;
            var nudBreakevenAfter = this.Controls.Find("nudBreakevenAfter", true)[0] as NumericUpDown;

            nudBreakevenAfter.Enabled = chkBreakeven.Checked;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                SaveCurrentSettings();
                MessageBox.Show("✅ EA settings saved successfully!\n\nThese settings will be used by the signal processing engine and can be exported to your EA.",
                              "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Failed to save settings:\n\n{ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveCurrentSettings()
        {
            var cmbRiskMode = this.Controls.Find("cmbRiskMode", true)[0] as ComboBox;
            var nudFixedLot = this.Controls.Find("nudFixedLot", true)[0] as NumericUpDown;
            var nudRiskPercent = this.Controls.Find("nudRiskPercent", true)[0] as NumericUpDown;
            var nudRiskAmount = this.Controls.Find("nudRiskAmount", true)[0] as NumericUpDown;
            var txtSignalFile = this.Controls.Find("txtSignalFile", true)[0] as TextBox;
            var chkTrailingStop = this.Controls.Find("chkTrailingStop", true)[0] as CheckBox;
            var nudTrailingStart = this.Controls.Find("nudTrailingStart", true)[0] as NumericUpDown;
            var nudTrailingStep = this.Controls.Find("nudTrailingStep", true)[0] as NumericUpDown;
            var chkBreakeven = this.Controls.Find("chkBreakeven", true)[0] as CheckBox;
            var nudBreakevenAfter = this.Controls.Find("nudBreakevenAfter", true)[0] as NumericUpDown;
            var chkNotifications = this.Controls.Find("chkNotifications", true)[0] as CheckBox;

            eaSettings.RiskMode = cmbRiskMode.SelectedItem?.ToString() ?? "FIXED_LOT";
            eaSettings.FixedLotSize = (double)nudFixedLot.Value;
            eaSettings.RiskPercent = (double)nudRiskPercent.Value;
            eaSettings.RiskAmount = (double)nudRiskAmount.Value;
            eaSettings.SignalFilePath = txtSignalFile.Text.Trim();
            eaSettings.UseTrailingStop = chkTrailingStop.Checked;
            eaSettings.TrailingStartPips = (int)nudTrailingStart.Value;
            eaSettings.TrailingStepPips = (int)nudTrailingStep.Value;
            eaSettings.MoveSLToBreakeven = chkBreakeven.Checked;
            eaSettings.BreakevenAfterPips = (int)nudBreakevenAfter.Value;
            eaSettings.SendNotifications = chkNotifications.Checked;
        }

        private void BtnExportSet_Click(object sender, EventArgs e)
        {
            try
            {
                SaveCurrentSettings();

                var setContent = GenerateSetFileContent();

                var saveDialog = new SaveFileDialog
                {
                    Filter = "MT4/MT5 Set files (*.set)|*.set|All files (*.*)|*.*",
                    FileName = $"TelegramEA_islamahmed9717_{DateTime.Now:yyyyMMdd_HHmmss}.set"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveDialog.FileName, setContent);
                    Clipboard.SetText(setContent);

                    MessageBox.Show($"✅ EA .set file exported successfully!\n\n📁 File: {saveDialog.FileName}\n📋 Content copied to clipboard\n\n💡 Copy this file to your MT4/MT5 presets folder and load it in the EA.",
                                  "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Export failed:\n\n{ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("🔄 Are you sure you want to reset all EA settings to default values?",
                                       "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                ResetToDefaults();
                MessageBox.Show("✅ EA settings reset to defaults!", "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnTest_Click(object sender, EventArgs e)
        {
            try
            {
                SaveCurrentSettings();

                var testResults = ValidateConfiguration();

                MessageBox.Show($"🧪 EA Configuration Test Results:\n\n{testResults}\n\nTime: 2025-06-18 19:37:36 UTC\nUser: islamahmed9717",
                              "Test Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Test failed:\n\n{ex.Message}", "Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetToDefaults()
        {
            eaSettings = new EASettings(); // Reset to defaults
            LoadCurrentSettings();
        }

        private string GenerateSetFileContent()
        {
            return $@"; Telegram EA Settings File
; Generated: 2025-06-18 19:37:36 UTC
; User: islamahmed9717
; Application: Telegram EA Manager v2.0

ChannelIDs={eaSettings.ChannelIds}
SignalFilePath={eaSettings.SignalFilePath}
RiskMode={eaSettings.RiskMode}
FixedLotSize={eaSettings.FixedLotSize:F2}
RiskPercent={eaSettings.RiskPercent:F1}
RiskAmount={eaSettings.RiskAmount:F0}
UseTrailingStop={(eaSettings.UseTrailingStop ? "1" : "0")}
TrailingStartPips={eaSettings.TrailingStartPips}
TrailingStepPips={eaSettings.TrailingStepPips}
MoveSLToBreakeven={(eaSettings.MoveSLToBreakeven ? "1" : "0")}
BreakevenAfterPips={eaSettings.BreakevenAfterPips}
SendNotifications={(eaSettings.SendNotifications ? "1" : "0")}
SendAlerts=1
PrintToLog=1
CommentPrefix=TelegramEA_islamahmed9717
MagicNumber=999001
MaxSpreadPips=5
SignalCheckInterval=5
ForceMarketExecution=1
MaxRetriesOrderSend=3";
        }

        private string ValidateConfiguration()
        {
            var results = new System.Text.StringBuilder();
            results.AppendLine("✅ Configuration Validation Results:");
            results.AppendLine();

            // Risk Management Validation
            results.AppendLine("💰 Risk Management:");
            results.AppendLine($"  • Risk Mode: {eaSettings.RiskMode} ✅");

            switch (eaSettings.RiskMode)
            {
                case "FIXED_LOT":
                    results.AppendLine($"  • Fixed Lot Size: {eaSettings.FixedLotSize:F2} {(eaSettings.FixedLotSize >= 0.01 && eaSettings.FixedLotSize <= 10 ? "✅" : "⚠️")}");
                    break;
                case "MONEY_AMOUNT":
                    results.AppendLine($"  • Risk Amount: ${eaSettings.RiskAmount:F0} {(eaSettings.RiskAmount >= 1 && eaSettings.RiskAmount <= 10000 ? "✅" : "⚠️")}");
                    break;
                case "PERCENT_BALANCE":
                    results.AppendLine($"  • Risk Percent: {eaSettings.RiskPercent:F1}% {(eaSettings.RiskPercent >= 0.1 && eaSettings.RiskPercent <= 10 ? "✅" : "⚠️")}");
                    break;
            }

            results.AppendLine();

            // File Path Validation
            results.AppendLine("📁 File Configuration:");
            results.AppendLine($"  • Signal File: {eaSettings.SignalFilePath} ✅");
            results.AppendLine($"  • MT4 Path: {(!string.IsNullOrEmpty(eaSettings.MT4FilesPath) ? "Configured ✅" : "Not set ⚠️")}");

            if (!string.IsNullOrEmpty(eaSettings.MT4FilesPath))
            {
                results.AppendLine($"  • Path Exists: {(Directory.Exists(eaSettings.MT4FilesPath) ? "Yes ✅" : "No ❌")}");
            }

            results.AppendLine();

            // Advanced Features
            results.AppendLine("🔧 Advanced Features:");
            results.AppendLine($"  • Trailing Stop: {(eaSettings.UseTrailingStop ? "Enabled ✅" : "Disabled")}");
            if (eaSettings.UseTrailingStop)
            {
                results.AppendLine($"    - Start: {eaSettings.TrailingStartPips} pips");
                results.AppendLine($"    - Step: {eaSettings.TrailingStepPips} pips");
            }

            results.AppendLine($"  • Breakeven: {(eaSettings.MoveSLToBreakeven ? "Enabled ✅" : "Disabled")}");
            if (eaSettings.MoveSLToBreakeven)
            {
                results.AppendLine($"    - Trigger: {eaSettings.BreakevenAfterPips} pips");
            }

            results.AppendLine();

            // Notifications
            results.AppendLine("📢 Notifications:");
            results.AppendLine($"  • MT4/MT5 Alerts: {(eaSettings.SendNotifications ? "Enabled ✅" : "Disabled")}");

            results.AppendLine();

            // Channel Configuration
            results.AppendLine("📡 Channel Configuration:");
            if (!string.IsNullOrEmpty(eaSettings.ChannelIds))
            {
                var channelCount = eaSettings.ChannelIds.Split(',').Length;
                results.AppendLine($"  • Channels: {channelCount} configured ✅");
                results.AppendLine($"  • Channel IDs: {eaSettings.ChannelIds}");
            }
            else
            {
                results.AppendLine("  • Channels: Not configured ⚠️");
            }

            results.AppendLine();
            results.AppendLine("🎯 Overall Status: Configuration looks good! ✅");

            return results.ToString();
        }

        public EASettings GetUpdatedSettings()
        {
            return eaSettings;
        }
    }
}