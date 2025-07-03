using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TelegramEAManager
{
    public static class TelegramHelper
    {
        public static async Task<bool> ConnectToTelegram(TelegramService telegramService)
        {
            try
            {
                // Request phone number
                using (var phoneForm = new Form())
                {
                    phoneForm.Text = "📱 Enter Phone Number";
                    phoneForm.Size = new Size(400, 200);
                    phoneForm.StartPosition = FormStartPosition.CenterScreen;
                    phoneForm.FormBorderStyle = FormBorderStyle.FixedDialog;

                    var lblMessage = new Label
                    {
                        Text = "📱 Enter your phone number (with country code):",
                        Location = new Point(20, 20),
                        Size = new Size(350, 40),
                        Font = new Font("Segoe UI", 10F)
                    };
                    phoneForm.Controls.Add(lblMessage);

                    var txtPhone = new TextBox
                    {
                        Location = new Point(20, 70),
                        Size = new Size(200, 25),
                        Font = new Font("Segoe UI", 12F),
                        PlaceholderText = "+1234567890"
                    };
                    phoneForm.Controls.Add(txtPhone);

                    var btnOK = new Button
                    {
                        Text = "✅ Connect",
                        Location = new Point(240, 70),
                        Size = new Size(100, 25),
                        DialogResult = DialogResult.OK
                    };
                    phoneForm.Controls.Add(btnOK);

                    phoneForm.AcceptButton = btnOK;
                    txtPhone.Focus();

                    if (phoneForm.ShowDialog() == DialogResult.OK)
                    {
                        var phone = txtPhone.Text.Trim();
                        if (!string.IsNullOrEmpty(phone))
                        {
                            return await telegramService.ConnectAsync(phone);
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Connection failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}