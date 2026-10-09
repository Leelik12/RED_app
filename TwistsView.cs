using System;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    // «Перчинки для боя»: бросок по таблицам Прожжённых шестёрок, лейтенантов и мини-боссов
    public static class TwistsView
    {
        private static readonly Random Rng = new Random();

        public static Window ShowWindow(Window owner, SheetViewModel vm)
        {
            var win = Ui.Dialog(owner, "ПЕРЧИНКИ ДЛЯ БОЯ", 760, 700);
            var root = new StackPanel { Margin = new Thickness(14) };
            root.Children.Add(Ui.Text("ПРОЖЖЁННЫЕ: ПЕРЧИНКИ ДЛЯ СТОЛКНОВЕНИЯ", "DangerBrush", 16, true));
            root.Children.Add(Ui.Text("Таблицы 1d10 из бесплатных DLC. Бросайте, когда столкновение слишком лёгкое для партии.", "MutedBrush", 12));

            foreach (var t in Twists.All)
            {
                var table = t;
                var box = new StackPanel();
                box.Children.Add(Ui.Text(t.Name, "AccentBrush", 15, true));
                box.Children.Add(Ui.Text(t.Hint, "MutedBrush", 12));

                var result = Ui.Text("", "WarnBrush", 14, true);
                result.Margin = new Thickness(0, 6, 0, 0);
                var bar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
                var roll = Ui.Button("🎲 БРОСИТЬ 1d10");
                var log = Ui.Button("В ЖУРНАЛ", "MutedBrush", false);
                log.Margin = new Thickness(8, 0, 0, 0);
                int last = 0;
                roll.Click += (s, e) =>
                {
                    last = Twists.RollD10(Rng);
                    result.Text = last + ". " + Twists.Line(table, last);
                    log.IsEnabled = vm != null;
                    log.Opacity = log.IsEnabled ? 1 : 0.4;
                };
                log.Click += (s, e) => { if (last > 0) { vm?.Journal?.Add("event", table.Name + " (" + last + "): " + Twists.Line(table, last)); log.IsEnabled = false; log.Opacity = 0.4; } };
                bar.Children.Add(roll); bar.Children.Add(log);
                box.Children.Add(bar);
                box.Children.Add(result);

                var exp = new Expander { Header = "Вся таблица", Margin = new Thickness(0, 6, 0, 0) };
                exp.SetResourceReference(Control.ForegroundProperty, "MutedBrush");
                var rows = new StackPanel();
                for (int i = 0; i < 10; i++)
                {
                    var r = Ui.Text((i + 1) + ". " + table.Rows[i], "TextBrush", 13);
                    r.Margin = new Thickness(0, 2, 0, 2);
                    rows.Children.Add(r);
                }
                exp.Content = rows;
                box.Children.Add(exp);

                root.Children.Add(Ui.Box(box, "AccentDarkBrush", "PanelBrush", 10));
                root.Children[root.Children.Count - 1].SetValue(FrameworkElement.MarginProperty, new Thickness(0, 12, 0, 0));
            }

            win.Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Ui.Present(win);
            return win;
        }
    }
}
