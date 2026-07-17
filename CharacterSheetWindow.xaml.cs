using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberpunkRED_Generator
{
    public partial class CharacterSheetWindow : Window
    {
        // --- МЕТОДЫ-МОСТЫ ДЛЯ ВАЛИДАЦИИ UI ---
        private void NumberValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
            => UIHelpers.NumberValidationTextBox(sender, e);

        private void NumberValidationTextBoxWithMinus(object sender, System.Windows.Input.TextCompositionEventArgs e)
            => UIHelpers.NumberValidationTextBoxWithMinus(sender, e);

        private void NumberLimit777_TextChanged(object sender, TextChangedEventArgs e)
            => UIHelpers.NumberLimit777_TextChanged(sender, e);

        private void NumberLimitFromMinus777To777_TextChanged(object sender, TextChangedEventArgs e)
            => UIHelpers.NumberLimitFromMinus777To777_TextChanged(sender, e);


        private string _currentFilePath;
        private CharacterSaveData _originalData;

        private void CurrentHumanity_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && int.TryParse(tb.Text, out int val))
            {
                if (this.DataContext is SheetViewModel vm)
                {
                    if (val > vm.MaxHumanity)
                    {
                        tb.Text = vm.MaxHumanity.ToString();
                        tb.SelectionStart = tb.Text.Length;
                    }
                    else if (val < -777)
                    {
                        tb.Text = "-777";
                        tb.SelectionStart = tb.Text.Length;
                    }
                }
            }
        }

        private void MaxHumanity_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && int.TryParse(tb.Text, out int val))
            {
                if (this.DataContext is SheetViewModel vm)
                {
                    int baseEmp = vm.Stats != null && vm.Stats.ContainsKey("EMP") ? vm.Stats["EMP"] : (vm.Stats != null && vm.Stats.ContainsKey("ЭМП") ? vm.Stats["ЭМП"] : 5);
                    int limit = baseEmp * 10;

                    if (val > limit)
                    {
                        tb.Text = limit.ToString();
                        tb.SelectionStart = tb.Text.Length;
                    }
                }
            }
        }

        private void CurrentHP_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && int.TryParse(tb.Text, out int val))
            {
                if (this.DataContext is SheetViewModel vm)
                {
                    if (val > vm.MaxHP)
                    {
                        tb.Text = vm.MaxHP.ToString();
                        tb.SelectionStart = tb.Text.Length;
                    }
                }
            }
        }

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
                        Lifepath = new ObservableCollection<LifepathItem>(_originalData.Lifepath.Select(kvp => new LifepathItem { Key = kvp.Key, Value = kvp.Value })),
                        RoleLifepath = new ObservableCollection<LifepathItem>(_originalData.RoleLifepath.Select(kvp => new LifepathItem { Key = kvp.Key, Value = kvp.Value })),
                        Friends = new ObservableCollection<StringItem>(_originalData.Friends.Select(x => new StringItem { Value = x })),
                        TragicLoves = new ObservableCollection<StringItem>(_originalData.TragicLoves.Select(x => new StringItem { Value = x })),
                        Enemies = _originalData.Enemies,
                        Notes = _originalData.Notes ?? "",
                        RoleAbilityNotes = _originalData.RoleAbilityNotes ?? "",

                        ReputationEvents = _originalData.ReputationEvents ?? "",
                        StyleNotes = _originalData.StyleNotes ?? "",
                        Housing = _originalData.Housing ?? "",
                        Rent = _originalData.Rent ?? "",
                        Lifestyle = _originalData.Lifestyle ?? "",
                        AmmoValue = _originalData.AmmoValue ?? "",
                        CashValue = _originalData.CashValue ?? "",

                        HexStats = new List<SheetStat>(),
                        CenterSkillCategories = new List<SheetSkillCategory>(),
                        RightSkillCategories1 = new List<SheetSkillCategory>(),
                        RightSkillCategories2 = new List<SheetSkillCategory>(),

                        Addictions = _originalData.Addictions ?? ""
                    };

                    viewModel.RoleRank = _originalData.SystemStats != null && _originalData.SystemStats.ContainsKey("RoleRank") ? _originalData.SystemStats["RoleRank"] : 4;

                    if (_originalData.SystemStats != null)
                    {
                        viewModel.SoloThreat = _originalData.SystemStats.ContainsKey("SoloThreat") ? _originalData.SystemStats["SoloThreat"] : 0;
                        viewModel.SoloAwareness = _originalData.SystemStats.ContainsKey("SoloAwareness") ? _originalData.SystemStats["SoloAwareness"] : 0;
                        viewModel.SoloDeflection = _originalData.SystemStats.ContainsKey("SoloDeflection") ? _originalData.SystemStats["SoloDeflection"] : 0;
                        viewModel.SoloFumble = _originalData.SystemStats.ContainsKey("SoloFumble") ? _originalData.SystemStats["SoloFumble"] : 0;
                        viewModel.SoloInitiative = _originalData.SystemStats.ContainsKey("SoloInitiative") ? _originalData.SystemStats["SoloInitiative"] : 0;
                        viewModel.SoloPrecision = _originalData.SystemStats.ContainsKey("SoloPrecision") ? _originalData.SystemStats["SoloPrecision"] : 0;

                        viewModel.TechField = _originalData.SystemStats.ContainsKey("TechField") ? _originalData.SystemStats["TechField"] : 0;
                        viewModel.TechUpgrade = _originalData.SystemStats.ContainsKey("TechUpgrade") ? _originalData.SystemStats["TechUpgrade"] : 0;
                        viewModel.TechFab = _originalData.SystemStats.ContainsKey("TechFab") ? _originalData.SystemStats["TechFab"] : 0;
                        viewModel.TechInvent = _originalData.SystemStats.ContainsKey("TechInvent") ? _originalData.SystemStats["TechInvent"] : 0;

                        viewModel.MedSurgery = _originalData.SystemStats.ContainsKey("MedSurgery") ? _originalData.SystemStats["MedSurgery"] : 0;
                        viewModel.MedPharma = _originalData.SystemStats.ContainsKey("MedPharma") ? _originalData.SystemStats["MedPharma"] : 0;
                        viewModel.MedCryo = _originalData.SystemStats.ContainsKey("MedCryo") ? _originalData.SystemStats["MedCryo"] : 0;
                    }

                    viewModel.MaxHP = _originalData.SystemStats != null && _originalData.SystemStats.ContainsKey("HP") ? _originalData.SystemStats["HP"] : 40;
                    viewModel.CurrentHP = _originalData.SystemStats != null && _originalData.SystemStats.ContainsKey("CurrentHP") ? _originalData.SystemStats["CurrentHP"] : viewModel.MaxHP;
                    viewModel.BaseDeathSave = _originalData.SystemStats != null && _originalData.SystemStats.ContainsKey("DeathSave") ? _originalData.SystemStats["DeathSave"] : 0;
                    viewModel.ImprovementPoints = _originalData.SystemStats != null && _originalData.SystemStats.ContainsKey("ImprovementPoints") ? _originalData.SystemStats["ImprovementPoints"] : 0;
                    viewModel.Reputation = _originalData.SystemStats != null && _originalData.SystemStats.ContainsKey("Reputation") ? _originalData.SystemStats["Reputation"] : 0;

                    viewModel.CriticalInjuriesList = new ObservableCollection<CriticalInjuryItem>();
                    if (_originalData.CriticalInjuriesList != null)
                    {
                        foreach (var injName in _originalData.CriticalInjuriesList)
                        {
                            var def = CoreDataBase.CriticalInjuries.FirstOrDefault(x => x.Name == injName);
                            if (def != null)
                            {
                                viewModel.CriticalInjuriesList.Add(new CriticalInjuryItem
                                {
                                    Name = def.Name,
                                    Description = def.Description,
                                    QuickFix = def.QuickFix,
                                    Treatment = def.Treatment,
                                    AllActionsPenalty = def.AllActionsPenalty,
                                    MovePenalty = def.MovePenalty,
                                    DeathSavePenalty = def.DeathSavePenalty,
                                    EffectText = def.EffectText,
                                    SkillModifiers = def.SkillModifiers
                                });
                            }
                        }
                    }
                    if (viewModel.AvailableInjuries.Count > 0) viewModel.SelectedInjuryToAdd = viewModel.AvailableInjuries[0];

                    viewModel.GearItems = new ObservableCollection<GearRowItem>();
                    if (_originalData.GearItems != null) foreach (var item in _originalData.GearItems) viewModel.GearItems.Add(item);
                    while (viewModel.GearItems.Count < 17) viewModel.GearItems.Add(new GearRowItem());

                    viewModel.CyberwareBlocks = new ObservableCollection<CyberwareBlockItem>();
                    if (_originalData.CyberwareBlocks != null && _originalData.CyberwareBlocks.Count > 0)
                    {
                        foreach (var b in _originalData.CyberwareBlocks) viewModel.CyberwareBlocks.Add(b);
                    }
                    else
                    {
                        string[] names = {
                            "НЕЙРОИНТЕРФЕЙС (NEURALWARE)", "КИБЕРАУДИО (CYBERAUDIO)", "ВНУТРЕННИЕ ИМПЛАНТЫ (INTERNAL)",
                            "ПРАВЫЙ КИБЕРГЛАЗ (CYBEROPTIC R)", "ЛЕВЫЙ КИБЕРГЛАЗ (CYBEROPTIC L)", "ВНЕШНИЕ ИМПЛАНТЫ (EXTERNAL)",
                            "ПРАВАЯ КИБЕРРУКА (CYBERARM R)", "ЛЕВАЯ КИБЕРРУКА (CYBERARM L)", "СТИЛЕВОЙ КИБЕРИМПЛАНТ (FASHIONWARE)",
                            "ПРАВАЯ КИБЕРНОГА (CYBERLEG R)", "ЛЕВАЯ КИБЕРНОГА (CYBERLEG L)", "БОРГИРОВАНИЕ (BORGWARE)"
                        };
                        int[] slots = { 5, 3, 7, 3, 3, 7, 4, 4, 7, 3, 3, 7 };

                        for (int i = 0; i < names.Length; i++)
                            viewModel.CyberwareBlocks.Add(new CyberwareBlockItem { Name = names[i], MaxSlots = slots[i], UsedSlots = 0, OptionsText = "" });
                    }

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

                        stat.PropertyChanged += (sender, args) =>
                        {
                            if (args.PropertyName == nameof(SheetStat.CurrentValue))
                            {
                                viewModel.UpdateDerivedCombatStats();
                            }
                        };

                        if (s == "УДЧ" || key == "LUCK")
                        {
                            stat.IsFractional = true; stat.IsReadOnly = false;
                            if (_originalData.SystemStats != null && _originalData.SystemStats.ContainsKey("CurrentLuck")) stat.CurrentValue = _originalData.SystemStats["CurrentLuck"];
                        }
                        else if (s == "ЭМП" || key == "EMP") { stat.IsFractional = true; stat.IsReadOnly = true; }
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
                        { "Дальний бой", new[] { "Стрельба из лука", "Автоматический огонь (x2)", "Пистолеты",  "Оружие кр. калибра (x2)", "Тактическое оружие"} }
                    };

                    var col3Structure = new Dictionary<string, string[]> {
                        { "Социальные Навыки", new[] { "Подкуп", "Общение", "Проницательность", "Допрос", "Убеждение", "Уход за собой", "Знаток Улиц", "Торговля", "Гардероб и стиль" } },
                        { "Технические Навыки", new[] { "Авиационные технологии", "Знание техники", "Кибертехника", "Подрывник (x2)", "Электроника/Безопасность (x2)", "Первая помощь", "Фальсификация", "Автомеханика", "Художественное ремесло", "Парамедик (x2)", "Кино- и фототехника", "Взлом замков", "Морские технологии", "Оружейник"} }
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

                    viewModel.RecalculatePenalties();

                    // первичный расчет при загрузке файла персонажа
                    viewModel.UpdateDerivedCombatStats();

                    this.DataContext = viewModel;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении файла:\n{ex.Message}", "ОШИБКА ДАННЫХ", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }
        private void BtnAddGear_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as SheetViewModel;
            if (vm != null)
            {
                vm.GearItems.Add(new GearRowItem { Name = "", Notes = "" });
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

            var savedSkill = charData.Skills.FirstOrDefault(s => s.Name == def.Name || (def.Name == "Язык (Родной)" && s.Name.StartsWith("Язык (Родной):")));
            int lvl = savedSkill != null ? savedSkill.Level : (def.IsBasic ? 2 : def.FreeLevels);

            // Вытаскиваем вписанный язык
            string subName = "";
            if (def.Name == "Язык (Родной)" && savedSkill != null && savedSkill.Name.Contains(":"))
            {
                subName = savedSkill.Name.Split(':')[1].Trim();
            }

            return new SheetSkill
            {
                Name = def.Name,
                BaseName = def.Name,
                StatName = statKey,
                StatValue = statVal,
                Level = lvl,
                Description = def.Description,
                CanAddMultiple = def.CanAddMultiple,
                IsVariant = false,
                SubName = subName
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

                    vm.RecalculatePenalties();
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

        private void BtnAddInjury_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as SheetViewModel;
            if (vm != null && !string.IsNullOrEmpty(vm.SelectedInjuryToAdd))
            {
                var def = CoreDataBase.CriticalInjuries.FirstOrDefault(x => x.Name == vm.SelectedInjuryToAdd);
                if (def != null)
                {
                    vm.CriticalInjuriesList.Add(new CriticalInjuryItem
                    {
                        Name = def.Name,
                        Description = def.Description,
                        QuickFix = def.QuickFix,
                        Treatment = def.Treatment,
                        AllActionsPenalty = def.AllActionsPenalty,
                        MovePenalty = def.MovePenalty,
                        DeathSavePenalty = def.DeathSavePenalty,
                        EffectText = def.EffectText,
                        SkillModifiers = def.SkillModifiers 
                    });
                    vm.RecalculatePenalties();
                }
            }
        }

        private void BtnRemoveInjury_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var injury = btn?.DataContext as CriticalInjuryItem;
            var vm = this.DataContext as SheetViewModel;
            if (injury != null && vm != null)
            {
                vm.CriticalInjuriesList.Remove(injury);
                vm.RecalculatePenalties();
            }
        }

        private void RoleRank_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && int.TryParse(tb.Text, out int val))
            {
                if (val > 777)
                {
                    tb.Text = "777";
                    tb.SelectionStart = tb.Text.Length;
                }
            }
        }

        private void BtnRolePlus_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as SheetViewModel;
            string tag = (sender as Button)?.Tag.ToString();

            // Логика Соло
            if (tag == "Threat" || tag == "Awareness" || tag == "Deflection" || tag == "Fumble" || tag == "Initiative" || tag == "Precision")
            {
                int cost = tag == "Deflection" ? 2 : (tag == "Precision" ? 3 : 1);
                if (vm.RoleRank - vm.SoloPointsUsed >= cost)
                {
                    if (tag == "Threat") vm.SoloThreat++;
                    else if (tag == "Awareness") vm.SoloAwareness++;
                    else if (tag == "Deflection") vm.SoloDeflection++;
                    else if (tag == "Fumble") vm.SoloFumble++;
                    else if (tag == "Initiative") vm.SoloInitiative++;
                    else if (tag == "Precision") vm.SoloPrecision++;
                }
            }
            // Логика Техника
            else if (tag == "TechField" || tag == "TechUpgrade" || tag == "TechFab" || tag == "TechInvent")
            {
                if (vm.RoleRank - vm.TechPointsUsed >= 1)
                {
                    if (tag == "TechField") vm.TechField++;
                    else if (tag == "TechUpgrade") vm.TechUpgrade++;
                    else if (tag == "TechFab") vm.TechFab++;
                    else if (tag == "TechInvent") vm.TechInvent++;
                }
            }
            // Логика Медтехника
            else if (tag == "MedSurgery" || tag == "MedPharma" || tag == "MedCryo")
            {
                if (vm.RoleRank - vm.MedtechPointsUsed >= 1)
                {
                    if (tag == "MedSurgery" && vm.MedSurgery < 5) vm.MedSurgery++;
                    else if (tag == "MedPharma" && vm.MedPharma < 5) vm.MedPharma++;
                    else if (tag == "MedCryo" && vm.MedCryo < 5) vm.MedCryo++;
                }
            }
        }

        private void BtnRoleMinus_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as SheetViewModel;
            string tag = (sender as Button)?.Tag.ToString();

            if (tag == "Threat" && vm.SoloThreat > 0) vm.SoloThreat--;
            else if (tag == "Awareness" && vm.SoloAwareness > 0) vm.SoloAwareness--;
            else if (tag == "Deflection" && vm.SoloDeflection > 0) vm.SoloDeflection--;
            else if (tag == "Fumble" && vm.SoloFumble > 0) vm.SoloFumble--;
            else if (tag == "Initiative" && vm.SoloInitiative > 0) vm.SoloInitiative--;
            else if (tag == "Precision" && vm.SoloPrecision > 0) vm.SoloPrecision--;

            else if (tag == "TechField" && vm.TechField > 0) vm.TechField--;
            else if (tag == "TechUpgrade" && vm.TechUpgrade > 0) vm.TechUpgrade--;
            else if (tag == "TechFab" && vm.TechFab > 0) vm.TechFab--;
            else if (tag == "TechInvent" && vm.TechInvent > 0) vm.TechInvent--;

            else if (tag == "MedSurgery" && vm.MedSurgery > 0) vm.MedSurgery--;
            else if (tag == "MedPharma" && vm.MedPharma > 0) vm.MedPharma--;
            else if (tag == "MedCryo" && vm.MedCryo > 0) vm.MedCryo--;
        }

        private bool SaveData()
        {
            try
            {
                if (_originalData != null && !string.IsNullOrEmpty(_currentFilePath))
                {
                    var vm = this.DataContext as SheetViewModel;

                    _originalData.Name = vm.Name;

                    if (_originalData.SystemStats == null) _originalData.SystemStats = new Dictionary<string, int>();
                    _originalData.SystemStats["CurrentHumanity"] = vm.CurrentHumanity;
                    _originalData.SystemStats["MaxHumanity"] = vm.MaxHumanity;
                    _originalData.SystemStats["HP"] = vm.MaxHP;
                    _originalData.SystemStats["CurrentHP"] = vm.CurrentHP;
                    _originalData.SystemStats["DeathSave"] = vm.BaseDeathSave;

                    _originalData.SystemStats["ImprovementPoints"] = vm.ImprovementPoints;
                    _originalData.SystemStats["Reputation"] = vm.Reputation;
                    _originalData.ReputationEvents = vm.ReputationEvents; // Добавь свойство ReputationEvents в класс CharacterSaveData, если его там нет!

                    _originalData.SystemStats["RoleRank"] = vm.RoleRank;
                    _originalData.SystemStats["SoloThreat"] = vm.SoloThreat;
                    _originalData.SystemStats["SoloAwareness"] = vm.SoloAwareness;
                    _originalData.SystemStats["SoloDeflection"] = vm.SoloDeflection;
                    _originalData.SystemStats["SoloFumble"] = vm.SoloFumble;
                    _originalData.SystemStats["SoloInitiative"] = vm.SoloInitiative;
                    _originalData.SystemStats["SoloPrecision"] = vm.SoloPrecision;

                    _originalData.SystemStats["TechField"] = vm.TechField;
                    _originalData.SystemStats["TechUpgrade"] = vm.TechUpgrade;
                    _originalData.SystemStats["TechFab"] = vm.TechFab;
                    _originalData.SystemStats["TechInvent"] = vm.TechInvent;

                    _originalData.SystemStats["MedSurgery"] = vm.MedSurgery;
                    _originalData.SystemStats["MedPharma"] = vm.MedPharma;
                    _originalData.SystemStats["MedCryo"] = vm.MedCryo;

                    var luckStat = vm.HexStats.FirstOrDefault(s => s.Name == "УДЧ" || s.Name == "LUCK");
                    if (luckStat != null) _originalData.SystemStats["CurrentLuck"] = luckStat.CurrentValue;

                    _originalData.Notes = vm.Notes;
                    _originalData.RoleAbilityNotes = vm.RoleAbilityNotes;
                    _originalData.Armor = vm.Armor;
                    _originalData.Weapons = vm.Weapons.ToList();

                    _originalData.StyleNotes = vm.StyleNotes;
                    _originalData.Housing = vm.Housing;
                    _originalData.Rent = vm.Rent;
                    _originalData.Lifestyle = vm.Lifestyle;
                    _originalData.AmmoValue = vm.AmmoValue;
                    _originalData.CashValue = vm.CashValue;
                    _originalData.GearItems = vm.GearItems.ToList();

                    _originalData.CyberwareBlocks = vm.CyberwareBlocks.ToList();

                    _originalData.CriticalInjuriesList = vm.CriticalInjuriesList.Select(x => x.Name).ToList();
                    _originalData.Addictions = vm.Addictions;

                    _originalData.Friends = vm.Friends.Select(x => x.Value).ToList();
                    _originalData.TragicLoves = vm.TragicLoves.Select(x => x.Value).ToList();
                    _originalData.Lifepath = vm.Lifepath.ToDictionary(x => x.Key, x => x.Value ?? "");
                    _originalData.RoleLifepath = vm.RoleLifepath.ToDictionary(x => x.Key, x => x.Value ?? "");

                    _originalData.Skills.Clear();
                    var allCategories = vm.CenterSkillCategories.Concat(vm.RightSkillCategories1).Concat(vm.RightSkillCategories2);
                    foreach (var cat in allCategories)
                    {
                        foreach (var skill in cat.Skills)
                        {
                            if (skill.CanAddMultiple) continue;
                            if (skill.IsVariant && string.IsNullOrWhiteSpace(skill.SubName)) continue;

                            if (skill.Level > 0 || skill.IsVariant || skill.BaseName == "Язык (Родной)")
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

                                _originalData.Skills.Add(new SkillSaveData { Name = exportName, Level = skill.Level, Total = skill.Base });
                            }
                        }
                    }

                    var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                    string newJson = JsonSerializer.Serialize(_originalData, options);
                    File.WriteAllText(_currentFilePath, newJson, System.Text.Encoding.UTF8);

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Системная ошибка при сохранении: {ex.Message}", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void BtnSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (SaveData())
            {
                MessageBox.Show("Все изменения в Листе Персонажа успешно сохранены!", "ТЕРМИНАЛ CYBERPUNK RED", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool _isConfirmedClose = false;

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_isConfirmedClose)
            {
                var res = MessageBox.Show("Хотите сохранить изменения перед выходом?", "СОХРАНЕНИЕ", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (res == MessageBoxResult.Cancel)
                {
                    e.Cancel = true; // передумал
                    return;
                }

                if (res == MessageBoxResult.Yes)
                {
                    if (!SaveData())
                    {
                        e.Cancel = true; // ошибка отменяем выход
                        return;
                    }
                }

                _isConfirmedClose = true;
                MainWindow mainMenu = new MainWindow();
                mainMenu.Show();
            }

            base.OnClosing(e);
        }

        private void BtnBackToMenu_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnOpenCyberwareManager_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is CyberwareBlockItem block)
            {
                var vm = this.DataContext as SheetViewModel;
                vm.CurrentCyberBlock = block;
                vm.OverlayTitle = $"УПРАВЛЕНИЕ: {block.Name}";
                vm.AvailableCyberware.Clear();

                // "Алиасы" для боргирования. Если это дополнительная конечность, ищем в базе стандартную
                string searchCat = block.Name;
                if (searchCat.Contains("ДОП. ПРАВАЯ КИБЕРРУКА")) searchCat = "ПРАВАЯ КИБЕРРУКА (CYBERARM R)";
                else if (searchCat.Contains("ДОП. ЛЕВАЯ КИБЕРРУКА")) searchCat = "ЛЕВАЯ КИБЕРРУКА (CYBERARM L)";
                else if (searchCat.Contains("ДОП. КИБЕРГЛАЗ")) searchCat = "ПРАВЫЙ КИБЕРГЛАЗ (CYBEROPTIC R)"; // Список опций для левого и правого глаза одинаков

                // Загружаем стандартные импланты по нашему алиасу
                foreach (var def in CoreDataBase.AllCyberware.Where(c => c.Category == searchCat))
                {
                    vm.AvailableCyberware.Add(def);
                }

                // Загружаем кастомные импланты (как созданные для базы, так и созданные конкретно в этом доп. блоке)
                if (_originalData.CustomCyberwareList != null)
                {
                    foreach (var def in _originalData.CustomCyberwareList.Where(c => c.Category == block.Name || c.Category == searchCat))
                    {
                        vm.AvailableCyberware.Add(def);
                    }
                }

                CyberwareOverlay.Visibility = Visibility.Visible;
            }
        }

        private void BtnCloseCyberwareManager_Click(object sender, RoutedEventArgs e) => CyberwareOverlay.Visibility = Visibility.Collapsed;

        private string GetTwinCategory(string cat)
        {
            // Обычные парные
            if (cat.Contains("ПРАВЫЙ КИБЕРГЛАЗ")) return "ЛЕВЫЙ КИБЕРГЛАЗ (CYBEROPTIC L)";
            if (cat.Contains("ЛЕВЫЙ КИБЕРГЛАЗ")) return "ПРАВЫЙ КИБЕРГЛАЗ (CYBEROPTIC R)";
            if (cat.Contains("ПРАВАЯ КИБЕРРУКА")) return "ЛЕВАЯ КИБЕРРУКА (CYBERARM L)";
            if (cat.Contains("ЛЕВАЯ КИБЕРРУКА")) return "ПРАВАЯ КИБЕРРУКА (CYBERARM R)";
            if (cat.Contains("ПРАВАЯ КИБЕРНОГА")) return "ЛЕВАЯ КИБЕРНОГА (CYBERLEG L)";
            if (cat.Contains("ЛЕВАЯ КИБЕРНОГА")) return "ПРАВАЯ КИБЕРНОГА (CYBERLEG R)";

            // Борговские дополнительные парные руки
            if (cat.Contains("ДОП. ПРАВАЯ КИБЕРРУКА")) return "ДОП. ЛЕВАЯ КИБЕРРУКА (EXTRA ARM L)";
            if (cat.Contains("ДОП. ЛЕВАЯ КИБЕРРУКА")) return "ДОП. ПРАВАЯ КИБЕРРУКА (EXTRA ARM R)";

            // Борговские дополнительные глаза (линкуем 1-ый со 2-ым)
            if (cat.Contains("ДОП. КИБЕРГЛАЗ 1")) return "ДОП. КИБЕРГЛАЗ 2 (EXTRA OPTIC 2)";
            if (cat.Contains("ДОП. КИБЕРГЛАЗ 2")) return "ДОП. КИБЕРГЛАЗ 1 (EXTRA OPTIC 1)";
            if (cat.Contains("ДОП. КИБЕРГЛАЗ 3")) return "ЛЕВЫЙ КИБЕРГЛАЗ (CYBEROPTIC L)";

            return "";
        }
        private void BtnInstallCyber_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as SheetViewModel;
            if (vm.CurrentCyberBlock == null) return;

            if ((sender as Button)?.Tag is CyberwareDef def)
            {
                // Проверка на ускорители
                if (def.Name == "Сандевистан" || def.Name == "Керензиков")
                {
                    bool hasSpeedware = vm.CyberwareBlocks.Any(b => b.InstalledItems.Any(i => i.Name == "Сандевистан" || i.Name == "Керензиков"));
                    if (hasSpeedware)
                    {
                        MessageBox.Show("У персонажа может быть установлен только один имплант-ускоритель (Керензиков ИЛИ Сандевистан) одновременно!", "ОШИБКА СОВМЕСТИМОСТИ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // ЗАЩИТА: Базовые импланты (Foundation) можно ставить только 1 раз в один блок
                if (def.IsFoundation)
                {
                    bool alreadyHasFoundation = vm.CurrentCyberBlock.InstalledItems.Any(i => i.IsFoundation && i.Name == def.Name);
                    if (alreadyHasFoundation)
                    {
                        MessageBox.Show($"Базовый имплант '{def.Name}' уже установлен в этом блоке! Вы не можете установить его дважды.", "ОШИБКА УСТАНОВКИ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Поиск зависимостей по всему телу
                if (!string.IsNullOrWhiteSpace(def.Requires))
                {
                    bool hasRequirement = vm.CyberwareBlocks.Any(b => b.InstalledItems.Any(i => i.Name == def.Requires));
                    if (!hasRequirement)
                    {
                        MessageBox.Show($"Для установки этого импланта сначала требуется установить базовый: {def.Requires}!", "ОШИБКА СОВМЕСТИМОСТИ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Уникальные требования
                if (def.Name == "Подкожный захват" && !vm.CyberwareBlocks.Any(b => b.InstalledItems.Any(i => i.Name == "Нейролинк")))
                {
                    MessageBox.Show("Для Подкожного захвата дополнительно требуется установленный Нейролинк!", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (def.Name == "Эндоскелет ∑ (Сигма)" && vm.CyberwareBlocks.Sum(b => b.InstalledItems.Count(i => i.Name == "Искусственные мышцы и усиленные кости")) < 1)
                {
                    MessageBox.Show("Для установки Эндоскелета Сигма требуется минимум 1 имплант 'Искусственные мышцы и усиленные кости'!", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (def.Name == "Эндоскелет ß (Бета)" && vm.CyberwareBlocks.Sum(b => b.InstalledItems.Count(i => i.Name == "Искусственные мышцы и усиленные кости")) < 2)
                {
                    MessageBox.Show("Для установки Эндоскелета Бета требуется минимум 2 импланта 'Искусственные мышцы и усиленные кости'!", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                CyberwareBlockItem twinBlock = null;

                // Проверка сопряжения (для парных имплантов)
                if (def.IsPaired)
                {
                    string twinName = GetTwinCategory(vm.CurrentCyberBlock.Name);
                    if (!string.IsNullOrEmpty(twinName))
                    {
                        twinBlock = vm.CyberwareBlocks.FirstOrDefault(b => b.Name == twinName);
                        if (twinBlock == null || !twinBlock.IsInstalled)
                        {
                            MessageBox.Show($"Этот имплант СОПРЯЖЕННЫЙ. Вы должны установить базовый имплант в парную конечность ({twinName}) перед его покупкой!", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        if (twinBlock.UsedSlots + def.Slots > twinBlock.MaxSlots)
                        {
                            MessageBox.Show($"В парной конечности ({twinName}) не хватает слотов для установки дубликата!", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                }

                // Установка в основной слот
                if (vm.CurrentCyberBlock.UsedSlots + def.Slots > vm.CurrentCyberBlock.MaxSlots)
                {
                    MessageBox.Show("В этом блоке не осталось свободных слотов!", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Запрос ПЧ
                // Запрос ПЧ
                int parsedHL = 0;
                if (def.HumanityLoss != "0" && def.HumanityLoss != "0 (—)")
                {
                    string promptInfo = def.IsPaired ? $"{def.HumanityLoss} (бросайте дважды за парный)" : def.HumanityLoss;

                    Window prompt = new Window
                    {
                        Title = "ПОТЕРЯ ЧЕЛОВЕЧНОСТИ",
                        Width = 350,
                        SizeToContent = SizeToContent.Height, // <-- Вот она, магия гибкой высоты
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize,
                        WindowStyle = WindowStyle.ToolWindow,
                        Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 10, 10)),
                        Topmost = true
                    };

                    var grid = new Grid { Margin = new Thickness(15) };
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var tbInfo = new TextBlock { Text = $"Установка: {def.Name}\nСделайте бросок кубиков: {promptInfo}\nВведите итоговый результат потери:", Foreground = System.Windows.Media.Brushes.White, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 15) };
                    Grid.SetRow(tbInfo, 0);

                    var inputTb = new TextBox { Background = System.Windows.Media.Brushes.Black, Foreground = System.Windows.Media.Brushes.Cyan, FontSize = 20, HorizontalContentAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 15) };
                    Grid.SetRow(inputTb, 1);

                    var btnOk = new Button { Content = "ПОДТВЕРДИТЬ УСТАНОВКУ", Background = System.Windows.Media.Brushes.Red, Foreground = System.Windows.Media.Brushes.Black, FontWeight = FontWeights.Bold, Height = 30, Cursor = System.Windows.Input.Cursors.Hand };
                    Grid.SetRow(btnOk, 2);

                    btnOk.Click += (s, ev) =>
                    {
                        if (int.TryParse(inputTb.Text, out int res)) { parsedHL = res; prompt.DialogResult = true; }
                        else { MessageBox.Show("Введите число!"); }
                    };

                    grid.Children.Add(tbInfo); grid.Children.Add(inputTb); grid.Children.Add(btnOk);
                    prompt.Content = grid;

                    bool? result = prompt.ShowDialog();
                    if (result != true) return;

                    vm.CurrentHumanity -= parsedHL;
                }

                vm.CurrentCyberBlock.InstalledItems.Add(def);
                vm.CurrentCyberBlock.UsedSlots += def.Slots;

                if (def.IsPaired && twinBlock != null)
                {
                    if (!twinBlock.InstalledItems.Any(i => i.Name == def.Name))
                    {
                        twinBlock.InstalledItems.Add(def);
                        twinBlock.UsedSlots += def.Slots;
                    }
                }

                // ЛОГИКА БОРГИРОВАНИЯ (Добавление новых конечностей)
                if (def.Name == "Искусственное плечевое крепление")
                {
                    if (!vm.CyberwareBlocks.Any(b => b.Name == "ДОП. ПРАВАЯ КИБЕРРУКА (EXTRA ARM R)"))
                    {
                        vm.CyberwareBlocks.Add(new CyberwareBlockItem { Name = "ДОП. ПРАВАЯ КИБЕРРУКА (EXTRA ARM R)", MaxSlots = 4, UsedSlots = 0, OptionsText = "" });
                        vm.CyberwareBlocks.Add(new CyberwareBlockItem { Name = "ДОП. ЛЕВАЯ КИБЕРРУКА (EXTRA ARM L)", MaxSlots = 4, UsedSlots = 0, OptionsText = "" });
                        MessageBox.Show("Плечевое крепление установлено! В ваш лист добавлены слоты для дополнительных киберрук.", "БОРГИРОВАНИЕ АКТИВИРОВАНО", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else if (def.Name == "Фасеточное крепление")
                {
                    if (!vm.CyberwareBlocks.Any(b => b.Name == "ДОП. КИБЕРГЛАЗ 1 (EXTRA OPTIC 1)"))
                    {
                        vm.CyberwareBlocks.Add(new CyberwareBlockItem { Name = "ДОП. КИБЕРГЛАЗ 1 (EXTRA OPTIC 1)", MaxSlots = 3, UsedSlots = 0, OptionsText = "" });
                        vm.CyberwareBlocks.Add(new CyberwareBlockItem { Name = "ДОП. КИБЕРГЛАЗ 2 (EXTRA OPTIC 2)", MaxSlots = 3, UsedSlots = 0, OptionsText = "" });
                        vm.CyberwareBlocks.Add(new CyberwareBlockItem { Name = "ДОП. КИБЕРГЛАЗ 3 (EXTRA OPTIC 3)", MaxSlots = 3, UsedSlots = 0, OptionsText = "" });
                        MessageBox.Show("Фасеточное крепление установлено! В ваш лист добавлены слоты для дополнительных киберглаз.", "БОРГИРОВАНИЕ АКТИВИРОВАНО", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                vm.RecalculatePenalties();
            }
        }

        private void BtnUninstallCyber_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as SheetViewModel;
            if ((sender as Button)?.Tag is CyberwareDef def)
            {
                foreach (var block in vm.CyberwareBlocks)
                {
                    if (block.InstalledItems.Contains(def))
                    {
                        if (def.IsFoundation) MessageBox.Show("Внимание: Вы удаляете базовый имплант. Зависимые опции могут перестать работать корректно!", "СИСТЕМНОЕ УВЕДОМЛЕНИЕ", MessageBoxButton.OK, MessageBoxImage.Information);

                        block.InstalledItems.Remove(def);
                        block.UsedSlots -= def.Slots;

                        // если он был сопряженным, ищем и удаляем его в парной конечности
                        if (def.IsPaired)
                        {
                            string twinName = GetTwinCategory(block.Name);
                            var twinBlock = vm.CyberwareBlocks.FirstOrDefault(b => b.Name == twinName);
                            if (twinBlock != null)
                            {
                                var twinItem = twinBlock.InstalledItems.FirstOrDefault(i => i.Name == def.Name);
                                if (twinItem != null)
                                {
                                    twinBlock.InstalledItems.Remove(twinItem);
                                    twinBlock.UsedSlots -= twinItem.Slots;
                                }
                            }
                        }

                        vm.RecalculatePenalties();
                        break;
                    }
                }
            }
        }

        private void BtnAddCustomMod_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as SheetViewModel;
            if (string.IsNullOrEmpty(vm.SelectedSkillForMod)) return;

            string internalModType = "Normal";
            if (vm.SelectedTypeForMod.Contains("Visual")) internalModType = "Visual";
            if (vm.SelectedTypeForMod.Contains("Audio")) internalModType = "Audio";

            vm.NewCyberModifiers.Add(new SkillModifierDef
            {
                SkillName = vm.SelectedSkillForMod,
                Value = vm.SelectedValueForMod,
                ModType = internalModType
            });
        }

        private void BtnDeleteCustomCyber_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is CyberwareDef def)
            {
                var vm = this.DataContext as SheetViewModel;
                var res = MessageBox.Show($"Удалить кастомный имплант '{def.Name}' из базы?", "УДАЛЕНИЕ", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    // удаляем со всех конечностей (даже парных), если он там уже установлен
                    foreach (var block in vm.CyberwareBlocks)
                    {
                        var installedItemsToRemove = block.InstalledItems.Where(i => i.Name == def.Name && i.IsCustom).ToList();
                        foreach (var item in installedItemsToRemove)
                        {
                            block.InstalledItems.Remove(item);
                            block.UsedSlots -= item.Slots;
                        }
                    }

                    _originalData.CustomCyberwareList?.Remove(def);
                    vm.AvailableCyberware.Remove(def);
                    vm.RecalculatePenalties();
                }
            }
        }

        private void BtnRemoveCustomMod_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is SkillModifierDef mod)
            {
                var vm = this.DataContext as SheetViewModel;
                vm.NewCyberModifiers.Remove(mod);
            }
        }

        private void BtnCreateCustomCyber_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as SheetViewModel;
            if (string.IsNullOrWhiteSpace(vm.NewCyberName) || vm.CurrentCyberBlock == null) return;

            var newItem = new CyberwareDef
            {
                Name = vm.NewCyberName,
                Category = vm.CurrentCyberBlock.Name,
                Description = vm.NewCyberDesc ?? "",
                HumanityLoss = vm.NewCyberHL ?? "0",
                Slots = int.TryParse(vm.NewCyberSlots, out int s) ? s : 1,
                IsFoundation = vm.NewCyberIsFoundation,
                IsPaired = vm.NewCyberIsPaired,
                Requires = vm.NewCyberRequires ?? "",
                IsCustom = true,
                SkillModifiers = vm.NewCyberModifiers.ToList()
            };

            if (_originalData.CustomCyberwareList == null) _originalData.CustomCyberwareList = new List<CyberwareDef>();
            _originalData.CustomCyberwareList.Add(newItem);
            vm.AvailableCyberware.Add(newItem);

            // очищаем форму
            vm.NewCyberName = ""; vm.NewCyberDesc = ""; vm.NewCyberHL = ""; vm.NewCyberSlots = "1"; vm.NewCyberIsFoundation = false; vm.NewCyberIsPaired = false; vm.NewCyberRequires = "";
            vm.NewCyberModifiers.Clear();
        }
    }
}