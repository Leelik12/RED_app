using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    // подбор жилья из DLC (кубические отели, контейнеры, конапты, студии): список, бросок 1d10, запись в лист
    public static class Housing
    {
        public static readonly string[] Types = { "Кубический отель", "Грузовой контейнер", "Квартира-студия", "Корпоративный конапт" };

        public static List<PackItem> Of(string type) => Packs.Enabled("property").Where(i => i.Group == type).OrderBy(i => int.TryParse(i.Get("roll"), out int n) ? n : 0).ToList();

        // 1d10 по таблице книги (порядок свойств = порядок таблицы)
        public static PackItem Roll(string type, int d10)
        {
            var list = Of(type);
            return list.FirstOrDefault(i => i.Get("roll") == d10.ToString());
        }

        public static string RentOf(PackItem p) => p.Get("rent") ?? "0";

        public static string Describe(PackItem p)
            => p.Name + " (" + p.Get("code") + ", " + p.Get("zone") + ")";
    }

    public static class HousingView
    {
        public static Window ShowWindow(Window owner, SheetViewModel vm)
        {
            var win = Ui.Dialog(owner, "ЖИЛЬЁ", 640, 640);
            var root = new StackPanel { Margin = new Thickness(14) };
            root.Children.Add(Ui.Text("ЖИЛЬЁ", "DangerBrush", 16, true));
            root.Children.Add(Ui.Text("Кубические отели и грузовые контейнеры, корпоративные конапты и студии из DLC. Можно выбрать или бросить 1d10 по таблице книги.", "MutedBrush", 12));

            var type = new ComboBox { Width = 240, Margin = new Thickness(0, 10, 0, 6), HorizontalAlignment = HorizontalAlignment.Left };
            foreach (var t in Housing.Types) type.Items.Add(t);
            type.SelectedIndex = 0;
            root.Children.Add(type);
            var pick = new ComboBox { Margin = new Thickness(0, 0, 0, 6) };
            root.Children.Add(pick);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 8) };
            var roll = Ui.Button("🎲 1d10");
            var apply = Ui.Button("ПОСЕЛИТЬСЯ", "DangerBrush", vm != null);
            buttons.Children.Add(roll); buttons.Children.Add(new Border { Width = 8 }); buttons.Children.Add(apply);
            root.Children.Add(buttons);

            var head = Ui.Text("", "AccentBrush", 16, true);
            var info = Ui.Text("", "TextBrush", 13);
            root.Children.Add(head);
            root.Children.Add(Ui.Box(info, pad: 10));

            var list = new List<PackItem>();
            Action fill = () =>
            {
                list = Housing.Of(Housing.Types[Math.Max(0, type.SelectedIndex)]);
                pick.Items.Clear();
                foreach (var p in list) pick.Items.Add(p.Get("roll") + ". " + p.Name + " — " + p.Get("zone"));
                if (list.Count > 0) pick.SelectedIndex = 0;
                else { head.Text = "Пакет DLC отключён в настройках."; info.Text = ""; }
            };
            Action show = () =>
            {
                if (pick.SelectedIndex < 0 || pick.SelectedIndex >= list.Count) return;
                var p = list[pick.SelectedIndex];
                head.Text = p.Name;
                string rent = p.Get("rentNote") ?? (Housing.RentOf(p) + "eb в месяц");
                info.Text = p.Get("code") + " · " + p.Get("zone") + "\nАренда: " + rent + "\n\n" + p.Info;
            };
            type.SelectionChanged += (s, e) => fill();
            pick.SelectionChanged += (s, e) => show();
            roll.Click += (s, e) =>
            {
                if (list.Count == 0) return;
                int d = DiceRoller.Die(10);
                var p = Housing.Roll(Housing.Types[type.SelectedIndex], d);
                if (p != null) pick.SelectedIndex = list.IndexOf(p);
            };
            apply.Click += (s, e) =>
            {
                if (vm == null || pick.SelectedIndex < 0 || pick.SelectedIndex >= list.Count) return;
                var p = list[pick.SelectedIndex];
                vm.Housing = Housing.Describe(p);
                vm.Rent = Housing.RentOf(p);
                MessageBox.Show(win, "Жильё записано в лист: " + vm.Housing, "ЖИЛЬЁ");
            };
            fill();

            win.Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Ui.Present(win);
            return win;
        }
    }
}
