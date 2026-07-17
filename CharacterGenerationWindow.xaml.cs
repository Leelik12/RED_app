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

            // стартовые значения
            UpdateLifepathLog(null, null);
            if (CbRole.SelectedItem != null) GenerateQuestions(CbRole.SelectedItem.ToString());
            UpdateRelationsLog(null, null);

            // связываем статы из этапа 1 с этапом 5 навыки
            UpdateAllSkillTotals();
        }


        // навигация и сохранение

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
                charData.Name = string.IsNullOrWhiteSpace(TxtCharName.Text) ? "Без_имени" : TxtCharName.Text.Trim();

                charData.Role = CbRole.SelectedItem?.ToString() ?? "Неизвестно";

                // статы
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

                // системные показатели
                charData.SystemStats["HP"] = int.Parse(TxtHp.Text);
                charData.SystemStats["WoundedThreshold"] = int.Parse(TxtWounded.Text);
                charData.SystemStats["DeathSave"] = int.Parse(TxtDeathSave.Text);
                charData.SystemStats["Humanity"] = int.Parse(TxtHumanity.Text);

                // жизненный путь
                charData.Lifepath["Культурное происхождение"] = CbOrigin.Text;
                charData.Lifepath["Личность"] = CbPersonality.Text;
                charData.Lifepath["Стиль одежды"] = CbClothing.Text;
                charData.Lifepath["Прическа"] = CbHair.Text;
                charData.Lifepath["Отличительная черта"] = CbFeature.Text;
                charData.Lifepath["Ценность"] = CbValue.Text;
                charData.Lifepath["Отношение к людям"] = CbPeople.Text;
                charData.Lifepath["Самый ценный человек"] = CbPerson.Text;
                charData.Lifepath["Самая ценная вещь"] = CbPossession.Text;
                charData.Lifepath["Семейное происхождение"] = CbFamilyBackground.Text;
                charData.Lifepath["Окружение детства"] = CbChildhood.Text;
                charData.Lifepath["Трагедия в семье"] = CbFamilyCrisis.Text;
                charData.Lifepath["Цель в жизни"] = CbLifeGoal.Text;

                // ролевой путь
                foreach (var cb in _dynamicRoleComboBoxes)
                {
                    if (!string.IsNullOrWhiteSpace(cb.Text) && cb.Tag != null)
                    {
                        charData.RoleLifepath[cb.Tag.ToString()] = cb.Text;
                    }
                }

                // связи
                int friends = CbCountFriends.SelectedIndex;
                if (friends >= 1) charData.Friends.Add(CbF1.Text);
                if (friends >= 2) charData.Friends.Add(CbF2.Text);
                if (friends >= 3) charData.Friends.Add(CbF3.Text);

                int enemies = CbCountEnemies.SelectedIndex;
                if (enemies >= 1) charData.Enemies.Add(new EnemyData { Who = CbE1Who.Text, Cause = CbE1Cause.Text, Hate = CbE1Hate.Text, Action = CbE1Action.Text });
                if (enemies >= 2) charData.Enemies.Add(new EnemyData { Who = CbE2Who.Text, Cause = CbE2Cause.Text, Hate = CbE2Hate.Text, Action = CbE2Action.Text });
                if (enemies >= 3) charData.Enemies.Add(new EnemyData { Who = CbE3Who.Text, Cause = CbE3Cause.Text, Hate = CbE3Hate.Text, Action = CbE3Action.Text });

                int love = CbCountLove.SelectedIndex;
                if (love >= 1) charData.TragicLoves.Add(CbL1.Text);
                if (love >= 2) charData.TragicLoves.Add(CbL2.Text);
                if (love >= 3) charData.TragicLoves.Add(CbL3.Text);

                // навыки сохраняем только вкачанные навыки > 0
                foreach (var cat in _categories)
                {
                    foreach (var skill in cat.Skills)
                    {
                        // не сохраняем пустые базовые плашки-кнопки
                        if (skill.Level > 0 && !skill.CanAddMultiple)
                        {
                            string exportName = skill.Name;
                            if (skill.IsVariant && !string.IsNullOrWhiteSpace(skill.SubName))
                            {
                                exportName = $"{skill.BaseName}: {skill.SubName.Trim()}";
                            }
                            else if (skill.BaseName == "Язык (Родной)" && !string.IsNullOrWhiteSpace(skill.SubName))
                            {
                                exportName = $"{skill.Name}: {skill.SubName.Trim()}";
                            }

                            charData.Skills.Add(new SkillSaveData
                            {
                                Name = exportName,
                                Level = skill.Level,
                                Total = skill.Total
                            });
                        }
                    }
                }

                // создание папки и джисона
                string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Characters");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

                string fileName = $"Character_{charData.Name}_{charData.Role}_{timestamp}.json";
                string filePath = Path.Combine(folderPath, fileName);

                // настройки json для правильного отображения русского языка и красивых отступов
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


        // логика АНТИХАЙПА (статов)

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
                    UpdateAllSkillTotals();
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
            UpdateAllSkillTotals();
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

        // связь характеристик и навыков
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

        // логика жизненного пути
        private void LoadLifepathData()
        {
            CbOrigin.ItemsSource = CoreDataBase.Origins; CbPersonality.ItemsSource = CoreDataBase.Personalities;
            CbClothing.ItemsSource = CoreDataBase.Clothing; CbHair.ItemsSource = CoreDataBase.Hair;
            CbFeature.ItemsSource = CoreDataBase.Features;
            CbValue.ItemsSource = CoreDataBase.Values; CbPeople.ItemsSource = CoreDataBase.People;
            CbPerson.ItemsSource = CoreDataBase.Persons; CbPossession.ItemsSource = CoreDataBase.Possessions;
            CbFamilyBackground.ItemsSource = CoreDataBase.FamilyBackground; CbChildhood.ItemsSource = CoreDataBase.ChildhoodEnvironment;
            CbFamilyCrisis.ItemsSource = CoreDataBase.FamilyCrisis;
            CbLifeGoal.ItemsSource = CoreDataBase.LifeGoals;

            CbOrigin.SelectedIndex = 0; CbPersonality.SelectedIndex = 0; CbClothing.SelectedIndex = 0; CbHair.SelectedIndex = 0;
            CbFeature.SelectedIndex = 0; CbValue.SelectedIndex = 0; CbPeople.SelectedIndex = 0; CbPerson.SelectedIndex = 0;
            CbPossession.SelectedIndex = 0; CbFamilyBackground.SelectedIndex = 0; CbChildhood.SelectedIndex = 0;
            CbFamilyCrisis.SelectedIndex = 0; CbLifeGoal.SelectedIndex = 0;
        }

        private void BtnLifepathRandom_Click(object sender, RoutedEventArgs e)
        {
            CbOrigin.SelectedIndex = _rnd.Next(0, 10); CbPersonality.SelectedIndex = _rnd.Next(0, 10);
            CbClothing.SelectedIndex = _rnd.Next(0, 10); CbHair.SelectedIndex = _rnd.Next(0, 10);
            CbFeature.SelectedIndex = _rnd.Next(0, 10);
            CbValue.SelectedIndex = _rnd.Next(0, 10); CbPeople.SelectedIndex = _rnd.Next(0, 10);
            CbPerson.SelectedIndex = _rnd.Next(0, 10); CbPossession.SelectedIndex = _rnd.Next(0, 10);
            CbFamilyBackground.SelectedIndex = _rnd.Next(0, 10); CbChildhood.SelectedIndex = _rnd.Next(0, 10);
            CbFamilyCrisis.SelectedIndex = _rnd.Next(0, 10);
            CbLifeGoal.SelectedIndex = _rnd.Next(0, 10);
        }

        private void UpdateLifepathLog(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded || TxtLifepathLog == null) return;

            string familyDesc = CbFamilyBackground.SelectedIndex >= 0 ? CoreDataBase.FamilyBackgroundDesc[CbFamilyBackground.SelectedIndex] : "Пользовательское описание";

            TxtLifepathLog.Text = $"> АНАЛИЗ БИОМЕТРИИ И ИСТОРИИ...\n\n" +
                                  $"> ПРОИСХОЖДЕНИЕ И СЕМЬЯ:\n  - Корни: {CbOrigin.Text}\n  - Семья: {CbFamilyBackground.Text}\n  - Описание: {familyDesc}\n  - Детство: {CbChildhood.Text}\n  - Трагедия: {CbFamilyCrisis.Text}\n\n" +
                                  $"> ХАРАКТЕР: {CbPersonality.Text}\n\n" +
                                  $"> ВНЕШНИЙ ВИД:\n  - Стиль одежды: {CbClothing.Text}\n  - Прическа: {CbHair.Text}\n  - Особенность: {CbFeature.Text}\n\n" +
                                  $"> ПСИХОЛОГИЧЕСКИЙ ПРОФИЛЬ:\n  - Отношение к людям: {CbPeople.Text}\n  - Ценность: {CbValue.Text}\n  - Важный человек: {CbPerson.Text}\n  - Важная вещь: {CbPossession.Text}\n\n" +
                                  $"> ЦЕЛЬ В ЖИЗНИ: {CbLifeGoal.Text}\n\n> СТАТУС: ФОРМИРОВАНИЕ ЗАВЕРШЕНО.";
        }


        // логика ролевого пути
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
                ComboBox cb = new ComboBox { ItemsSource = table.Options, SelectedIndex = 0, Tag = table.Title, IsEditable = true };

                cb.SelectionChanged += (s, e) => UpdateRoleLog();
                cb.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent, new System.Windows.Controls.TextChangedEventHandler((s, ev) => UpdateRoleLog()));
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
            log.AppendLine($"> АНАЛИЗ КАРЬЕРЫ: {CbRole.Text.ToUpper()}\n");

            foreach (var cb in _dynamicRoleComboBoxes)
                if (!string.IsNullOrWhiteSpace(cb.Text)) log.AppendLine($"  - {cb.Tag}:\n    [{cb.Text}]\n");

            log.AppendLine("> СТАТУС: ПРОФИЛЬ УТВЕРЖДЕН.");
            TxtRoleLog.Text = log.ToString();
        }

        // логика связей
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

        private void UpdateRelationsLog(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded || TxtRelationsLog == null) return;
            StringBuilder log = new StringBuilder();
            log.AppendLine("> АНАЛИЗ СОЦИАЛЬНЫХ СВЯЗЕЙ...\n");

            int friends = CbCountFriends.SelectedIndex;
            log.AppendLine($"> ДРУЗЬЯ (Найдено: {friends}):");
            if (friends == 0) log.AppendLine("  - Отсутствуют.");
            if (friends >= 1) log.AppendLine($"  1. {CbF1.Text}");
            if (friends >= 2) log.AppendLine($"  2. {CbF2.Text}");
            if (friends >= 3) log.AppendLine($"  3. {CbF3.Text}");

            int enemies = CbCountEnemies.SelectedIndex;
            log.AppendLine($"\n> ВРАГИ (Найдено: {enemies}):");
            if (enemies == 0) log.AppendLine("  - Отсутствуют.");
            if (enemies >= 1) log.AppendLine($"  1. {CbE1Who.Text}. Причина: {CbE1Cause.Text}. {CbE1Hate.Text}. {CbE1Action.Text}.");
            if (enemies >= 2) log.AppendLine($"  2. {CbE2Who.Text}. Причина: {CbE2Cause.Text}. {CbE2Hate.Text}. {CbE2Action.Text}.");
            if (enemies >= 3) log.AppendLine($"  3. {CbE3Who.Text}. Причина: {CbE3Cause.Text}. {CbE3Hate.Text}. {CbE3Action.Text}.");

            int love = CbCountLove.SelectedIndex;
            log.AppendLine($"\n> ТРАГИЧЕСКИЕ РОМАНЫ (Найдено: {love}):");
            if (love == 0) log.AppendLine("  - Отсутствуют.");
            if (love >= 1) log.AppendLine($"  1. {CbL1.Text}");
            if (love >= 2) log.AppendLine($"  2. {CbL2.Text}");
            if (love >= 3) log.AppendLine($"  3. {CbL3.Text}");

            log.AppendLine("\n> СТАТУС: АНАЛИЗ ЗАВЕРШЕН.");
            TxtRelationsLog.Text = log.ToString();
        }

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
                        BaseName = def.Name,
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

            // РАСПРЕДЕЛЯЕМ КАТЕГОРИИ ПО 3 СТОЛБЦАМ
            var col1 = new List<SkillCategory>();
            var col2 = new List<SkillCategory>();
            var col3 = new List<SkillCategory>();

            for (int i = 0; i < _categories.Count; i++)
            {
                if (i % 3 == 0) col1.Add(_categories[i]);
                else if (i % 3 == 1) col2.Add(_categories[i]);
                else col3.Add(_categories[i]);
            }

            IcCategories1.ItemsSource = col1;
            IcCategories2.ItemsSource = col2;
            IcCategories3.ItemsSource = col3;

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
                        BaseName = parentSkill.Name,
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

        private void BtnLazyMode_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder prompt = new StringBuilder();

            prompt.AppendLine("Сгенерируй детальную, глубокую и атмосферную предысторию (квенту) для моего персонажа в мире настольной ролевой игры Cyberpunk RED.\n");
            prompt.AppendLine("ОБЯЗАТЕЛЬНЫЕ УСЛОВИЯ:");
            prompt.AppendLine("1. Повествование СТРОГО от первого лица («Я вырос...», «Моя семья...»).");
            prompt.AppendLine("2. Объем должен быть большим и детальным (около 6-8 объемных абзацев, в 2 раза больше стандартного ответа). Опиши эмоции, мотивацию и покажи тяжелый путь от прошлого к жизни наемника (эджраннера) в Найт-Сити.");
            prompt.AppendLine("3. Максимально точно следуй предоставленным фактам из блока «ЖИЗНЕННЫЙ ПУТЬ». Не искажай их, а гармонично вплетай в повествование, объясняя причины и следствия.");
            prompt.AppendLine("4. Придумай подходящие киберпанк-имена или прозвища АБСОЛЮТНО ВСЕМ упомянутым людям (друзьям, врагам, бывшим, напарникам, родителям), если они фигурируют в истории.\n");

            prompt.AppendLine($"Имя моего персонажа: {(string.IsNullOrWhiteSpace(TxtCharName.Text) ? "Неизвестно (придумай крутое имя/прозвище)" : TxtCharName.Text.Trim())}");
            prompt.AppendLine($"Роль (Класс): {CbRole.SelectedItem}\n");

            prompt.AppendLine("=== ХАРАКТЕРИСТИКИ ===");
            prompt.AppendLine($"ИНТ: {GetStat(TxtInt)}, РЕА: {GetStat(TxtRef)}, ЛВК: {GetStat(TxtDex)}, ТЕХ: {GetStat(TxtTech)}, ХАР: {GetStat(TxtCool)}, ВОЛЯ: {GetStat(TxtWill)}, УДЧ: {GetStat(TxtLuck)}, СКО: {GetStat(TxtMove)}, ТЕЛ: {GetStat(TxtBody)}, ЭМП: {GetStat(TxtEmp)}\n");

            prompt.AppendLine("=== ЖИЗНЕННЫЙ ПУТЬ ===");
            prompt.AppendLine($"- Культурное происхождение: {CbOrigin.Text}");
            prompt.AppendLine($"- Личность: {CbPersonality.Text}");
            prompt.AppendLine($"- Стиль одежды: {CbClothing.Text}");
            prompt.AppendLine($"- Прическа: {CbHair.Text}");
            prompt.AppendLine($"- Особенность: {CbFeature.Text}");
            prompt.AppendLine($"- Что ценит больше всего: {CbValue.Text}");
            prompt.AppendLine($"- Отношение к людям: {CbPeople.Text}");
            prompt.AppendLine($"- Самый ценный человек: {CbPerson.Text}");
            prompt.AppendLine($"- Самая ценная вещь: {CbPossession.Text}");
            prompt.AppendLine($"- Семейное происхождение: {CbFamilyBackground.Text}");
            prompt.AppendLine($"- Окружение детства: {CbChildhood.Text}");
            prompt.AppendLine($"- Трагедия в семье: {CbFamilyCrisis.Text}");
            prompt.AppendLine($"- Цель в жизни: {CbLifeGoal.Text}\n");

            prompt.AppendLine("=== РОЛЕВОЙ ЖИЗНЕННЫЙ ПУТЬ ===");
            foreach (var cb in _dynamicRoleComboBoxes)
            {
                if (!string.IsNullOrWhiteSpace(cb.Text)) prompt.AppendLine($"- {cb.Tag}: {cb.Text}");
            }
            prompt.AppendLine();

            prompt.AppendLine("=== СОЦИАЛЬНЫЕ СВЯЗИ ===");
            if (CbCountFriends.SelectedIndex > 0)
            {
                prompt.AppendLine("Друзья:");
                if (CbCountFriends.SelectedIndex >= 1) prompt.AppendLine($"  1. {CbF1.Text}");
                if (CbCountFriends.SelectedIndex >= 2) prompt.AppendLine($"  2. {CbF2.Text}");
                if (CbCountFriends.SelectedIndex >= 3) prompt.AppendLine($"  3. {CbF3.Text}");
            }

            if (CbCountEnemies.SelectedIndex > 0)
            {
                prompt.AppendLine("Враги:");
                if (CbCountEnemies.SelectedIndex >= 1) prompt.AppendLine($"  1. {CbE1Who.Text}. Причина: {CbE1Cause.Text}. Намерения: {CbE1Action.Text}");
                if (CbCountEnemies.SelectedIndex >= 2) prompt.AppendLine($"  2. {CbE2Who.Text}. Причина: {CbE2Cause.Text}. Намерения: {CbE2Action.Text}");
                if (CbCountEnemies.SelectedIndex >= 3) prompt.AppendLine($"  3. {CbE3Who.Text}. Причина: {CbE3Cause.Text}. Намерения: {CbE3Action.Text}");
            }

            if (CbCountLove.SelectedIndex > 0)
            {
                prompt.AppendLine("Трагические романы:");
                if (CbCountLove.SelectedIndex >= 1) prompt.AppendLine($"  1. {CbL1.Text}");
                if (CbCountLove.SelectedIndex >= 2) prompt.AppendLine($"  2. {CbL2.Text}");
                if (CbCountLove.SelectedIndex >= 3) prompt.AppendLine($"  3. {CbL3.Text}");
            }

            TxtLazyPrompt.Text = prompt.ToString();
            LazyModeOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseLazy_Click(object sender, RoutedEventArgs e)
        {
            LazyModeOverlay.Visibility = Visibility.Collapsed;
        }

        private void BtnCopyLazy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(TxtLazyPrompt.Text);
            MessageBox.Show("Промпт успешно скопирован в буфер обмена!\n\nПросто откройте любимую нейросеть (ChatGPT, Claude, YandexGPT) и вставьте текст (Ctrl+V).", "СКОПИРОВАНО", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void WizardTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}