using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    // «Эффекты и таймеры»: что действует на персонажа сейчас и сколько раундов осталось
    public static class EffectsView
    {
        private static EffectsData GmEffects;   // таймеры экрана ГМа живут, пока открыта программа

        public static Window ShowWindow(Window owner, SheetViewModel vm)
        {
            var win = Ui.Dialog(owner, "ЭФФЕКТЫ И ТАЙМЕРЫ", 640, 700);
            var fx = vm == null ? (GmEffects ?? (GmEffects = new EffectsData())) : (vm.Effects ?? (vm.Effects = new EffectsData()));

            var root = new Grid { Margin = new Thickness(14) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var head = new StackPanel();
            head.Children.Add(Ui.Text("ЭФФЕКТЫ И ТАЙМЕРЫ", "DangerBrush", 16, true));
            head.Children.Add(Ui.Text("Раунд = 3 секунды, минута = 20 раундов. «Раунд» уменьшает все таймеры; истёкшие эффекты снимаются и пишутся в журнал.", "MutedBrush", 12));
            Grid.SetRow(head, 0); root.Children.Add(head);

            var form = new WrapPanel { Margin = new Thickness(0, 10, 0, 10) };
            var name = Ui.Input(180);
            var note = Ui.Input(150);
            var amount = Ui.Input(50);
            amount.Text = "10";
            var unit = new ComboBox { Width = 90, Margin = new Thickness(6, 0, 6, 0) };
            foreach (var u in new[] { "раундов", "минут", "часов" }) unit.Items.Add(u);
            unit.SelectedIndex = 0;
            var forever = new CheckBox { Content = "до отмены", Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
            forever.SetResourceReference(Control.ForegroundProperty, "AccentBrush");
            var add = Ui.Button("+ ДОБАВИТЬ");
            form.Children.Add(Ui.Text("Название ", "TextBrush", 13)); form.Children.Add(name);
            form.Children.Add(Ui.Text("  Заметка ", "TextBrush", 13)); form.Children.Add(note);
            form.Children.Add(Ui.Text("  Срок ", "TextBrush", 13)); form.Children.Add(amount); form.Children.Add(unit);
            form.Children.Add(forever); form.Children.Add(add);
            var bar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            var tick = Ui.Button("▶ РАУНД (−1)");
            var tick5 = Ui.Button("▶▶ 5 РАУНДОВ");
            tick5.Margin = new Thickness(8, 0, 0, 0);
            var tickMin = Ui.Button("▶▶▶ МИНУТА");
            tickMin.Margin = new Thickness(8, 0, 0, 0);
            bar.Children.Add(tick); bar.Children.Add(tick5); bar.Children.Add(tickMin);
            var top = new StackPanel(); top.Children.Add(form); top.Children.Add(bar);
            Grid.SetRow(top, 1); root.Children.Add(top);

            var list = new StackPanel();
            var scroll = new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Grid.SetRow(scroll, 2); root.Children.Add(scroll);

            Action refresh = () =>
            {
                list.Children.Clear();
                if (fx.Items.Count == 0) list.Children.Add(Ui.Text("Активных эффектов нет.", "MutedBrush", 13));
                foreach (var e in fx.Items.ToList())
                {
                    var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
                    row.ColumnDefinitions.Add(new ColumnDefinition());
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var left = new StackPanel();
                    left.Children.Add(Ui.Text(e.Name, "AccentBrush", 15, true));
                    if (!string.IsNullOrWhiteSpace(e.Note)) left.Children.Add(Ui.Text(e.Note, "MutedBrush", 12));
                    var t = Ui.Text("⏱ " + EffectsData.Describe(e.RoundsLeft), e.RoundsLeft >= 0 && e.RoundsLeft <= 3 ? "WarnBrush" : "BrightBrush", 14, true);
                    t.VerticalAlignment = VerticalAlignment.Center; t.Margin = new Thickness(10, 0, 10, 0);
                    Grid.SetColumn(t, 1);
                    var x = Ui.Button("СНЯТЬ", "DangerBrush");
                    Grid.SetColumn(x, 2);
                    var item = e;
                    x.Click += (s, ev) => { fx.Remove(item); vm?.Journal?.Add("event", "Эффект снят: " + item.Name); };
                    row.Children.Add(left); row.Children.Add(t); row.Children.Add(x);
                    list.Children.Add(Ui.Box(row, "LineBrush", "PanelBrush", 8));
                    list.Children.Add(new Border { Height = 4 });
                }
            };
            Action onChanged = refresh;
            fx.Changed += onChanged;
            win.Closing += (s, e) => { fx.Changed -= onChanged; };

            add.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(name.Text)) return;
                int.TryParse(amount.Text, out int n);
                int rounds = forever.IsChecked == true ? -1 : EffectsData.ToRounds(Math.Max(1, n), (string)unit.SelectedItem);
                fx.Add(name.Text.Trim(), note.Text.Trim(), rounds);
                vm?.Journal?.Add("event", "Эффект: " + name.Text.Trim() + " (" + EffectsData.Describe(rounds) + ")");
                name.Text = ""; note.Text = "";
            };
            Action<int> pass = n =>
            {
                foreach (var ex in fx.Tick(n)) vm?.Journal?.Add("event", "Эффект закончился: " + ex.Name);
            };
            tick.Click += (s, e) => pass(1);
            tick5.Click += (s, e) => pass(5);
            tickMin.Click += (s, e) => pass(EffectsData.RoundsPerMinute);
            refresh();

            win.Content = root;
            Ui.Present(win);
            return win;
        }
    }
}
