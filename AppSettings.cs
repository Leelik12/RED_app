using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberpunkRED_Generator
{
    public class ThemePreset
    {
        public string Id, Name;
        public Color Accent, AccentDark, Danger;
    }

    public class SettingsData
    {
        public string Theme { get; set; } = "cyan";
        public bool KeepAspect { get; set; }   // false — растягивать на всё окно, true — сохранять пропорции
    }

    // настройки оформления: цветовая схема и режим масштаба; хранятся в settings.json рядом с программой
    public static class AppSettings
    {
        public static string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static readonly List<ThemePreset> Themes = new List<ThemePreset>
        {
            new ThemePreset { Id = "cyan",   Name = "Неон (бирюзовый)",  Accent = Color.FromRgb(0x00, 0xFF, 0xCC), AccentDark = Color.FromRgb(0x00, 0xD4, 0xB2), Danger = Color.FromRgb(0xFF, 0x33, 0x33) },
            new ThemePreset { Id = "amber",  Name = "Янтарь",            Accent = Color.FromRgb(0xFF, 0xB0, 0x00), AccentDark = Color.FromRgb(0xD4, 0x92, 0x00), Danger = Color.FromRgb(0xFF, 0x44, 0x22) },
            new ThemePreset { Id = "green",  Name = "Терминал (зелёный)", Accent = Color.FromRgb(0x33, 0xFF, 0x66), AccentDark = Color.FromRgb(0x28, 0xD4, 0x52), Danger = Color.FromRgb(0xFF, 0x33, 0x33) },
            new ThemePreset { Id = "pink",   Name = "Розовый неон",      Accent = Color.FromRgb(0xFF, 0x4D, 0xD2), AccentDark = Color.FromRgb(0xD4, 0x3F, 0xB0), Danger = Color.FromRgb(0xFF, 0xCC, 0x00) },
            new ThemePreset { Id = "white",  Name = "Контрастный белый", Accent = Color.FromRgb(0xFF, 0xFF, 0xFF), AccentDark = Color.FromRgb(0xD9, 0xD9, 0xD9), Danger = Color.FromRgb(0xFF, 0x33, 0x33) },
        };

        private static SettingsData _data;
        public static event Action Changed;

        public static SettingsData Data
        {
            get
            {
                if (_data == null) _data = Load();
                return _data;
            }
        }

        public static ThemePreset CurrentTheme => Themes.FirstOrDefault(t => t.Id == Data.Theme) ?? Themes[0];

        private static SettingsData Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(FilePath)) ?? new SettingsData();
            }
            catch { }
            return new SettingsData();
        }

        // подменить файл настроек (для тестов) и перечитать
        public static void Reload(string path)
        {
            FilePath = path;
            _data = null;
        }

        public static void Set(string themeId, bool keepAspect)
        {
            if (!Themes.Any(t => t.Id == themeId)) themeId = Themes[0].Id;
            Data.Theme = themeId;
            Data.KeepAspect = keepAspect;
            try { File.WriteAllText(FilePath, JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true })); }
            catch { }
            Changed?.Invoke();
        }
    }

    // цвета окон берутся из ресурсов AccentBrush / AccentDarkBrush / DangerBrush, которые тут и подменяются
    public static class Theme
    {
        private static readonly List<WeakReference<FrameworkElement>> _targets = new List<WeakReference<FrameworkElement>>();

        public static SolidColorBrush Accent() => new SolidColorBrush(AppSettings.CurrentTheme.Accent);
        public static SolidColorBrush Danger() => new SolidColorBrush(AppSettings.CurrentTheme.Danger);

        // вызывать в конструкторе окна ДО InitializeComponent: ключи нужны, пока разбирается разметка
        public static void Install(FrameworkElement element)
        {
            Fill(element.Resources);
            _targets.Add(new WeakReference<FrameworkElement>(element));
            element.Loaded += (s, e) => ApplyLayout(element);
            AppSettings.Changed -= ApplyAll;
            AppSettings.Changed += ApplyAll;
        }

        private static void Fill(ResourceDictionary r)
        {
            var t = AppSettings.CurrentTheme;
            r["AccentColor"] = t.Accent;
            r["AccentDarkColor"] = t.AccentDark;
            r["DangerColor"] = t.Danger;
            r["AccentBrush"] = new SolidColorBrush(t.Accent);
            r["AccentDarkBrush"] = new SolidColorBrush(t.AccentDark);
            r["DangerBrush"] = new SolidColorBrush(t.Danger);
        }

        private static void ApplyLayout(FrameworkElement element)
        {
            if (element is Window w && w.Content is Viewbox vb)
                vb.Stretch = AppSettings.Data.KeepAspect ? Stretch.Uniform : Stretch.Fill;
        }

        public static void ApplyAll()
        {
            _targets.RemoveAll(x => !x.TryGetTarget(out _));
            foreach (var wr in _targets)
            {
                if (!wr.TryGetTarget(out var el)) continue;
                Fill(el.Resources);
                ApplyLayout(el);
            }
        }
    }
}
