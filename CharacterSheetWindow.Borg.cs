using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    // полная конверсия тела (ПКТ): выбор корпуса, потеря человечности, переход в корпус / смена корпуса
    public partial class CharacterSheetWindow
    {
        private void InitBorgMenu()
        {
            RegisterMenuItem("🤖 Полная конверсия (ПКТ)", ShowBorgDialog, "ПЕРСОНАЖ");
        }

        private void ShowBorgDialog()
        {
            CloseOverlays();
            var vm = DataContext as SheetViewModel;
            if (vm == null) return;
            var chassis = Borg.All;

            var win = Ui.Dialog(this, "ПОЛНАЯ КОНВЕРСИЯ ТЕЛА", 680, 760);
            var root = new StackPanel { Margin = new Thickness(14) };
            root.Children.Add(Ui.Text("ПОЛНАЯ КОНВЕРСИЯ ТЕЛА (ПКТ)", "DangerBrush", 16, true));
            root.Children.Add(Ui.Text(vm.IsBorg
                    ? "Текущий корпус: " + (Borg.Find(vm.Chassis)?.FullName ?? vm.Chassis)
                    : "Сейчас у персонажа мясное тело. Операция необратима: мозг и нужные органы переносятся в биосистему, мясное тело теряется.",
                vm.IsBorg ? "AccentBrush" : "TextBrush", 13, vm.IsBorg));

            if (chassis.Count == 0)
            {
                root.Children.Add(Ui.Text("Пакет DLC «Переходи на Металл» отключён в настройках.", "WarnBrush", 13));
                win.Content = root;
                Ui.Present(win);
                return;
            }

            var pick = new ComboBox { Margin = new Thickness(0, 10, 0, 6) };
            foreach (var c in chassis.OrderBy(c => c.Price)) pick.Items.Add(c.FullName + " — " + c.Price + "eb");
            pick.SelectedIndex = 0;
            root.Children.Add(Ui.Text(vm.IsBorg ? "СМЕНИТЬ КОРПУС" : "ВЫБОР КОРПУСА", "DangerBrush", 13, true));
            root.Children.Add(pick);

            var info = Ui.Text("", "TextBrush", 12);
            var infoBox = Ui.Box(info, pad: 10);
            root.Children.Add(infoBox);

            Func<BorgChassis> current = () => chassis.OrderBy(c => c.Price).ElementAt(Math.Max(0, pick.SelectedIndex));
            Action showInfo = () =>
            {
                var c = current();
                info.Text = c.Info + "\n\n" +
                    "Цена: " + c.Price + "eb (" + c.Tier + ") · установка: Госпиталь\n" +
                    "ПЧ за комплект: " + c.HlAvg + " (" + c.HlFormula + ")" + (vm.IsBorg ? "" : " + биосистема " + Borg.BiosystemHlAvg + " (4d6), 10000eb") + "\n" +
                    "Эндоскелет: " + c.EndoName + " → ТЕЛ " + c.EndoBody + (c.HydraulicFloor > 0 ? " · РЕА/ЛВК/СКО не ниже " + c.HydraulicFloor : "") + "\n" +
                    "Максимум человечности: −" + (c.MaxHumanityLoss + (vm.IsBorg ? 0 : Borg.BiosystemMaxHumanityLoss)) + "\n\n" +
                    "Без ПЧ: " + string.Join(", ", c.Free) + "\n" +
                    "С ПЧ: " + string.Join(", ", c.Kit.Select(k => k.ToString()));
            };
            pick.SelectionChanged += (s, e) => showInfo();
            showInfo();

            var go = Ui.Button(vm.IsBorg ? "СМЕНИТЬ КОРПУС" : "ПЕРЕЙТИ В КОРПУС", "DangerBrush");
            go.HorizontalAlignment = HorizontalAlignment.Left;
            go.Margin = new Thickness(0, 10, 0, 10);
            go.Click += (s, e) =>
            {
                ConvertToBorg(vm, current(), this, () => win.Close());
            };
            root.Children.Add(go);

            root.Children.Add(Ui.Text("ПРАВИЛА ПКТ", "DangerBrush", 13, true));
            var rules = Ui.Text(Borg.Rules, "MutedBrush", 12);
            rules.Margin = new Thickness(0, 4, 0, 0);
            root.Children.Add(rules);

            win.Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            Ui.Present(win);
        }

        // запрашивает итоговую потерю человечности (среднее или бросок); null — отмена
        private void AskBorgHumanityLoss(Window owner, string what, int average, Func<int> roll, Action<int> confirmed)
        {
            var dlg = Ui.Dialog(owner, "ПОТЕРЯ ЧЕЛОВЕЧНОСТИ", 440, 260);
            var root = new StackPanel { Margin = new Thickness(14) };
            root.Children.Add(Ui.Text(what, "TextBrush", 13));
            root.Children.Add(Ui.Text("Среднее значение: " + average + " (можно брать без броска). Итог можно поправить вручную.", "MutedBrush", 12));
            var box = Ui.Input(120);
            box.Text = average.ToString();
            box.FontSize = 18;
            box.HorizontalAlignment = HorizontalAlignment.Left;
            box.Margin = new Thickness(0, 10, 0, 10);
            root.Children.Add(box);
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            var avg = Ui.Button("СРЕДНЕЕ");
            var dice = Ui.Button("🎲 БРОСИТЬ");
            var ok = Ui.Button("ПОДТВЕРДИТЬ", "DangerBrush");
            avg.Click += (s, e) => box.Text = average.ToString();
            dice.Click += (s, e) => box.Text = roll().ToString();
            ok.Click += (s, e) =>
            {
                if (int.TryParse(box.Text, out int v) && v >= 0) { dlg.Close(); confirmed(v); }
                else MessageBox.Show(owner, "Введите неотрицательное число.", "ПЧ");
            };
            dice.Margin = new Thickness(8, 0, 8, 0);
            row.Children.Add(avg); row.Children.Add(dice); row.Children.Add(ok);
            root.Children.Add(row);
            dlg.Content = root;
            Ui.Present(dlg);
        }

        // подтверждения и запрос потери человечности идут по цепочке: корпус меняется только после последнего ответа
        private void ConvertToBorg(SheetViewModel vm, BorgChassis c, Window owner, Action done)
        {
            bool first = !vm.IsBorg;
            if (!first && vm.Chassis == c.Name)
            {
                MessageBox.Show(owner, "Персонаж уже в этом корпусе.", "ПКТ");
                return;
            }

            var removed = new List<string>();
            if (first)
            {
                foreach (var b in vm.CyberwareBlocks) removed.AddRange(b.InstalledItems.Select(i => i.Name));
                string warn = "Переход в корпус «" + c.FullName + "» необратим: мясное тело будет потеряно.\n" +
                              "Все установленные импланты снимаются и возвращаются" + (removed.Count > 0 ? " (" + removed.Count + " шт.)" : "") + ".\n" +
                              "Стоимость: биосистема 10000eb + корпус " + c.Price + "eb.\n\nПродолжить?";
                if (MessageBox.Show(owner, warn, "ПОЛНАЯ КОНВЕРСИЯ", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            }
            else if (MessageBox.Show(owner, "Сменить корпус на «" + c.FullName + "»?\nИмпланты прежнего корпуса отключатся; за новый корпус — новая потеря человечности.\nСтоимость: " + c.Price + "eb.",
                         "СМЕНА КОРПУСА", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            int avgTotal = c.HlAvg + (first ? Borg.BiosystemHlAvg : 0);
            string what = (first ? "Биосистема: 4d6" + "\n" : "") + "Корпус «" + c.Name + "»: " + c.HlFormula + "\nИтого — потеря человечности.";
            AskBorgHumanityLoss(owner, what, avgTotal, () => Borg.RollKitHl(c) + (first ? Borg.RollBiosystemHl() : 0), loss =>
            {
            if (first)
            {
                foreach (var b in vm.CyberwareBlocks)
                {
                    b.InstalledItems.Clear();
                    b.UsedSlots = 0;
                    if (!b.BorgDoubled && b.Name != null && !b.Name.Contains("BORGWARE") && !b.Name.Contains("FASHIONWARE"))
                    {
                        b.MaxSlots *= 2;
                        b.BorgDoubled = true;
                    }
                }
            }
            vm.Chassis = c.Name;
            vm.RecalculatePenalties();
            vm.CurrentHumanity = Math.Max(0, vm.CurrentHumanity - loss);

            string line = (first ? "[ПКТ] Переход в корпус " : "[ПКТ] Смена корпуса на ") + c.FullName + "; потеря человечности " + loss + "." +
                          (removed.Count > 0 ? " Возвращённые импланты: " + string.Join(", ", removed) + "." : "");
            vm.Notes = string.IsNullOrEmpty(vm.Notes) ? line : vm.Notes + "\n" + line;
            RefreshInstalledCyberware(vm.CyberwareBlocks);
            done();
            });
        }
    }
}
