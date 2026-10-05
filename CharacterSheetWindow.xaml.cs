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

                    // Загрузка мультиклассов (если их еще нет, создаем из старой роли)
                    if (_originalData.ActiveRoles != null && _originalData.ActiveRoles.Count > 0)
                    {
                        viewModel.ActiveRoles = new ObservableCollection<RoleData>(_originalData.ActiveRoles);
                    }
                    else
                    {
                        viewModel.ActiveRoles = new ObservableCollection<RoleData> {
                            new RoleData { Name = _originalData.Role, Rank = _originalData.SystemStats != null && _originalData.SystemStats.ContainsKey("RoleRank") ? _originalData.SystemStats["RoleRank"] : 4 }
                        };
                    }
                    viewModel.ChangeRoleIndex(0); // Инициализация

                    if (_originalData.SystemStats != null)
                    {
                        viewModel.SoloThreat = _originalData.SystemStats.ContainsKey("SoloThreat") ? _originalData.SystemStats["SoloThreat"] : 0;
                        viewModel.SoloAwareness = _originalData.SystemStats.ContainsKey("SoloAwareness") ? _originalData.SystemStats["SoloAwareness"] : 0;
                        viewModel.SoloDeflection = _originalData.SystemStats.ContainsKey("SoloDeflection") ? _originalData.SystemStats["SoloDeflection"] : 0;
                        viewModel.SoloFumble = _originalData.SystemStats.ContainsKey("SoloFumble") ? Math.Min(1, _originalData.SystemStats["SoloFumble"]) : 0;
                        viewModel.SoloInitiative = _originalData.SystemStats.ContainsKey("SoloInitiative") ? _originalData.SystemStats["SoloInitiative"] : 0;
                        viewModel.SoloPrecision = _originalData.SystemStats.ContainsKey("SoloPrecision") ? _originalData.SystemStats["SoloPrecision"] : 0;

                        viewModel.TechField = _originalData.SystemStats.ContainsKey("TechField") ? _originalData.SystemStats["TechField"] : 0;
                        viewModel.TechUpgrade = _originalData.SystemStats.ContainsKey("TechUpgrade") ? _originalData.SystemStats["TechUpgrade"] : 0;
                        viewModel.TechFab = _originalData.SystemStats.ContainsKey("TechFab") ? _originalData.SystemStats["TechFab"] : 0;
                        viewModel.TechInvent = _originalData.SystemStats.ContainsKey("TechInvent") ? _originalData.SystemStats["TechInvent"] : 0;

                        viewModel.MedSurgery = _originalData.SystemStats.ContainsKey("MedSurgery") ? _originalData.SystemStats["MedSurgery"] : 0;
                        viewModel.MedPharma = _originalData.SystemStats.ContainsKey("MedPharma") ? _originalData.SystemStats["MedPharma"] : 0;
                        viewModel.MedCryo = _originalData.SystemStats.ContainsKey("MedCryo") ? _originalData.SystemStats["MedCryo"] : 0;

                        viewModel.DS0 = _originalData.SystemStats.ContainsKey("DS0") && _originalData.SystemStats["DS0"] == 1;
                        viewModel.DS1 = _originalData.SystemStats.ContainsKey("DS1") && _originalData.SystemStats["DS1"] == 1;
                        viewModel.DS2 = _originalData.SystemStats.ContainsKey("DS2") && _originalData.SystemStats["DS2"] == 1;
                        viewModel.DS3 = _originalData.SystemStats.ContainsKey("DS3") && _originalData.SystemStats["DS3"] == 1;
                        viewModel.DS4 = _originalData.SystemStats.ContainsKey("DS4") && _originalData.SystemStats["DS4"] == 1;
                        viewModel.DS5 = _originalData.SystemStats.ContainsKey("DS5") && _originalData.SystemStats["DS5"] == 1;
                        viewModel.DS6 = _originalData.SystemStats.ContainsKey("DS6") && _originalData.SystemStats["DS6"] == 1;
                        viewModel.DS7 = _originalData.SystemStats.ContainsKey("DS7") && _originalData.SystemStats["DS7"] == 1;
                        viewModel.DS8 = _originalData.SystemStats.ContainsKey("DS8") && _originalData.SystemStats["DS8"] == 1;
                        viewModel.DS9 = _originalData.SystemStats.ContainsKey("DS9") && _originalData.SystemStats["DS9"] == 1;
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

                    // Загрузка снаряжения и нового упрощенного режима
                    viewModel.IsGearSimpleMode = _originalData.IsGearSimpleMode;
                    viewModel.GearSimpleNames = _originalData.GearSimpleNames ?? "";
                    viewModel.GearSimpleNotes = _originalData.GearSimpleNotes ?? "";

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

                    RefreshInstalledCyberware(viewModel.CyberwareBlocks);

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
                        { "Технические Навыки", new[] { "Авиационные технологии", "Знание техники", "Кибертехника", "Подрывник (x2)", "Электроника/Безопасность (x2)", "Первая помощь", "Фальсификация", "Автомеханика", "Художественное ремесло", "Парамедик (x2)", "Кино- и фототехника", "Взлом замков", "Карманник", "Морские технологии", "Оружейник"} }
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

                    // Загружаем NPC
                    if (_originalData.NPCs != null)
                    {
                        foreach (var npcData in _originalData.NPCs)
                        {
                            var npcVM = new CombatantViewModel
                            {
                                OriginalData = npcData,
                                Name = npcData.Name,
                                MaxHP = npcData.HP,
                                CurrentHP = npcData.CurrentHP ?? npcData.HP,
                                HeadArmor = npcData.CurrentHeadArmor ?? npcData.HeadArmor,
                                BodyArmor = npcData.CurrentBodyArmor ?? npcData.BodyArmor,
                                BaseMove = npcData.BaseMove,
                                BaseDeathSave = npcData.BaseDeathSave,
                                Weapons = npcData.Weapons,
                                Gear = npcData.Gear,
                                Cyberware = npcData.Cyberware
                            };
                            npcVM.RebuildStatsDisplay();
                            viewModel.NPCs.Add(npcVM);
                        }
                    }

                    viewModel.RecalculatePenalties();
                    viewModel.UpdateDerivedCombatStats();

                    this.DataContext = viewModel;
                    LoadEnemiesFromFolder(); // Подгружаем врагов для комбобокса
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении файла:\n{ex.Message}", "ОШИБКА ДАННЫХ", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }
        // в сохранении установленные импланты лежат копиями; подтягиваем актуальные данные из базы (кастомные не трогаем)
        private static void RefreshInstalledCyberware(IEnumerable<CyberwareBlockItem> blocks)
        {
            foreach (var block in blocks)
            {
                for (int i = 0; i < block.InstalledItems.Count; i++)
                {
                    var saved = block.InstalledItems[i];
                    if (saved.IsCustom) continue;
                    var def = CoreDataBase.AllCyberware.FirstOrDefault(c => c.Name == saved.Name && c.Category == saved.Category);
                    if (def == null) continue;
                    block.InstalledItems[i] = def;
                    block.UsedSlots += def.Slots - saved.Slots;
                }
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
                            string subName = custom.Name.Contains(":") ? custom.Name.Substring(custom.Name.IndexOf(':') + 1).Trim() : custom.Name;
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
                subName = savedSkill.Name.Substring(savedSkill.Name.IndexOf(':') + 1).Trim();
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
                int cost = tag == "Deflection" ? 2 : (tag == "Precision" ? 3 : (tag == "Fumble" ? 4 : 1));
                if (vm.RoleRank - vm.SoloPointsUsed >= cost)
                {
                    if (tag == "Threat") vm.SoloThreat++;
                    else if (tag == "Awareness") vm.SoloAwareness++;
                    else if (tag == "Deflection") vm.SoloDeflection++;
                    else if (tag == "Fumble" && vm.SoloFumble < 1) vm.SoloFumble++;
                    else if (tag == "Initiative") vm.SoloInitiative++;
                    else if (tag == "Precision") vm.SoloPrecision++;
                }
            }
            // Логика Техника
            else if (tag == "TechField" || tag == "TechUpgrade" || tag == "TechFab" || tag == "TechInvent")
            {
                // за ранг — 1 очко в две разные специализации, значит в одной специализации не больше ранга
                if (vm.TechPointsTotal - vm.TechPointsUsed >= 1)
                {
                    if (tag == "TechField" && vm.TechField < vm.RoleRank) vm.TechField++;
                    else if (tag == "TechUpgrade" && vm.TechUpgrade < vm.RoleRank) vm.TechUpgrade++;
                    else if (tag == "TechFab" && vm.TechFab < vm.RoleRank) vm.TechFab++;
                    else if (tag == "TechInvent" && vm.TechInvent < vm.RoleRank) vm.TechInvent++;
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

                    _originalData.SystemStats["DS0"] = vm.DS0 ? 1 : 0;
                    _originalData.SystemStats["DS1"] = vm.DS1 ? 1 : 0;
                    _originalData.SystemStats["DS2"] = vm.DS2 ? 1 : 0;
                    _originalData.SystemStats["DS3"] = vm.DS3 ? 1 : 0;
                    _originalData.SystemStats["DS4"] = vm.DS4 ? 1 : 0;
                    _originalData.SystemStats["DS5"] = vm.DS5 ? 1 : 0;
                    _originalData.SystemStats["DS6"] = vm.DS6 ? 1 : 0;
                    _originalData.SystemStats["DS7"] = vm.DS7 ? 1 : 0;
                    _originalData.SystemStats["DS8"] = vm.DS8 ? 1 : 0;
                    _originalData.SystemStats["DS9"] = vm.DS9 ? 1 : 0;

                    _originalData.SystemStats["ImprovementPoints"] = vm.ImprovementPoints;
                    _originalData.SystemStats["Reputation"] = vm.Reputation;
                    _originalData.ReputationEvents = vm.ReputationEvents;

                    // Сохраняем мультикласс и активный ранг
                    _originalData.ActiveRoles = vm.ActiveRoles.ToList();
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

                    // Сохраняем снаряжение (оба режима)
                    _originalData.IsGearSimpleMode = vm.IsGearSimpleMode;
                    _originalData.GearSimpleNames = vm.GearSimpleNames;
                    _originalData.GearSimpleNotes = vm.GearSimpleNotes;
                    _originalData.GearItems = vm.GearItems.ToList();

                    // Сохраняем вкладку NPC
                    foreach (var npc in vm.NPCs)
                    {
                        if (npc.OriginalData == null) continue;
                        npc.OriginalData.CurrentHP = npc.CurrentHP;
                        npc.OriginalData.CurrentHeadArmor = npc.HeadArmor;
                        npc.OriginalData.CurrentBodyArmor = npc.BodyArmor;
                    }
                    _originalData.NPCs = vm.NPCs.Select(npc => npc.OriginalData).ToList();

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

                            // базовый навык, опущенный ниже стартового уровня (2), иначе при загрузке вернётся к нему
                            var skillDef = CoreDataBase.AllSkills.FirstOrDefault(d => d.Name == skill.BaseName);
                            int defaultLevel = skillDef == null ? 0 : (skillDef.IsBasic ? 2 : skillDef.FreeLevels);
                            bool belowDefault = !skill.IsVariant && skill.Level < defaultLevel;

                            if (skill.Level > 0 || skill.IsVariant || skill.BaseName == "Язык (Родной)" || belowDefault)
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

                                _originalData.Skills.Add(new SkillSaveData { Name = exportName, Level = skill.Level, Total = skill.StatValue + skill.Level });
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

                // Опции конечностей и глаз ставятся только туда, где установлен базовый имплант.
                // Исключение — мясная рука/нога: в неё можно поставить один имплант из списка, стандартная кисть/стопа не в счёт
                string blockName = vm.CurrentCyberBlock.Name;
                bool isLimbBlock = blockName.Contains("КИБЕРРУКА") || blockName.Contains("КИБЕРНОГА") || blockName.Contains("КИБЕРГЛАЗ");
                if (isLimbBlock && !def.IsFoundation && !def.IsCustom && !vm.CurrentCyberBlock.IsInstalled)
                {
                    bool isMeatLimb = !blockName.Contains("ДОП.") && !blockName.Contains("КИБЕРГЛАЗ");
                    if (!isMeatLimb || !CoreDataBase.MeatLimbCyberware.Contains(def.Name))
                    {
                        MessageBox.Show("Сначала установите базовый имплант в этот блок (киберглаз, киберруку или киберногу)!", "ОШИБКА СОВМЕСТИМОСТИ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    bool isStandard = def.Name == "Стандартная кисть" || def.Name == "Стандартная стопа";
                    bool occupied = vm.CurrentCyberBlock.InstalledItems.Any(i => isStandard
                        ? i.Name == def.Name
                        : i.Name != "Стандартная кисть" && i.Name != "Стандартная стопа");
                    if (occupied)
                    {
                        MessageBox.Show("В мясную конечность можно установить только один киберимплант (стандартная кисть/стопа не в счёт)!", "ОШИБКА СОВМЕСТИМОСТИ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Поиск зависимостей по всему телу
                if (!string.IsNullOrWhiteSpace(def.Requires) && def.Name != "Подкожный захват")
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
                int currentBody = vm.HexStats.FirstOrDefault(s => s.Name == "ТЕЛ" || s.Name == "BODY")?.CurrentValue ?? 0;
                if (def.Name == "Эндоскелет ∑ (Сигма)" && currentBody < 6)
                {
                    MessageBox.Show("Для установки Эндоскелета Сигма требуется ТЕЛО 6!", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (def.Name == "Эндоскелет ß (Бета)" && currentBody < 8)
                {
                    MessageBox.Show("Для установки Эндоскелета Бета требуется ТЕЛО 8!", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                // СТАЛО:
                else if (def.Name == "Фасеточное крепление")
                {
                    if (!vm.CyberwareBlocks.Any(b => b.Name == "ДОП. КИБЕРГЛАЗ 1 (EXTRA OPTIC 1)"))
                    {
                        vm.CyberwareBlocks.Add(new CyberwareBlockItem { Name = "ДОП. КИБЕРГЛАЗ 1 (EXTRA OPTIC 1)", MaxSlots = 3, UsedSlots = 0, OptionsText = "" });
                        vm.CyberwareBlocks.Add(new CyberwareBlockItem { Name = "ДОП. КИБЕРГЛАЗ 2 (EXTRA OPTIC 2)", MaxSlots = 3, UsedSlots = 0, OptionsText = "" });
                        vm.CyberwareBlocks.Add(new CyberwareBlockItem { Name = "ДОП. КИБЕРГЛАЗ 3 (EXTRA OPTIC 3)", MaxSlots = 3, UsedSlots = 0, OptionsText = "" });
                        vm.CyberwareBlocks.Add(new CyberwareBlockItem { Name = "ДОП. КИБЕРГЛАЗ 4 (EXTRA OPTIC 4)", MaxSlots = 3, UsedSlots = 0, OptionsText = "" });
                        vm.CyberwareBlocks.Add(new CyberwareBlockItem { Name = "ДОП. КИБЕРГЛАЗ 5 (EXTRA OPTIC 5)", MaxSlots = 3, UsedSlots = 0, OptionsText = "" });
                        MessageBox.Show("Фасеточное крепление установлено! В ваш лист добавлены слоты для 5 дополнительных киберглаз.", "БОРГИРОВАНИЕ АКТИВИРОВАНО", MessageBoxButton.OK, MessageBoxImage.Information);
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
                        // крепления боргирования держат дополнительные руки/глаза: убираем их вместе с креплением
                        string[] extraNames = def.Name == "Искусственное плечевое крепление"
                            ? new[] { "ДОП. ПРАВАЯ КИБЕРРУКА (EXTRA ARM R)", "ДОП. ЛЕВАЯ КИБЕРРУКА (EXTRA ARM L)" }
                            : def.Name == "Фасеточное крепление"
                                ? new[] { "ДОП. КИБЕРГЛАЗ 1 (EXTRA OPTIC 1)", "ДОП. КИБЕРГЛАЗ 2 (EXTRA OPTIC 2)", "ДОП. КИБЕРГЛАЗ 3 (EXTRA OPTIC 3)", "ДОП. КИБЕРГЛАЗ 4 (EXTRA OPTIC 4)", "ДОП. КИБЕРГЛАЗ 5 (EXTRA OPTIC 5)" }
                                : new string[0];
                        var extraBlocks = vm.CyberwareBlocks.Where(b => extraNames.Contains(b.Name)).ToList();
                        if (extraBlocks.Any(b => b.InstalledItems.Count > 0)
                            && MessageBox.Show("Вместе с креплением будут удалены дополнительные конечности и все их импланты. Продолжить?", "УДАЛЕНИЕ КРЕПЛЕНИЯ", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                            return;

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

                        foreach (var extra in extraBlocks) vm.CyberwareBlocks.Remove(extra);

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
                HumanityLoss = string.IsNullOrWhiteSpace(vm.NewCyberHL) ? "0" : vm.NewCyberHL.Trim(),
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

        private void BtnToggleGearMode_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SheetViewModel vm) vm.IsGearSimpleMode = !vm.IsGearSimpleMode;
        }

        private void BtnPrevRole_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SheetViewModel vm && vm.ActiveRoles.Count > 1)
                vm.ChangeRoleIndex(vm.ActiveRoles.IndexOf(vm.CurrentActiveRole) - 1);
        }

        private void BtnNextRole_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SheetViewModel vm && vm.ActiveRoles.Count > 1)
                vm.ChangeRoleIndex(vm.ActiveRoles.IndexOf(vm.CurrentActiveRole) + 1);
        }

        private void BtnAddRole_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as SheetViewModel;
            var availableRoles = CoreDataBase.Roles.Where(r => !vm.ActiveRoles.Any(ar => ar.Name == r)).ToList();
            if (availableRoles.Count == 0)
            {
                MessageBox.Show("Вы уже взяли все возможные роли!", "ИНФО", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // новую роль можно взять, только когда последняя взятая дошла до ранга 4 (стр. 145)
            var lastRole = vm.ActiveRoles.LastOrDefault();
            if (lastRole != null && lastRole.Rank < 4)
            {
                MessageBox.Show($"Новую роль можно взять, когда последняя роль ({lastRole.Name}) достигнет 4 ранга. Сейчас ранг {lastRole.Rank}.", "МУЛЬТИКЛАСС НЕДОСТУПЕН", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Window prompt = new Window { Title = "ВЫБОР РОЛИ", Width = 300, Height = 130, Background = new SolidColorBrush(Color.FromRgb(10, 10, 10)), WindowStartupLocation = WindowStartupLocation.CenterScreen, ResizeMode = ResizeMode.NoResize, WindowStyle = WindowStyle.ToolWindow };
            prompt.Resources = this.Resources; // ПРИМЕНЯЕМ СТИЛИ ОКНА, чтобы комбобокс стал темным

            var combo = new ComboBox { ItemsSource = availableRoles, SelectedIndex = 0, Margin = new Thickness(10) };
            var btn = new Button { Content = "ДОБАВИТЬ МУЛЬТИКЛАСС", Margin = new Thickness(10), Background = Brushes.Red, Foreground = Brushes.Black, FontWeight = FontWeights.Bold, Cursor = System.Windows.Input.Cursors.Hand };
            btn.Click += (s, ev) => {
                if (combo.SelectedItem != null)
                {
                    vm.ActiveRoles.Add(new RoleData { Name = combo.SelectedItem.ToString(), Rank = 1 });
                    vm.ChangeRoleIndex(vm.ActiveRoles.Count - 1);
                    prompt.DialogResult = true;
                }
            };
            var stack = new StackPanel(); stack.Children.Add(combo); stack.Children.Add(btn);
            prompt.Content = stack;
            prompt.ShowDialog();
        }

        private void BtnRemoveRole_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as SheetViewModel;
            if (vm != null)
            {
                if (vm.ActiveRoles.Count > 1)
                {
                    var res = MessageBox.Show($"Вы уверены, что хотите удалить роль '{vm.CurrentActiveRole.Name}'?\nВсе вложенные в нее очки способностей будут сброшены.", "УДАЛЕНИЕ РОЛИ", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (res == MessageBoxResult.Yes)
                    {
                        vm.RemoveCurrentRole();
                    }
                }
                else
                {
                    MessageBox.Show("У персонажа должна быть хотя бы одна роль! Вы не можете удалить последнюю.", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        private void LoadEnemiesFromFolder()
        {
            if (!(this.DataContext is SheetViewModel vm)) return;
            vm.AvailableEnemies.Clear();
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Enemies");
            if (Directory.Exists(folderPath))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                foreach (string file in Directory.GetFiles(folderPath, "*.json"))
                {
                    try { vm.AvailableEnemies.Add(JsonSerializer.Deserialize<EnemySaveData>(File.ReadAllText(file), options)); } catch { }
                }
            }
        }

        private void BtnAddNPC_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as SheetViewModel;
            if (vm != null && sender is Button btn && btn.Tag is ComboBox cb && cb.SelectedItem is EnemySaveData enemyData)
            {
                var combatant = new CombatantViewModel
                {
                    OriginalData = enemyData.Clone(),
                    Name = enemyData.Name,
                    MaxHP = enemyData.HP,
                    CurrentHP = enemyData.HP,
                    HeadArmor = enemyData.HeadArmor,
                    BodyArmor = enemyData.BodyArmor,
                    BaseMove = enemyData.BaseMove,
                    BaseDeathSave = enemyData.BaseDeathSave,
                    Weapons = enemyData.Weapons,
                    Gear = enemyData.Gear,
                    Cyberware = enemyData.Cyberware
                };
                combatant.RebuildStatsDisplay();
                vm.NPCs.Add(combatant);
            }
        }

        private void BtnRemoveNPC_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is CombatantViewModel c && this.DataContext is SheetViewModel vm) vm.NPCs.Remove(c); }
        private void BtnHealNPC_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is CombatantViewModel c) c.CurrentHP++; }
        private void BtnDamageNPC_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is CombatantViewModel c) c.CurrentHP--; }
        private void BtnAblateHeadNPC_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is CombatantViewModel c && c.HeadArmor > 0) c.HeadArmor--; }
        private void BtnAblateBodyNPC_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is CombatantViewModel c && c.BodyArmor > 0) c.BodyArmor--; }

        private void BtnOpenEnemyManager_Click(object sender, RoutedEventArgs e)
        {
            LoadEnemiesFromFolder();
            _editingEnemyName = null;
            RbGenCN.IsChecked = true;
            if (this.DataContext is SheetViewModel vm)
            {
                vm.NewEnemySkills.Clear();
                vm.NewEnemyCyberware.Clear();
            }
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
            var vm = this.DataContext as SheetViewModel;
            if (vm != null && CbGenSkills.SelectedItem != null && int.TryParse(TxtGenSkillLevel.Text, out int inputValue))
            {
                string skillName = CbGenSkills.SelectedItem.ToString();
                int skillLevel = inputValue;

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

                        int statValue = 5;
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
                vm.NewEnemySkills.Add(new SkillSaveData { Name = skillName, Total = skillLevel });
            }
        }

        private void BtnRemoveGenSkill_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is SkillSaveData s && this.DataContext is SheetViewModel vm) vm.NewEnemySkills.Remove(s);
        }

        private void BtnAddGenCyber_Click(object sender, RoutedEventArgs e)
        {
            string cyberName = CbGenCyber.Text?.Trim();
            var vm = this.DataContext as SheetViewModel;
            if (vm != null && !string.IsNullOrEmpty(cyberName) && !vm.NewEnemyCyberware.Contains(cyberName))
            {
                bool exists = CoreDataBase.AllCyberware.Any(c => c.Name.Equals(cyberName, StringComparison.OrdinalIgnoreCase));
                if (exists)
                {
                    vm.NewEnemyCyberware.Add(cyberName);
                    CbGenCyber.Text = "";
                }
                else MessageBox.Show("Такого импланта нет в базе! Проверьте правильность написания.", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnRemoveGenCyber_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is string cyber && this.DataContext is SheetViewModel vm) vm.NewEnemyCyberware.Remove(cyber);
        }

        // имя пресета, открытого на редактирование (чтобы при переименовании предложить удалить старый)
        private string _editingEnemyName;

        private void BtnEditEnemyPreset_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is EnemySaveData enemyData && this.DataContext is SheetViewModel vm)
            {
                _editingEnemyName = enemyData.Name;
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

                    vm.NewEnemySkills.Clear();
                    if (enemyData.Skills != null)
                    {
                        foreach (var kvp in enemyData.Skills) vm.NewEnemySkills.Add(new SkillSaveData { Name = kvp.Key, Total = kvp.Value });
                    }
                }

                vm.NewEnemyCyberware.Clear();
                if (!string.IsNullOrWhiteSpace(enemyData.Cyberware))
                {
                    var parts = enemyData.Cyberware.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        string cleanName = part.Trim();
                        if (!string.IsNullOrWhiteSpace(cleanName)) vm.NewEnemyCyberware.Add(cleanName);
                    }
                }
            }
        }

        private void BtnDeleteEnemyPreset_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is EnemySaveData enemyData && this.DataContext is SheetViewModel vm)
            {
                var res = MessageBox.Show($"Удалить пресет '{enemyData.Name}' из базы навсегда?", "ПОДТВЕРДИТЕ УДАЛЕНИЕ", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                {
                    string filePath = UIHelpers.EnemyFilePath(enemyData.Name);
                    if (File.Exists(filePath))
                    {
                        try { File.Delete(filePath); } catch { }
                    }
                    vm.AvailableEnemies.Remove(enemyData);
                }
            }
        }

        private void BtnSaveNewEnemy_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as SheetViewModel;
            if (vm == null) return;

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
                Cyberware = string.Join(", ", vm.NewEnemyCyberware)
            };

            if (enemy.IsCNMode)
            {
                enemy.CombatNumber = int.TryParse(TxtGenCN.Text, out int cn) ? cn : 12;
                enemy.BaseDeathSave = int.TryParse(TxtGenDeathSaveCN.Text, out int ds) ? ds : 5;
                enemy.BaseMove = int.TryParse(TxtGenMove.Text, out int mv) ? mv : 5;
            }
            else
            {
                enemy.Stats["INT"] = UIHelpers.ParseIntOr(StINT.Text, 5);
                enemy.Stats["REF"] = UIHelpers.ParseIntOr(StREF.Text, 5);
                enemy.Stats["DEX"] = UIHelpers.ParseIntOr(StDEX.Text, 5);
                enemy.Stats["TECH"] = UIHelpers.ParseIntOr(StTECH.Text, 5);
                enemy.Stats["COOL"] = UIHelpers.ParseIntOr(StCOOL.Text, 5);
                enemy.Stats["WILL"] = UIHelpers.ParseIntOr(StWILL.Text, 5);
                enemy.Stats["LUCK"] = UIHelpers.ParseIntOr(StLUCK.Text, 5);
                enemy.Stats["MOVE"] = UIHelpers.ParseIntOr(StMOVE.Text, 5);
                enemy.Stats["BODY"] = UIHelpers.ParseIntOr(StBODY.Text, 5);
                enemy.Stats["EMP"] = UIHelpers.ParseIntOr(StEMP.Text, 5);

                enemy.BaseMove = enemy.Stats["MOVE"];
                enemy.BaseDeathSave = enemy.Stats["BODY"];

                foreach (var sk in vm.NewEnemySkills) enemy.Skills[sk.Name] = sk.Total;
            }

            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Enemies");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string filePath = UIHelpers.EnemyFilePath(enemy.Name);
            var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            File.WriteAllText(filePath, JsonSerializer.Serialize(enemy, options), System.Text.Encoding.UTF8);

            var existingItem = vm.AvailableEnemies.FirstOrDefault(x => x.Name.Equals(enemy.Name, StringComparison.OrdinalIgnoreCase));
            if (existingItem != null) vm.AvailableEnemies.Remove(existingItem);
            vm.AvailableEnemies.Add(enemy);

            // пресет переименовали: предлагаем убрать старый, а не оставлять дубль
            if (_editingEnemyName != null && !_editingEnemyName.Equals(enemy.Name, StringComparison.OrdinalIgnoreCase)
                && MessageBox.Show($"Имя изменено. Удалить старый пресет '{_editingEnemyName}'?", "ПЕРЕИМЕНОВАНИЕ", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try { File.Delete(UIHelpers.EnemyFilePath(_editingEnemyName)); } catch { }
                var old = vm.AvailableEnemies.FirstOrDefault(x => x.Name.Equals(_editingEnemyName, StringComparison.OrdinalIgnoreCase));
                if (old != null) vm.AvailableEnemies.Remove(old);
            }
            _editingEnemyName = null;

            TxtGenName.Text = ""; TxtGenWeapons.Text = ""; TxtGenGear.Text = ""; CbGenCyber.Text = ""; vm.NewEnemySkills.Clear(); vm.NewEnemyCyberware.Clear();

            MessageBox.Show($"Враг '{enemy.Name}' сохранен в базу!", "СОХРАНЕНО", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}