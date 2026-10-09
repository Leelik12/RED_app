using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    // «Ночные рынки»: карточка рынка из книги и случайный ассортимент на вечер
    public static class NightMarketView
    {
        private static readonly Random Rng = new Random();

        public static Window ShowWindow(Window owner, SheetViewModel vm)
        {
            var win = Ui.Dialog(owner, "НОЧНЫЕ РЫНКИ", 820, 780);
            var root = new Grid { Margin = new Thickness(14) };
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
            root.ColumnDefinitions.Add(new ColumnDefinition());

            var left = new StackPanel { Margin = new Thickness(0, 0, 14, 0) };
            left.Children.Add(Ui.Text("НОЧНЫЕ РЫНКИ", "DangerBrush", 16, true));
            left.Children.Add(Ui.Text("«Чёрный хром», гл. 15", "MutedBrush", 12));
            left.Children.Add(new Border { Height = 8 });
            Grid.SetColumn(left, 0);
            root.Children.Add(left);

            var right = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var body = new StackPanel();
            right.Content = body;
            Grid.SetColumn(right, 1);
            root.Children.Add(right);

            Action<NightMarketDef> show = null;
            show = m =>
            {
                body.Children.Clear();
                body.Children.Add(Ui.Text(m.Name, "AccentBrush", 20, true));
                body.Children.Add(Ui.Text(m.NameEn + " — " + m.Runner, "MutedBrush", 12));
                foreach (var kv in new[] { ("Когда", m.Frequency), ("Где", m.Place), ("Как попасть", m.Invite), ("Что продают", m.Sells) })
                {
                    var h = Ui.Text(kv.Item1.ToUpperInvariant(), "DangerBrush", 12, true);
                    h.Margin = new Thickness(0, 10, 0, 0);
                    body.Children.Add(h);
                    var t = Ui.Text(kv.Item2, "TextBrush", 13);
                    t.TextWrapping = TextWrapping.Wrap;
                    body.Children.Add(t);
                }

                var bar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 14, 0, 8) };
                var gen = Ui.Button("🎲 АССОРТИМЕНТ НА ВЕЧЕР");
                bar.Children.Add(gen);
                body.Children.Add(bar);

                var note = Ui.Text(m.StockNote + " Ассортимент собирается случайно из подключённых каталогов — это помощник ГМа, в книге списков по вечерам нет.", "MutedBrush", 12);
                note.TextWrapping = TextWrapping.Wrap;
                body.Children.Add(note);
                var stock = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
                body.Children.Add(stock);

                gen.Click += (s, e) =>
                {
                    stock.Children.Clear();
                    var items = NightMarket.Generate(m, Rng);
                    if (items.Count == 0) { stock.Children.Add(Ui.Text("Нет подходящих предметов в подключённых каталогах.", "WarnBrush", 13)); return; }
                    string kind = null;
                    foreach (var it in items)
                    {
                        if (it.Kind != kind)
                        {
                            kind = it.Kind;
                            var kh = Ui.Text(NightMarket.KindName(kind).ToUpperInvariant(), "DangerBrush", 12, true);
                            kh.Margin = new Thickness(0, 8, 0, 2);
                            stock.Children.Add(kh);
                        }
                        var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                        row.ColumnDefinitions.Add(new ColumnDefinition());
                        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        var name = Ui.Text(it.Name + (string.IsNullOrEmpty(it.Group) ? "" : "  · " + it.Group), "TextBrush", 14);
                        name.ToolTip = string.IsNullOrWhiteSpace(it.Info) ? null : (it.Info.Length > 600 ? it.Info.Substring(0, 600) + "…" : it.Info);
                        var price = Ui.Text(it.Cost.ToString("N0") + " eb", "WarnBrush", 14, true);
                        price.Margin = new Thickness(10, 0, 10, 0);
                        Grid.SetColumn(price, 1);
                        var item = it;
                        var buy = Ui.Button("В ЖУРНАЛ", "MutedBrush", vm != null);
                        buy.ToolTip = "Записать покупку в журнал сессии (деньги и снаряжение вносятся вручную)";
                        Grid.SetColumn(buy, 2);
                        buy.Click += (s2, e2) => { vm?.Journal?.Add("event", $"{m.Name}: {item.Name} — {item.Cost:N0} eb"); buy.IsEnabled = false; };
                        row.Children.Add(name); row.Children.Add(price); row.Children.Add(buy);
                        stock.Children.Add(row);
                    }
                };
            };

            foreach (var m in NightMarket.All)
            {
                var def = m;
                var b = Ui.Button(m.Name);
                b.HorizontalContentAlignment = HorizontalAlignment.Left;
                b.Margin = new Thickness(0, 0, 0, 4);
                b.Click += (s, e) => show(def);
                left.Children.Add(b);
            }
            show(NightMarket.All[0]);

            win.Content = root;
            Ui.Present(win);
            return win;
        }
    }
}
