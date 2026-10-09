using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace CyberpunkRED_Generator
{
    // единый журнал ошибок: необработанное исключение не роняет программу молча, а пишется в Logs/
    public static class ErrorLog
    {
        private static readonly object _lock = new object();
        public static string FolderOverride;   // для тестов

        public static string Folder => FolderOverride ?? AppInfo.DataFolder("Logs");
        public static string CurrentFile => Path.Combine(Folder, $"log_{DateTime.Now:yyyy-MM-dd}.txt");

        public static void Install(Application app)
        {
            app.DispatcherUnhandledException += (s, e) =>
            {
                Write("UI", e.Exception);
                e.Handled = true;   // автосохранение листа уже защищает данные
                ShowDialog(e.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Write("DOMAIN", e.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (s, e) => { Write("TASK", e.Exception); e.SetObserved(); };
            Info("Запуск " + AppInfo.Title);
        }

        public static void Info(string message) => Append("INFO", message);

        public static void Write(string source, Exception ex)
        {
            if (ex == null) return;
            Append("ERROR", $"[{source}] {ex}");
        }

        private static void Append(string level, string text)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(CurrentFile, $"{DateTime.Now:HH:mm:ss} {level} {text}{Environment.NewLine}", Encoding.UTF8);
                    TrimOld();
                }
            }
            catch { /* журнал не должен сам становиться причиной ошибки */ }
        }

        private static void TrimOld()
        {
            foreach (var f in new DirectoryInfo(Folder).GetFiles("log_*.txt"))
                if (f.LastWriteTime < DateTime.Now.AddDays(-30)) { try { f.Delete(); } catch { } }
        }

        private static bool _dialogOpen;
        private static void ShowDialog(Exception ex)
        {
            if (_dialogOpen) return;   // одна ошибка в цикле не должна заваливать окнами
            _dialogOpen = true;
            try
            {
                var r = MessageBox.Show(
                    "Что-то пошло не так, но программа продолжает работать.\n\n" + ex.Message +
                    "\n\nПодробности записаны в журнал. Открыть папку с журналом?",
                    "ОШИБКА", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r == MessageBoxResult.Yes) System.Diagnostics.Process.Start("explorer.exe", Folder);
            }
            catch { }
            finally { _dialogOpen = false; }
        }
    }
}
