using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    // окно «Погода в Найт-Сити»: выбор месяца, бросок, механика текущей погоды и короткая история
    public static class WeatherView
    {
        private static readonly List<string> History = new List<string>();

        public static Window ShowWindow(Window owner)
        {
            var win = Ui.Dialog(owner, "ПОГОДА В НАЙТ-СИТИ", 520, 560);

            var root = new StackPanel { Margin = new Thickness(14) };
            root.Children.Add(Ui.Text("ПОГОДА В НАЙТ-СИТИ", "DangerBrush", 16, true));
            root.Children.Add(Ui.Text("Два броска 1d6 по таблице месяца: температура и условия. При 6 на условиях — странная погода.", "MutedBrush", 12));

            var top = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 10) };
            var month = new ComboBox { Width = 150, Margin = new Thickness(0, 0, 10, 0) };
            foreach (var m in Weather.MonthNames) month.Items.Add(m);
            month.SelectedIndex = DateTime.Now.Month - 1;
            var roll = Ui.Button("🎲 БРОСИТЬ");
            top.Children.Add(Ui.Text("Месяц в игре:", "TextBrush", 13));
            top.Children.Add(new Border { Width = 8 });
            top.Children.Add(month);
            top.Children.Add(roll);
            root.Children.Add(top);

            var season = Ui.Text("", "MutedBrush", 12);
            root.Children.Add(season);
            Action updSeason = () => season.Text = "Таблица: " + Weather.SeasonName(month.SelectedIndex + 1);
            month.SelectionChanged += (s, e) => updSeason();
            updSeason();

            var head = Ui.Text("Нажми «Бросить»", "AccentBrush", 18, true);
            head.Margin = new Thickness(0, 10, 0, 4);
            root.Children.Add(head);
            var detail = Ui.Text("", "MutedBrush", 12);
            root.Children.Add(detail);
            var rules = Ui.Text("", "TextBrush", 13);
            rules.Margin = new Thickness(0, 8, 0, 0);
            root.Children.Add(Ui.Box(rules, pad: 10));

            root.Children.Add(Ui.Text("ИСТОРИЯ", "DangerBrush", 13, true));
            var hist = Ui.Text("", "MutedBrush", 12);
            hist.Margin = new Thickness(0, 4, 0, 0);

            Action showHist = () => hist.Text = string.Join("\n", History);
            roll.Click += (s, e) =>
            {
                var r = Weather.Roll(month.SelectedIndex + 1);
                head.Text = Weather.Summary(r);
                detail.Text = "d6 температура: " + r.TempRoll + " · d6 условия: " + r.CondRoll +
                              (r.Strange != null ? " · d10 странная: " + r.StrangeRoll + " · длится " + r.Duration : "");
                rules.Text = r.Rules;
                History.Insert(0, Weather.MonthNames[r.Month - 1] + ": " + Weather.Summary(r));
                if (History.Count > 12) History.RemoveAt(History.Count - 1);
                showHist();
            };
            root.Children.Add(hist);
            showHist();

            win.Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Ui.Present(win);
            return win;
        }
    }
}
