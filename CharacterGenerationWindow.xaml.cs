using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberpunkRED_Generator
{
    // ==========================================
    // ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ ДЛЯ НАВЫКОВ
    // ==========================================
    public class SkillRow : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string StatName { get; set; }
        public bool IsBasic { get; set; }
        public bool IsX2 { get; set; }

        private int _baseStatValue = 5; // Позже можно связать с результатами из Вкладки 1

        private int _level;
        public int Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(); OnPropertyChanged(nameof(Total)); }
        }

        public int Total => _baseStatValue + Level;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class SkillCategory
    {
        public string CategoryName { get; set; }
        public List<SkillRow> Skills { get; set; }
    }

    // ==========================================
    // ГЛАВНЫЙ КЛАСС ОКНА
    // ==========================================
    public partial class CharacterGenerationWindow : Window
    {
        private Random _rnd = new Random();
        private bool _isLoaded = false;

        // Переменные для Ролевого Пути
        private List<ComboBox> _dynamicRoleComboBoxes = new List<ComboBox>();

        // Переменные для Навыков
        private List<SkillCategory> _categories;
        private const int MAX_SKILL_POINTS = 86;

        public CharacterGenerationWindow()
        {
            InitializeComponent();

            // Инициализация ЭТАП 1: Статы
            CalculateDerivedStats();

            // Инициализация ЭТАП 2: Жизненный путь
            LoadLifepathData();

            // Инициализация ЭТАП 3: Ролевой путь
            LoadRoleLifepathData();

            // Инициализация ЭТАП 4: Связи
            LoadRelationsData();

            // Инициализация ЭТАП 5: Навыки
            LoadSkillsData();

            _isLoaded = true;

            // ИСПРАВЛЕНИЕ: Принудительно отрисовываем стартовые значения для всех вкладок
            UpdateLifepathLog(null, null);

            if (CbRole.SelectedItem != null)
            {
                GenerateQuestions(CbRole.SelectedItem.ToString());
            }

            UpdateRelationsLog(null, null);
        }

        // ==========================================
        // НАВИГАЦИЯ ПО ВКЛАДКАМ (МАСТЕР)
        // ==========================================
        private void BtnNextStep_Click(object sender, RoutedEventArgs e)
        {
            if (WizardTabControl.SelectedIndex < WizardTabControl.Items.Count - 1)
            {
                WizardTabControl.SelectedIndex++;
                UpdateProgressIndicator();
            }
        }

        private void BtnPrevStep_Click(object sender, RoutedEventArgs e)
        {
            if (WizardTabControl.SelectedIndex > 0)
            {
                WizardTabControl.SelectedIndex--;
                UpdateProgressIndicator();
            }
        }

        private void UpdateProgressIndicator()
        {
            if (TxtProgress == null) return;
            switch (WizardTabControl.SelectedIndex)
            {
                case 0: TxtProgress.Text = "ЭТАП 1: ХАРАКТЕРИСТИКИ"; break;
                case 1: TxtProgress.Text = "ЭТАП 2: ЖИЗНЕННЫЙ ПУТЬ"; break;
                case 2: TxtProgress.Text = "ЭТАП 3: РОЛЕВОЙ ПУТЬ"; break;
                case 3: TxtProgress.Text = "ЭТАП 4: СВЯЗИ"; break;
                case 4: TxtProgress.Text = "ЭТАП 5: РОЛЬ И НАВЫКИ"; break;
            }
        }

        private void BtnFinish_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Персонаж успешно сгенерирован!", "ТЕРМИНАЛ CYBERPUNK RED", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        // ==========================================
        // ЭТАП 1: ЛОГИКА ХАРАКТЕРИСТИК (СТАТОВ)
        // ==========================================
        private void ModeChanged(object sender, RoutedEventArgs e)
        {
            if (PanelCalc == null || PanelRandom == null) return;
            bool isManual = RbCalc.IsChecked == true;
            PanelCalc.Visibility = isManual ? Visibility.Visible : Visibility.Collapsed;
            PanelRandom.Visibility = isManual ? Visibility.Collapsed : Visibility.Visible;
        }

        private void CbRank_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePointsLeft();
        }

        private void StatBtnMinus_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag != null)
            {
                TextBlock tb = FindName(btn.Tag.ToString()) as TextBlock;
                if (tb != null) AdjustStat(tb, -1);
            }
        }

        private void StatBtnPlus_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag != null)
            {
                TextBlock tb = FindName(btn.Tag.ToString()) as TextBlock;
                if (tb != null) AdjustStat(tb, 1);
            }
        }

        private void AdjustStat(TextBlock tb, int delta)
        {
            if (int.TryParse(tb.Text, out int val))
            {
                int newVal = val + delta;
                if (newVal >= 2 && newVal <= 8)
                {
                    if (delta > 0 && RbCalc.IsChecked == true)
                    {
                        int maxPoints = GetMaxPoints();
                        int currentPoints = GetTotalPointsSpent();
                        if (currentPoints >= maxPoints) return;
                    }
                    tb.Text = newVal.ToString();
                    UpdatePointsLeft();
                    CalculateDerivedStats();
                }
            }
        }

        private void BtnRollStats_Click(object sender, RoutedEventArgs e)
        {
            TxtInt.Text = RollStat().ToString(); TxtRef.Text = RollStat().ToString();
            TxtDex.Text = RollStat().ToString(); TxtTech.Text = RollStat().ToString();
            TxtCool.Text = RollStat().ToString(); TxtWill.Text = RollStat().ToString();
            TxtLuck.Text = RollStat().ToString(); TxtMove.Text = RollStat().ToString();
            TxtBody.Text = RollStat().ToString(); TxtEmp.Text = RollStat().ToString();
            UpdatePointsLeft();
            CalculateDerivedStats();
        }

        private int RollStat()
        {
            List<int> rolls = new List<int> { _rnd.Next(1, 5), _rnd.Next(1, 5), _rnd.Next(1, 5) };
            return rolls.OrderByDescending(x => x).Take(2).Sum();
        }

        private int GetStat(TextBlock tb)
        {
            if (tb == null) return 2;
            return int.TryParse(tb.Text, out int result) ? result : 2;
        }

        private int GetTotalPointsSpent()
        {
            return GetStat(TxtInt) + GetStat(TxtRef) + GetStat(TxtDex) + GetStat(TxtTech) +
                   GetStat(TxtCool) + GetStat(TxtWill) + GetStat(TxtLuck) + GetStat(TxtMove) +
                   GetStat(TxtBody) + GetStat(TxtEmp);
        }

        private int GetMaxPoints()
        {
            if (CbRank?.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out int max)) return max;
            return 62;
        }

        private void UpdatePointsLeft()
        {
            int currentPoints = GetTotalPointsSpent();
            int left = GetMaxPoints() - currentPoints;
            if (TxtPointsLeft != null) TxtPointsLeft.Text = left.ToString();
            if (TxtTotalStats != null) TxtTotalStats.Text = currentPoints.ToString();
        }

        private void CalculateDerivedStats()
        {
            if (TxtHp == null) return;
            int body = GetStat(TxtBody);
            int will = GetStat(TxtWill);
            int emp = GetStat(TxtEmp);

            int hp = 10 + (5 * (int)Math.Ceiling((body + will) / 2.0));
            int wounded = (int)Math.Ceiling(hp / 2.0);
            int humanity = emp * 10;

            TxtHp.Text = hp.ToString();
            TxtWounded.Text = wounded.ToString();
            TxtDeathSave.Text = body.ToString();
            TxtHumanity.Text = humanity.ToString();
        }

        // ==========================================
        // ЭТАП 2: ЛОГИКА ЖИЗНЕННОГО ПУТИ
        // ==========================================
        private void LoadLifepathData()
        {
            CbOrigin.ItemsSource = CoreDataBase.Origins; CbPersonality.ItemsSource = CoreDataBase.Personalities;
            CbClothing.ItemsSource = CoreDataBase.Clothing; CbHair.ItemsSource = CoreDataBase.Hair;
            CbValue.ItemsSource = CoreDataBase.Values; CbPeople.ItemsSource = CoreDataBase.People;
            CbPerson.ItemsSource = CoreDataBase.Persons; CbPossession.ItemsSource = CoreDataBase.Possessions;
            CbFamilyBackground.ItemsSource = CoreDataBase.FamilyBackground; CbChildhood.ItemsSource = CoreDataBase.ChildhoodEnvironment;
            CbParents.ItemsSource = CoreDataBase.Parents; CbFamilyCrisis.ItemsSource = CoreDataBase.FamilyCrisis;
            CbLifeGoal.ItemsSource = CoreDataBase.LifeGoals;

            CbOrigin.SelectedIndex = 0; CbPersonality.SelectedIndex = 0; CbClothing.SelectedIndex = 0; CbHair.SelectedIndex = 0;
            CbValue.SelectedIndex = 0; CbPeople.SelectedIndex = 0; CbPerson.SelectedIndex = 0; CbPossession.SelectedIndex = 0;
            CbFamilyBackground.SelectedIndex = 0; CbChildhood.SelectedIndex = 0; CbParents.SelectedIndex = 0;
            CbFamilyCrisis.SelectedIndex = 0; CbLifeGoal.SelectedIndex = 0;
        }

        private void BtnLifepathRandom_Click(object sender, RoutedEventArgs e)
        {
            CbOrigin.SelectedIndex = _rnd.Next(0, 10); CbPersonality.SelectedIndex = _rnd.Next(0, 10);
            CbClothing.SelectedIndex = _rnd.Next(0, 10); CbHair.SelectedIndex = _rnd.Next(0, 10);
            CbValue.SelectedIndex = _rnd.Next(0, 10); CbPeople.SelectedIndex = _rnd.Next(0, 10);
            CbPerson.SelectedIndex = _rnd.Next(0, 10); CbPossession.SelectedIndex = _rnd.Next(0, 10);
            CbFamilyBackground.SelectedIndex = _rnd.Next(0, 10); CbChildhood.SelectedIndex = _rnd.Next(0, 10);
            CbParents.SelectedIndex = _rnd.Next(0, 10); CbFamilyCrisis.SelectedIndex = _rnd.Next(0, 10);
            CbLifeGoal.SelectedIndex = _rnd.Next(0, 10);
        }

        private void UpdateLifepathLog(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || TxtLifepathLog == null || CbOrigin.SelectedItem == null || CbLifeGoal.SelectedItem == null || CbFamilyBackground.SelectedIndex < 0) return;

            string familyDesc = CoreDataBase.FamilyBackgroundDesc[CbFamilyBackground.SelectedIndex];
            TxtLifepathLog.Text = $"> АНАЛИЗ БИОМЕТРИИ И ИСТОРИИ...\n\n" +
                                  $"> ПРОИСХОЖДЕНИЕ И СЕМЬЯ:\n" +
                                  $"  - Корни: {CbOrigin.SelectedItem}\n" +
                                  $"  - Семья: {CbFamilyBackground.SelectedItem}\n" +
                                  $"    [БД Корпо]: {familyDesc}\n" +
                                  $"  - Родители: {CbParents.SelectedItem}\n" +
                                  $"  - Детство: {CbChildhood.SelectedItem}\n" +
                                  $"  - Трагедия: {CbFamilyCrisis.SelectedItem}\n\n" +
                                  $"> ХАРАКТЕР: {CbPersonality.SelectedItem}\n\n" +
                                  $"> ВНЕШНИЙ ВИД:\n" +
                                  $"  - Стиль одежды: {CbClothing.SelectedItem}\n" +
                                  $"  - Прическа: {CbHair.SelectedItem}\n\n" +
                                  $"> ПСИХОЛОГИЧЕСКИЙ ПРОФИЛЬ:\n" +
                                  $"  - Отношение к людям: {CbPeople.SelectedItem}\n" +
                                  $"  - Ценность: {CbValue.SelectedItem}\n" +
                                  $"  - Важный человек: {CbPerson.SelectedItem}\n" +
                                  $"  - Важная вещь: {CbPossession.SelectedItem}\n\n" +
                                  $"> ЦЕЛЬ В ЖИЗНИ: {CbLifeGoal.SelectedItem}\n\n" +
                                  $"> СТАТУС: ФОРМИРОВАНИЕ ЗАВЕРШЕНО.";
        }

        // ==========================================
        // ЭТАП 3: ЛОГИКА РОЛЕВОГО ПУТИ
        // ==========================================
        private void LoadRoleLifepathData()
        {
            CbRole.ItemsSource = CoreDataBase.Roles;
            CbRole.SelectedIndex = 0;
        }

        private void CbRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || CbRole.SelectedItem == null) return;
            GenerateQuestions(CbRole.SelectedItem.ToString());
        }

        private void GenerateQuestions(string role)
        {
            PanelQuestions.Children.Clear();
            _dynamicRoleComboBoxes.Clear();

            if (!CoreDataBase.RoleLifepaths.ContainsKey(role)) return;

            foreach (var table in CoreDataBase.RoleLifepaths[role])
            {
                TextBlock tb = new TextBlock
                {
                    Text = table.Title,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999")),
                    Margin = new Thickness(0, 0, 0, 5)
                };

                ComboBox cb = new ComboBox
                {
                    ItemsSource = table.Options,
                    SelectedIndex = 0,
                    Tag = table.Title
                };

                cb.SelectionChanged += (s, e) => UpdateRoleLog();
                _dynamicRoleComboBoxes.Add(cb);
                PanelQuestions.Children.Add(tb);
                PanelQuestions.Children.Add(cb);
            }
            UpdateRoleLog();
        }

        private void BtnRoleRandom_Click(object sender, RoutedEventArgs e)
        {
            foreach (var cb in _dynamicRoleComboBoxes)
                if (cb.Items.Count > 0) cb.SelectedIndex = _rnd.Next(0, cb.Items.Count);
        }

        private void UpdateRoleLog()
        {
            if (!_isLoaded || TxtRoleLog == null || CbRole.SelectedItem == null) return;

            StringBuilder log = new StringBuilder();
            log.AppendLine($"> АНАЛИЗ КАРЬЕРЫ: {CbRole.SelectedItem.ToString().ToUpper()}\n");

            foreach (var cb in _dynamicRoleComboBoxes)
            {
                if (cb.SelectedItem != null)
                {
                    log.AppendLine($"  - {cb.Tag}:\n    [{cb.SelectedItem}]\n");
                }
            }
            log.AppendLine("> СТАТУС: ПРОФИЛЬ УТВЕРЖДЕН.");
            TxtRoleLog.Text = log.ToString();
        }

        // ==========================================
        // ЭТАП 4: ЛОГИКА СВЯЗЕЙ
        // ==========================================
        private void LoadRelationsData()
        {
            CbF1.ItemsSource = CoreDataBase.Friends; CbF2.ItemsSource = CoreDataBase.Friends; CbF3.ItemsSource = CoreDataBase.Friends;
            CbE1Who.ItemsSource = CoreDataBase.EnemiesWho; CbE1Cause.ItemsSource = CoreDataBase.EnemiesCause; CbE1Hate.ItemsSource = CoreDataBase.EnemiesWhoHates; CbE1Action.ItemsSource = CoreDataBase.EnemiesAction;
            CbE2Who.ItemsSource = CoreDataBase.EnemiesWho; CbE2Cause.ItemsSource = CoreDataBase.EnemiesCause; CbE2Hate.ItemsSource = CoreDataBase.EnemiesWhoHates; CbE2Action.ItemsSource = CoreDataBase.EnemiesAction;
            CbE3Who.ItemsSource = CoreDataBase.EnemiesWho; CbE3Cause.ItemsSource = CoreDataBase.EnemiesCause; CbE3Hate.ItemsSource = CoreDataBase.EnemiesWhoHates; CbE3Action.ItemsSource = CoreDataBase.EnemiesAction;
            CbL1.ItemsSource = CoreDataBase.TragicLove; CbL2.ItemsSource = CoreDataBase.TragicLove; CbL3.ItemsSource = CoreDataBase.TragicLove;

            CbF1.SelectedIndex = 0; CbF2.SelectedIndex = 0; CbF3.SelectedIndex = 0;
            CbE1Who.SelectedIndex = 0; CbE1Cause.SelectedIndex = 0; CbE1Hate.SelectedIndex = 0; CbE1Action.SelectedIndex = 0;
            CbE2Who.SelectedIndex = 0; CbE2Cause.SelectedIndex = 0; CbE2Hate.SelectedIndex = 0; CbE2Action.SelectedIndex = 0;
            CbE3Who.SelectedIndex = 0; CbE3Cause.SelectedIndex = 0; CbE3Hate.SelectedIndex = 0; CbE3Action.SelectedIndex = 0;
            CbL1.SelectedIndex = 0; CbL2.SelectedIndex = 0; CbL3.SelectedIndex = 0;

            CbCountFriends.SelectedIndex = 0; CbCountEnemies.SelectedIndex = 0; CbCountLove.SelectedIndex = 0;
        }

        private void CountChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;

            int friends = CbCountFriends.SelectedIndex;
            PanelFriends.Visibility = friends > 0 ? Visibility.Visible : Visibility.Collapsed;
            SlotF1.Visibility = friends >= 1 ? Visibility.Visible : Visibility.Collapsed;
            SlotF2.Visibility = friends >= 2 ? Visibility.Visible : Visibility.Collapsed;
            SlotF3.Visibility = friends >= 3 ? Visibility.Visible : Visibility.Collapsed;

            int enemies = CbCountEnemies.SelectedIndex;
            PanelEnemies.Visibility = enemies > 0 ? Visibility.Visible : Visibility.Collapsed;
            SlotE1.Visibility = enemies >= 1 ? Visibility.Visible : Visibility.Collapsed;
            SlotE2.Visibility = enemies >= 2 ? Visibility.Visible : Visibility.Collapsed;
            SlotE3.Visibility = enemies >= 3 ? Visibility.Visible : Visibility.Collapsed;

            int love = CbCountLove.SelectedIndex;
            PanelLove.Visibility = love > 0 ? Visibility.Visible : Visibility.Collapsed;
            SlotL1.Visibility = love >= 1 ? Visibility.Visible : Visibility.Collapsed;
            SlotL2.Visibility = love >= 2 ? Visibility.Visible : Visibility.Collapsed;
            SlotL3.Visibility = love >= 3 ? Visibility.Visible : Visibility.Collapsed;

            UpdateRelationsLog(null, null);
        }

        private void BtnRelationsRandom_Click(object sender, RoutedEventArgs e)
        {
            CbCountFriends.SelectedIndex = Math.Max(0, _rnd.Next(1, 11) - 7);
            CbCountEnemies.SelectedIndex = Math.Max(0, _rnd.Next(1, 11) - 7);
            CbCountLove.SelectedIndex = Math.Max(0, _rnd.Next(1, 11) - 7);

            CbF1.SelectedIndex = _rnd.Next(0, 10); CbF2.SelectedIndex = _rnd.Next(0, 10); CbF3.SelectedIndex = _rnd.Next(0, 10);
            CbE1Who.SelectedIndex = _rnd.Next(0, 10); CbE1Cause.SelectedIndex = _rnd.Next(0, 10); CbE1Hate.SelectedIndex = _rnd.Next(0, 10); CbE1Action.SelectedIndex = _rnd.Next(0, 10);
            CbE2Who.SelectedIndex = _rnd.Next(0, 10); CbE2Cause.SelectedIndex = _rnd.Next(0, 10); CbE2Hate.SelectedIndex = _rnd.Next(0, 10); CbE2Action.SelectedIndex = _rnd.Next(0, 10);
            CbE3Who.SelectedIndex = _rnd.Next(0, 10); CbE3Cause.SelectedIndex = _rnd.Next(0, 10); CbE3Hate.SelectedIndex = _rnd.Next(0, 10); CbE3Action.SelectedIndex = _rnd.Next(0, 10);
            CbL1.SelectedIndex = _rnd.Next(0, 10); CbL2.SelectedIndex = _rnd.Next(0, 10); CbL3.SelectedIndex = _rnd.Next(0, 10);
        }

        private void UpdateRelationsLog(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || TxtRelationsLog == null) return;

            StringBuilder log = new StringBuilder();
            log.AppendLine("> АНАЛИЗ СОЦИАЛЬНЫХ СВЯЗЕЙ...\n");

            int friends = CbCountFriends.SelectedIndex;
            log.AppendLine($"> ДРУЗЬЯ (Найдено: {friends}):");
            if (friends == 0) log.AppendLine("  - Отсутствуют.");
            if (friends >= 1) log.AppendLine($"  1. {CbF1.SelectedItem}");
            if (friends >= 2) log.AppendLine($"  2. {CbF2.SelectedItem}");
            if (friends >= 3) log.AppendLine($"  3. {CbF3.SelectedItem}");
            log.AppendLine();

            int enemies = CbCountEnemies.SelectedIndex;
            log.AppendLine($"> ВРАГИ (Найдено: {enemies}):");
            if (enemies == 0) log.AppendLine("  - Отсутствуют.");
            if (enemies >= 1) log.AppendLine($"  1. {CbE1Who.SelectedItem}. Причина: {CbE1Cause.SelectedItem}. {CbE1Hate.SelectedItem}. {CbE1Action.SelectedItem}.");
            if (enemies >= 2) log.AppendLine($"  2. {CbE2Who.SelectedItem}. Причина: {CbE2Cause.SelectedItem}. {CbE2Hate.SelectedItem}. {CbE2Action.SelectedItem}.");
            if (enemies >= 3) log.AppendLine($"  3. {CbE3Who.SelectedItem}. Причина: {CbE3Cause.SelectedItem}. {CbE3Hate.SelectedItem}. {CbE3Action.SelectedItem}.");
            log.AppendLine();

            int love = CbCountLove.SelectedIndex;
            log.AppendLine($"> ТРАГИЧЕСКИЕ РОМАНЫ (Найдено: {love}):");
            if (love == 0) log.AppendLine("  - Отсутствуют.");
            if (love >= 1) log.AppendLine($"  1. {CbL1.SelectedItem}");
            if (love >= 2) log.AppendLine($"  2. {CbL2.SelectedItem}");
            if (love >= 3) log.AppendLine($"  3. {CbL3.SelectedItem}");

            log.AppendLine("\n> СТАТУС: АНАЛИЗ ЗАВЕРШЕН.");
            TxtRelationsLog.Text = log.ToString();
        }

        // ==========================================
        // ЭТАП 5: ЛОГИКА НАВЫКОВ
        // ==========================================
        private void LoadSkillsData()
        {
            _categories = new List<SkillCategory>();
            var grouped = CoreDataBase.AllSkills.GroupBy(s => s.Category);

            foreach (var group in grouped)
            {
                var cat = new SkillCategory { CategoryName = group.Key.ToUpper(), Skills = new List<SkillRow>() };
                foreach (var def in group)
                {
                    cat.Skills.Add(new SkillRow { Name = def.Name, StatName = def.Stat, IsBasic = def.IsBasic, IsX2 = def.IsX2, Level = def.IsBasic ? 2 : 0 });
                }
                _categories.Add(cat);
            }

            IcCategories.ItemsSource = _categories;
            UpdatePointsCounter();
        }

        private void SkillBtnMinus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is SkillRow skill)
            {
                int minLevel = skill.IsBasic ? 2 : 0;
                if (skill.Level > minLevel)
                {
                    skill.Level--;
                    UpdatePointsCounter();
                }
            }
        }

        private void SkillBtnPlus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is SkillRow skill)
            {
                if (skill.Level < 6)
                {
                    int cost = skill.IsX2 ? 2 : 1;
                    if (GetSpentPoints() + cost <= MAX_SKILL_POINTS)
                    {
                        skill.Level++;
                        UpdatePointsCounter();
                    }
                }
            }
        }

        private int GetSpentPoints()
        {
            int spent = 0;
            foreach (var cat in _categories)
                foreach (var skill in cat.Skills)
                    spent += skill.Level * (skill.IsX2 ? 2 : 1);
            return spent;
        }

        private void UpdatePointsCounter()
        {
            if (TxtSkillPoints != null) TxtSkillPoints.Text = (MAX_SKILL_POINTS - GetSpentPoints()).ToString();
        }
    }
}