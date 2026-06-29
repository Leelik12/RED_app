using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;

namespace CyberpunkRED_Generator
{
    public class GearRowItem : INotifyPropertyChanged
    {
        private string _name; public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        private string _notes; public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ArmorData : INotifyPropertyChanged
    {
        private string _headSp; public string HeadSp { get => _headSp; set { _headSp = value; OnPropertyChanged(); } }
        private string _headPenalty; public string HeadPenalty { get => _headPenalty; set { _headPenalty = value; OnPropertyChanged(); } }
        private string _bodySp; public string BodySp { get => _bodySp; set { _bodySp = value; OnPropertyChanged(); } }
        private string _bodyPenalty; public string BodyPenalty { get => _bodyPenalty; set { _bodyPenalty = value; OnPropertyChanged(); } }
        private string _shieldSp; public string ShieldSp { get => _shieldSp; set { _shieldSp = value; OnPropertyChanged(); } }
        private string _shieldPenalty; public string ShieldPenalty { get => _shieldPenalty; set { _shieldPenalty = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class WeaponData : INotifyPropertyChanged
    {
        private string _name; public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        private string _damage; public string Damage { get => _damage; set { _damage = value; OnPropertyChanged(); } }
        private string _ammo; public string Ammo { get => _ammo; set { _ammo = value; OnPropertyChanged(); } }
        private string _rof; public string Rof { get => _rof; set { _rof = value; OnPropertyChanged(); } }
        private string _notes; public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class SheetStat : INotifyPropertyChanged
    {
        public string Name { get; set; }

        private int _baseValue;
        public int BaseValue { get => _baseValue; set { _baseValue = value; UpdateCurrentValue(); } }

        private int _armorPenalty;
        public int ArmorPenalty { get => _armorPenalty; set { _armorPenalty = value; UpdateCurrentValue(); } }

        private int _implantModifier;
        public int ImplantModifier { get => _implantModifier; set { _implantModifier = value; UpdateCurrentValue(); } }

        public int Value { get => CurrentValue; set => CurrentValue = value; }

        private int _currentValue;
        public int CurrentValue
        {
            get => _currentValue;
            set
            {
                _currentValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(TooltipText));
            }
        }

        public bool IsFractional { get; set; }
        public bool IsReadOnly { get; set; }

        public string TooltipText => GenerateTooltip();

        private void UpdateCurrentValue()
        {
            if (Name == "УДЧ" || Name == "LUCK" || Name == "ЭМП" || Name == "EMP") return;
            CurrentValue = BaseValue - ArmorPenalty + ImplantModifier;
        }

        private string GenerateTooltip()
        {
            if (Name == "ЭМП" || Name == "EMP") return $"+{BaseValue} базовое\n(Текущая зависит от Человечности)";
            if (Name == "УДЧ" || Name == "LUCK") return $"+{BaseValue} базовое\n(Расходуемый пул)";

            var parts = new List<string> { $"+{BaseValue} базовое" };
            if (ArmorPenalty > 0) parts.Add($"-{ArmorPenalty} штраф за броню");
            if (ImplantModifier > 0) parts.Add($"+{ImplantModifier} имплант");
            else if (ImplantModifier < 0) parts.Add($"{ImplantModifier} имплант");

            return string.Join("\n", parts);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class SheetSkill : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string StatName { get; set; }

        private int _statValue;
        public int StatValue { get => _statValue; set { _statValue = value; OnPropertyChanged(); OnPropertyChanged(nameof(Base)); } }

        private int _level;
        public int Level { get => _level; set { _level = value; OnPropertyChanged(); OnPropertyChanged(nameof(Base)); } }

        private int _modifier;
        public int Modifier { get => _modifier; set { _modifier = value; OnPropertyChanged(); OnPropertyChanged(nameof(Base)); } }

        public int Base => StatValue + Level + Modifier;

        public string Description { get; set; }
        public bool CanAddMultiple { get; set; }
        public bool IsVariant { get; set; }
        public string BaseName { get; set; }

        private string _subName;
        public string SubName { get => _subName; set { _subName = value; OnPropertyChanged(); } }

        public Visibility AddBtnVis => CanAddMultiple ? Visibility.Visible : Visibility.Collapsed;
        public Visibility RemoveBtnVis => IsVariant ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SubNameVis => IsVariant ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BaseNameVis => !IsVariant ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BaseVariantLabelVis => IsVariant ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class SheetSkillCategory
    {
        public string Name { get; set; }
        public ObservableCollection<SheetSkill> Skills { get; set; }
    }

    public class SheetViewModel : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Role { get; set; }

        public List<SheetStat> HexStats { get; set; }
        public Dictionary<string, int> Stats { get; set; }
        public Dictionary<string, int> SystemStats { get; set; }

        private int _currentHumanity;
        public int CurrentHumanity { get => _currentHumanity; set { _currentHumanity = value; OnPropertyChanged(); UpdateEmp(); } }

        private int _maxHumanity;
        public int MaxHumanity { get => _maxHumanity; set { _maxHumanity = value; OnPropertyChanged(); } }

        private string _notes;
        public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }

        // --- НОВЫЕ ПОЛЯ ДЛЯ ВКЛАДКИ 2 ---
        public ObservableCollection<GearRowItem> GearItems { get; set; }

        private string _ammoValue; public string AmmoValue { get => _ammoValue; set { _ammoValue = value; OnPropertyChanged(); } }
        private string _cashValue; public string CashValue { get => _cashValue; set { _cashValue = value; OnPropertyChanged(); } }
        private string _styleNotes; public string StyleNotes { get => _styleNotes; set { _styleNotes = value; OnPropertyChanged(); } }
        private string _housing; public string Housing { get => _housing; set { _housing = value; OnPropertyChanged(); } }
        private string _rent; public string Rent { get => _rent; set { _rent = value; OnPropertyChanged(); } }
        private string _lifestyle; public string Lifestyle { get => _lifestyle; set { _lifestyle = value; OnPropertyChanged(); } }
        // ---------------------------------

        private ArmorData _armor;
        public ArmorData Armor
        {
            get => _armor;
            set
            {
                if (_armor != null) _armor.PropertyChanged -= Armor_PropertyChanged;
                _armor = value;
                if (_armor != null) _armor.PropertyChanged += Armor_PropertyChanged;
                OnPropertyChanged();
                UpdateArmorPenalty();
            }
        }

        public ObservableCollection<WeaponData> Weapons { get; set; }
        public List<SheetSkillCategory> CenterSkillCategories { get; set; }
        public List<SheetSkillCategory> RightSkillCategories1 { get; set; }
        public List<SheetSkillCategory> RightSkillCategories2 { get; set; }

        public Dictionary<string, string> Lifepath { get; set; }
        public Dictionary<string, string> RoleLifepath { get; set; }
        public List<string> Friends { get; set; }
        public List<EnemyData> Enemies { get; set; }
        public List<string> TragicLoves { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void Armor_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != null && e.PropertyName.EndsWith("Penalty")) UpdateArmorPenalty();
        }

        private void UpdateArmorPenalty()
        {
            if (HexStats == null) return;
            int hp = 0, bp = 0, sp = 0;
            string headStr = Armor?.HeadPenalty?.Replace("-", "")?.Trim();
            string bodyStr = Armor?.BodyPenalty?.Replace("-", "")?.Trim();
            string shieldStr = Armor?.ShieldPenalty?.Replace("-", "")?.Trim();

            int.TryParse(headStr, out hp);
            int.TryParse(bodyStr, out bp);
            int.TryParse(shieldStr, out sp);

            int maxPenalty = Math.Max(hp, Math.Max(bp, sp));

            ApplyStatPenalty("РЕА", "REF", maxPenalty);
            ApplyStatPenalty("ЛВК", "DEX", maxPenalty);
            ApplyStatPenalty("СКО", "MOVE", maxPenalty);
        }

        private void ApplyStatPenalty(string nameRu, string nameEn, int penalty)
        {
            var stat = HexStats.FirstOrDefault(s => s.Name == nameRu || s.Name == nameEn);
            if (stat != null)
            {
                stat.ArmorPenalty = penalty;
                var allCats = new List<SheetSkillCategory>();
                if (CenterSkillCategories != null) allCats.AddRange(CenterSkillCategories);
                if (RightSkillCategories1 != null) allCats.AddRange(RightSkillCategories1);
                if (RightSkillCategories2 != null) allCats.AddRange(RightSkillCategories2);

                foreach (var cat in allCats)
                    foreach (var skill in cat.Skills)
                        if (skill.StatName == nameEn || skill.StatName == nameRu) skill.StatValue = stat.CurrentValue;
            }
        }

        private void UpdateEmp()
        {
            if (HexStats == null) return;
            var empStat = HexStats.FirstOrDefault(s => s.Name == "ЭМП" || s.Name == "EMP");
            if (empStat != null)
            {
                empStat.CurrentValue = (int)Math.Floor(_currentHumanity / 10.0);
                var allCats = new List<SheetSkillCategory>();
                if (CenterSkillCategories != null) allCats.AddRange(CenterSkillCategories);
                if (RightSkillCategories1 != null) allCats.AddRange(RightSkillCategories1);
                if (RightSkillCategories2 != null) allCats.AddRange(RightSkillCategories2);

                foreach (var cat in allCats)
                    foreach (var skill in cat.Skills)
                        if (skill.StatName == "EMP" || skill.StatName == "ЭМП") skill.StatValue = empStat.CurrentValue;
            }
        }
    }

    public partial class CharacterSheetWindow : Window
    {
        private string _currentFilePath;
        private CharacterSaveData _originalData;

        public CharacterSheetWindow(string jsonFilePath)
        {
            InitializeComponent();
            LoadCharacterData(jsonFilePath);
        }

        private void LoadCharacterData(string filePath)
        {
            try
            {
                _currentFilePath = filePath;
                string jsonString = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                _originalData = JsonSerializer.Deserialize<CharacterSaveData>(jsonString, options);

                if (_originalData != null)
                {
                    var viewModel = new SheetViewModel
                    {
                        Name = _originalData.Name,
                        Role = _originalData.Role,
                        Stats = _originalData.Stats,
                        SystemStats = _originalData.SystemStats,
                        Lifepath = _originalData.Lifepath,
                        RoleLifepath = _originalData.RoleLifepath,
                        Friends = _originalData.Friends,
                        Enemies = _originalData.Enemies,
                        TragicLoves = _originalData.TragicLoves,
                        Notes = _originalData.Notes ?? "",

                        // Загрузка новых полей
                        StyleNotes = _originalData.StyleNotes ?? "",
                        Housing = _originalData.Housing ?? "",
                        Rent = _originalData.Rent ?? "",
                        Lifestyle = _originalData.Lifestyle ?? "",
                        AmmoValue = _originalData.AmmoValue ?? "",
                        CashValue = _originalData.CashValue ?? "",

                        HexStats = new List<SheetStat>(),
                        CenterSkillCategories = new List<SheetSkillCategory>(),
                        RightSkillCategories1 = new List<SheetSkillCategory>(),
                        RightSkillCategories2 = new List<SheetSkillCategory>()
                    };

                    // Генерация 17 строк инвентаря
                    viewModel.GearItems = new ObservableCollection<GearRowItem>();
                    if (_originalData.GearItems != null)
                    {
                        foreach (var item in _originalData.GearItems) viewModel.GearItems.Add(item);
                    }
                    while (viewModel.GearItems.Count < 17) viewModel.GearItems.Add(new GearRowItem());


                    string[] statOrder = { "ИНТ", "РЕА", "ЛВК", "ТЕХ", "ХАР", "ВОЛЯ", "УДЧ", "СКО", "ТЕЛ", "ЭМП" };
                    foreach (string s in statOrder)
                    {
                        string key = s;
                        switch (s)
                        {
                            case "ИНТ": key = "INT"; break;
                            case "РЕА": key = "REF"; break;
                            case "ЛВК": key = "DEX"; break;
                            case "ТЕХ": key = "TECH"; break;
                            case "ХАР": key = "COOL"; break;
                            case "ВОЛЯ": key = "WILL"; break;
                            case "УДЧ": key = "LUCK"; break;
                            case "СКО": key = "MOVE"; break;
                            case "ТЕЛ": key = "BODY"; break;
                            case "ЭМП": key = "EMP"; break;
                        }

                        int val = _originalData.Stats.ContainsKey(key) ? _originalData.Stats[key] : (_originalData.Stats.ContainsKey(s) ? _originalData.Stats[s] : 5);
                        var stat = new SheetStat { Name = s, BaseValue = val, CurrentValue = val, Value = val };

                        if (s == "УДЧ" || key == "LUCK")
                        {
                            stat.IsFractional = true; stat.IsReadOnly = false;
                            if (_originalData.SystemStats != null && _originalData.SystemStats.ContainsKey("CurrentLuck"))
                                stat.CurrentValue = _originalData.SystemStats["CurrentLuck"];
                        }
                        else if (s == "ЭМП" || key == "EMP")
                        {
                            stat.IsFractional = true; stat.IsReadOnly = true;
                        }
                        viewModel.HexStats.Add(stat);
                    }

                    var col1Structure = new Dictionary<string, string[]> {
                        { "Навыки Восприятия", new[] { "Концентрация", "Скрытие/раскрытие объекта", "Чтение по губам", "Внимательность", "Выслеживание" } },
                        { "Физические Навыки", new[] { "Атлетика", "Акробатика", "Танец", "Выносливость", "Сопр. пыткам/наркотикам", "Скрытность" } },
                        { "Навыки Управления", new[] { "Вождение", "Пилотирование (x2)", "Судоходство", "Верховая езда" } },
                        { "Образовательные Навыки", new[] { "Бухгалтерия", "Обращ. с животными", "Бюрократия", "Бизнес", "Композиция", "Криминология", "Криптография", "Дедукция", "Образование", "Азартные игры" } }
                    };

                    var col2Structure = new Dictionary<string, string[]> {
                        { "Образовательные (прод.)", new[] {  "Язык", "Язык (Уличный Сленг)", "Язык (Родной)", "Знание местности (Твой дом)", "Знание местности", "Поиск информации", "Наука", "Тактика", "Выживание в пустыне" } },
                        { "Ближний бой", new[] { "Рукопашный бой", "Уклонение", "Боевые искусства (x2)", "Оружие ближнего боя" } },
                        { "Сценические Навыки", new[] { "Актерское мастерство", "Игра на инструментах" } },
                        { "Дальний бой", new[] { "Стрельба из лука", "Автоматический огонь (x2)", "Пистолеты" } }
                    };

                    var col3Structure = new Dictionary<string, string[]> {
                        { "Дальний бой (прод.)", new[] { "Оружие кр. калибра (x2)", "Тактическое оружие" } },
                        { "Социальные Навыки", new[] { "Подкуп", "Общение", "Проницательность", "Допрос", "Убеждение", "Уход за собой", "Знаток Улиц", "Торговля", "Гардероб и стиль" } },
                        { "Технические Навыки", new[] { "Авиационные технологии", "Знание техники", "Кибертехника", "Подрывник (x2)", "Электроника/Безопасность (x2)", "Первая помощь", "Фальсификация", "Автомеханика", "Художественное ремесло", "Парамедик (x2)", "Кино- и фототехника", "Взлом замков", "Морские технологии", "Оружейник" } }
                    };

                    viewModel.CenterSkillCategories = BuildColumn(col1Structure, _originalData);
                    viewModel.RightSkillCategories1 = BuildColumn(col2Structure, _originalData);
                    viewModel.RightSkillCategories2 = BuildColumn(col3Structure, _originalData);

                    if (_originalData.SystemStats != null)
                    {
                        int baseEmp = _originalData.Stats.ContainsKey("EMP") ? _originalData.Stats["EMP"] : (_originalData.Stats.ContainsKey("ЭМП") ? _originalData.Stats["ЭМП"] : 5);
                        viewModel.MaxHumanity = _originalData.SystemStats.ContainsKey("MaxHumanity") ? _originalData.SystemStats["MaxHumanity"] : (_originalData.SystemStats.ContainsKey("Humanity") ? _originalData.SystemStats["Humanity"] : baseEmp * 10);
                        viewModel.CurrentHumanity = _originalData.SystemStats.ContainsKey("CurrentHumanity") ? _originalData.SystemStats["CurrentHumanity"] : viewModel.MaxHumanity;
                    }

                    viewModel.Weapons = new ObservableCollection<WeaponData>();
                    if (_originalData.Weapons != null) foreach (var w in _originalData.Weapons) viewModel.Weapons.Add(w);
                    while (viewModel.Weapons.Count < 5) viewModel.Weapons.Add(new WeaponData());

                    viewModel.Armor = _originalData.Armor ?? new ArmorData();
                    this.DataContext = viewModel;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении файла:\n{ex.Message}", "ОШИБКА ДАННЫХ", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private List<SheetSkillCategory> BuildColumn(Dictionary<string, string[]> structure, CharacterSaveData charData)
        {
            var result = new List<SheetSkillCategory>();
            foreach (var kvp in structure)
            {
                var cat = new SheetSkillCategory { Name = kvp.Key, Skills = new ObservableCollection<SheetSkill>() };
                foreach (var skillName in kvp.Value)
                {
                    var def = CoreDataBase.AllSkills.FirstOrDefault(s => s.Name == skillName);
                    if (def == null) continue;

                    cat.Skills.Add(CreateSheetSkill(def, charData));

                    if (def.CanAddMultiple)
                    {
                        var customSkills = charData.Skills.Where(s => s.Name.StartsWith(def.Name + ":")).ToList();
                        foreach (var custom in customSkills)
                        {
                            string subName = custom.Name.Contains(":") ? custom.Name.Split(':')[1].Trim() : custom.Name;
                            cat.Skills.Add(new SheetSkill
                            {
                                Name = custom.Name,
                                BaseName = def.Name,
                                SubName = subName,
                                StatName = cat.Skills.Last().StatName,
                                StatValue = cat.Skills.Last().StatValue,
                                Level = custom.Level,
                                Description = def.Description,
                                CanAddMultiple = false,
                                IsVariant = true
                            });
                        }
                    }
                }
                if (cat.Skills.Count > 0) result.Add(cat);
            }
            return result;
        }

        private SheetSkill CreateSheetSkill(SkillDef def, CharacterSaveData charData)
        {
            int statVal = 5; string statKey = def.Stat;
            switch (def.Stat)
            {
                case "ИНТ": statKey = "INT"; break;
                case "РЕА": statKey = "REF"; break;
                case "ЛВК": statKey = "DEX"; break;
                case "ТЕХ": statKey = "TECH"; break;
                case "ХАР": statKey = "COOL"; break;
                case "ВОЛЯ": statKey = "WILL"; break;
                case "ЭМП": statKey = "EMP"; break;
            }
            if (charData.Stats.ContainsKey(statKey)) statVal = charData.Stats[statKey];

            if (statKey == "EMP")
            {
                int currHum = statVal * 10;
                if (charData.SystemStats != null)
                {
                    if (charData.SystemStats.ContainsKey("CurrentHumanity")) currHum = charData.SystemStats["CurrentHumanity"];
                    else if (charData.SystemStats.ContainsKey("Humanity")) currHum = charData.SystemStats["Humanity"];
                }
                statVal = (int)Math.Floor(currHum / 10.0);
            }

            var savedSkill = charData.Skills.FirstOrDefault(s => s.Name == def.Name);
            int lvl = savedSkill != null ? savedSkill.Level : (def.IsBasic ? 2 : def.FreeLevels);

            return new SheetSkill
            {
                Name = def.Name,
                BaseName = def.Name,
                StatName = statKey,
                StatValue = statVal,
                Level = lvl,
                Description = def.Description,
                CanAddMultiple = def.CanAddMultiple,
                IsVariant = false
            };
        }

        private void BtnAddVariant_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.Button)?.Tag is SheetSkill parentSkill)
            {
                var vm = this.DataContext as SheetViewModel;
                var allCategories = vm.CenterSkillCategories.Concat(vm.RightSkillCategories1).Concat(vm.RightSkillCategories2);
                var category = allCategories.FirstOrDefault(c => c.Skills.Contains(parentSkill));
                if (category != null)
                {
                    int index = category.Skills.IndexOf(parentSkill);
                    var newVariant = new SheetSkill
                    {
                        Name = parentSkill.Name,
                        BaseName = parentSkill.Name,
                        StatName = parentSkill.StatName,
                        StatValue = parentSkill.StatValue,
                        Level = 0,
                        Description = parentSkill.Description,
                        CanAddMultiple = false,
                        IsVariant = true,
                        SubName = "Новый"
                    };
                    category.Skills.Insert(index + 1, newVariant);
                }
            }
        }

        private void BtnRemoveVariant_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.Button)?.Tag is SheetSkill variantSkill)
            {
                var vm = this.DataContext as SheetViewModel;
                var allCategories = vm.CenterSkillCategories.Concat(vm.RightSkillCategories1).Concat(vm.RightSkillCategories2);
                var category = allCategories.FirstOrDefault(c => c.Skills.Contains(variantSkill));
                if (category != null) category.Skills.Remove(variantSkill);
            }
        }

        private void BtnSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_originalData != null && !string.IsNullOrEmpty(_currentFilePath))
                {
                    var vm = this.DataContext as SheetViewModel;

                    if (_originalData.SystemStats == null) _originalData.SystemStats = new Dictionary<string, int>();
                    _originalData.SystemStats["CurrentHumanity"] = vm.CurrentHumanity;
                    _originalData.SystemStats["MaxHumanity"] = vm.MaxHumanity;

                    var luckStat = vm.HexStats.FirstOrDefault(s => s.Name == "УДЧ" || s.Name == "LUCK");
                    if (luckStat != null) _originalData.SystemStats["CurrentLuck"] = luckStat.CurrentValue;

                    _originalData.Notes = vm.Notes;
                    _originalData.Armor = vm.Armor;
                    _originalData.Weapons = vm.Weapons.ToList();

                    // Сохранение новых полей
                    _originalData.StyleNotes = vm.StyleNotes;
                    _originalData.Housing = vm.Housing;
                    _originalData.Rent = vm.Rent;
                    _originalData.Lifestyle = vm.Lifestyle;
                    _originalData.AmmoValue = vm.AmmoValue;
                    _originalData.CashValue = vm.CashValue;
                    _originalData.GearItems = vm.GearItems.ToList();

                    _originalData.Skills.Clear();
                    var allCategories = vm.CenterSkillCategories.Concat(vm.RightSkillCategories1).Concat(vm.RightSkillCategories2);
                    foreach (var cat in allCategories)
                    {
                        foreach (var skill in cat.Skills)
                        {
                            if (skill.CanAddMultiple) continue;
                            if (skill.IsVariant && string.IsNullOrWhiteSpace(skill.SubName)) continue;

                            if (skill.Level > 0 || skill.IsVariant)
                            {
                                string exportName = skill.IsVariant ? $"{skill.BaseName}: {skill.SubName.Trim()}" : skill.Name;
                                _originalData.Skills.Add(new SkillSaveData { Name = exportName, Level = skill.Level, Total = skill.Base });
                            }
                        }
                    }

                    var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                    string newJson = JsonSerializer.Serialize(_originalData, options);
                    File.WriteAllText(_currentFilePath, newJson, System.Text.Encoding.UTF8);

                    MessageBox.Show("Все изменения в Листе Персонажа успешно сохранены!", "ТЕРМИНАЛ CYBERPUNK RED", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Системная ошибка при сохранении: {ex.Message}", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBackToMenu_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}