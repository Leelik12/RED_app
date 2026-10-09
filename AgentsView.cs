using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    // окно «Все об агентах»: агент персонажа и его приложения, калькулятор взлома, внедрение команд, мелкие броски приложений
    public static class AgentsView
    {
        public static Window ShowWindow(Window owner, SheetViewModel vm)
        {
            var win = Ui.Dialog(owner, "ВСЕ ОБ АГЕНТАХ", 700, 780);
            var data = AppSettings.Data;
            var state = vm != null ? (vm.Agent ?? (vm.Agent = new AgentState())) : new AgentState();

            var root = new StackPanel { Margin = new Thickness(14) };
            root.Children.Add(Ui.Text("ВСЕ ОБ АГЕНТАХ", "DangerBrush", 16, true));
            var enabled = new CheckBox { Content = "Использовать агентов за этим столом", IsChecked = data.AgentsEnabled, Margin = new Thickness(0, 8, 0, 8), FontSize = 14 };
            enabled.SetResourceReference(Control.ForegroundProperty, "AccentBrush");
            root.Children.Add(enabled);
            var body = new StackPanel();
            root.Children.Add(body);

            // агент и приложения
            body.Children.Add(Ui.Text("МОЙ АГЕНТ", "DangerBrush", 13, true));
            var models = Agents.Models();
            var model = new ComboBox { Margin = new Thickness(0, 4, 0, 4) };
            model.Items.Add("— корник: стандартный агент —");
            foreach (var m in models) model.Items.Add(m.Name + " · " + Agents.QualityName(m.Get("quality")) + " · " + m.Cost + "eb");
            int idx = state.Model == null ? -1 : models.FindIndex(m => m.Name == state.Model);
            model.SelectedIndex = idx + 1;
            var modelInfo = Ui.Text("", "MutedBrush", 12);
            body.Children.Add(model);
            body.Children.Add(modelInfo);
            var bonus = Ui.Text("", "AccentBrush", 13, true);
            bonus.Margin = new Thickness(0, 6, 0, 6);
            body.Children.Add(bonus);

            body.Children.Add(Ui.Text("ПРИЛОЖЕНИЯ", "DangerBrush", 13, true));
            var appPanel = new StackPanel { Margin = new Thickness(0, 4, 0, 8) };
            Action refreshBonus = null;
            foreach (var app in Agents.AppItems())
            {
                var a = app;
                var cb = new CheckBox { Content = a.Name + " — " + (a.Cost == 0 ? "бесплатно" : a.Cost + "eb"), IsChecked = state.Apps.Contains(a.Name), Margin = new Thickness(0, 4, 0, 0) };
                cb.SetResourceReference(Control.ForegroundProperty, "TextBrush");
                cb.ToolTip = a.Info;
                cb.Checked += (s, e) => { if (!state.Apps.Contains(a.Name)) state.Apps.Add(a.Name); refreshBonus(); };
                cb.Unchecked += (s, e) => { state.Apps.Remove(a.Name); refreshBonus(); };
                var line = new StackPanel();
                line.Children.Add(cb);
                var t = Ui.Text(a.Info, "MutedBrush", 11); t.Margin = new Thickness(22, 0, 0, 0);
                line.Children.Add(t);
                appPanel.Children.Add(line);
            }
            body.Children.Add(appPanel);

            model.SelectionChanged += (s, e) =>
            {
                state.Model = model.SelectedIndex <= 0 ? null : models[model.SelectedIndex - 1].Name;
                refreshBonus();
            };
            refreshBonus = () =>
            {
                string q = state.Model == null ? Agents.Standard : Agents.QualityOf(state.Model);
                var item = state.Model == null ? null : models.FirstOrDefault(m => m.Name == state.Model);
                modelInfo.Text = (item != null ? item.Info : "Обычный агент из корника: звонки, сообщения, Дата Пул.") + (Agents.IsInternal(state.Model) ? "\nВнутренний: взлом Взломщиком невозможен." : "");
                int w = Agents.WardrobeBonus(state);
                bonus.Text = "Качество: " + Agents.QualityName(q) + " · Поиск информации +" + Agents.SearchBonus(state) + (w > 0 ? " · Гардероб и стиль +" + w : "");
            };

            // взлом
            body.Children.Add(Ui.Text("ВЗЛОМ АГЕНТА", "DangerBrush", 13, true));
            var target = new ComboBox { Width = 220, Margin = new Thickness(0, 4, 0, 4), HorizontalAlignment = HorizontalAlignment.Left };
            target.Items.Add("Плохое качество"); target.Items.Add("Стандартное качество"); target.Items.Add("Отличное качество");
            target.SelectedIndex = 1;
            var physical = new CheckBox { Content = "Физический доступ (Взломщик не нужен)", Margin = new Thickness(0, 2, 0, 2) };
            var intel = new CheckBox { Content = "Есть данные о защите (+2)", Margin = new Thickness(0, 2, 0, 2) };
            var whistle = new CheckBox { Content = "Свисток для обработки (ранг Интерфейса)", Margin = new Thickness(0, 2, 0, 2) };
            foreach (var c in new[] { physical, intel, whistle }) c.SetResourceReference(Control.ForegroundProperty, "TextBrush");
            var skill = Ui.Input(40); skill.Text = "11";
            var iface = Ui.Input(40); iface.Text = "4";
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            row.Children.Add(Ui.Text("Электроника/Безопасность (база):", "TextBrush", 13)); row.Children.Add(new Border { Width = 6 }); row.Children.Add(skill);
            row.Children.Add(new Border { Width = 12 }); row.Children.Add(Ui.Text("Интерфейс:", "TextBrush", 13)); row.Children.Add(new Border { Width = 6 }); row.Children.Add(iface);
            var dcText = Ui.Text("", "AccentBrush", 14, true);
            var go = Ui.Button("🎲 ВЗЛОМАТЬ", "DangerBrush");
            go.HorizontalAlignment = HorizontalAlignment.Left; go.Margin = new Thickness(0, 4, 0, 4);
            var result = Ui.Text("", "TextBrush", 13);
            Action dc = () => dcText.Text = "СЛ взлома: " + Agents.HackDc(QualityByIndex(target.SelectedIndex), physical.IsChecked == true) + " (проверка 1 минута)";
            target.SelectionChanged += (s, e) => dc();
            physical.Click += (s, e) => dc();
            go.Click += (s, e) =>
            {
                int.TryParse(skill.Text, out int b); int.TryParse(iface.Text, out int f);
                int dcv = Agents.HackDc(QualityByIndex(target.SelectedIndex), physical.IsChecked == true);
                int r = DiceRoller.D10Check(out string detail);
                int total = r + b + (whistle.IsChecked == true ? f : 0) + (intel.IsChecked == true ? 2 : 0);
                bool ok = Agents.HackSucceeds(total, dcv);
                result.Text = "Бросок " + detail + " + база " + b + (whistle.IsChecked == true ? " + Интерфейс " + f : "") + (intel.IsChecked == true ? " + 2" : "") + " = " + total + " против СЛ " + dcv + " — " +
                              (ok ? "успех: внедрите одну команду без проверки." : "провал: САИИ цели через 30 секунд предупредит владельца.");
            };
            body.Children.Add(target); body.Children.Add(physical); body.Children.Add(intel); body.Children.Add(whistle);
            body.Children.Add(row); body.Children.Add(dcText); body.Children.Add(go); body.Children.Add(result);

            body.Children.Add(Ui.Text("КОМАНДЫ ДЛЯ ВНЕДРЕНИЯ", "DangerBrush", 13, true));
            foreach (var c in Agents.Commands)
            {
                var p = new StackPanel();
                p.Children.Add(Ui.Text(c.Name + " — " + c.Time, "AccentBrush", 13, true));
                p.Children.Add(Ui.Text(c.Text, "MutedBrush", 12));
                var box = Ui.Box(p, pad: 6); box.Margin = new Thickness(0, 4, 0, 0);
                body.Children.Add(box);
            }
            var rules = Ui.Text(Agents.HackRules, "MutedBrush", 12); rules.Margin = new Thickness(0, 8, 0, 0);
            body.Children.Add(rules);

            // броски приложений
            body.Children.Add(Ui.Text("БРОСКИ ПРИЛОЖЕНИЙ", "DangerBrush", 13, true));
            var apps = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
            var hus = Ui.Button("HusTool 1d10"); var thr = Ui.Button("Вдохновитель 1d10");
            apps.Children.Add(hus); apps.Children.Add(new Border { Width = 8 }); apps.Children.Add(thr);
            var appResult = Ui.Text("", "TextBrush", 13);
            hus.Click += (s, e) => { int d = DiceRoller.Die(10); appResult.Text = "HusTool (1d10: " + d + "): " + Agents.HusTool(d); };
            thr.Click += (s, e) => { int d = DiceRoller.Die(10); appResult.Text = "Вдохновитель (1d10: " + d + "): " + Agents.Therapy(d); };
            body.Children.Add(apps); body.Children.Add(appResult);

            body.Children.Add(Ui.Text("ДЛЯ ГМА", "DangerBrush", 13, true));
            var gm = Ui.Text(Agents.GmNotes, "MutedBrush", 12); gm.Margin = new Thickness(0, 4, 0, 0);
            body.Children.Add(gm);

            Action sync = () =>
            {
                bool on = enabled.IsChecked == true;
                body.IsEnabled = on; body.Opacity = on ? 1 : 0.4;
            };
            enabled.Checked += (s, e) => { data.AgentsEnabled = true; AppSettings.Save(); sync(); };
            enabled.Unchecked += (s, e) => { data.AgentsEnabled = false; AppSettings.Save(); sync(); };
            refreshBonus(); dc(); sync();

            win.Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Ui.Present(win);
            return win;
        }

        private static string QualityByIndex(int i) => i == 0 ? Agents.Bad : i == 2 ? Agents.Good : Agents.Standard;
    }
}
