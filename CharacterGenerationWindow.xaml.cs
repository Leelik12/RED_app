using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;

namespace CyberpunkRED_Generator
{
    // ==========================================
    // КЛАССЫ ДЛЯ СОХРАНЕНИЯ В JSON
    // ==========================================
    public class CharacterSaveData
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public Dictionary<string, int> Stats { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> SystemStats { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, string> Lifepath { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> RoleLifepath { get; set; } = new Dictionary<string, string>();
        public List<string> Friends { get; set; } = new List<string>();
        public List<EnemyData> Enemies { get; set; } = new List<EnemyData>();
        public List<string> TragicLoves { get; set; } = new List<string>();
        public List<SkillSaveData> Skills { get; set; } = new List<SkillSaveData>();
        public string Notes { get; set; }
        public CyberpunkRED_Generator.ArmorData Armor { get; set; } = new CyberpunkRED_Generator.ArmorData();
        public List<CyberpunkRED_Generator.WeaponData> Weapons { get; set; } = new List<CyberpunkRED_Generator.WeaponData>();
        public string StyleNotes { get; set; }
        public string Housing { get; set; }
        public string Rent { get; set; }
        public string Lifestyle { get; set; }
        public string AmmoValue { get; set; }
        public string CashValue { get; set; }
        public List<CyberpunkRED_Generator.GearRowItem> GearItems { get; set; } = new List<CyberpunkRED_Generator.GearRowItem>();
    }

    public class EnemyData
    {
        public string Who { get; set; }
        public string Cause { get; set; }
        public string Hate { get; set; }
        public string Action { get; set; }
    }

    public class SkillSaveData
    {
        public string Name { get; set; }
        public int Level { get; set; }
        public int Total { get; set; }
    }

    // ==========================================
    // ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ ДЛЯ НАВЫКОВ
    // ==========================================
    public class SkillRow : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string StatName { get; set; }
        public bool IsBasic { get; set; }
        public bool IsX2 { get; set; }

        //
        public string Category { get; set; }
        public bool CanAddMultiple { get; set; }
        public bool IsVariant { get; set; }
        public int FreeLevels { get; set; }

        private string _subName = "";
        public string SubName
        {
            get => _subName;
            set { _subName = value; OnPropertyChanged(); }
        }

        public Visibility NormalControlsVisibility => CanAddMultiple ? Visibility.Collapsed : Visibility.Visible;
        public Visibility AddButtonVisibility => CanAddMultiple ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SubNameVisibility => IsVariant ? Visibility.Visible : Visibility.Collapsed;
        public Visibility RemoveButtonVisibility => IsVariant ? Visibility.Visible : Visibility.Collapsed;
        //

        private int _baseStatValue = 5;

        private int _level;
        public int Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(); OnPropertyChanged(nameof(Total)); }
        }

        public string Description { get; set; }

        public int Total => _baseStatValue + Level;

        public void UpdateBaseStat(int newValue)
        {
            _baseStatValue = newValue;
            OnPropertyChanged(nameof(Total));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class SkillCategory
    {
        public string CategoryName { get; set; }
        public ObservableCollection<SkillRow> Skills { get; set; }
    }

    // ==========================================
    // ГЛАВНЫЙ КЛАСС ОКНА
    // ==========================================
    public partial class CharacterGenerationWindow : Window
    {
        private Random _rnd = new Random();
        private bool _isLoaded = false;

        private List<ComboBox> _dynamicRoleComboBoxes = new List<ComboBox>();
        private List<SkillCategory> _categories;
        private const int MAX_SKILL_POINTS = 86;

        public CharacterGenerationWindow()
        {
            InitializeComponent();

            CalculateDerivedStats();
            LoadLifepathData();
            LoadRoleLifepathData();
            LoadRelationsData();
            LoadSkillsData();

            _isLoaded = true;

            // Принудительно отрисовываем стартовые значения
            UpdateLifepathLog(null, null);
            if (CbRole.SelectedItem != null) GenerateQuestions(CbRole.SelectedItem.ToString());
            UpdateRelationsLog(null, null);

            // Связываем статы из Этапа 1 с Этапом 5 (Навыки)
            UpdateAllSkillTotals();
        }

        // ==========================================
        // НАВИГАЦИЯ И СОХРАНЕНИЕ
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
            try
            {
                var charData = new CharacterSaveData();
                // ДОБАВЛЕНО: Проверяем, вписал ли игрок имя, если нет — даем дефолтное
                charData.Name = string.IsNullOrWhiteSpace(TxtCharName.Text) ? "Без_имени" : TxtCharName.Text.Trim();

                charData.Role = CbRole.SelectedItem?.ToString() ?? "Неизвестно";

                // 1. Статы
                charData.Stats["INT"] = GetStat(TxtInt);
                charData.Stats["REF"] = GetStat(TxtRef);
                charData.Stats["DEX"] = GetStat(TxtDex);
                charData.Stats["TECH"] = GetStat(TxtTech);
                charData.Stats["COOL"] = GetStat(TxtCool);
                charData.Stats["WILL"] = GetStat(TxtWill);
                charData.Stats["LUCK"] = GetStat(TxtLuck);
                charData.Stats["MOVE"] = GetStat(TxtMove);
                charData.Stats["BODY"] = GetStat(TxtBody);
                charData.Stats["EMP"] = GetStat(TxtEmp);

                // 2. Системные показатели
                charData.SystemStats["HP"] = int.Parse(TxtHp.Text);
                charData.SystemStats["WoundedThreshold"] = int.Parse(TxtWounded.Text);
                charData.SystemStats["DeathSave"] = int.Parse(TxtDeathSave.Text);
                charData.SystemStats["Humanity"] = int.Parse(TxtHumanity.Text);

                // 3. Жизненный путь
                charData.Lifepath["Культурное происхождение"] = CbOrigin.SelectedItem?.ToString();
                charData.Lifepath["Личность"] = CbPersonality.SelectedItem?.ToString();
                charData.Lifepath["Стиль одежды"] = CbClothing.SelectedItem?.ToString();
                charData.Lifepath["Прическа"] = CbHair.SelectedItem?.ToString();
                charData.Lifepath["Ценность"] = CbValue.SelectedItem?.ToString();
                charData.Lifepath["Отношение к людям"] = CbPeople.SelectedItem?.ToString();
                charData.Lifepath["Самый ценный человек"] = CbPerson.SelectedItem?.ToString();
                charData.Lifepath["Самая ценная вещь"] = CbPossession.SelectedItem?.ToString();
                charData.Lifepath["Семейное происхождение"] = CbFamilyBackground.SelectedItem?.ToString();
                charData.Lifepath["Окружение детства"] = CbChildhood.SelectedItem?.ToString();
                charData.Lifepath["Родители"] = CbParents.SelectedItem?.ToString();
                charData.Lifepath["Трагедия в семье"] = CbFamilyCrisis.SelectedItem?.ToString();
                charData.Lifepath["Цель в жизни"] = CbLifeGoal.SelectedItem?.ToString();

                // 4. Ролевой путь
                foreach (var cb in _dynamicRoleComboBoxes)
                {
                    if (cb.SelectedItem != null && cb.Tag != null)
                    {
                        charData.RoleLifepath[cb.Tag.ToString()] = cb.SelectedItem.ToString();
                    }
                }

                // 5. Связи
                int friends = CbCountFriends.SelectedIndex;
                if (friends >= 1) charData.Friends.Add(CbF1.SelectedItem?.ToString());
                if (friends >= 2) charData.Friends.Add(CbF2.SelectedItem?.ToString());
                if (friends >= 3) charData.Friends.Add(CbF3.SelectedItem?.ToString());

                int enemies = CbCountEnemies.SelectedIndex;
                if (enemies >= 1) charData.Enemies.Add(new EnemyData { Who = CbE1Who.SelectedItem?.ToString(), Cause = CbE1Cause.SelectedItem?.ToString(), Hate = CbE1Hate.SelectedItem?.ToString(), Action = CbE1Action.SelectedItem?.ToString() });
                if (enemies >= 2) charData.Enemies.Add(new EnemyData { Who = CbE2Who.SelectedItem?.ToString(), Cause = CbE2Cause.SelectedItem?.ToString(), Hate = CbE2Hate.SelectedItem?.ToString(), Action = CbE2Action.SelectedItem?.ToString() });
                if (enemies >= 3) charData.Enemies.Add(new EnemyData { Who = CbE3Who.SelectedItem?.ToString(), Cause = CbE3Cause.SelectedItem?.ToString(), Hate = CbE3Hate.SelectedItem?.ToString(), Action = CbE3Action.SelectedItem?.ToString() });

                int love = CbCountLove.SelectedIndex;
                if (love >= 1) charData.TragicLoves.Add(CbL1.SelectedItem?.ToString());
                if (love >= 2) charData.TragicLoves.Add(CbL2.SelectedItem?.ToString());
                if (love >= 3) charData.TragicLoves.Add(CbL3.SelectedItem?.ToString());

                // 6. Навыки (сохраняем только вкачанные навыки > 0)
                foreach (var cat in _categories)
                {
                    foreach (var skill in cat.Skills)
                    {
                        // Не сохраняем "пустые" базовые плашки-кнопки (CanAddMultiple)
                        if (skill.Level > 0 && !skill.CanAddMultiple)
                        {
                            string exportName = skill.IsVariant && !string.IsNullOrWhiteSpace(skill.SubName)
                                                ? $"{skill.Name}: {skill.SubName}"
                                                : skill.Name;

                            charData.Skills.Add(new SkillSaveData
                            {
                                Name = exportName,
                                Level = skill.Level,
                                Total = skill.Total
                            });
                        }
                    }
                }

                // ================== СОЗДАНИЕ ПАПКИ И JSON ==================
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Characters");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

                // ИСПРАВЛЕНО: Теперь в названии файла фигурирует имя персонажа
                string fileName = $"Character_{charData.Name}_{charData.Role}_{timestamp}.json";
                string filePath = Path.Combine(folderPath, fileName);

                // Настройки JSON для правильного отображения русского языка и красивых отступов
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string jsonString = JsonSerializer.Serialize(charData, options);
                File.WriteAllText(filePath, jsonString, Encoding.UTF8);

                MessageBox.Show($"Персонаж успешно сгенерирован и сохранен!\n\nПуть: {filePath}", "ТЕРМИНАЛ CYBERPUNK RED", MessageBoxButton.OK, MessageBoxImage.Information);
                MainWindow mainMenu = new MainWindow();
                mainMenu.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Системный сбой при сохранении: {ex.Message}", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void CbRank_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdatePointsLeft();

        private void StatBtnMinus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is string tag && FindName(tag) as TextBlock is TextBlock tb) AdjustStat(tb, -1);
        }

        private void StatBtnPlus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is string tag && FindName(tag) as TextBlock is TextBlock tb) AdjustStat(tb, 1);
        }

        private void AdjustStat(TextBlock tb, int delta)
        {
            if (int.TryParse(tb.Text, out int val))
            {
                int newVal = val + delta;
                if (newVal >= 2 && newVal <= 8)
                {
                    if (delta > 0 && RbCalc.IsChecked == true && GetTotalPointsSpent() >= GetMaxPoints()) return;
                    tb.Text = newVal.ToString();
                    UpdatePointsLeft();
                    CalculateDerivedStats();
                    UpdateAllSkillTotals(); // Обновляем навыки при ручном клике
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
            UpdateAllSkillTotals(); // Обновляем навыки при рандоме
        }

        private int RollStat() => new List<int> { _rnd.Next(1, 5), _rnd.Next(1, 5), _rnd.Next(1, 5) }.OrderByDescending(x => x).Take(2).Sum();
        private int GetStat(TextBlock tb) => tb != null && int.TryParse(tb.Text, out int result) ? result : 2;
        private int GetTotalPointsSpent() => GetStat(TxtInt) + GetStat(TxtRef) + GetStat(TxtDex) + GetStat(TxtTech) + GetStat(TxtCool) + GetStat(TxtWill) + GetStat(TxtLuck) + GetStat(TxtMove) + GetStat(TxtBody) + GetStat(TxtEmp);
        private int GetMaxPoints() => CbRank?.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out int max) ? max : 62;

        private void UpdatePointsLeft()
        {
            int currentPoints = GetTotalPointsSpent();
            if (TxtPointsLeft != null) TxtPointsLeft.Text = (GetMaxPoints() - currentPoints).ToString();
            if (TxtTotalStats != null) TxtTotalStats.Text = currentPoints.ToString();
        }

        private void CalculateDerivedStats()
        {
            if (TxtHp == null) return;
            int body = GetStat(TxtBody), will = GetStat(TxtWill), emp = GetStat(TxtEmp);

            TxtHp.Text = (10 + (5 * (int)Math.Ceiling((body + will) / 2.0))).ToString();
            TxtWounded.Text = ((int)Math.Ceiling(int.Parse(TxtHp.Text) / 2.0)).ToString();
            TxtDeathSave.Text = body.ToString();
            TxtHumanity.Text = (emp * 10).ToString();
        }

        // Связь Характеристик и Навыков
        private void UpdateAllSkillTotals()
        {
            if (_categories == null) return;
            foreach (var cat in _categories)
            {
                foreach (var skill in cat.Skills)
                {
                    int statVal = 5;
                    switch (skill.StatName)
                    {
                        case "ИНТ": statVal = GetStat(TxtInt); break;
                        case "РЕА": statVal = GetStat(TxtRef); break;
                        case "ЛВК": statVal = GetStat(TxtDex); break;
                        case "ТЕХ": statVal = GetStat(TxtTech); break;
                        case "ХАР": statVal = GetStat(TxtCool); break;
                        case "ВОЛЯ": statVal = GetStat(TxtWill); break;
                        case "ЭМП": statVal = GetStat(TxtEmp); break;
                    }
                    skill.UpdateBaseStat(statVal);
                }
            }
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
                                  $"> ПРОИСХОЖДЕНИЕ И СЕМЬЯ:\n  - Корни: {CbOrigin.SelectedItem}\n  - Семья: {CbFamilyBackground.SelectedItem}\n  - Описание: {familyDesc}\n  - Родители: {CbParents.SelectedItem}\n  - Детство: {CbChildhood.SelectedItem}\n  - Трагедия: {CbFamilyCrisis.SelectedItem}\n\n" +
                                  $"> ХАРАКТЕР: {CbPersonality.SelectedItem}\n\n" +
                                  $"> ВНЕШНИЙ ВИД:\n  - Стиль одежды: {CbClothing.SelectedItem}\n  - Прическа: {CbHair.SelectedItem}\n\n" +
                                  $"> ПСИХОЛОГИЧЕСКИЙ ПРОФИЛЬ:\n  - Отношение к людям: {CbPeople.SelectedItem}\n  - Ценность: {CbValue.SelectedItem}\n  - Важный человек: {CbPerson.SelectedItem}\n  - Важная вещь: {CbPossession.SelectedItem}\n\n" +
                                  $"> ЦЕЛЬ В ЖИЗНИ: {CbLifeGoal.SelectedItem}\n\n> СТАТУС: ФОРМИРОВАНИЕ ЗАВЕРШЕНО.";
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
                TextBlock tb = new TextBlock { Text = table.Title, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999")), Margin = new Thickness(0, 0, 0, 5) };
                ComboBox cb = new ComboBox { ItemsSource = table.Options, SelectedIndex = 0, Tag = table.Title };

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
                if (cb.SelectedItem != null) log.AppendLine($"  - {cb.Tag}:\n    [{cb.SelectedItem}]\n");

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
            CbCountFriends.SelectedIndex = Math.Max(0, _rnd.Next(1, 11) - 7); CbCountEnemies.SelectedIndex = Math.Max(0, _rnd.Next(1, 11) - 7); CbCountLove.SelectedIndex = Math.Max(0, _rnd.Next(1, 11) - 7);
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

            int enemies = CbCountEnemies.SelectedIndex;
            log.AppendLine($"\n> ВРАГИ (Найдено: {enemies}):");
            if (enemies == 0) log.AppendLine("  - Отсутствуют.");
            if (enemies >= 1) log.AppendLine($"  1. {CbE1Who.SelectedItem}. Причина: {CbE1Cause.SelectedItem}. {CbE1Hate.SelectedItem}. {CbE1Action.SelectedItem}.");
            if (enemies >= 2) log.AppendLine($"  2. {CbE2Who.SelectedItem}. Причина: {CbE2Cause.SelectedItem}. {CbE2Hate.SelectedItem}. {CbE2Action.SelectedItem}.");
            if (enemies >= 3) log.AppendLine($"  3. {CbE3Who.SelectedItem}. Причина: {CbE3Cause.SelectedItem}. {CbE3Hate.SelectedItem}. {CbE3Action.SelectedItem}.");

            int love = CbCountLove.SelectedIndex;
            log.AppendLine($"\n> ТРАГИЧЕСКИЕ РОМАНЫ (Найдено: {love}):");
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
                var cat = new SkillCategory { CategoryName = group.Key.ToUpper(), Skills = new ObservableCollection<SkillRow>() };
                foreach (var def in group)
                {
                    cat.Skills.Add(new SkillRow
                    {
                        Category = def.Category,
                        Name = def.Name,
                        StatName = def.Stat,
                        IsBasic = def.IsBasic,
                        IsX2 = def.IsX2,
                        //Level = def.IsBasic ? 2 : 0,
                        Description = def.Description,
                        CanAddMultiple = def.CanAddMultiple,
                        IsVariant = false,
                        Level = def.IsBasic ? 2 : def.FreeLevels,
                        FreeLevels = def.FreeLevels
                    });
                }
                _categories.Add(cat);
            }

            IcCategories.ItemsSource = _categories;
            UpdatePointsCounter();
        }

        private void BtnAddVariant_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is SkillRow parentSkill)
            {
                var category = _categories.FirstOrDefault(c => c.CategoryName == parentSkill.Category.ToUpper());
                if (category != null)
                {
                    int index = category.Skills.IndexOf(parentSkill);

                    var variant = new SkillRow
                    {
                        Category = parentSkill.Category,
                        Name = parentSkill.Name,
                        StatName = parentSkill.StatName,
                        IsBasic = false,
                        IsX2 = parentSkill.IsX2,
                        Description = parentSkill.Description,
                        CanAddMultiple = false,
                        IsVariant = true,
                        Level = 0
                    };

                    int statVal = 5;
                    switch (variant.StatName)
                    {
                        case "ИНТ": statVal = GetStat(TxtInt); break;
                        case "РЕА": statVal = GetStat(TxtRef); break;
                        case "ЛВК": statVal = GetStat(TxtDex); break;
                        case "ТЕХ": statVal = GetStat(TxtTech); break;
                        case "ХАР": statVal = GetStat(TxtCool); break;
                        case "ВОЛЯ": statVal = GetStat(TxtWill); break;
                        case "ЭМП": statVal = GetStat(TxtEmp); break;
                    }
                    variant.UpdateBaseStat(statVal);

                    category.Skills.Insert(index + 1, variant);
                }
            }
        }

        private void BtnRemoveVariant_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is SkillRow variantSkill)
            {
                var category = _categories.FirstOrDefault(c => c.CategoryName == variantSkill.Category.ToUpper());
                if (category != null)
                {
                    category.Skills.Remove(variantSkill);
                    UpdatePointsCounter();
                }
            }
        }

        private void SkillBtnMinus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is SkillRow skill)
            {
                int minLevel = skill.IsBasic ? 2 : skill.FreeLevels;
                if (skill.Level > minLevel)
                {
                    skill.Level--;
                    UpdatePointsCounter();
                }
            }
        }

        private void SkillBtnPlus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is SkillRow skill && skill.Level < 6)
            {
                int cost = skill.IsX2 ? 2 : 1;
                if (GetSpentPoints() + cost <= MAX_SKILL_POINTS)
                {
                    skill.Level++;
                    UpdatePointsCounter();
                }
            }
        }

        private int GetSpentPoints()
        {
            int spent = 0;
            foreach (var cat in _categories)
            {
                foreach (var skill in cat.Skills)
                {
                    int paidLevels = Math.Max(0, skill.Level - skill.FreeLevels);
                    spent += paidLevels * (skill.IsX2 ? 2 : 1);
                }
            }
            return spent;
        }

        private void UpdatePointsCounter()
        {
            if (TxtSkillPoints != null) TxtSkillPoints.Text = (MAX_SKILL_POINTS - GetSpentPoints()).ToString();
        }
    }
}