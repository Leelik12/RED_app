using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberpunkRED_Generator
{
    // стиль листа: «минималистичный» (как задано в разметке) или «детальный» — словарь DetailedStyle.xaml подменяет одноимённые стили на лету
    public partial class CharacterSheetWindow
    {
        private const double HudHeight = 46;
        private readonly Dictionary<string, ResourceDictionary> _styleSets = new Dictionary<string, ResourceDictionary>();
        private readonly Dictionary<object, object> _minimalStyles = new Dictionary<object, object>();
        private readonly HashSet<object> _seenKeys = new HashSet<object>();

        private void InitStyle()
        {
            Action apply = ApplySheetStyle;
            Loaded += (s, e) => apply();
            AppSettings.StyleChanged += apply;
            Closed += (s, e) => AppSettings.StyleChanged -= apply;
        }

        private ResourceDictionary LoadStyleSet(string file)
        {
            string asm = typeof(CharacterSheetWindow).Assembly.GetName().Name;
            return (ResourceDictionary)Application.LoadComponent(new Uri("/" + asm + ";component/" + file + ".xaml", UriKind.Relative));
        }

        private void ApplySheetStyle()
        {
            string id = AppSettings.StyleId;
            bool detailed = id != "minimal";
            if (detailed && !_styleSets.ContainsKey(id))
            {
                var set = LoadStyleSet(id == "detailed2" ? "DetailedStyle2" : "DetailedStyle");
                // минималистичные определения запоминаются до первой подмены (ключ уже занят другим детальным стилем — значит, он запомнен)
                foreach (object key in set.Keys)
                    if (_seenKeys.Add(key) && Resources.Contains(key)) _minimalStyles[key] = Resources[key];
                _styleSets[id] = set;
            }
            _styleSets.TryGetValue(id, out var current);
            // ключи всех загруженных словарей: чего нет в выбранном стиле, возвращается к минималистичному определению
            foreach (object key in _styleSets.Values.SelectMany(d => d.Keys.Cast<object>()).Distinct().ToList())
            {
                if (detailed && current != null && current.Contains(key)) Resources[key] = current[key];
                else if (_minimalStyles.TryGetValue(key, out var original)) Resources[key] = original;
                else Resources.Remove(key);   // в минималистичном стиле такого определения не было
            }
            HudBar.Visibility = detailed ? Visibility.Visible : Visibility.Collapsed;
            SetLayoutGeometry(_sideActive);
            if (MiniSheet != null) MiniSheet.Visibility = MiniSheetShown() ? Visibility.Visible : Visibility.Collapsed;
            RebuildSideNav();
        }

        // мини-лист не нужен там, где все показатели и так на виду: на первой вкладке, в боковой раскладке и в детальном стиле (шапка)
        private bool MiniSheetShown() => !_sideActive && !AppSettings.DetailedStyle && SheetTabControl.SelectedIndex > 0;

        // портрет в круглой рамке шапки; без портрета — силуэт
        private void UpdateHudPortrait(ImageSource portrait)
        {
            if (HudPortrait == null) return;
            if (portrait == null)
            {
                HudPortrait.SetResourceReference(Border.BackgroundProperty, "RaisedBrush");
                HudGlyph.Visibility = Visibility.Visible;
            }
            else
            {
                HudPortrait.Background = new ImageBrush(portrait) { Stretch = Stretch.UniformToFill };
                HudGlyph.Visibility = Visibility.Collapsed;
            }
        }
    }
}
