using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Serilog;

namespace DefaultBrowser.Models
{
    public class NotificationService
    {
        private static readonly Lazy<NotificationService> _instance = new Lazy<NotificationService>(() => new NotificationService());
        public static NotificationService Instance => _instance.Value;

        private NotificationService() { }

        public void ShowNotification(string title, string message)
        {
            try
            {
                Log.Information("Showing notification - Title: {Title}, Message: {Message}", title, message);
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    ShowWindowsNotification(title, message);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    ShowMacOSNotification(title, message);
                }
                else // Linux and others
                {
                    ShowLinuxNotification(title, message);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to show notification");
            }
        }

        private void ShowWindowsNotification(string title, string message)
        {
            try
            {
                // Use PowerShell to show a Windows notification
                var escapedTitle = title.Replace("\"", "\\\"");
                var escapedMessage = message.Replace("\"", "\\\"");
                
                var script = $@"
                [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
                [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

                $template = [Windows.UI.Notifications.ToastTemplateType]::ToastText02
                $xml = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent($template)
                $text = $xml.GetElementsByTagName('text')
                $text[0].AppendChild($xml.CreateTextNode('{escapedTitle}')) | Out-Null
                $text[1].AppendChild($xml.CreateTextNode('{escapedMessage}')) | Out-Null

                $toast = [Windows.UI.Notifications.ToastNotification]::new($xml)
                $notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('Browser Redirector')
                $notifier.Show($toast);
                ";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to show Windows notification");
                // Fallback to simpler method
                ShowSimpleWindowsNotification(title, message);
            }
        }

        private void ShowSimpleWindowsNotification(string title, string message)
        {
            try
            {
                var script = $"Add-Type -AssemblyName System.Windows.Forms; $notify = New-Object System.Windows.Forms.NotifyIcon; $notify.Icon = [System.Drawing.SystemIcons]::Information; $notify.Visible = $true; $notify.ShowBalloonTip(0, '{title.Replace("'", "`'")}', '{message.Replace("'", "`'")}', [System.Windows.Forms.ToolTipIcon]::None)";
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to show simple Windows notification");
            }
        }

        private void ShowMacOSNotification(string title, string message)
        {
            try
            {
                var escapedTitle = title.Replace("\"", "\\\"");
                var escapedMessage = message.Replace("\"", "\\\"");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = $"-e 'display notification \"{escapedMessage}\" with title \"{escapedTitle}\"'",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to show macOS notification");
            }
        }

        private void ShowLinuxNotification(string title, string message)
        {
            try
            {
                var escapedTitle = title.Replace("\"", "\\\"");
                var escapedMessage = message.Replace("\"", "\\\"");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "notify-send",
                    Arguments = $"\"{escapedTitle}\" \"{escapedMessage}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit();
                
                // If notify-send fails, try zenity as a fallback
                if (process?.ExitCode != 0)
                {
                    Log.Information("notify-send failed, trying zenity...");
                    ShowLinuxZenityNotification(title, message);
                }
            }
            catch
            {
                // If notify-send isn't available, try zenity
                Log.Information("notify-send not available, trying zenity...");
                ShowLinuxZenityNotification(title, message);
            }
        }

        private void ShowLinuxZenityNotification(string title, string message)
        {
            try
            {
                var escapedTitle = title.Replace("\"", "\\\"");
                var escapedMessage = message.Replace("\"", "\\\"");
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "zenity",
                    Arguments = $"--notification --text=\"{escapedTitle}: {escapedMessage}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to show Linux zenity notification");
            }
        }
    }
}