using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CyberpunkRED_Generator
{
    // «Правила (выдержки)»: одна длинная страница из блоков — всё, что нужно за столом, без сканов книги.
    // Сверху поиск и кнопки перехода к блокам; ПКМ по навыку на листе открывает страницу сразу на нужном блоке.
    public static class RulesView
    {
        public const string WindowTitle = "ПРАВИЛА (ВЫДЕРЖКИ)";
        private static ToolWindow _current;
        private static Action<string> _goto;

        public static Window ShowWindow(Window owner, string query = null)
        {
            if (_current != null)
            {
                _goto?.Invoke(query);
                if (_current.CloseHosted == null) { _current.Activate(); return _current; }
                // страница уже открыта во вкладке листа: просто переключаемся на неё
                var stub = Ui.Dialog(owner, WindowTitle, 100, 100);
                stub.Content = new Grid();
                Ui.Present(stub);
                return _current;
            }

            bool en = Loc.English;
            var win = Ui.Dialog(owner, WindowTitle, 1000, 760);
            var blocks = Rules.Blocks;

            var root = new Grid { Margin = new Thickness(14) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var top = new StackPanel();
            var bar = new StackPanel { Orientation = Orientation.Horizontal };
            bar.Children.Add(Ui.Text(en ? "SEARCH: " : "ПОИСК: ", "DangerBrush", 13, true));
            var search = Ui.Input(300);
            bar.Children.Add(search);
            var found = Ui.Text("", "MutedBrush", 12);
            found.Margin = new Thickness(12, 0, 0, 0);
            bar.Children.Add(found);
            top.Children.Add(bar);
            var chips = new WrapPanel { Margin = new Thickness(0, 8, 0, 8) };
            top.Children.Add(chips);
            Grid.SetRow(top, 0);
            root.Children.Add(top);

            var page = new StackPanel { Margin = new Thickness(0, 0, 14, 20) };
            var scroll = new ScrollViewer { Content = page, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            Action<RuleBlock> scrollTo = null;
            var shown = new Dictionary<RuleBlock, StackPanel>();
            var headers = new Dictionary<RuleBlock, Border>();
            foreach (var b in blocks)
            {
                var block = new StackPanel { Margin = new Thickness(0, 0, 0, 22) };
                var head = new StackPanel();
                head.Children.Add(Ui.Text(b.Title, "DangerBrush", 17, true));
                head.Children.Add(Ui.Text(b.Source, "MutedBrush", 11));
                var headBox = new Border { Padding = new Thickness(8, 6, 8, 6), BorderThickness = new Thickness(0, 0, 0, 2), Margin = new Thickness(0, 0, 0, 8), Child = head };
                headBox.SetResourceReference(Border.BorderBrushProperty, "DangerBrush");
                headBox.SetResourceReference(Border.BackgroundProperty, "PanelBrush");
                block.Children.Add(headBox);
                foreach (var it in b.Items) block.Children.Add(Render(it, en));
                page.Children.Add(block);
                shown[b] = block;
                headers[b] = headBox;

                var rb = b;
                var chip = Ui.Button(ShortTitle(b.Title), "AccentBrush");
                chip.FontSize = 11;
                chip.Padding = new Thickness(7, 2, 7, 2);
                chip.Margin = new Thickness(0, 0, 6, 6);
                chip.Click += (s, e) => { search.Text = ""; scrollTo(rb); };
                chips.Children.Add(chip);
            }

            Action<RuleBlock> flash = null;
            DispatcherTimer timer = null;
            flash = b =>
            {
                var box = headers[b];
                box.SetResourceReference(Border.BackgroundProperty, "HoverBrush");
                timer?.Stop();
                timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1400) };
                timer.Tick += (s, e) => { timer.Stop(); box.SetResourceReference(Border.BackgroundProperty, "PanelBrush"); };
                timer.Start();
            };
            scrollTo = b =>
            {
                if (b == null || !headers.ContainsKey(b)) return;
                scroll.UpdateLayout();
                var block = shown[b];
                if (block.Visibility != Visibility.Visible) return;
                double y = block.TranslatePoint(new Point(0, 0), page).Y;
                scroll.ScrollToVerticalOffset(Math.Max(0, y - 4));
                flash(b);
            };

            Action applyFilter = () =>
            {
                string q = search.Text;
                int n = 0;
                foreach (var b in blocks)
                {
                    bool ok = Rules.Matches(b, q);
                    shown[b].Visibility = ok ? Visibility.Visible : Visibility.Collapsed;
                    if (ok) n++;
                }
                found.Text = string.IsNullOrWhiteSpace(q) ? "" : (en ? "blocks: " : "блоков: ") + n + (n == 0 ? (en ? " — nothing found" : " — ничего не найдено") : "");
                scroll.ScrollToTop();
            };
            search.TextChanged += (s, e) => applyFilter();

            _goto = q =>
            {
                if (string.IsNullOrWhiteSpace(q)) return;
                search.Text = "";
                var b = Rules.Find(q);
                if (b != null) win.Dispatcher.BeginInvoke(new Action(() => scrollTo(b)), DispatcherPriority.Loaded);
            };
            _current = win;
            win.Closing += (s, e) => { if (_current == win) { _current = null; _goto = null; } };

            win.Content = root;
            Ui.Present(win);
            _goto(query);
            return win;
        }

        // подпись на кнопке перехода: до двоеточия или целиком, не длиннее ~26 знаков
        private static string ShortTitle(string title)
        {
            int i = title.IndexOf(':');
            string t = i > 0 ? title.Substring(0, i) : title;
            return t.Length > 26 ? t.Substring(0, 25) + "…" : t;
        }

        private static FrameworkElement Render(RuleItem it, bool en)
        {
            switch (it.Kind)
            {
                case RuleKind.Formula:
                {
                    var t = Ui.Text(Rules.Expand(en ? it.En : it.Ru, en), "AccentBrush", 14, true);
                    var box = Ui.Box(t, "AccentBrush", "RaisedBrush", 8, 1);
                    box.Margin = new Thickness(0, 4, 0, 8);
                    return box;
                }
                case RuleKind.Note:
                {
                    var t = Ui.Text(Rules.Expand(en ? it.En : it.Ru, en), "WarnBrush", 12);
                    t.Margin = new Thickness(0, 2, 0, 10);
                    return t;
                }
                case RuleKind.Bullets:
                {
                    var list = new StackPanel { Margin = new Thickness(0, 2, 0, 8) };
                    foreach (var line in en ? it.BulletsEn : it.BulletsRu)
                    {
                        var row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
                        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        var dot = Ui.Text("•", "DangerBrush", 13, true);
                        dot.VerticalAlignment = VerticalAlignment.Top;
                        var text = Ui.Text(Rules.Expand(line, en), "TextBrush", 13);
                        text.VerticalAlignment = VerticalAlignment.Top;
                        Grid.SetColumn(text, 1);
                        row.Children.Add(dot);
                        row.Children.Add(text);
                        list.Children.Add(row);
                    }
                    return list;
                }
                case RuleKind.Table:
                    return Table(it, en);
                default:
                {
                    var t = Ui.Text(Rules.Expand(en ? it.En : it.Ru, en), "TextBrush", 13);
                    t.Margin = new Thickness(0, 0, 0, 8);
                    return t;
                }
            }
        }

        private static FrameworkElement Table(RuleItem it, bool en)
        {
            int cols = it.Head.Length;
            var g = new Grid { Margin = new Thickness(0, 4, 0, 12) };
            for (int c = 0; c < cols; c++)
            {
                int max = Math.Max(Rules.Pick(it.Head[c], en).Length, it.Rows.Max(r => c < r.Length ? Rules.Pick(r[c], en).Length : 0));
                // короткие столбцы по размеру содержимого, длинный текст делит остаток пропорционально
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = max <= 22 ? GridLength.Auto : new GridLength(Math.Min(max, 90) / 20.0, GridUnitType.Star) });
            }
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int c = 0; c < cols; c++)
            {
                var t = Ui.Text(Rules.Pick(it.Head[c], en), "AccentBrush", 12, true);
                t.TextWrapping = TextWrapping.Wrap;
                var cell = new Border { Padding = new Thickness(6, 4, 10, 4), BorderThickness = new Thickness(0, 0, 0, 2), Child = t };
                cell.SetResourceReference(Border.BorderBrushProperty, "AccentBrush");
                cell.SetResourceReference(Border.BackgroundProperty, "RaisedBrush");
                Grid.SetColumn(cell, c);
                g.Children.Add(cell);
            }

            for (int r = 0; r < it.Rows.Count; r++)
            {
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var row = it.Rows[r];
                for (int c = 0; c < cols; c++)
                {
                    var t = Ui.Text(c < row.Length ? Rules.Pick(row[c], en) : "", c == 0 ? "TextBrush" : "TextBrush", 12, c == 0);
                    t.VerticalAlignment = VerticalAlignment.Top;
                    var cell = new Border { Padding = new Thickness(6, 3, 10, 3), BorderThickness = new Thickness(0, 0, 0, 1), Child = t };
                    cell.SetResourceReference(Border.BorderBrushProperty, "LineBrush");
                    if (r % 2 == 1) cell.SetResourceReference(Border.BackgroundProperty, "PanelBrush");
                    Grid.SetRow(cell, r + 1);
                    Grid.SetColumn(cell, c);
                    g.Children.Add(cell);
                }
            }
            return g;
        }
    }
}
