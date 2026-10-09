using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CyberpunkRED_Generator
{
    // трата очков улучшения (I.P.) по правилам книги, стр. 408–409
    public partial class CharacterSheetWindow
    {
        public const int ImproveMaxLevel = 10;
        public const int ImproveSkillStep = 20;   // обычный навык: 20 × новый уровень
        public const int ImproveRoleStep = 60;    // ранг роли: 60 × новый ранг (в 3 раза дороже навыка)

        // стоимость повышения навыка до уровня currentLevel + 1
        public static int SkillImproveCost(int currentLevel, bool isX2)
            => (currentLevel + 1) * ImproveSkillStep * (isX2 ? 2 : 1);

        // стоимость повышения ранга роли до currentRank + 1 (новая роль берётся с ранга 0 → 1)
        public static int RoleImproveCost(int currentRank) => (currentRank + 1) * ImproveRoleStep;

        private class ImprovePurchase
        {
            public string Description;
            public int Cost;
            public Action Revert;
        }

        private void BtnImprove_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is SheetViewModel vm)) return;
            CloseOverlays();
            ShowImprovementDialog(vm);
        }

        private static TextBlock MakeText(string text, Brush color, double size = 14, FontWeight? weight = null)
            => new TextBlock { Text = text, Foreground = color, FontSize = size, FontWeight = weight ?? FontWeights.Normal, VerticalAlignment = VerticalAlignment.Center };

        private static Button MakeButton(string text, Brush border, bool enabled = true)
        {
            var b = new Button
            {
                Content = text,
                Foreground = border,
                Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)),
                BorderBrush = border,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 4, 10, 4),
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                IsEnabled = enabled,
                Opacity = enabled ? 1 : 0.4
            };
            return b;
        }

        private void ShowImprovementDialog(SheetViewModel vm)
        {
            var cyan = Theme.Accent();
            var red = Theme.Danger();
            var gray = new SolidColorBrush(Color.FromRgb(153, 153, 153));
            var white = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            var yellow = new SolidColorBrush(Color.FromRgb(255, 204, 0));

            var win = new Window
            {
                Title = "ПРОКАЧКА ПЕРСОНАЖА",
                Owner = this,
                Width = 760,
                Height = 820,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = new SolidColorBrush(Color.FromRgb(5, 5, 5)),
                FontFamily = new FontFamily("Consolas")
            };
            win.Resources = this.Resources;

            var purchases = new List<ImprovePurchase>();

            var root = new Grid { Margin = new Thickness(14) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            var ipText = MakeText("", yellow, 20, FontWeights.Bold);
            header.Children.Add(ipText);
            Grid.SetRow(header, 0);

            var searchPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            searchPanel.Children.Add(MakeText("ПОИСК НАВЫКА: ", red, 13, FontWeights.Bold));
            var search = new TextBox { Width = 260, Background = Brushes.Black, Foreground = cyan, BorderBrush = gray, CaretBrush = cyan, Padding = new Thickness(3) };
            searchPanel.Children.Add(search);
            var hint = MakeText("   Правило: уровни не пропускаются, максимум 10", gray, 12);
            searchPanel.Children.Add(hint);
            Grid.SetRow(searchPanel, 1);

            var list = new StackPanel();
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = list };
            Grid.SetRow(scroll, 2);

            var footer = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var logText = new TextBlock { Foreground = gray, FontSize = 12, TextWrapping = TextWrapping.Wrap, MaxHeight = 90 };
            var btnRefund = MakeButton("ОТМЕНИТЬ ПОСЛЕДНЮЮ", yellow, false);
            btnRefund.Margin = new Thickness(8, 0, 8, 0);
            var btnClose = MakeButton("ЗАКРЫТЬ", red);
            Grid.SetColumn(btnRefund, 1); Grid.SetColumn(btnClose, 2);
            footer.Children.Add(logText); footer.Children.Add(btnRefund); footer.Children.Add(btnClose);
            Grid.SetRow(footer, 3);

            root.Children.Add(header); root.Children.Add(searchPanel); root.Children.Add(scroll); root.Children.Add(footer);
            win.Content = root;

            Action refresh = null;

            // списывает очки, применяет изменение и возвращает запись о покупке (для отмены); null — не хватило очков
            Func<string, int, Action, ImprovePurchase> spend = (desc, cost, apply) =>
            {
                if (vm.ImprovementPoints < cost) return null;
                PushImproveSnapshot();
                vm.ImprovementPoints -= cost;
                apply();
                var purchase = new ImprovePurchase { Description = desc, Cost = cost };
                purchases.Add(purchase);
                AppendImproveLog($"{DateTime.Now:dd.MM.yyyy HH:mm}  {desc}  (−{cost} ОУ)");
                return purchase;
            };

            btnRefund.Click += (s, e) =>
            {
                if (purchases.Count == 0) return;
                var last = purchases[purchases.Count - 1];
                purchases.RemoveAt(purchases.Count - 1);
                last.Revert?.Invoke();
                vm.ImprovementPoints += last.Cost;
                AppendImproveLog($"{DateTime.Now:dd.MM.yyyy HH:mm}  Отмена: {last.Description}  (+{last.Cost} ОУ)");
                refresh();
            };
            btnClose.Click += (s, e) => win.Close();

            Func<SheetSkill, string> skillTitle = sk => sk.IsVariant && !string.IsNullOrWhiteSpace(sk.SubName) ? $"{sk.BaseName}: {sk.SubName.Trim()}" : sk.Name;

            Action rebuild = null;
            refresh = () =>
            {
                double offset = scroll.VerticalOffset;
                rebuild();
                scroll.UpdateLayout();
                scroll.ScrollToVerticalOffset(offset);
            };

            rebuild = () =>
            {
                ipText.Text = $"ОЧКОВ УЛУЧШЕНИЯ: {vm.ImprovementPoints}";
                list.Children.Clear();

                // роли
                list.Children.Add(MakeText("РОЛИ (ранг × 60)", red, 15, FontWeights.Bold));
                foreach (var role in vm.ActiveRoles.ToList())
                {
                    var roleRef = role;
                    var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    row.Children.Add(MakeText($"{role.Name}: ранг {role.Rank}", white, 14, FontWeights.Bold));
                    int cost = RoleImproveCost(role.Rank);
                    bool can = role.Rank < ImproveMaxLevel && vm.ImprovementPoints >= cost;
                    string label = role.Rank >= ImproveMaxLevel ? "МАКСИМУМ" : $"→ {role.Rank + 1}   {cost} ОУ";
                    var btn = MakeButton(label, cyan, can);
                    Grid.SetColumn(btn, 1);
                    btn.Click += (s, e) =>
                    {
                        int before = roleRef.Rank;
                        var p = spend($"Роль {roleRef.Name}: ранг {before} → {before + 1}", cost, () => { SetRoleRank(vm, roleRef, before + 1); });
                        if (p != null) p.Revert = () => SetRoleRank(vm, roleRef, before);
                        refresh();
                    };
                    row.Children.Add(btn);
                    list.Children.Add(row);
                }

                // новая роль
                var lastRole = vm.ActiveRoles.LastOrDefault();
                var free = CoreDataBase.Roles.Where(r => !vm.ActiveRoles.Any(ar => ar.Name == r)).ToList();
                var newRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 10) };
                if (free.Count > 0)
                {
                    bool unlocked = lastRole == null || lastRole.Rank >= 4;
                    var combo = new ComboBox { ItemsSource = free, SelectedIndex = 0, Width = 180, Margin = new Thickness(0, 0, 8, 0), IsEnabled = unlocked };
                    int cost = RoleImproveCost(0);
                    var btn = MakeButton($"ВЗЯТЬ НОВУЮ РОЛЬ  {cost} ОУ", cyan, unlocked && vm.ImprovementPoints >= cost);
                    btn.Click += (s, e) =>
                    {
                        string name = combo.SelectedItem as string;
                        if (name == null) return;
                        RoleData added = null;
                        var p = spend($"Новая роль {name} (ранг 1)", cost, () =>
                        {
                            added = new RoleData { Name = name, Rank = 1 };
                            vm.ActiveRoles.Add(added);
                            vm.ChangeRoleIndex(vm.ActiveRoles.Count - 1);
                            vm.RecalculatePenalties();
                        });
                        if (p != null) p.Revert = () =>
                        {
                            if (added != null && vm.ActiveRoles.Contains(added))
                            {
                                vm.ActiveRoles.Remove(added);
                                vm.ChangeRoleIndex(0);
                                vm.RecalculatePenalties();
                            }
                        };
                        refresh();
                    };
                    newRow.Children.Add(combo);
                    newRow.Children.Add(btn);
                    if (!unlocked) newRow.Children.Add(MakeText($"   нужен ранг 4 в роли {lastRole.Name} (сейчас {lastRole.Rank})", gray, 12));
                }
                list.Children.Add(newRow);

                // навыки
                list.Children.Add(MakeText("НАВЫКИ (новый уровень × 20, сложные x2 — × 40)", red, 15, FontWeights.Bold));
                string filter = (search.Text ?? "").Trim();
                var allSkills = vm.CenterSkillCategories.Concat(vm.RightSkillCategories1).Concat(vm.RightSkillCategories2)
                    .SelectMany(c => c.Skills.Select(sk => new { Cat = c, Skill = sk }));

                string lastCat = null;
                foreach (var item in allSkills)
                {
                    var sk = item.Skill;
                    if (sk.CanAddMultiple && !sk.IsVariant) continue;                    // заготовка «+», не сам навык
                    if (sk.IsVariant && string.IsNullOrWhiteSpace(sk.SubName)) continue; // безымянный вариант
                    string title = skillTitle(sk);
                    if (filter.Length > 0 && title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;

                    if (lastCat != item.Cat.Name)
                    {
                        lastCat = item.Cat.Name;
                        var catText = MakeText(item.Cat.Name, gray, 12, FontWeights.Bold);
                        catText.Margin = new Thickness(0, 8, 0, 2);
                        list.Children.Add(catText);
                    }

                    var skillRef = sk;
                    var def = CoreDataBase.AllSkills.FirstOrDefault(d => d.Name == sk.BaseName);
                    bool x2 = def != null && def.IsX2;
                    int cost = SkillImproveCost(sk.Level, x2);
                    bool can = sk.Level < ImproveMaxLevel && vm.ImprovementPoints >= cost;

                    var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
                    row.Children.Add(MakeText(title, white, 14));
                    var lvl = MakeText($"ур. {sk.Level}", cyan, 14, FontWeights.Bold);
                    Grid.SetColumn(lvl, 1); row.Children.Add(lvl);
                    string label = sk.Level >= ImproveMaxLevel ? "МАКСИМУМ" : $"→ {sk.Level + 1}   {cost} ОУ";
                    var btn = MakeButton(label, cyan, can);
                    btn.HorizontalAlignment = HorizontalAlignment.Right;
                    Grid.SetColumn(btn, 2);
                    btn.Click += (s, e) =>
                    {
                        int before = skillRef.Level;
                        var p = spend($"Навык {skillTitle(skillRef)}: {before} → {before + 1}", cost, () => { skillRef.Level = before + 1; vm.RecalculatePenalties(); });
                        if (p != null) p.Revert = () => { skillRef.Level = before; vm.RecalculatePenalties(); };
                        refresh();
                    };
                    row.Children.Add(btn);
                    list.Children.Add(row);
                }

                var log = _originalData?.ImprovementLog ?? new List<string>();
                logText.Text = log.Count == 0 ? "Журнал трат пуст." : "ЖУРНАЛ:\n" + string.Join("\n", log.Skip(Math.Max(0, log.Count - 4)));
                btnRefund.IsEnabled = purchases.Count > 0;
                btnRefund.Opacity = purchases.Count > 0 ? 1 : 0.4;
            };

            // поиск: перерисовываем список с начала, фокус остаётся в поле
            search.TextChanged += (s, e) => { rebuild(); scroll.ScrollToTop(); };

            refresh();
            win.ShowDialog();
            QueueCommit();
        }

        private void SetRoleRank(SheetViewModel vm, RoleData role, int rank)
        {
            role.Rank = rank;
            int idx = vm.ActiveRoles.IndexOf(vm.CurrentActiveRole);
            vm.ChangeRoleIndex(idx < 0 ? 0 : idx);   // обновляет тексты способностей и счётчики очков
            vm.RecalculatePenalties();
        }

        private void PushImproveSnapshot() => CommitChange();

        private void AppendImproveLog(string line)
        {
            if (_originalData == null) return;
            if (_originalData.ImprovementLog == null) _originalData.ImprovementLog = new List<string>();
            _originalData.ImprovementLog.Add(line);
            if (_originalData.ImprovementLog.Count > 200) _originalData.ImprovementLog.RemoveAt(0);
        }
    }
}
