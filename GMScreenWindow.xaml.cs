using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CyberpunkRED_Generator
{
    //модели данных для врагов
    public class EnemySaveData
    {
        public string Name { get; set; }
        public int HP { get; set; }
        public int HeadArmor { get; set; }
        public int BodyArmor { get; set; }

        public bool IsCNMode { get; set; }
        public int CombatNumber { get; set; }
        public int BaseDeathSave { get; set; }
        public int BaseMove { get; set; }

        public Dictionary<string, int> Stats { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> Skills { get; set; } = new Dictionary<string, int>();

        public string Weapons { get; set; }
        public string Gear { get; set; }
        public string Cyberware { get; set; }

        public Visibility IsCNVis => IsCNMode ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsFullVis => !IsCNMode ? Visibility.Visible : Visibility.Collapsed;
    }

    // класс для красивого отображения травм
    public class GMCriticalInjuryItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string QuickFix { get; set; }
        public string Treatment { get; set; }
        public int AllActionsPenalty { get; set; }
        public int MovePenalty { get; set; }
        public string EffectText { get; set; }
        public int DeathSavePenalty { get; set; }

        public string TooltipText => $"{Description}\n\nЭФФЕКТ РАНЕНИЯ:\n{EffectText}\n\nСЛОЖНОСТЬ СТАБИЛИЗАЦИИ:\nПервая помощь: {QuickFix}\nЛечение: {Treatment}";
    }

    // класс для отображения импланта
    public class CyberwareItemViewModel
    {
        public string Name { get; set; }
        public string TooltipText { get; set; }
    }

    public class CombatantViewModel : INotifyPropertyChanged
    {
        private bool _isActiveTurn;
        public bool IsActiveTurn { get => _isActiveTurn; set { _isActiveTurn = value; OnPropertyChanged(); } }

        public bool IsPlayer { get; set; }

        private int _initiative;
        public int Initiative { get => _initiative; set { _initiative = value; OnPropertyChanged(); } }

        private string _name;
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        private int _currentHP;
        public int CurrentHP { get => _currentHP; set { _currentHP = value; OnPropertyChanged(); } }

        private int _maxHP;
        public int MaxHP { get => _maxHP; set { _maxHP = value; OnPropertyChanged(); } }

        private int _headArmor;
        public int HeadArmor { get => _headArmor; set { _headArmor = Math.Max(0, value); OnPropertyChanged(); } }

        private int _bodyArmor;
        public int BodyArmor { get => _bodyArmor; set { _bodyArmor = Math.Max(0, value); OnPropertyChanged(); } }

        public string CombatStatsDisplay { get; set; }
        public ObservableCollection<string> DisplaySkills { get; set; } = new ObservableCollection<string>();

        private string _weapons; public string Weapons { get => _weapons; set { _weapons = value; OnPropertyChanged(); } }
        private string _gear; public string Gear { get => _gear; set { _gear = value; OnPropertyChanged(); } }
        public ObservableCollection<CyberwareItemViewModel> ParsedCyberware { get; set; } = new ObservableCollection<CyberwareItemViewModel>();

        private string _cyberware;
        public string Cyberware
        {
            get => _cyberware;
            set
            {
                _cyberware = value;
                OnPropertyChanged();
                ParseCyberware(); // автоматически парсим строку в блоки
            }
        }

        public int BaseMove { get; set; }
        public int BaseDeathSave { get; set; }

        public ObservableCollection<GMCriticalInjuryItem> ActiveInjuries { get; set; } = new ObservableCollection<GMCriticalInjuryItem>();

        public List<string> AvailableInjuries => CoreDataBase.CriticalInjuries.Select(x => x.Name).ToList();

        private string _selectedInjuryToAdd;
        public string SelectedInjuryToAdd { get => _selectedInjuryToAdd; set { _selectedInjuryToAdd = value; OnPropertyChanged(); } }

        public int CurrentMove => Math.Max(1, BaseMove - ActiveInjuries.Sum(i => i.MovePenalty));
        public int CurrentDeathSave => BaseDeathSave + ActiveInjuries.Sum(i => i.DeathSavePenalty);
        public int ActionPenalty => ActiveInjuries.Sum(i => i.AllActionsPenalty);
        public Visibility PenaltyVis => ActionPenalty > 0 ? Visibility.Visible : Visibility.Collapsed;

        public CombatantViewModel()
        {
            if (AvailableInjuries.Count > 0) SelectedInjuryToAdd = AvailableInjuries[0];
        }

        private void ParseCyberware()
        {
            ParsedCyberware.Clear();
            if (string.IsNullOrWhiteSpace(Cyberware)) return;
            var parts = Cyberware.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                string cleanName = p.Trim();
                if (string.IsNullOrWhiteSpace(cleanName)) continue;

                var def = CoreDataBase.AllCyberware.FirstOrDefault(c => c.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase));
                ParsedCyberware.Add(new CyberwareItemViewModel
                {
                    Name = cleanName,
                    TooltipText = def != null ? $"{def.Description}\n\nПЧ: {def.HumanityLoss} | Слотов: {def.Slots}" : "Кастомный имплант (нет описания)"
                });
            }
        }

        public EnemySaveData OriginalData { get; set; }

        public void Recalculate()
        {
            OnPropertyChanged(nameof(CurrentMove));
            OnPropertyChanged(nameof(CurrentDeathSave));
            OnPropertyChanged(nameof(ActionPenalty));
            OnPropertyChanged(nameof(PenaltyVis));

            RebuildStatsDisplay();
        }

        public void RebuildStatsDisplay()
        {
            if (OriginalData == null || IsPlayer) return;

            int penalty = ActionPenalty;

            if (OriginalData.IsCNMode)
            {
                int currentCN = Math.Max(0, OriginalData.CombatNumber - penalty);
                CombatStatsDisplay = $"Боевой Номер (БН): {currentCN}";
            }
            else
            {
                var stList = new List<string>();
                foreach (var kvp in OriginalData.Stats)
                {
                    int val = kvp.Value;
                    if (kvp.Key != "LUCK" && kvp.Key != "УДЧ" && kvp.Key != "EMP" && kvp.Key != "ЭМП" && kvp.Key != "BODY" && kvp.Key != "ТЕЛ" && kvp.Key != "MOVE" && kvp.Key != "СКО")
                    {
                        val -= penalty;
                    }
                    stList.Add($"{kvp.Key}:{val}");
                }
                CombatStatsDisplay = string.Join(" ", stList);

                var skillDisplays = new ObservableCollection<string>();
                foreach (var kvp in OriginalData.Skills)
                {
                    int total = kvp.Value;
                    var def = CoreDataBase.AllSkills.FirstOrDefault(s => s.Name == kvp.Key);
                    if (def != null)
                    {
                        string statKey = def.Stat;
                        switch (statKey)
                        {
                            case "ИНТ": statKey = "INT"; break;
                            case "РЕА": statKey = "REF"; break;
                            case "ЛВК": statKey = "DEX"; break;
                            case "ТЕХ": statKey = "TECH"; break;
                            case "ХАР": statKey = "COOL"; break;
                            case "ВОЛЯ": statKey = "WILL"; break;
                            case "УДЧ": statKey = "LUCK"; break;
                            case "СКО": statKey = "MOVE"; break;
                            case "ТЕЛ": statKey = "BODY"; break;
                            case "ЭМП": statKey = "EMP"; break;
                        }
                        if (OriginalData.Stats.ContainsKey(statKey))
                        {
                            total += OriginalData.Stats[statKey];
                        }
                    }

                    total -= penalty;
                    if (total < 0) total = 0;

                    skillDisplays.Add($"{kvp.Key} [{total}]");
                }
                DisplaySkills = skillDisplays;
            }

            OnPropertyChanged(nameof(CombatStatsDisplay));
            OnPropertyChanged(nameof(DisplaySkills));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class MultiParamConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture) => values.Clone();
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    // логика АНТИХАЙПА (окна гма)

    public partial class GMScreenWindow : Window
    {
        public ObservableCollection<CombatantViewModel> Combatants { get; set; } = new ObservableCollection<CombatantViewModel>();
        public ObservableCollection<EnemySaveData> AvailableEnemies { get; set; } = new ObservableCollection<EnemySaveData>();

        // для создания врагов
        public List<string> AllSkillNames => CoreDataBase.AllSkills.Select(s => s.Name).OrderBy(n => n).ToList();
        public ObservableCollection<SkillSaveData> NewEnemySkills { get; set; } = new ObservableCollection<SkillSaveData>();

        public List<string> AllCyberwareNames => CoreDataBase.AllCyberware.Select(c => c.Name).Distinct().OrderBy(n => n).ToList();
        public ObservableCollection<string> NewEnemyCyberware { get; set; } = new ObservableCollection<string>();

        private int _currentTurnIndex = -1;
        private int _roundCount = 1;

        public GMScreenWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void BtnNextTurn_Click(object sender, RoutedEventArgs e)
        {
            if (Combatants.Count == 0) return;

            if (_currentTurnIndex >= 0 && _currentTurnIndex < Combatants.Count)
                Combatants[_currentTurnIndex].IsActiveTurn = false;

            _currentTurnIndex++;

            if (_currentTurnIndex >= Combatants.Count)
            {
                _currentTurnIndex = 0;
                _roundCount++;
                TxtRoundCounter.Text = $"РАУНД: {_roundCount}";
                TxtTimeCounter.Text = $"Прошло времени: {(_roundCount - 1) * 3} сек.";
            }

            Combatants[_currentTurnIndex].IsActiveTurn = true;
        }

        private void BtnSortInitiative_Click(object sender, RoutedEventArgs e)
        {
            if (Combatants.Count == 0) return;

            var sorted = Combatants.OrderByDescending(c => c.Initiative).ToList();
            Combatants.Clear();
            foreach (var c in sorted) Combatants.Add(c);

            _currentTurnIndex = -1;
            foreach (var c in Combatants) c.IsActiveTurn = false;

            _roundCount = 1;
            TxtRoundCounter.Text = "РАУНД: 1";
            TxtTimeCounter.Text = "Прошло времени: 0 сек.";

            MessageBox.Show("Инициатива отсортирована. Бой начался!", "ИНФО", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // карточка воина
        private void BtnAblateHead_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is CombatantViewModel c && c.HeadArmor > 0) c.HeadArmor--; }
        private void BtnAblateBody_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is CombatantViewModel c && c.BodyArmor > 0) c.BodyArmor--; }
        private void BtnDamage_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is CombatantViewModel c) c.CurrentHP--; }
        private void BtnHeal_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is CombatantViewModel c) c.CurrentHP++; }
        private void BtnRemoveCombatant_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is CombatantViewModel c) Combatants.Remove(c); }

        private void BtnAddGenCyber_Click(object sender, RoutedEventArgs e)
        {
            string cyberName = CbGenCyber.Text?.Trim();
            if (!string.IsNullOrEmpty(cyberName) && !NewEnemyCyberware.Contains(cyberName))
            {
                NewEnemyCyberware.Add(cyberName);
                CbGenCyber.Text = "";
            }
        }

        private void BtnRemoveGenCyber_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is string cyber)
            {
                NewEnemyCyberware.Remove(cyber);
            }
        }

        private void BtnAddCritToCombatant_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is CombatantViewModel c && !string.IsNullOrEmpty(c.SelectedInjuryToAdd))
            {
                var def = CoreDataBase.CriticalInjuries.FirstOrDefault(x => x.Name == c.SelectedInjuryToAdd);
                if (def != null)
                {
                    c.ActiveInjuries.Add(new GMCriticalInjuryItem
                    {
                        Name = def.Name,
                        Description = def.Description,
                        QuickFix = def.QuickFix,
                        Treatment = def.Treatment,
                        AllActionsPenalty = def.AllActionsPenalty,
                        MovePenalty = def.MovePenalty,
                        DeathSavePenalty = def.DeathSavePenalty,
                        EffectText = def.EffectText
                    });
                    c.Recalculate();
                }
            }
        }

        private void BtnRemoveCritFromCombatant_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is object[] args && args.Length == 2)
            {
                if (args[0] is CombatantViewModel combatant && args[1] is GMCriticalInjuryItem injury)
                {
                    combatant.ActiveInjuries.Remove(injury);
                    combatant.Recalculate();
                }
            }
        }

        private void BtnAddPlayer_Click(object sender, RoutedEventArgs e)
        {
            Combatants.Add(new CombatantViewModel
            {
                Name = "ИГРОК",
                IsPlayer = true,
                Initiative = 0,
                CurrentHP = 40,
                MaxHP = 40,
                HeadArmor = 11,
                BodyArmor = 11,
                BaseMove = 5,
                BaseDeathSave = 5,
                CombatStatsDisplay = "Смотреть в листе игрока",
                Weapons = "",
                Gear = "",
                Cyberware = ""
            });
        }

        // генератор и оверлей
        private void BtnOpenEnemyManager_Click(object sender, RoutedEventArgs e)
        {
            LoadEnemiesFromFolder();
            RbGenCN.IsChecked = true;
            NewEnemySkills.Clear();
            NewEnemyCyberware.Clear();
            EnemyManagerOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseEnemyManager_Click(object sender, RoutedEventArgs e)
        {
            EnemyManagerOverlay.Visibility = Visibility.Collapsed;
        }

        private void GenMode_Changed(object sender, RoutedEventArgs e)
        {
            if (PanelGenCN == null || PanelGenFull == null) return;
            bool isCN = RbGenCN.IsChecked == true;
            PanelGenCN.Visibility = isCN ? Visibility.Visible : Visibility.Collapsed;
            PanelGenFull.Visibility = isCN ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BtnAddGenSkill_Click(object sender, RoutedEventArgs e)
        {
            if (CbGenSkills.SelectedItem != null && int.TryParse(TxtGenSkillLevel.Text, out int inputValue))
            {
                string skillName = CbGenSkills.SelectedItem.ToString();
                int skillLevel = inputValue;

                // если галочка стоит значит ввели финалку (Навык + СТАТ) из корника. 
                // нам нужно вычесть СТАТ, чтобы сохранить в базу чистый уровень навыка.
                if (ChkSkillIsTotal.IsChecked == true)
                {
                    var def = CoreDataBase.AllSkills.FirstOrDefault(s => s.Name == skillName);
                    if (def != null)
                    {
                        string statKey = def.Stat;
                        switch (statKey)
                        {
                            case "ИНТ": statKey = "INT"; break;
                            case "РЕА": statKey = "REF"; break;
                            case "ЛВК": statKey = "DEX"; break;
                            case "ТЕХ": statKey = "TECH"; break;
                            case "ХАР": statKey = "COOL"; break;
                            case "ВОЛЯ": statKey = "WILL"; break;
                            case "УДЧ": statKey = "LUCK"; break;
                            case "СКО": statKey = "MOVE"; break;
                            case "ТЕЛ": statKey = "BODY"; break;
                            case "ЭМП": statKey = "EMP"; break;
                        }

                        int statValue = 5; // базовое на всякий
                        switch (statKey)
                        {
                            case "INT": int.TryParse(StINT.Text, out statValue); break;
                            case "REF": int.TryParse(StREF.Text, out statValue); break;
                            case "DEX": int.TryParse(StDEX.Text, out statValue); break;
                            case "TECH": int.TryParse(StTECH.Text, out statValue); break;
                            case "COOL": int.TryParse(StCOOL.Text, out statValue); break;
                            case "WILL": int.TryParse(StWILL.Text, out statValue); break;
                            case "LUCK": int.TryParse(StLUCK.Text, out statValue); break;
                            case "MOVE": int.TryParse(StMOVE.Text, out statValue); break;
                            case "BODY": int.TryParse(StBODY.Text, out statValue); break;
                            case "EMP": int.TryParse(StEMP.Text, out statValue); break;
                        }

                        skillLevel = inputValue - statValue;

                        if (skillLevel < 0) skillLevel = 0;
                    }
                }

                NewEnemySkills.Add(new SkillSaveData { Name = skillName, Total = skillLevel });
            }
        }

        private void BtnEditEnemyPreset_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is EnemySaveData enemyData)
            {
                TxtGenName.Text = enemyData.Name;
                TxtGenHP.Text = enemyData.HP.ToString();
                TxtGenHead.Text = enemyData.HeadArmor.ToString();
                TxtGenBody.Text = enemyData.BodyArmor.ToString();
                TxtGenWeapons.Text = enemyData.Weapons;
                TxtGenGear.Text = enemyData.Gear;

                if (enemyData.IsCNMode)
                {
                    RbGenCN.IsChecked = true;
                    TxtGenCN.Text = enemyData.CombatNumber.ToString();
                    TxtGenDeathSaveCN.Text = enemyData.BaseDeathSave.ToString();
                    TxtGenMove.Text = enemyData.BaseMove.ToString();
                }
                else
                {
                    RbGenFull.IsChecked = true;
                    StINT.Text = enemyData.Stats.ContainsKey("INT") ? enemyData.Stats["INT"].ToString() : "5";
                    StREF.Text = enemyData.Stats.ContainsKey("REF") ? enemyData.Stats["REF"].ToString() : "5";
                    StDEX.Text = enemyData.Stats.ContainsKey("DEX") ? enemyData.Stats["DEX"].ToString() : "5";
                    StTECH.Text = enemyData.Stats.ContainsKey("TECH") ? enemyData.Stats["TECH"].ToString() : "5";
                    StCOOL.Text = enemyData.Stats.ContainsKey("COOL") ? enemyData.Stats["COOL"].ToString() : "5";
                    StWILL.Text = enemyData.Stats.ContainsKey("WILL") ? enemyData.Stats["WILL"].ToString() : "5";
                    StLUCK.Text = enemyData.Stats.ContainsKey("LUCK") ? enemyData.Stats["LUCK"].ToString() : "5";
                    StMOVE.Text = enemyData.Stats.ContainsKey("MOVE") ? enemyData.Stats["MOVE"].ToString() : "5";
                    StBODY.Text = enemyData.Stats.ContainsKey("BODY") ? enemyData.Stats["BODY"].ToString() : "5";
                    StEMP.Text = enemyData.Stats.ContainsKey("EMP") ? enemyData.Stats["EMP"].ToString() : "5";

                    NewEnemySkills.Clear();
                    if (enemyData.Skills != null)
                    {
                        foreach (var kvp in enemyData.Skills)
                        {
                            NewEnemySkills.Add(new SkillSaveData { Name = kvp.Key, Total = kvp.Value });
                        }
                    }
                }

                NewEnemyCyberware.Clear();
                if (!string.IsNullOrWhiteSpace(enemyData.Cyberware))
                {
                    var parts = enemyData.Cyberware.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        string cleanName = part.Trim();
                        if (!string.IsNullOrWhiteSpace(cleanName))
                        {
                            NewEnemyCyberware.Add(cleanName);
                        }
                    }
                }
            }
        }
        private void BtnRemoveGenSkill_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is SkillSaveData s) NewEnemySkills.Remove(s);
        }

        private void LoadEnemiesFromFolder()
        {
            AvailableEnemies.Clear();
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Enemies");
            if (Directory.Exists(folderPath))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                foreach (string file in Directory.GetFiles(folderPath, "*.json"))
                {
                    try { AvailableEnemies.Add(JsonSerializer.Deserialize<EnemySaveData>(File.ReadAllText(file), options)); } catch { }
                }
            }
        }

        private void BtnSpawnEnemy_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is EnemySaveData enemyData)
            {
                var combatant = new CombatantViewModel
                {
                    OriginalData = enemyData,
                    Name = enemyData.Name,
                    IsPlayer = false,
                    Initiative = 0,
                    CurrentHP = enemyData.HP,
                    MaxHP = enemyData.HP,
                    HeadArmor = enemyData.HeadArmor,
                    BodyArmor = enemyData.BodyArmor,
                    BaseMove = enemyData.BaseMove,
                    BaseDeathSave = enemyData.BaseDeathSave,
                    Weapons = enemyData.Weapons,
                    Gear = enemyData.Gear,
                    Cyberware = enemyData.Cyberware
                };

                combatant.RebuildStatsDisplay();
                Combatants.Add(combatant);

                MessageBox.Show($"{enemyData.Name} добавлен в трекер инициативы!", "УСПЕХ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnSaveNewEnemy_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtGenName.Text))
            {
                MessageBox.Show("Имя врага не может быть пустым!", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var enemy = new EnemySaveData
            {
                Name = TxtGenName.Text.Trim(),
                HP = int.TryParse(TxtGenHP.Text, out int hp) ? hp : 20,
                HeadArmor = int.TryParse(TxtGenHead.Text, out int ha) ? ha : 0,
                BodyArmor = int.TryParse(TxtGenBody.Text, out int ba) ? ba : 0,
                IsCNMode = RbGenCN.IsChecked == true,
                Weapons = TxtGenWeapons.Text,
                Gear = TxtGenGear.Text,
                Cyberware = string.Join(", ", NewEnemyCyberware)
            };

            if (enemy.IsCNMode)
            {
                enemy.CombatNumber = int.TryParse(TxtGenCN.Text, out int cn) ? cn : 12;
                enemy.BaseDeathSave = int.TryParse(TxtGenDeathSaveCN.Text, out int ds) ? ds : 5;
                enemy.BaseMove = int.TryParse(TxtGenMove.Text, out int mv) ? mv : 5;
            }
            else
            {
                enemy.Stats["INT"] = int.Parse(StINT.Text);
                enemy.Stats["REF"] = int.Parse(StREF.Text);
                enemy.Stats["DEX"] = int.Parse(StDEX.Text);
                enemy.Stats["TECH"] = int.Parse(StTECH.Text);
                enemy.Stats["COOL"] = int.Parse(StCOOL.Text);
                enemy.Stats["WILL"] = int.Parse(StWILL.Text);
                enemy.Stats["LUCK"] = int.Parse(StLUCK.Text);
                enemy.Stats["MOVE"] = int.Parse(StMOVE.Text);
                enemy.Stats["BODY"] = int.Parse(StBODY.Text);
                enemy.Stats["EMP"] = int.Parse(StEMP.Text);

                enemy.BaseMove = enemy.Stats["MOVE"];
                enemy.BaseDeathSave = enemy.Stats["BODY"];

                foreach (var sk in NewEnemySkills) enemy.Skills[sk.Name] = sk.Total;
            }

            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Enemies");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string fileName = $"Enemy_{enemy.Name.Replace(" ", "_")}.json";
            string filePath = Path.Combine(folderPath, fileName);
            var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            File.WriteAllText(filePath, JsonSerializer.Serialize(enemy, options), System.Text.Encoding.UTF8);

            var existingItem = AvailableEnemies.FirstOrDefault(x => x.Name.Equals(enemy.Name, StringComparison.OrdinalIgnoreCase));
            if (existingItem != null)
            {
                AvailableEnemies.Remove(existingItem);
            }
            AvailableEnemies.Add(enemy);
            TxtGenName.Text = ""; TxtGenWeapons.Text = ""; TxtGenGear.Text = ""; CbGenCyber.Text = ""; NewEnemySkills.Clear(); NewEnemyCyberware.Clear();

            MessageBox.Show($"Враг '{enemy.Name}' сохранен в базу!", "СОХРАНЕНО", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDeleteEnemyPreset_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is EnemySaveData enemyData)
            {
                var res = MessageBox.Show($"Удалить пресет '{enemyData.Name}' из базы навсегда?", "ПОДТВЕРДИТЕ УДАЛЕНИЕ", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                {
                    string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Enemies");
                    string fileName = $"Enemy_{enemyData.Name.Replace(" ", "_")}.json";
                    string filePath = Path.Combine(folderPath, fileName);

                    if (File.Exists(filePath))
                    {
                        try
                        {
                            File.Delete(filePath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при удалении файла: {ex.Message}", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    AvailableEnemies.Remove(enemyData);
                }
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Закрыть терминал ГМа? Текущий бой будет потерян.", "ВЫХОД", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                MainWindow main = new MainWindow();
                main.Show();
                this.Close();
            }
        }
    }
}