using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    // окно бармена: смешивание напитков Drink Master 5000 и список рецептов
    public static class DrinksView
    {
        public static Window ShowWindow(Window owner)
        {
            var win = Ui.Dialog(owner, "БАР VA-11 HALL-A", 640, 700);
            var all = Drinks.All();
            var counts = new int[5];
            var numbers = new TextBlock[5];

            var root = new StackPanel { Margin = new Thickness(14) };
            root.Children.Add(Ui.Text("БАР VA-11 HALL-A", "DangerBrush", 16, true));
            root.Children.Add(Ui.Text("Drink Master 5000 смешивает пять химикатов. Набери смесь, выбери приготовление — программа подскажет напиток.", "MutedBrush", 12));
            if (all.Count == 0)
            {
                root.Children.Add(Ui.Text("Пакет DLC «Мешая напитки» отключён в настройках.", "WarnBrush", 13));
                win.Content = root;
                Ui.Present(win);
                return win;
            }

            var result = Ui.Text("", "AccentBrush", 18, true);
            var detail = Ui.Text("", "MutedBrush", 12);
            var intox = Ui.Text("", "TextBrush", 13);

            var ice = new CheckBox { Content = "Со льдом", Margin = new Thickness(0, 0, 14, 0) };
            var aged = new CheckBox { Content = "Выдержанный", Margin = new Thickness(0, 0, 14, 0) };
            var blended = new CheckBox { Content = "Взбитый (иначе — Смешанный)" };
            foreach (var c in new[] { ice, aged, blended }) c.SetResourceReference(Control.ForegroundProperty, "AccentBrush");

            Action refresh = () =>
            {
                for (int k = 0; k < 5; k++) numbers[k].Text = counts[k].ToString();
                var d = Drinks.Match(all, counts, ice.IsChecked == true, aged.IsChecked == true, blended.IsChecked == true);
                int dc = Drinks.IntoxicationDc(counts[4]);
                if (counts.All(n => n == 0)) { result.Text = "Пустой стакан"; detail.Text = ""; }
                else if (d != null) { result.Text = d.Name + " — " + d.Price + "eb"; detail.Text = d.Taste + " · " + d.How; }
                else { result.Text = "Неизвестная смесь"; detail.Text = "Такого напитка нет в списке Drink Master 5000."; }
                intox.Text = dc == 0 ? "Кармотрин " + counts[4] + ": опьянения нет (порог — 8 единиц, как две бутылки пива)."
                                     : "Кармотрин " + counts[4] + ": опьянение, Сопротивление пыткам/наркотикам СЛ" + dc + ", штраф −4 (−2 при лёгком подпитии).";
            };
            ice.Checked += (s, e) => refresh(); ice.Unchecked += (s, e) => refresh();
            aged.Checked += (s, e) => refresh(); aged.Unchecked += (s, e) => refresh();
            blended.Checked += (s, e) => refresh(); blended.Unchecked += (s, e) => refresh();

            var grid = new Grid { Margin = new Thickness(0, 10, 0, 10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            for (int k = 0; k < 5; k++)
            {
                int i = k;
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var name = Ui.Text(Drinks.Chemicals[k], "TextBrush", 13);
                var minus = Ui.Button("−"); minus.Width = 30;
                var plus = Ui.Button("+"); plus.Width = 30;
                numbers[k] = Ui.Text("0", "AccentBrush", 15, true);
                numbers[k].HorizontalAlignment = HorizontalAlignment.Center;
                minus.Click += (s, e) => { if (counts[i] > 0) counts[i]--; refresh(); };
                plus.Click += (s, e) => { if (counts[i] < 20) counts[i]++; refresh(); };
                Grid.SetRow(name, k); Grid.SetRow(minus, k); Grid.SetRow(numbers[k], k); Grid.SetRow(plus, k);
                Grid.SetColumn(name, 0); Grid.SetColumn(minus, 1); Grid.SetColumn(numbers[k], 2); Grid.SetColumn(plus, 3);
                name.Margin = new Thickness(0, 3, 0, 3);
                grid.Children.Add(name); grid.Children.Add(minus); grid.Children.Add(numbers[k]); grid.Children.Add(plus);
            }
            root.Children.Add(grid);

            var how = new StackPanel { Orientation = Orientation.Horizontal };
            how.Children.Add(ice); how.Children.Add(aged); how.Children.Add(blended);
            root.Children.Add(how);

            var clear = Ui.Button("ОЧИСТИТЬ", "DangerBrush");
            clear.HorizontalAlignment = HorizontalAlignment.Left;
            clear.Margin = new Thickness(0, 10, 0, 8);
            clear.Click += (s, e) => { Array.Clear(counts, 0, 5); ice.IsChecked = aged.IsChecked = blended.IsChecked = false; refresh(); };
            root.Children.Add(clear);

            root.Children.Add(Ui.Box(new StackPanel { Children = { result, detail, intox } }, pad: 10));

            var rt = Ui.Text("РЕЦЕПТЫ (нажми, чтобы загрузить в миксер)", "DangerBrush", 13, true);
            rt.Margin = new Thickness(0, 14, 0, 4);
            root.Children.Add(rt);
            foreach (var d in all.OrderBy(x => x.Name))
            {
                var dd = d;
                var b = Ui.Button(d.Name + " — " + d.Price + "eb · " + d.Taste, "MutedBrush");
                b.HorizontalContentAlignment = HorizontalAlignment.Left;
                b.Margin = new Thickness(0, 1, 0, 1);
                b.Click += (s, e) =>
                {
                    for (int k = 0; k < 5; k++) counts[k] = dd.Chem[k];
                    ice.IsChecked = dd.Ice; aged.IsChecked = dd.Aged; blended.IsChecked = dd.Blended;
                    refresh();
                };
                root.Children.Add(b);
            }
            refresh();

            win.Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Ui.Present(win);
            return win;
        }
    }
}
