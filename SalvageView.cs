using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    // окно «Сбор обломков»: категория, проверка, стоимость улова и список подходящих находок
    public static class SalvageView
    {
        private static readonly Random Rng = new Random();
        // пункты списка «конкретная категория» → номер категории в таблице (1..6)
        private static readonly int[] PickToCategory = { 0, 1, 2, 3, 5, 6 };

        public static Window ShowWindow(Window owner)
        {
            var win = Ui.Dialog(owner, "СБОР ОБЛОМКОВ", 600, 720);
            var root = new StackPanel { Margin = new Thickness(14) };
            root.Children.Add(Ui.Text("СБОР ОБЛОМКОВ НАЙТ-СИТИ", "DangerBrush", 16, true));
            root.Children.Add(Ui.Text(Salvage.Rules, "MutedBrush", 12));

            root.Children.Add(Ui.Text("1. КАТЕГОРИЯ", "DangerBrush", 13, true));
            var cat = new ComboBox { Margin = new Thickness(0, 4, 0, 4), Width = 260, HorizontalAlignment = HorizontalAlignment.Left };
            foreach (var n in new[] { "Любая (бросок 1d6)", "Киберснаряжение", "Оружие", "Стандартное снаряжение", "Броня", "Кибердеки и программы" }) cat.Items.Add(n);
            cat.SelectedIndex = 0;
            root.Children.Add(cat);

            root.Children.Add(Ui.Text("2. ПРОВЕРКА", "DangerBrush", 13, true));
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            var bonus = Ui.Input(60); bonus.Text = "0";
            var go = Ui.Button("🎲 СОБИРАТЬ", "DangerBrush");
            row.Children.Add(Ui.Text("Бонус навыка (ТЕХ + навык):", "TextBrush", 13));
            row.Children.Add(new Border { Width = 8 });
            row.Children.Add(bonus);
            row.Children.Add(new Border { Width = 12 });
            row.Children.Add(go);
            root.Children.Add(row);

            var head = Ui.Text("Нажми «Собирать»", "AccentBrush", 16, true);
            head.Margin = new Thickness(0, 8, 0, 4);
            root.Children.Add(head);
            var detail = Ui.Text("", "TextBrush", 13);
            root.Children.Add(Ui.Box(detail, pad: 10));
            var list = Ui.Text("", "MutedBrush", 12);
            list.Margin = new Thickness(0, 8, 0, 0);
            root.Children.Add(list);

            go.Click += (s, e) =>
            {
                int.TryParse(bonus.Text, out int b);
                bool specific = cat.SelectedIndex > 0;
                int catRoll = 0;
                int category;
                if (specific) category = PickToCategory[cat.SelectedIndex];
                else { catRoll = Rng.Next(1, 7); category = catRoll; }
                int d10 = Rng.Next(1, 11);
                int total = d10 + b;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Категория: " + Salvage.Category(category) + (catRoll > 0 ? " (1d6: " + catRoll + ")" : " (выбрана)"));
                sb.Append("Проверка: 1d10 " + d10 + " + " + b + " = " + total);
                if (d10 == 1)
                {
                    int h = Rng.Next(1, 7);
                    head.Text = "КРИТИЧЕСКИЙ ПРОВАЛ — опасность!";
                    sb.AppendLine().Append("Опасность (1d6: " + h + "): " + Salvage.Hazard(h));
                    list.Text = "";
                }
                else
                {
                    int v = Salvage.Value(total, specific);
                    head.Text = v == 0 ? "Ничего ценного" : "Улов на " + v + "eb";
                    sb.AppendLine().Append(v == 0
                        ? "Итог ниже СЛ " + (specific ? Salvage.DcSpecific[0] : Salvage.DcAny[0]) + " — ничего не найдено."
                        : "Выберите находки в категории на сумму не больше " + v + "eb.");
                    var items = v == 0 ? new List<KeyValuePair<string, int>>() : Salvage.Eligible(category, v);
                    list.Text = items.Count == 0 ? "" : "ПОДХОДЯТ (" + items.Count + ", дороже — выше):\n" +
                        string.Join("\n", items.Take(60).Select(i => "• " + i.Key + " — " + i.Value + "eb")) +
                        (items.Count > 60 ? "\n… и ещё " + (items.Count - 60) : "");
                }
                detail.Text = sb.ToString();
            };

            root.Children.Add(Ui.Text("СЛ ПО СТОИМОСТИ", "DangerBrush", 13, true));
            var t = new System.Text.StringBuilder();
            for (int i = 0; i < Salvage.Prices.Length; i++)
                t.AppendLine(Salvage.Prices[i] + "eb — СЛ " + Salvage.DcAny[i] + " (конкретная категория: " + (Salvage.DcSpecific[i] > 0 ? "СЛ " + Salvage.DcSpecific[i] : "нет") + ")");
            var tt = Ui.Text(t.ToString().TrimEnd(), "MutedBrush", 12);
            tt.Margin = new Thickness(0, 4, 0, 0);
            root.Children.Add(tt);

            win.Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Ui.Present(win);
            return win;
        }
    }
}
