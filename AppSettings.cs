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
        // нейтральные цвета интерфейса; у тем без своих значений — тёмная схема
        public Color Back = Color.FromRgb(5, 5, 5), Panel = Color.FromRgb(10, 10, 10), Raised = Color.FromRgb(26, 26, 26);
        public Color Text = Color.FromRgb(224, 224, 224), Muted = Color.FromRgb(153, 153, 153), Warn = Color.FromRgb(255, 204, 0);
        public Color Line = Color.FromRgb(68, 68, 68), Bright = Color.FromRgb(255, 255, 255), Hover = Color.FromRgb(42, 42, 42);   // рамки, самый яркий текст, наведение
        public bool Light;                                                                                                         // светлая схема: подложка поверх акцента — тёмный текст не нужен
    }

    public class SettingsData
    {
        public string Theme { get; set; } = "cyan";
        public bool KeepAspect { get; set; }   // false — растягивать на всё окно, true — сохранять пропорции
        public List<string> DisabledPacks { get; set; } = new List<string>();   // отключённые DLC (id пакетов)
        public string UiLayout { get; set; } = "side";                           // раскладка листа: side (боковая панель) | classic (вкладки сверху)
        public string UiStyle { get; set; } = "minimal";                         // стиль листа: minimal (минималистичный) | detailed (детальный, «GUI») | detailed2 (детальный «неоновый»)
        public string Language { get; set; } = "ru";                            // язык интерфейса: ru | en (подстановка из Data/loc_en.json)
        public bool CheckUpdates { get; set; }                                   // проверять релизы на GitHub при запуске (по умолчанию выключено)
        public string UiFont { get; set; } = "Bahnschrift";                    // шрифт интерфейса (ресурс UiFont)
        public bool FontMigrated { get; set; }                                   // старый шрифт по умолчанию (Consolas) один раз заменён на Bahnschrift
        public bool AgentsEnabled { get; set; }                                  // подсистема «Все об агентах» (DLC)
        public bool TarotEnabled { get; set; }                                   // подсистема «Карты Таро Найт-Сити» (DLC)
        public List<int> TarotDrawn { get; set; } = new List<int>();             // номера уже вытянутых карт
        public List<string> TarotLog { get; set; } = new List<string>();         // журнал вытянутых карт
        public List<MapPin> MapPins { get; set; } = new List<MapPin>();          // свои метки на карте Найт-Сити
        public List<string> MapHidden { get; set; } = new List<string>();        // скрытые слои карты (cube, container, conapt, studio, market, custom)
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
            new ThemePreset { Id = "blood",  Name = "Кровь и хром", Accent = Color.FromRgb(0xFF, 0x5A, 0x4D), AccentDark = Color.FromRgb(0xD4, 0x43, 0x38), Danger = Color.FromRgb(0xFF, 0xE0, 0x66),
                Back = Color.FromRgb(0x0B, 0x04, 0x04), Panel = Color.FromRgb(0x15, 0x08, 0x08), Raised = Color.FromRgb(0x24, 0x10, 0x10), Line = Color.FromRgb(0x5A, 0x2A, 0x2A), Hover = Color.FromRgb(0x3A, 0x18, 0x18) },
            new ThemePreset { Id = "synth",  Name = "Синтвейв", Accent = Color.FromRgb(0x4D, 0xE8, 0xFF), AccentDark = Color.FromRgb(0x3B, 0xB8, 0xCC), Danger = Color.FromRgb(0xFF, 0x4D, 0xD2),
                Back = Color.FromRgb(0x0A, 0x08, 0x1E), Panel = Color.FromRgb(0x12, 0x0F, 0x2E), Raised = Color.FromRgb(0x20, 0x1B, 0x4A), Line = Color.FromRgb(0x4A, 0x40, 0x8C), Hover = Color.FromRgb(0x2E, 0x28, 0x66), Muted = Color.FromRgb(0xA8, 0xA2, 0xD6) },
            new ThemePreset { Id = "ice",    Name = "Лёд (синий)", Accent = Color.FromRgb(0x66, 0xB8, 0xFF), AccentDark = Color.FromRgb(0x4A, 0x96, 0xD6), Danger = Color.FromRgb(0xFF, 0x8A, 0x4D),
                Back = Color.FromRgb(0x04, 0x09, 0x10), Panel = Color.FromRgb(0x08, 0x12, 0x1E), Raised = Color.FromRgb(0x12, 0x22, 0x36), Line = Color.FromRgb(0x2E, 0x4A, 0x6A), Hover = Color.FromRgb(0x1C, 0x32, 0x4E) },
            new ThemePreset { Id = "paper",  Name = "Светлый (чертёж)", Light = true, Accent = Color.FromRgb(0x00, 0x6B, 0x8F), AccentDark = Color.FromRgb(0x00, 0x52, 0x70), Danger = Color.FromRgb(0xC4, 0x1E, 0x3A),
                Back = Color.FromRgb(0xEE, 0xEC, 0xE4), Panel = Color.FromRgb(0xF8, 0xF6, 0xEE), Raised = Color.FromRgb(0xDC, 0xD8, 0xC8), Line = Color.FromRgb(0x8A, 0x86, 0x78), Hover = Color.FromRgb(0xCC, 0xC8, 0xB6),
                Text = Color.FromRgb(0x22, 0x22, 0x22), Muted = Color.FromRgb(0x55, 0x55, 0x55), Warn = Color.FromRgb(0xA8, 0x6A, 0x00), Bright = Color.FromRgb(0x00, 0x00, 0x00) },
        };

        private static SettingsData _data;
        public static event Action Changed;
        public static event Action LayoutChanged;
        public static event Action StyleChanged;

        public static bool SideLayout => !string.Equals(Data.UiLayout, "classic", StringComparison.OrdinalIgnoreCase);

        // minimal | detailed | detailed2: два детальных варианта отличаются только словарём стилей
        public static string StyleId => string.Equals(Data.UiStyle, "detailed", StringComparison.OrdinalIgnoreCase) ? "detailed"
            : string.Equals(Data.UiStyle, "detailed2", StringComparison.OrdinalIgnoreCase) ? "detailed2" : "minimal";

        public static bool DetailedStyle => StyleId != "minimal";

        public static void SetStyle(string id)
        {
            Data.UiStyle = new[] { "detailed", "detailed2" }.FirstOrDefault(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase)) ?? "minimal";
            Save();
            StyleChanged?.Invoke();
        }

        public static void SetLayout(string id)
        {
            Data.UiLayout = string.Equals(id, "classic", StringComparison.OrdinalIgnoreCase) ? "classic" : "side";
            Save();
            LayoutChanged?.Invoke();
        }

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
                    return Migrate(JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(FilePath)) ?? new SettingsData());
            }
            catch { }
            return new SettingsData();
        }

        // Consolas был шрифтом по умолчанию; один раз переводим сохранённые настройки на новый (выбранный позже вручную остаётся)
        internal static SettingsData Migrate(SettingsData d)
        {
            if (!d.FontMigrated)
            {
                if (string.IsNullOrWhiteSpace(d.UiFont) || d.UiFont == "Consolas") d.UiFont = "Bahnschrift";
                d.FontMigrated = true;
            }
            return d;
        }

        // подменить файл настроек (для тестов) и перечитать
        public static void Reload(string path)
        {
            FilePath = path;
            _data = null;
        }

        public static void SetPackEnabled(string packId, bool enabled)
        {
            if (enabled) Data.DisabledPacks.Remove(packId);
            else if (!Data.DisabledPacks.Contains(packId)) Data.DisabledPacks.Add(packId);
            Save();
            Packs.Apply();
            Changed?.Invoke();
        }

        public static void Save()
        {
            try { File.WriteAllText(FilePath, JsonSerializer.Serialize(Data, new JsonSerializerOptions { WriteIndented = true })); }
            catch { }
        }

        public static void SetFont(string name)
        {
            Data.UiFont = string.IsNullOrWhiteSpace(name) ? "Bahnschrift" : name;
            Data.FontMigrated = true;
            Save();
            Changed?.Invoke();
        }

        public static void Set(string themeId, bool keepAspect)
        {
            if (!Themes.Any(t => t.Id == themeId)) themeId = Themes[0].Id;
            Data.Theme = themeId;
            Data.KeepAspect = keepAspect;
            Save();
            Changed?.Invoke();
        }
    }

    // цвета окон берутся из ресурсов AccentBrush / AccentDarkBrush / DangerBrush, которые тут и подменяются
    public static class Theme
    {
        private static readonly List<WeakReference<FrameworkElement>> _targets = new List<WeakReference<FrameworkElement>>();

        public static SolidColorBrush Accent() => new SolidColorBrush(AppSettings.CurrentTheme.Accent);
        public static SolidColorBrush Danger() => new SolidColorBrush(AppSettings.CurrentTheme.Danger);

        // кисть токена текущей темы для кода (Back, Panel, Raised, Text, Muted, Warn, Line, Bright, Hover, Tint, Accent, Danger, OnAccent)
        private static string _cacheKey;
        private static ResourceDictionary _cache;
        // шрифт интерфейса из настроек — для окон, собираемых в коде
        public static FontFamily CurrentFont => Family(AppSettings.Data.UiFont);

        // Bahnschrift есть не везде (Windows 10 1709+): запасной шрифт — Segoe UI, затем Consolas
        public static FontFamily Family(string name)
            => new FontFamily((string.IsNullOrWhiteSpace(name) ? "Bahnschrift" : name) + ", Segoe UI, Consolas");

        public static SolidColorBrush Get(string token)
        {
            string key = AppSettings.Data.Theme + "|" + AppSettings.Data.UiFont;
            if (_cache == null || key != _cacheKey) { _cache = new ResourceDictionary(); Fill(_cache); _cacheKey = key; }
            return new SolidColorBrush(((SolidColorBrush)_cache[token + "Brush"]).Color);
        }

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
            r["BackColor"] = t.Back; r["BackBrush"] = new SolidColorBrush(t.Back);
            r["PanelColor"] = t.Panel; r["PanelBrush"] = new SolidColorBrush(t.Panel);
            r["RaisedColor"] = t.Raised; r["RaisedBrush"] = new SolidColorBrush(t.Raised);
            r["TextColor"] = t.Text; r["TextBrush"] = new SolidColorBrush(t.Text);
            r["MutedColor"] = t.Muted; r["MutedBrush"] = new SolidColorBrush(t.Muted);
            r["WarnColor"] = t.Warn; r["WarnBrush"] = new SolidColorBrush(t.Warn);
            r["LineColor"] = t.Line; r["LineBrush"] = new SolidColorBrush(t.Line);
            r["BrightColor"] = t.Bright; r["BrightBrush"] = new SolidColorBrush(t.Bright);
            r["HoverColor"] = t.Hover; r["HoverBrush"] = new SolidColorBrush(t.Hover);
            // оттенок акцента для выделенных строк и тёмный полупрозрачный фон оверлеев
            var tint = Color.FromRgb((byte)(t.Back.R + (t.Accent.R - t.Back.R) / 10), (byte)(t.Back.G + (t.Accent.G - t.Back.G) / 10), (byte)(t.Back.B + (t.Accent.B - t.Back.B) / 10));
            r["TintColor"] = tint; r["TintBrush"] = new SolidColorBrush(tint);
            var ov = Color.FromArgb(0xE5, t.Back.R, t.Back.G, t.Back.B);
            r["OverlayColor"] = ov; r["OverlayBrush"] = new SolidColorBrush(ov);
            // текст на плашках акцентного цвета: у тёмных схем почти чёрный, у светлой — светлый
            var on = t.Light ? t.Panel : t.Back;
            r["OnAccentColor"] = on; r["OnAccentBrush"] = new SolidColorBrush(on);
            // смесь цветов для плавных градиентов детального стиля: акцентные тона, притушенные к панели, и приподнятая/утопленная панель
            Color Mix(Color a, Color b, double k) => Color.FromRgb((byte)(a.R + (b.R - a.R) * k), (byte)(a.G + (b.G - a.G) * k), (byte)(a.B + (b.B - a.B) * k));
            r["DangerSoftColor"] = Mix(t.Danger, t.Panel, 0.55); r["DangerSoftBrush"] = new SolidColorBrush(Mix(t.Danger, t.Panel, 0.55));
            r["DangerDeepColor"] = Mix(t.Danger, Colors.Black, 0.25); r["DangerDeepBrush"] = new SolidColorBrush(Mix(t.Danger, Colors.Black, 0.25));
            r["AccentSoftColor"] = Mix(t.Accent, t.Panel, 0.6); r["AccentSoftBrush"] = new SolidColorBrush(Mix(t.Accent, t.Panel, 0.6));
            r["PanelLiftColor"] = Mix(t.Panel, t.Accent, 0.08); r["PanelLiftBrush"] = new SolidColorBrush(Mix(t.Panel, t.Accent, 0.08));
            r["PanelDeepColor"] = Mix(t.Panel, t.Back, 0.55); r["PanelDeepBrush"] = new SolidColorBrush(Mix(t.Panel, t.Back, 0.55));
            r["UiFont"] = Family(AppSettings.Data.UiFont);
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
