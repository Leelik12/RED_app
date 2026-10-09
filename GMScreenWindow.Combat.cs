using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    // ГМ-экран: сохранение боя, состояния, генератор столкновений
    public partial class GMScreenWindow
    {
        private static readonly Random Rng = new Random();
        private static string CombatsFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Combats");
        private static string AutosavePath => Path.Combine(CombatsFolder, "autosave.json");

        // бой сохраняется при закрытии терминала — «ЗАГРУЗИТЬ БОЙ» предложит его первым
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            try { if (!e.Cancel && Combatants.Count > 0) WriteCombat(AutosavePath); }
            catch (Exception ex) { ErrorLog.Write("combat-autosave", ex); }
        }

        private void WriteCombat(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, CombatSaves.ToJson(CombatSaves.Capture(Combatants, _roundCount, _currentTurnIndex)));
        }

        private void BtnSaveCombat_Click(object sender, RoutedEventArgs e)
        {
            if (Combatants.Count == 0) { MessageBox.Show("В бою никого нет.", "СОХРАНЕНИЕ", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            Directory.CreateDirectory(CombatsFolder);
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                InitialDirectory = CombatsFolder, Filter = "Бой (*.json)|*.json", FileName = "Бой_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm"), DefaultExt = ".json"
            };
            if (dlg.ShowDialog(this) != true) return;
            try { WriteCombat(dlg.FileName); }
            catch (Exception ex) { ErrorLog.Write("combat-save", ex); MessageBox.Show("Не удалось сохранить бой: " + ex.Message, "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnLoadCombat_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(CombatsFolder);
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                InitialDirectory = CombatsFolder, Filter = "Бой (*.json)|*.json", FileName = File.Exists(AutosavePath) ? "autosave.json" : ""
            };
            if (dlg.ShowDialog(this) != true) return;
            if (Combatants.Count > 0 && MessageBox.Show("Текущий бой будет заменён загруженным. Продолжить?", "ЗАГРУЗКА", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                var data = CombatSaves.FromJson(File.ReadAllText(dlg.FileName));
                Combatants.Clear();
                foreach (var s in data.Combatants) Combatants.Add(CombatSaves.Restore(s));
                _roundCount = Math.Max(1, data.Round);
                _currentTurnIndex = data.Turn >= 0 && data.Turn < Combatants.Count ? data.Turn : -1;
                if (_currentTurnIndex >= 0) Combatants[_currentTurnIndex].IsActiveTurn = true;
                TxtRoundCounter.Text = $"РАУНД: {_roundCount}";
                TxtTimeCounter.Text = $"Прошло времени: {(_roundCount - 1) * 3} сек.";
            }
            catch (Exception ex) { ErrorLog.Write("combat-load", ex); MessageBox.Show("Не удалось загрузить бой: " + ex.Message, "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        // быстрые состояния: выбор из списка добавляет метку, уже стоящая — снимается
        private void BtnAddCondition_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Tag is CombatantViewModel c)) return;
            var menu = new ContextMenu { PlacementTarget = (UIElement)sender, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
            foreach (var preset in CombatSaves.ConditionPresets)
            {
                string p = preset;
                bool on = (c.Conditions ?? "").Split(',').Any(x => x.Trim().Equals(p, StringComparison.OrdinalIgnoreCase));
                var item = new MenuItem { Header = p, IsCheckable = true, IsChecked = on };
                item.Click += (s, a) => c.Conditions = on ? CombatSaves.RemoveCondition(c.Conditions, p) : CombatSaves.AddCondition(c.Conditions, p);
                menu.Items.Add(item);
            }
            var clear = new MenuItem { Header = "Снять все" };
            clear.Click += (s, a) => c.Conditions = "";
            menu.Items.Add(new Separator());
            menu.Items.Add(clear);
            menu.IsOpen = true;
        }

        private void BtnEncounter_Click(object sender, RoutedEventArgs e)
        {
            LoadEnemiesFromFolder();
            var win = Ui.Dialog(this, "ГЕНЕРАТОР СТОЛКНОВЕНИЯ", 720, 700);
            var root = new StackPanel { Margin = new Thickness(14) };
            root.Children.Add(Ui.Text("ГЕНЕРАТОР СТОЛКНОВЕНИЯ", "DangerBrush", 16, true));
            root.Children.Add(Ui.Text("Пропорции из DLC «Прожжённые»: шестёрка на эджраннера, лейтенант на двоих, мини-босс на троих. Противники берутся из базы (включая подключённые DLC); это помощник, а не правило книги.", "MutedBrush", 12));

            var form = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 6) };
            var players = Ui.Input(44);
            players.Text = Math.Max(1, Combatants.Count(c => c.IsPlayer)).ToString();
            players.HorizontalContentAlignment = HorizontalAlignment.Center;
            var diff = new ComboBox { Width = 120, Margin = new Thickness(10, 0, 0, 0) };
            foreach (var d in Encounters.Difficulties) diff.Items.Add(d);
            diff.SelectedIndex = 1;
            var boss = new CheckBox { Content = "с боссом", Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            boss.SetResourceReference(Control.ForegroundProperty, "TextBrush");
            form.Children.Add(Ui.Text("Эджраннеров: ", "TextBrush", 13)); form.Children.Add(players);
            form.Children.Add(Ui.Text("   Сложность: ", "TextBrush", 13)); form.Children.Add(diff); form.Children.Add(boss);
            root.Children.Add(form);

            var bar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            var gen = Ui.Button("🎲 ПОДОБРАТЬ");
            var toTracker = Ui.Button("В ТРЕКЕР БОЯ", "WarnBrush", false); toTracker.Margin = new Thickness(8, 0, 0, 0);
            var lootBtn = Ui.Button("🎁 ДОБЫЧА", "MutedBrush", false); lootBtn.Margin = new Thickness(8, 0, 0, 0);
            bar.Children.Add(gen); bar.Children.Add(toTracker); bar.Children.Add(lootBtn);
            root.Children.Add(bar);

            var list = new StackPanel();
            root.Children.Add(list);
            var lootBox = Ui.Text("", "TextBrush", 13);
            lootBox.Margin = new Thickness(0, 10, 0, 0);
            root.Children.Add(lootBox);

            List<EncounterSlot> current = null;
            Action<bool> setActions = on => { toTracker.IsEnabled = on; toTracker.Opacity = on ? 1 : 0.4; lootBtn.IsEnabled = on; lootBtn.Opacity = on ? 1 : 0.4; };
            gen.Click += (s, a) =>
            {
                int n = int.TryParse(players.Text, out int v) ? Math.Max(1, Math.Min(12, v)) : 4;
                current = Encounters.Generate(AvailableEnemies.ToList(), n, Math.Max(0, diff.SelectedIndex), Rng, boss.IsChecked == true);
                list.Children.Clear(); lootBox.Text = "";
                if (current.Count == 0) { list.Children.Add(Ui.Text("В базе нет противников.", "WarnBrush", 13)); setActions(false); return; }
                foreach (var slot in current)
                {
                    var row = Ui.Text($"{slot.Count} × {slot.Enemy.Name}  · {slot.Tier}  · ПЗ {slot.Enemy.HP}, ОС {slot.Enemy.HeadArmor}/{slot.Enemy.BodyArmor}" + (string.IsNullOrEmpty(slot.Enemy.Source) ? "" : "  [" + slot.Enemy.Source + "]"), "AccentBrush", 14, true);
                    row.Margin = new Thickness(0, 2, 0, 2);
                    list.Children.Add(row);
                }
                list.Children.Add(Ui.Text("Всего бойцов: " + Encounters.TotalCount(current), "MutedBrush", 12));
                setActions(true);
            };
            toTracker.Click += (s, a) =>
            {
                if (current == null) return;
                foreach (var slot in current) SpawnEnemy(slot.Enemy, slot.Count);
                toTracker.IsEnabled = false; toTracker.Opacity = 0.4;
            };
            lootBtn.Click += (s, a) => { if (current != null) lootBox.Text = "ДОБЫЧА\n" + string.Join("\n", Encounters.Loot(current, Rng)); };

            win.Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            win.Show();
        }
    }
}
