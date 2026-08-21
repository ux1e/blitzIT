using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.UI.Xaml.Controls.Primitives;

namespace ServerBrowser.Functions
{
    public static class Helper
    {
        public static async Task<bool> IsIPReachable(string ip)
        {
            try
            {
                using (var ping = new Ping())
                {
                    PingReply reply = await ping.SendPingAsync(ip, 1000).ConfigureAwait(false);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке IP {ip}: {ex.Message}");
                return false;
            }
        }

        public static void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void ShowWarning(string message)
        {
            MessageBox.Show(message, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static void ShowNotification(string message)
        {
            new ToastContentBuilder()
                .AddArgument("action", "viewConversation")
                .AddText("Изменение статуса IP-адреса")
                .AddText(message)
                .Show();
        }

        public static bool IsValidIp(string ip)
        {
            return System.Net.IPAddress.TryParse(ip, out _);
        }

        public static void ShutDownBy(string ip, int time)
        {
            System.Diagnostics.Process.Start("CMD.exe", $"/C shutdown /s /t {time} /m {ip}");
        }

        public static void AntiShutdown(string ip)
        {
            System.Diagnostics.Process.Start("CMD.exe", $"/C shutdown /a /m {ip}");
        }
    }
}
