using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    // окно «Панкноут»: конструктор, проверка сборки, срок работы, таблица гибели
    public static class PunknautView
    {
        public static Window ShowWindow(Window owner)
        {
            var win = Ui.Dialog(owner, "УЖАСАЮЩИЙ ПАНКНОУТ", 760, 800);
            var build = new PunknautBuild();
            build.Set("base", 1);

            var root = new StackPanel { Margin = new Thickness(14) };
            root.Children.Add(Ui.Text("УЖАСАЮЩИЙ ПАНКНОУТ", "DangerBrush", 16, true));
            root.Children.Add(Ui.Text("Максимум насилия, максимум скорости. Выберите детали — окно считает время, СЛ, характеристики и срок работы.", "MutedBrush", 12));

            var summary = Ui.Text("", "TextBrush", 13);
            var warn = Ui.Text("", "WarnBrush", 12, true);
            var partsPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 8) };
            Action refresh = null;
            var counters = new System.Collections.Generic.List<TextBlock>();

            foreach (var part in Punknaut.Parts)
            {
                var p = part;
                var row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var info = new StackPanel();
                info.Children.Add(Ui.Text(p.Name + " — " + p.Days + " дн · сбор СЛ " + p.GatherDc + " · сборка " + (p.Mod >= 0 ? "+" : "") + p.Mod + (p.Max > 0 ? " · макс " + p.Max : ""), "AccentBrush", 13, true));
                info.Children.Add(Ui.Text(p.Text, "MutedBrush", 11));
                Grid.SetColumn(info, 0);
                var ctl = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                var minus = Ui.Button("−"); var plus = Ui.Button("+");
                var n = Ui.Text("", "TextBrush", 14, true); n.Width = 30; n.TextAlignment = TextAlignment.Center;
                minus.Click += (s, e) => { build.Set(p.Id, build.Of(p.Id) - 1); refresh(); };
                plus.Click += (s, e) => { build.Set(p.Id, build.Of(p.Id) + 1); refresh(); };
                ctl.Children.Add(minus); ctl.Children.Add(n); ctl.Children.Add(plus);
                Grid.SetColumn(ctl, 1);
                row.Children.Add(info); row.Children.Add(ctl);
                partsPanel.Children.Add(Ui.Box(row, pad: 6));

                counters.Add(n);
            }

            refresh = () =>
            {
                for (int i = 0; i < counters.Count; i++) counters[i].Text = build.Of(Punknaut.Parts[i].Id).ToString();
                summary.Text =
                    "Время: " + build.TotalDays + " дн · СЛ сборки (Автомеханика): " + build.BuildDc + "\n" +
                    "ПЗТ " + build.Hp + " · места " + build.Seats + " · боевая скорость " + build.CombatSpeed + " СКО · " + build.NarrativeKmh + " км/ч\n" +
                    "Тяжесть управления: сумма навыка вождения выше " + build.SteerSkillOver + " и ТЕЛ выше " + build.SteerBodyOver +
                    (build.SteerPenalty > 0 ? " · штраф к вождению −" + build.SteerPenalty : "") +
                    (build.RamBonusD6 > 0 ? "\nТаран: +" + build.RamBonusD6 + "d6" : "") +
                    (build.SpikeRamD6 > 0 ? (build.RamBonusD6 > 0 ? " · " : "\n") + "с NOS и шипами +" + build.SpikeRamD6 + "d6" : "");
                var err = build.Validate();
                warn.Text = string.Join("\n", err);
            };

            root.Children.Add(partsPanel);
            root.Children.Add(Ui.Text("ИТОГ", "DangerBrush", 13, true));
            root.Children.Add(Ui.Box(summary, pad: 10));
            root.Children.Add(warn);

            // проверка сборки и гибель
            var act = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 6) };
            var bonus = Ui.Input(50); bonus.Text = "6";
            var go = Ui.Button("🎲 СОБРАТЬ", "DangerBrush");
            var doom = Ui.Button("💥 ГИБЕЛЬ (1d6)");
            act.Children.Add(Ui.Text("Автомеханика + Полевая экспертиза:", "TextBrush", 13));
            act.Children.Add(new Border { Width = 6 }); act.Children.Add(bonus); act.Children.Add(new Border { Width = 10 });
            act.Children.Add(go); act.Children.Add(new Border { Width = 6 }); act.Children.Add(doom);
            root.Children.Add(act);
            var result = Ui.Text("", "AccentBrush", 14, true);
            root.Children.Add(result);
            go.Click += (s, e) =>
            {
                if (build.Validate().Count > 0) { result.Text = "Сначала исправьте нарушения сборки."; return; }
                int.TryParse(bonus.Text, out int b);
                int r = DiceRoller.D10Check(out string detail);
                int total = r + b;
                var h = Punknaut.Hours(total, build.BuildDc);
                result.Text = "Проверка " + detail + " + " + b + " = " + total + " против СЛ " + build.BuildDc + " — " +
                              (h == null ? "Панкноут развалился: детали и " + build.TotalDays + " дн потеряны." : h == 0 ? "работает, но 0 часов запаса." : "Панкноут работает " + h + " ч до спасбросков от смерти (против ТЕХ Босса).");
            };
            doom.Click += (s, e) =>
            {
                int d = DiceRoller.Die(6);
                result.Text = "Гибель (1d6: " + d + "): " + Punknaut.DoomResult(d);
            };

            root.Children.Add(Ui.Text("ПРАВИЛА", "DangerBrush", 13, true));
            var rules = Ui.Text(Punknaut.Rules, "MutedBrush", 12); rules.Margin = new Thickness(0, 4, 0, 0);
            root.Children.Add(rules);
            root.Children.Add(Ui.Text("ТАБЛИЦА ГИБЕЛИ", "DangerBrush", 13, true));
            for (int i = 0; i < 6; i++)
            {
                var t = Ui.Text((i + 1) + ". " + Punknaut.Doom[i], "MutedBrush", 12); t.Margin = new Thickness(0, 3, 0, 0);
                root.Children.Add(t);
            }

            refresh();
            win.Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Ui.Present(win);
            return win;
        }
    }
}
