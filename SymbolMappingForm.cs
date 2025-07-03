using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace TelegramEAManager
{
    public partial class SymbolMappingForm : Form
    {
        private SymbolMapping symbolMapping;
        private SymbolMapping originalMapping;

        public SymbolMappingForm(SymbolMapping currentMapping)
        {
            symbolMapping = new SymbolMapping
            {
                Mappings = new Dictionary<string, string>(currentMapping.Mappings),
                Prefix = currentMapping.Prefix,
                Suffix = currentMapping.Suffix,
                SkipPrefixSuffix = new List<string>(currentMapping.SkipPrefixSuffix),
                ExcludedSymbols = new List<string>(currentMapping.ExcludedSymbols),
                AllowedSymbols = new List<string>(currentMapping.AllowedSymbols)
            };

            originalMapping = currentMapping;

            InitializeComponent();
            SetupUI();
            LoadCurrentSettings();
        }

        private void SetupUI()
        {
            this.Text = "🗺️ Symbol Mapping & Broker Configuration - islamahmed9717";
            this.Size = new Size(900, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            CreateHeaderSection();
            CreateMappingSection();
            CreatePrefixSuffixSection();
            CreateFilterSection();
            CreateExampleSection();
            CreateButtonSection();
        }

        private void CreateHeaderSection()
        {
            var headerPanel = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(840, 80),
                BackColor = Color.FromArgb(240, 249, 255),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTitle = new Label
            {
                Text = "🗺️ SYMBOL MAPPING & BROKER CONFIGURATION",
                Location = new Point(20, 15),
                Size = new Size(600, 30),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235)
            };
            headerPanel.Controls.Add(lblTitle);

            var lblDescription = new Label
            {
                Text = $"Configure symbol mappings for your broker | Current Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss} UTC | User: islamahmed9717",
                Location = new Point(20, 45),
                Size = new Size(800, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray
            };
            headerPanel.Controls.Add(lblDescription);

            this.Controls.Add(headerPanel);
        }

        private void CreateMappingSection()
        {
            var lblMappingTitle = new Label
            {
                Text = "🗺️ SYMBOL MAPPINGS (Provider:Broker format)",
                Location = new Point(20, 120),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235)
            };
            this.Controls.Add(lblMappingTitle);

            var txtMappings = new TextBox
            {
                Name = "txtMappings",
                Location = new Point(20, 150),
                Size = new Size(840, 80),
                Font = new Font("Consolas", 10F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "GOLD:XAUUSD,SILVER:XAGUSD,BITCOIN:BTCUSD,US30:US30Cash"
            };
            this.Controls.Add(txtMappings);

            var buttonPanel = new Panel
            {
                Location = new Point(20, 235),
                Size = new Size(840, 35)
            };

            var btnCommonMappings = new Button
            {
                Text = "📋 Add Common Mappings",
                Location = new Point(0, 5),
                Size = new Size(160, 25),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            btnCommonMappings.Click += (s, e) => AddCommonMappings();
            buttonPanel.Controls.Add(btnCommonMappings);

            var btnClearMappings = new Button
            {
                Text = "🗑️ Clear All",
                Location = new Point(170, 5),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            btnClearMappings.Click += (s, e) => txtMappings.Text = "";
            buttonPanel.Controls.Add(btnClearMappings);

            var btnTestMappings = new Button
            {
                Text = "🧪 Test Mappings",
                Location = new Point(260, 5),
                Size = new Size(120, 25),
                BackColor = Color.FromArgb(249, 115, 22),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            btnTestMappings.Click += (s, e) => TestMappings();
            buttonPanel.Controls.Add(btnTestMappings);

            this.Controls.Add(buttonPanel);
        }

        private void CreatePrefixSuffixSection()
        {
            var lblPrefixSuffixTitle = new Label
            {
                Text = "🔧 PREFIX & SUFFIX CONFIGURATION",
                Location = new Point(20, 285),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235)
            };
            this.Controls.Add(lblPrefixSuffixTitle);

            // Prefix
            var lblPrefix = new Label
            {
                Text = "Symbol Prefix:",
                Location = new Point(20, 320),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(lblPrefix);

            var txtPrefix = new TextBox
            {
                Name = "txtPrefix",
                Location = new Point(130, 318),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "e.g., oct."
            };
            this.Controls.Add(txtPrefix);

            var lblPrefixExample = new Label
            {
                Text = "Example: oct.GBPJPY",
                Location = new Point(290, 320),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray
            };
            this.Controls.Add(lblPrefixExample);

            // Suffix
            var lblSuffix = new Label
            {
                Text = "Symbol Suffix:",
                Location = new Point(20, 350),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(lblSuffix);

            var txtSuffix = new TextBox
            {
                Name = "txtSuffix",
                Location = new Point(130, 348),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "e.g., .m"
            };
            this.Controls.Add(txtSuffix);

            var lblSuffixExample = new Label
            {
                Text = "Example: GBPJPY.m",
                Location = new Point(290, 350),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray
            };
            this.Controls.Add(lblSuffixExample);

            // Skip prefix/suffix pairs
            var lblSkipPairs = new Label
            {
                Text = "Skip Prefix/Suffix Pairs:",
                Location = new Point(20, 380),
                Size = new Size(150, 20),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(lblSkipPairs);

            var txtSkipPairs = new TextBox
            {
                Name = "txtSkipPairs",
                Location = new Point(180, 378),
                Size = new Size(680, 25),
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "XAUUSD.Cash,SILVER.Cash,BTCUSD (comma-separated)"
            };
            this.Controls.Add(txtSkipPairs);
        }

        private void CreateFilterSection()
        {
            var lblFilterTitle = new Label
            {
                Text = "🚫 SYMBOL FILTERING",
                Location = new Point(20, 420),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235)
            };
            this.Controls.Add(lblFilterTitle);

            // Excluded symbols
            var lblExcluded = new Label
            {
                Text = "Excluded Symbols:",
                Location = new Point(20, 455),
                Size = new Size(120, 20),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(lblExcluded);

            var txtExcluded = new TextBox
            {
                Name = "txtExcluded",
                Location = new Point(150, 453),
                Size = new Size(710, 25),
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "XAUUSD,NAS100,BTCUSD (symbols to never trade)"
            };
            this.Controls.Add(txtExcluded);

            // Allowed symbols (whitelist)
            var lblAllowed = new Label
            {
                Text = "Allowed Symbols:",
                Location = new Point(20, 485),
                Size = new Size(120, 20),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(lblAllowed);

            var txtAllowed = new TextBox
            {
                Name = "txtAllowed",
                Location = new Point(150, 483),
                Size = new Size(710, 25),
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "EURUSD,GBPUSD,XAUUSD (leave empty to allow all symbols)"
            };
            this.Controls.Add(txtAllowed);
        }

        private void CreateExampleSection()
        {
            var lblExampleTitle = new Label
            {
                Text = "🏦 BROKER EXAMPLES",
                Location = new Point(20, 525),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235)
            };
            this.Controls.Add(lblExampleTitle);

            var examplePanel = new Panel
            {
                Location = new Point(20, 555),
                Size = new Size(840, 100),
                BackColor = Color.FromArgb(248, 249, 250),
                BorderStyle = BorderStyle.FixedSingle
            };

            var examples = new[]
            {
                "IC Markets: No prefix/suffix - Standard symbols",
                "FXTM: Suffix '.m' - EURUSD.m, GBPJPY.m",
                "Exness: Mixed suffixes - XAUUSD.a, BTCUSD.a",
                "Alpari: Prefix 'm-' - m-EURUSD, m-GBPUSD",
                "XM: Mixed - XM.EURUSD, GOLD.cash"
            };

            for (int i = 0; i < examples.Length; i++)
            {
                var lblExample = new Label
                {
                    Text = $"• {examples[i]}",
                    Location = new Point(10, 10 + (i * 18)),
                    Size = new Size(820, 15),
                    Font = new Font("Segoe UI", 8F),
                    ForeColor = Color.FromArgb(59, 130, 246)
                };
                examplePanel.Controls.Add(lblExample);
            }

            this.Controls.Add(examplePanel);
        }

        private void CreateButtonSection()
        {
            var buttonPanel = new Panel
            {
                Location = new Point(20, 670),
                Size = new Size(840, 50)
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

            var btnExport = new Button
            {
                Text = "📤 EXPORT CONFIG",
                Location = new Point(260, 10),
                Size = new Size(140, 35),
                BackColor = Color.FromArgb(168, 85, 247),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnExport.Click += BtnExport_Click;
            buttonPanel.Controls.Add(btnExport);

            var btnReset = new Button
            {
                Text = "🔄 RESET",
                Location = new Point(410, 10),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(249, 115, 22),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnReset.Click += BtnReset_Click;
            buttonPanel.Controls.Add(btnReset);

            this.Controls.Add(buttonPanel);
        }

        private void LoadCurrentSettings()
        {
            var txtMappings = this.Controls.Find("txtMappings", true)[0] as TextBox;
            var txtPrefix = this.Controls.Find("txtPrefix", true)[0] as TextBox;
            var txtSuffix = this.Controls.Find("txtSuffix", true)[0] as TextBox;
            var txtSkipPairs = this.Controls.Find("txtSkipPairs", true)[0] as TextBox;
            var txtExcluded = this.Controls.Find("txtExcluded", true)[0] as TextBox;
            var txtAllowed = this.Controls.Find("txtAllowed", true)[0] as TextBox;

            // Load mappings
            txtMappings.Text = string.Join(",", symbolMapping.Mappings.Select(m => $"{m.Key}:{m.Value}"));
            txtPrefix.Text = symbolMapping.Prefix;
            txtSuffix.Text = symbolMapping.Suffix;
            txtSkipPairs.Text = string.Join(",", symbolMapping.SkipPrefixSuffix);
            txtExcluded.Text = string.Join(",", symbolMapping.ExcludedSymbols);
            txtAllowed.Text = string.Join(",", symbolMapping.AllowedSymbols);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                SaveCurrentSettings();
                MessageBox.Show("✅ Symbol mapping settings saved successfully!\n\nThe EA will use these settings for symbol processing.",
                              "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Failed to save settings:\n\n{ex.Message}",
                              "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveCurrentSettings()
        {
            var txtMappings = this.Controls.Find("txtMappings", true)[0] as TextBox;
            var txtPrefix = this.Controls.Find("txtPrefix", true)[0] as TextBox;
            var txtSuffix = this.Controls.Find("txtSuffix", true)[0] as TextBox;
            var txtSkipPairs = this.Controls.Find("txtSkipPairs", true)[0] as TextBox;
            var txtExcluded = this.Controls.Find("txtExcluded", true)[0] as TextBox;
            var txtAllowed = this.Controls.Find("txtAllowed", true)[0] as TextBox;

            // Parse mappings
            symbolMapping.Mappings.Clear();
            if (!string.IsNullOrEmpty(txtMappings.Text))
            {
                var mappingPairs = txtMappings.Text.Split(',');
                foreach (var pair in mappingPairs)
                {
                    var parts = pair.Split(':');
                    if (parts.Length == 2)
                    {
                        var from = parts[0].Trim().ToUpper();
                        var to = parts[1].Trim();
                        symbolMapping.Mappings[from] = to;
                    }
                }
            }

            // Save other settings
            symbolMapping.Prefix = txtPrefix.Text.Trim();
            symbolMapping.Suffix = txtSuffix.Text.Trim();

            symbolMapping.SkipPrefixSuffix.Clear();
            if (!string.IsNullOrEmpty(txtSkipPairs.Text))
            {
                symbolMapping.SkipPrefixSuffix.AddRange(
                    txtSkipPairs.Text.Split(',').Select(s => s.Trim().ToUpper()).Where(s => !string.IsNullOrEmpty(s))
                );
            }

            symbolMapping.ExcludedSymbols.Clear();
            if (!string.IsNullOrEmpty(txtExcluded.Text))
            {
                symbolMapping.ExcludedSymbols.AddRange(
                    txtExcluded.Text.Split(',').Select(s => s.Trim().ToUpper()).Where(s => !string.IsNullOrEmpty(s))
                );
            }

            symbolMapping.AllowedSymbols.Clear();
            if (!string.IsNullOrEmpty(txtAllowed.Text))
            {
                symbolMapping.AllowedSymbols.AddRange(
                    txtAllowed.Text.Split(',').Select(s => s.Trim().ToUpper()).Where(s => !string.IsNullOrEmpty(s))
                );
            }
        }

        private void AddCommonMappings()
        {
            var txtMappings = this.Controls.Find("txtMappings", true)[0] as TextBox;

            var commonMappings = "GOLD:XAUUSD,SILVER:XAGUSD,BITCOIN:BTCUSD,ETHEREUM:ETHUSD,US30:US30,NAS100:NAS100,SPX500:SPX500,GER30:GER30,UK100:UK100,JPN225:JPN225,CRUDE:USOIL,BRENT:UKOIL,COPPER:COPPER";

            if (!string.IsNullOrEmpty(txtMappings.Text))
                txtMappings.Text += ",";

            txtMappings.Text += commonMappings;
        }

        private void TestMappings()
        {
            try
            {
                SaveCurrentSettings();

                var testSymbols = new[] { "GOLD", "SILVER", "EURUSD", "GBPJPY", "US30", "BITCOIN" };
                var results = new List<string>();

                foreach (var symbol in testSymbols)
                {
                    var result = TestSymbolTransformation(symbol);
                    results.Add(result);
                }

                var resultText = string.Join("\n", results);

                MessageBox.Show($"🧪 Symbol Mapping Test Results:\n\n{resultText}\n\nTime: {DateTime.Now:yyyy-MM-dd HH:mm:ss} UTC",
                              "Test Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Test failed:\n\n{ex.Message}", "Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string TestSymbolTransformation(string originalSymbol)
        {
            var mapped = originalSymbol;

            // Apply mapping
            if (symbolMapping.Mappings.ContainsKey(originalSymbol.ToUpper()))
            {
                mapped = symbolMapping.Mappings[originalSymbol.ToUpper()];
            }

            // Apply prefix/suffix if not skipped
            var final = mapped;
            if (!symbolMapping.SkipPrefixSuffix.Contains(mapped.ToUpper()))
            {
                final = symbolMapping.Prefix + mapped + symbolMapping.Suffix;
            }

            // Check filters
            var status = "✅ Allowed";
            if (symbolMapping.ExcludedSymbols.Contains(final.ToUpper()) || symbolMapping.ExcludedSymbols.Contains(originalSymbol.ToUpper()))
            {
                status = "❌ Excluded";
            }
            else if (symbolMapping.AllowedSymbols.Count > 0 &&
                     !symbolMapping.AllowedSymbols.Contains(final.ToUpper()) &&
                     !symbolMapping.AllowedSymbols.Contains(originalSymbol.ToUpper()))
            {
                status = "⚠️ Not in whitelist";
            }

            return $"{originalSymbol} → {mapped} → {final} ({status})";
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            try
            {
                SaveCurrentSettings();

                var config = GenerateConfigurationText();

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FileName = $"SymbolMapping_islamahmed9717_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveDialog.FileName, config);
                    Clipboard.SetText(config);

                    MessageBox.Show($"✅ Symbol mapping configuration exported!\n\n📁 File: {saveDialog.FileName}\n📋 Configuration copied to clipboard\n\n💡 Use this configuration in your EA settings.",
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
            var result = MessageBox.Show("🔄 Are you sure you want to reset all symbol mapping settings to default values?",
                                       "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                ResetToDefaults();
                MessageBox.Show("✅ Symbol mapping settings reset to defaults!", "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ResetToDefaults()
        {
            var txtMappings = this.Controls.Find("txtMappings", true)[0] as TextBox;
            var txtPrefix = this.Controls.Find("txtPrefix", true)[0] as TextBox;
            var txtSuffix = this.Controls.Find("txtSuffix", true)[0] as TextBox;
            var txtSkipPairs = this.Controls.Find("txtSkipPairs", true)[0] as TextBox;
            var txtExcluded = this.Controls.Find("txtExcluded", true)[0] as TextBox;
            var txtAllowed = this.Controls.Find("txtAllowed", true)[0] as TextBox;

            txtMappings.Text = "";
            txtPrefix.Text = "";
            txtSuffix.Text = "";
            txtSkipPairs.Text = "";
            txtExcluded.Text = "";
            txtAllowed.Text = "";
        }

        private string GenerateConfigurationText()
        {
            return $@"//+------------------------------------------------------------------+
//|                    Symbol Mapping Configuration                  |
//|                Generated: 2025-06-18 19:37:36 UTC              |
//|                User: islamahmed9717                              |
//+------------------------------------------------------------------+

//--- Symbol Mappings ---
SymbolsMapping = ""{string.Join(",", symbolMapping.Mappings.Select(m => $"{m.Key}:{m.Value}"))}"" 

//--- Prefix & Suffix Settings ---
SymbolPrefix = ""{symbolMapping.Prefix}""
SymbolSuffix = ""{symbolMapping.Suffix}""
SkipSuffixPrefixPairs = ""{string.Join(",", symbolMapping.SkipPrefixSuffix)}""

//--- Symbol Filtering ---
ExcludedSymbols = ""{string.Join(",", symbolMapping.ExcludedSymbols)}""
SymbolsToTrade = ""{string.Join(",", symbolMapping.AllowedSymbols)}""

//--- Configuration Details ---
/*
Mappings Count: {symbolMapping.Mappings.Count}
Has Prefix: {(!string.IsNullOrEmpty(symbolMapping.Prefix) ? "Yes" : "No")}
Has Suffix: {(!string.IsNullOrEmpty(symbolMapping.Suffix) ? "Yes" : "No")}
Skip Pairs: {symbolMapping.SkipPrefixSuffix.Count}
Excluded: {symbolMapping.ExcludedSymbols.Count}
Whitelist: {(symbolMapping.AllowedSymbols.Count > 0 ? symbolMapping.AllowedSymbols.Count.ToString() : "None (All allowed)")}

Generated by: islamahmed9717
Timestamp: 2025-06-18 19:37:36 UTC
Application: Telegram EA Manager v2.0 - Real Implementation
*/

//--- Example Symbol Transformations ---
/*
Test Results:
{string.Join("\n", new[] { "GOLD", "SILVER", "EURUSD", "GBPJPY" }.Select(TestSymbolTransformation))}
*/";
        }

        public SymbolMapping GetUpdatedMapping()
        {
            return symbolMapping;
        }
    }
}