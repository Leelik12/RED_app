using System;
using System.Reflection;

namespace CyberpunkRED_Generator
{
    // версия программы и общие сведения (берутся из AssemblyInfo, чтобы не дублировать)
    public static class AppInfo
    {
        public const string Name = "Cyberpunk RED Terminal";
        public const string Repository = "Leelik12/RED_app";   // для проверки обновлений

        public static Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version;

        // 2.0.0.0 -> "2.0.0"
        public static string VersionText { get; } = $"{Version.Major}.{Version.Minor}.{Version.Build}";

        public static string Title => $"{Name} v{VersionText}";

        // папка рядом с программой, а если писать туда нельзя — в профиль пользователя
        public static string DataFolder(string sub)
        {
            string main = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, sub);
            try
            {
                System.IO.Directory.CreateDirectory(main);
                string probe = System.IO.Path.Combine(main, ".w");
                System.IO.File.WriteAllText(probe, "1"); System.IO.File.Delete(probe);
                return main;
            }
            catch
            {
                string alt = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CyberpunkRED", sub);
                System.IO.Directory.CreateDirectory(alt);
                return alt;
            }
        }
    }
}
