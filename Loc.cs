using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace CyberpunkRED_Generator
{
    // Локализация интерфейса RU → EN поверх русского оригинала. Исходные строки остаются русскими (в сохранениях, названиях навыков и т. п.),
    // а при включённом английском каждая загружаемая подпись (TextBlock, кнопка, заголовок, подсказка), чьё русское
    // значение есть в словаре Data/loc_en.json, подменяется на английское. Данные книг (описания правил, предметы) остаются на русском.
    //
    // В словаре ключ без плейсхолдеров — точное совпадение; ключ с {#} (число) или {*} (любой текст) — шаблон.
    // В переводе шаблона {0},{1}… — найденные значения как есть, {t0},{t1}… — найденные значения, переведённые по словарю.
    public static class Loc
    {
        public const string ResourceName = "Data.loc_en.json";
        private const int CacheLimit = 6000;

        private sealed class Rule
        {
            public Regex Rx;
            public string Template;
            public int Weight;
        }

        private static Dictionary<string, string> _map;
        private static Dictionary<string, string> _exact;
        private static List<Rule> _rules;
        private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>();
        private static bool _installed;
        private static bool _guard;

        private static readonly Regex Placeholder = new Regex(@"\{[#*]\}", RegexOptions.CultureInvariant);
        private static readonly Regex Slot = new Regex(@"\{(t?)(\d)\}", RegexOptions.CultureInvariant);
        private static readonly Regex Counter = new Regex(@"^(.*?)\s*(\[\d+\])$", RegexOptions.CultureInvariant | RegexOptions.Singleline);
        private static readonly Regex MapLabel = new Regex(@"^([A-Z])(\s+[—·]\s+)(.+)$", RegexOptions.CultureInvariant | RegexOptions.Singleline);
        private static readonly Regex Bracket = new Regex(@"^(.+?)\s*(\([^()]*\))$", RegexOptions.CultureInvariant | RegexOptions.Singleline);
        private static readonly Regex Label = new Regex(@"^([^:\r\n]{1,48}:)\s*(.+)$", RegexOptions.CultureInvariant | RegexOptions.Singleline);
        private static readonly string[] Delimiters = { " · ", " — " };

        // для сбора русских строк интерфейса (инструмент подготовки словаря): если задано, туда попадает всё увиденное
        public static HashSet<string> Harvest;

        public static bool English => string.Equals(AppSettings.Data.Language, "en", StringComparison.OrdinalIgnoreCase);

        public static IReadOnlyDictionary<string, string> Map { get { Ensure(); return _map; } }

        private static void Ensure()
        {
            if (_map != null) return;
            var map = new Dictionary<string, string>();
            try
            {
                using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
                    if (s != null) map = Parse(s);
            }
            catch (Exception ex) { ErrorLog.Write("loc", ex); }
            Build(map);
        }

        private static void Build(Dictionary<string, string> map)
        {
            _map = map;
            _exact = new Dictionary<string, string>();
            _rules = new List<Rule>();
            Cache.Clear();
            foreach (var kv in map)
            {
                if (!Placeholder.IsMatch(kv.Key)) { _exact[kv.Key.Trim()] = kv.Value; continue; }
                _rules.Add(new Rule { Rx = ToRegex(kv.Key), Template = kv.Value, Weight = Placeholder.Replace(kv.Key, "").Length });
            }
            // сначала самые конкретные шаблоны
            _rules = _rules.OrderByDescending(r => r.Weight).ToList();
        }

        private static Regex ToRegex(string key)
        {
            var sb = new System.Text.StringBuilder("^");
            int pos = 0;
            foreach (Match m in Placeholder.Matches(key))
            {
                sb.Append(Literal(key.Substring(pos, m.Index - pos)));
                sb.Append(m.Value == "{#}" ? @"(\d+(?:[.,]\d+)?)" : "(.+?)");
                pos = m.Index + m.Length;
            }
            sb.Append(Literal(key.Substring(pos))).Append('$');
            return new Regex(sb.ToString(), RegexOptions.CultureInvariant | RegexOptions.Singleline);
        }

        private static string Literal(string s) => Regex.Replace(Regex.Escape(s), @"(?:\\ )+", @"\s+");

        public static Dictionary<string, string> Parse(Stream s)
            => JsonSerializer.Deserialize<Dictionary<string, string>>(s, new JsonSerializerOptions { PropertyNameCaseInsensitive = false }) ?? new Dictionary<string, string>();

        // null — вернуть встроенный словарь
        public static void SetForTests(Dictionary<string, string> map)
        {
            if (map == null) { _map = null; Cache.Clear(); }
            else Build(map);
        }

        // перевод строки: у русского режима и у неизвестных строк возвращается оригинал
        public static string T(string ru)
        {
            if (string.IsNullOrEmpty(ru) || !English) return ru;
            return Translate(ru);
        }

        public static string T(string ru, params object[] args) => string.Format(T(ru), args);

        // перевод независимо от настройки языка (для тестов и инструментов)
        public static string Translate(string ru)
        {
            if (string.IsNullOrEmpty(ru) || !HasCyrillic(ru)) return ru;
            Ensure();
            lock (Cache)
            {
                if (Cache.TryGetValue(ru, out var hit)) return hit;
                string result = Compute(ru);
                if (Cache.Count >= CacheLimit) Cache.Clear();
                Cache[ru] = result;
                return result;
            }
        }

        private static string Compute(string ru)
        {
            int a = 0, b = ru.Length;
            while (a < b && char.IsWhiteSpace(ru[a])) a++;
            while (b > a && char.IsWhiteSpace(ru[b - 1])) b--;
            if (a == b) return ru;
            string core = ru.Substring(a, b - a);
            string r = Core(core, 0);
            return ReferenceEquals(r, core) || r == core ? ru : ru.Substring(0, a) + r + ru.Substring(b);
        }

        private static string Core(string s, int depth)
        {
            if (string.IsNullOrEmpty(s) || !HasCyrillic(s)) return s;
            if (_exact.TryGetValue(s, out var en)) return en;
            foreach (var rule in _rules)
            {
                var m = rule.Rx.Match(s);
                if (!m.Success) continue;
                return Slot.Replace(rule.Template, x =>
                {
                    int i = x.Groups[2].Value[0] - '0';
                    if (i + 1 >= m.Groups.Count) return x.Value;
                    string v = m.Groups[i + 1].Value;
                    return x.Groups[1].Length > 0 ? Core(v, depth + 1) : v;
                });
            }
            if (depth >= 3) return s;

            // подсказки из нескольких строк переводим построчно
            if (s.IndexOf('\n') >= 0)
            {
                var lines = s.Split('\n');
                bool any = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    string raw = lines[i].Trim();
                    string tr = Core(raw, depth + 1);
                    if (tr != raw) { lines[i] = lines[i].Replace(raw, tr); any = true; }
                }
                return any ? string.Join("\n", lines) : s;
            }

            // «Атлетика [26]» — значение рядом с названием
            var c = Counter.Match(s);
            if (c.Success && c.Groups[1].Length > 0)
            {
                string head = Core(c.Groups[1].Value, depth + 1);
                if (head != c.Groups[1].Value) return head + " " + c.Groups[2].Value;
            }

            // «A — Малая Европа» — буква района на карте
            var ml = MapLabel.Match(s);
            if (ml.Success)
            {
                string rest = Core(ml.Groups[3].Value, depth + 1);
                if (rest != ml.Groups[3].Value) return ml.Groups[1].Value + ml.Groups[2].Value + rest;
            }

            // «Название (уточнение)» — переводим части по отдельности
            var br = Bracket.Match(s);
            if (br.Success)
            {
                string head = Core(br.Groups[1].Value, depth + 1);
                string paren = br.Groups[2].Value;
                string tp = Core(paren, depth + 1);
                if (tp == paren) tp = "(" + Core(paren.Substring(1, paren.Length - 2), depth + 1) + ")";
                if (head != br.Groups[1].Value || tp != paren) return head + " " + tp;
            }

            // «ПОДПИСЬ: значение» и составные строки через « · » и « — »
            var lb = Label.Match(s);
            if (lb.Success)
            {
                string head = Core(lb.Groups[1].Value, depth + 1);
                if (head != lb.Groups[1].Value) return head + " " + Core(lb.Groups[2].Value, depth + 1);
            }
            foreach (var d in Delimiters)
            {
                if (s.IndexOf(d, StringComparison.Ordinal) < 0) continue;
                var parts = s.Split(new[] { d }, StringSplitOptions.None);
                bool changed = false;
                for (int i = 0; i < parts.Length; i++)
                {
                    string t = Core(parts[i].Trim(), depth + 1);
                    if (t != parts[i].Trim()) { parts[i] = t; changed = true; }
                }
                if (changed) return string.Join(d, parts);
            }
            return s;
        }

        public static bool HasCyrillic(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s) if (c >= 'Ѐ' && c <= 'ӿ') return true;
            return false;
        }

        // подключается один раз при старте приложения; при русском интерфейсе ничего не делает (кроме сбора, если он включён)
        public static void Install()
        {
            if (_installed || !(English || Harvest != null)) return;
            _installed = true;
            // Loaded у элементов без своих подписчиков WPF не рассылает, а SizeChanged приходит каждому элементу при первой раскладке
            EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnLoaded), true);
            EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.SizeChangedEvent, new RoutedEventHandler(OnLoaded), true);
        }

        // включить сбор из тестов, когда приложение уже запущено
        public static void StartHarvest(HashSet<string> sink)
        {
            Harvest = sink;
            Install();
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is FrameworkElement fe)) return;
            try { Apply(fe); }
            catch (Exception ex) { ErrorLog.Write("loc", ex); }
        }

        private static void Apply(FrameworkElement fe)
        {
            if (fe is Window w) w.Title = Pick(w.Title);

            if (fe is TextBlock tb)
            {
                if (BindingOperations.IsDataBound(tb, TextBlock.TextProperty))
                {
                    // значения, пришедшие из модели, меняются после загрузки: следим за свойством и переводим заново
                    var dpd = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
                    dpd.RemoveValueChanged(tb, TextChanged);
                    dpd.AddValueChanged(tb, TextChanged);
                }
                SetText(tb);
            }
            else if (fe is ContentControl cc && cc.Content is string str)
            {
                Set(cc, ContentControl.ContentProperty, str);
            }
            if (fe is HeaderedContentControl hc && hc.Header is string hs) Set(hc, HeaderedContentControl.HeaderProperty, hs);
            else if (fe is HeaderedItemsControl hi && hi.Header is string his) Set(hi, HeaderedItemsControl.HeaderProperty, his);

            if (fe.ToolTip is string tip) { string t2 = Pick(tip); if (t2 != tip) fe.ToolTip = t2; }
        }

        private static void TextChanged(object sender, EventArgs e)
        {
            if (_guard || !(sender is TextBlock tb)) return;
            SetText(tb);
        }

        // текст с разной разметкой внутри (жирные куски, ссылки) не трогаем, чтобы не потерять оформление
        private static void SetText(TextBlock tb)
        {
            if (tb.Inlines.Count > 1 || (tb.Inlines.Count == 1 && !(tb.Inlines.FirstInline is Run))) return;
            Set(tb, TextBlock.TextProperty, tb.Text);
        }

        private static void Set(DependencyObject d, DependencyProperty p, string current)
        {
            string result = Pick(current);
            if (result == current) return;
            _guard = true;
            try { d.SetCurrentValue(p, result); }
            finally { _guard = false; }
        }

        // собирает русские строки и возвращает перевод, если он есть и включён английский
        private static string Pick(string ru)
        {
            if (string.IsNullOrEmpty(ru)) return ru;
            if (Harvest != null && HasCyrillic(ru)) Harvest.Add(ru);
            return T(ru);
        }
    }
}
