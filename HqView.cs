using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    // окно «Штаб-квартира»: описание, ОУ ШК, улучшения и модернизации, действующие бонусы, пример Cyber6
    public static class HqView
    {
        public static Window ShowWindow(Window owner, SheetViewModel vm)
        {
            var win = Ui.Dialog(owner, "ШТАБ-КВАРТИРА", 760, 820);
            var h = vm != null ? (vm.Hq ?? (vm.Hq = new HqData())) : new HqData();
            var upgrades = Hq.Upgrades();

            var root = new StackPanel { Margin = new Thickness(14) };
            root.Children.Add(Ui.Text("ШТАБ-КВАРТИРА", "DangerBrush", 16, true));
            if (upgrades.Count == 0)
            {
                root.Children.Add(Ui.Text("Пакет «Нет места лучше дома» отключён в настройках.", "WarnBrush", 13));
                win.Content = root; Ui.Present(win); return win;
            }
            root.Children.Add(Ui.Text("Команда копит ОУ ШК (столько же, сколько получает в групповой колонке после миссии) и тратит их на улучшения по 40 ОУ; модернизация тоже 40. Решение принимает вся команда. Не заплатили аренду — потеряли ШК и её бонусы.", "MutedBrush", 12));

            // паспорт базы
            var name = Ui.Input(); var loc = Ui.Input(); var rent = Ui.Input(90); var beds = Ui.Input(50); var desc = Ui.Input();
            desc.AcceptsReturn = true; desc.TextWrapping = TextWrapping.Wrap; desc.MinHeight = 48;
            name.Text = h.Name; loc.Text = h.Location; rent.Text = h.Rent.ToString(); beds.Text = h.BaseBeds.ToString(); desc.Text = h.Description;
            Func<string, UIElement, UIElement> field = (label, box) =>
            {
                var p = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
                p.Children.Add(Ui.Text(label, "MutedBrush", 11)); p.Children.Add(box);
                return p;
            };
            root.Children.Add(field("НАЗВАНИЕ", name));
            root.Children.Add(field("ЛОКАЦИЯ", loc));
            var line = new StackPanel { Orientation = Orientation.Horizontal };
            line.Children.Add(field("АРЕНДА В МЕСЯЦ, eb", rent)); line.Children.Add(new Border { Width = 14 });
            line.Children.Add(field("СПАЛЬНЫХ МЕСТ ДО «СНИЖЕНИЯ АРЕНДЫ»", beds));
            root.Children.Add(line);
            root.Children.Add(field("ОПИСАНИЕ", desc));

            // очки
            var points = Ui.Text("", "AccentBrush", 15, true);
            points.Margin = new Thickness(0, 10, 0, 4);
            root.Children.Add(points);
            var add = Ui.Input(60); add.Text = "40";
            var note = Ui.Input(220);
            var addBtn = Ui.Button("+ ОУ ШК");
            var subBtn = Ui.Button("−", "DangerBrush");
            var bar = new StackPanel { Orientation = Orientation.Horizontal };
            bar.Children.Add(add); bar.Children.Add(new Border { Width = 6 }); bar.Children.Add(note); bar.Children.Add(new Border { Width = 6 });
            bar.Children.Add(addBtn); bar.Children.Add(new Border { Width = 4 }); bar.Children.Add(subBtn);
            root.Children.Add(bar);
            root.Children.Add(Ui.Text("(справа от числа — заметка, например «миссия: Чёрный Пёс»)", "MutedBrush", 11));
            var log = Ui.Text("", "MutedBrush", 11);
            root.Children.Add(log);

            var benefits = Ui.Text("", "TextBrush", 12);
            root.Children.Add(Ui.Text("ДЕЙСТВУЮЩИЕ БОНУСЫ", "DangerBrush", 13, true));
            root.Children.Add(Ui.Box(benefits, pad: 8));

            // улучшения
            root.Children.Add(Ui.Text("УЛУЧШЕНИЯ И МОДЕРНИЗАЦИИ", "DangerBrush", 13, true));
            var rows = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
            root.Children.Add(rows);
            Action rebuild = null;

            Action refresh = () =>
            {
                points.Text = "ОУ ШК: накоплено " + h.Earned + " · потрачено " + Hq.Spent(h) + " · свободно " + Hq.Remaining(h);
                log.Text = h.Log.Count == 0 ? "" : string.Join("\n", h.Log.Take(6));
                var b = Hq.Benefits(h);
                benefits.Text = b.Count == 0 ? "Пока ничего не куплено." : "• " + string.Join("\n• ", b);
            };

            rebuild = () =>
            {
                rows.Children.Clear();
                foreach (var u in upgrades)
                {
                    var it = u; string uid = it.Get("uid");
                    bool multi = it.Get("multi") != null;
                    var box = new StackPanel();
                    var top = new StackPanel { Orientation = Orientation.Horizontal };
                    var own = new CheckBox { Content = it.Name + " — 40 ОУ", IsChecked = Hq.Has(h, uid), FontSize = 14, FontWeight = FontWeights.Bold };
                    own.SetResourceReference(Control.ForegroundProperty, "AccentBrush");
                    top.Children.Add(own);
                    box.Children.Add(top);
                    var t = Ui.Text(it.Info, "MutedBrush", 11); t.Margin = new Thickness(22, 2, 0, 2);
                    box.Children.Add(t);

                    var modRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(22, 2, 0, 0) };
                    if (multi)
                    {
                        var minus = Ui.Button("−"); var plus = Ui.Button("+ модернизация (40)");
                        var cnt = Ui.Text(uid == "rent" ? h.RentMods + " из " + Hq.MaxRentMods(h) : h.MoralMods + " из " + Hq.MaxMoralMods, "TextBrush", 13, true);
                        cnt.Margin = new Thickness(8, 0, 8, 0); cnt.VerticalAlignment = VerticalAlignment.Center;
                        minus.IsEnabled = plus.IsEnabled = Hq.Has(h, uid);
                        minus.Click += (s, e) => { Hq.Unmod(h, uid); rebuild(); refresh(); };
                        plus.Click += (s, e) =>
                        {
                            if (!Hq.Mod(h, uid)) MessageBox.Show(win, "Не хватает ОУ ШК или достигнут предел модернизаций.", "ШТАБ-КВАРТИРА");
                            rebuild(); refresh();
                        };
                        modRow.Children.Add(minus); modRow.Children.Add(cnt); modRow.Children.Add(plus);
                    }
                    else
                    {
                        var mod = new CheckBox { Content = "Модернизация — 40 ОУ", IsChecked = h.Modded.Contains(uid), IsEnabled = Hq.Has(h, uid) };
                        mod.SetResourceReference(Control.ForegroundProperty, "TextBrush");
                        mod.Checked += (s, e) =>
                        {
                            if (!Hq.Mod(h, uid)) { MessageBox.Show(win, "Не хватает ОУ ШК.", "ШТАБ-КВАРТИРА"); }
                            rebuild(); refresh();
                        };
                        mod.Unchecked += (s, e) => { Hq.Unmod(h, uid); rebuild(); refresh(); };
                        modRow.Children.Add(mod);
                    }
                    box.Children.Add(modRow);
                    var mt = Ui.Text("Модернизация: " + it.Get("mod"), "MutedBrush", 11); mt.Margin = new Thickness(22, 2, 0, 2);
                    box.Children.Add(mt);

                    own.Checked += (s, e) =>
                    {
                        if (!Hq.Buy(h, uid)) MessageBox.Show(win, "Не хватает ОУ ШК.", "ШТАБ-КВАРТИРА");
                        rebuild(); refresh();
                    };
                    own.Unchecked += (s, e) => { Hq.Remove(h, uid); rebuild(); refresh(); };
                    var wrap = Ui.Box(box, pad: 8); wrap.Margin = new Thickness(0, 0, 0, 6);
                    rows.Children.Add(wrap);
                    if (uid == "moral" && Hq.Has(h, uid) && h.MoralMods > 0)
                    {
                        var act = Hq.ActiveMoral(h.MoralMods);
                        var tbl = Hq.MoralTable().Where(m => act.Contains(int.Parse(m.Get("n")))).Select(m => m.Get("n") + ". " + m.Info);
                        var ms = Ui.Text("Действует:\n" + string.Join("\n", tbl), "TextBrush", 11);
                        ms.Margin = new Thickness(22, 0, 0, 6);
                        rows.Children.Add(ms);
                    }
                }
            };

            // подгрузка полей
            Action readFields = () =>
            {
                h.Name = name.Text; h.Location = loc.Text; h.Description = desc.Text;
                int.TryParse(rent.Text, out int r); h.Rent = r;
                int.TryParse(beds.Text, out int bd); h.BaseBeds = Math.Max(1, bd);
            };
            name.LostFocus += (s, e) => readFields();
            loc.LostFocus += (s, e) => readFields();
            desc.LostFocus += (s, e) => readFields();
            rent.LostFocus += (s, e) => readFields();
            beds.LostFocus += (s, e) => { readFields(); rebuild(); refresh(); };
            addBtn.Click += (s, e) => { int.TryParse(add.Text, out int n); Hq.AddPoints(h, n, note.Text); note.Text = ""; rebuild(); refresh(); };
            subBtn.Click += (s, e) => { int.TryParse(add.Text, out int n); Hq.AddPoints(h, -n, note.Text); note.Text = ""; rebuild(); refresh(); };
            win.Closing += (s, e) => readFields();

            var sample = Ui.Button("Пример из книги: ШК Cyber6");
            sample.HorizontalAlignment = HorizontalAlignment.Left; sample.Margin = new Thickness(0, 10, 0, 0);
            sample.Click += (s, e) =>
            {
                if (MessageBox.Show(win, "Заменить данные штаб-квартиры примером Cyber6 (акт 3)?", "ШТАБ-КВАРТИРА", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
                var ex = Hq.SampleCyber6();
                h.Name = ex.Name; h.Location = ex.Location; h.Description = ex.Description; h.Rent = ex.Rent; h.BaseBeds = ex.BaseBeds; h.Earned = ex.Earned;
                h.Upgrades = ex.Upgrades; h.Modded = ex.Modded; h.RentMods = ex.RentMods; h.MoralMods = ex.MoralMods; h.Log.Clear();
                name.Text = h.Name; loc.Text = h.Location; desc.Text = h.Description; rent.Text = h.Rent.ToString(); beds.Text = h.BaseBeds.ToString();
                rebuild(); refresh();
            };
            root.Children.Add(sample);

            var fr = Hq.FactionRule();
            if (fr != null)
            {
                root.Children.Add(Ui.Text("ШТАБЫ ФРАКЦИЙ НИП", "DangerBrush", 13, true));
                var f = Ui.Text(fr.Info, "MutedBrush", 12); f.Margin = new Thickness(0, 4, 0, 0);
                root.Children.Add(f);
            }

            rebuild(); refresh();
            win.Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Ui.Present(win);
            return win;
        }
    }
}
