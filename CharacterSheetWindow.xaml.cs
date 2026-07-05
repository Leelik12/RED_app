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

namespace CyberpunkRED_Generator
{
    // Новый класс для хранения любых баффов и дебаффов навыков
    public class SkillModifierDef
    {
        public string SkillName { get; set; } // Какому навыку даем бонус (или "Все")
        public int Value { get; set; }        // Значение (+1, -4 и т.д.)
        public string ModType { get; set; } = "Normal"; // "Normal", "Visual", "Audio"
    }

    public class CriticalInjuryItem : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string QuickFix { get; set; }
        public string Treatment { get; set; }
        public int AllActionsPenalty { get; set; }
        public int MovePenalty { get; set; }
        public string EffectText { get; set; }
        public int DeathSavePenalty { get; set; }

        // ДОБАВЛЕНО: Список модификаторов для UI-класса
        public List<SkillModifierDef> SkillModifiers { get; set; } = new List<SkillModifierDef>();

        public string TooltipText => $"{Description}\n\nЭФФЕКТ РАНЕНИЯ:\n{EffectText}\n\nСЛОЖНОСТЬ СТАБИЛИЗАЦИИ:\nПервая помощь: {QuickFix}\nЛечение: {Treatment}";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class CyberwareDef : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string HumanityLoss { get; set; }
        public int Slots { get; set; } = 1;

        public bool IsCustom { get; set; } = false;
        public bool IsFoundation { get; set; } = false;
        public string Requires { get; set; } = "";

        public List<SkillModifierDef> SkillModifiers { get; set; } = new List<SkillModifierDef>();

        public string TooltipText => $"{Description}\n\nПЧ: {HumanityLoss} | Слотов: {Slots}" +
                                     (string.IsNullOrEmpty(Requires) ? "" : $"\nТРЕБУЕТ: {Requires}");

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class CyberwareBlockItem : INotifyPropertyChanged
    {
        private string _name; public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        // Автоматически проверяет, есть ли в установленных имплантах "База"
        public bool IsInstalled => InstalledItems.Any(i => i.IsFoundation);

        private int _usedSlots; public int UsedSlots { get => _usedSlots; set { _usedSlots = value; OnPropertyChanged(); OnPropertyChanged(nameof(SlotsText)); } }
        private int _maxSlots; public int MaxSlots { get => _maxSlots; set { _maxSlots = value; OnPropertyChanged(); OnPropertyChanged(nameof(SlotsText)); } }
        private string _optionsText; public string OptionsText { get => _optionsText; set { _optionsText = value; OnPropertyChanged(); } }
        public string SlotsText => $"свободно {MaxSlots - UsedSlots}/{MaxSlots}";

        private ObservableCollection<CyberwareDef> _installedItems;
        public ObservableCollection<CyberwareDef> InstalledItems
        {
            get => _installedItems;
            set
            {
                if (_installedItems != null) _installedItems.CollectionChanged -= InstalledItems_CollectionChanged;
                _installedItems = value;
                if (_installedItems != null) _installedItems.CollectionChanged += InstalledItems_CollectionChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsInstalled));
            }
        }

        public CyberwareBlockItem()
        {
            InstalledItems = new ObservableCollection<CyberwareDef>();
        }

        private void InstalledItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsInstalled)); // Зажигает чекбокс при добавлении базы
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
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
        private int _baseValue; public int BaseValue { get => _baseValue; set { _baseValue = value; UpdateCurrentValue(); } }
        private int _armorPenalty; public int ArmorPenalty { get => _armorPenalty; set { _armorPenalty = value; UpdateCurrentValue(); } }
        private int _implantModifier; public int ImplantModifier { get => _implantModifier; set { _implantModifier = value; UpdateCurrentValue(); } }
        private int _woundPenalty; public int WoundPenalty { get => _woundPenalty; set { _woundPenalty = value; UpdateCurrentValue(); } }

        public int Value { get => CurrentValue; set => CurrentValue = value; }

        private int _currentValue;
        public int CurrentValue
        {
            get => _currentValue;
            set { _currentValue = value; OnPropertyChanged(); OnPropertyChanged(nameof(Value)); OnPropertyChanged(nameof(TooltipText)); }
        }

        public bool IsFractional { get; set; }
        public bool IsReadOnly { get; set; }
        public string TooltipText => GenerateTooltip();

        public void UpdateCurrentValue()
        {
            if (Name == "УДЧ" || Name == "LUCK" || Name == "ЭМП" || Name == "EMP") return;

            int val = BaseValue - ArmorPenalty + ImplantModifier + WoundPenalty;

            // СКО (MOVE) не может опуститься ниже 1 из-за ранений/брони
            if ((Name == "СКО" || Name == "MOVE") && val < 1) val = 1;

            CurrentValue = val;
        }

        private string GenerateTooltip()
        {
            if (Name == "ЭМП" || Name == "EMP") return $"+{BaseValue} базовое\n(Текущая зависит от Человечности)";
            if (Name == "УДЧ" || Name == "LUCK") return $"+{BaseValue} базовое\n(Расходуемый пул)";

            var parts = new List<string> { $"+{BaseValue} базовое" };
            if (ArmorPenalty > 0) parts.Add($"-{ArmorPenalty} штраф за броню");
            if (ImplantModifier > 0) parts.Add($"+{ImplantModifier} имплант");
            else if (ImplantModifier < 0) parts.Add($"{ImplantModifier} имплант");
            if (WoundPenalty < 0) parts.Add($"{WoundPenalty} крит. ранение/травма");

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
        public int StatValue { get => _statValue; set { _statValue = value; OnPropertyChanged(); OnPropertyChanged(nameof(Base)); OnPropertyChanged(nameof(DisplayModifier)); OnPropertyChanged(nameof(DisplayBase)); } }

        private int _level;
        public int Level { get => _level; set { _level = value; OnPropertyChanged(); OnPropertyChanged(nameof(Base)); OnPropertyChanged(nameof(DisplayModifier)); OnPropertyChanged(nameof(DisplayBase)); } }

        private int _woundPenalty;
        public int WoundPenalty { get => _woundPenalty; set { _woundPenalty = value; OnPropertyChanged(); OnPropertyChanged(nameof(Base)); OnPropertyChanged(nameof(DisplayModifier)); OnPropertyChanged(nameof(DisplayBase)); } }

        private int _equipmentMod;
        public int EquipmentMod { get => _equipmentMod; set { _equipmentMod = value; OnPropertyChanged(); OnPropertyChanged(nameof(Base)); OnPropertyChanged(nameof(DisplayModifier)); OnPropertyChanged(nameof(DisplayBase)); } }

        private int _visualMod;
        public int VisualMod { get => _visualMod; set { _visualMod = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayModifier)); OnPropertyChanged(nameof(DisplayBase)); } }

        private int _audioMod;
        public int AudioMod { get => _audioMod; set { _audioMod = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayModifier)); OnPropertyChanged(nameof(DisplayBase)); } }

        // Общий численный модификатор (Травмы + Броня + Обычные импланты)
        public int GeneralMod => WoundPenalty + EquipmentMod;

        // Строка для колонки "ДОП" (Только для чтения интерфейсом)
        public string DisplayModifier
        {
            get
            {
                int vis = GeneralMod + VisualMod;
                int aud = GeneralMod + AudioMod;

                if (vis == aud) return vis.ToString();
                return $"{vis}в/{aud}а";
            }
        }

        // Общая база в виде числа (используется для сохранения в JSON)
        public int Base => StatValue + Level + GeneralMod;

        // Строка для колонки "СУМ" (УР + СТАТ + ДОП)
        public string DisplayBase
        {
            get
            {
                int baseVal = Base;
                int vis = baseVal + VisualMod;
                int aud = baseVal + AudioMod;

                if (vis == aud) return vis.ToString();
                return $"{vis}в/{aud}а";
            }
        }

        public string Description { get; set; }
        public bool CanAddMultiple { get; set; }
        public bool IsVariant { get; set; }
        public string BaseName { get; set; }
        private string _subName; public string SubName { get => _subName; set { _subName = value; OnPropertyChanged(); } }

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

        private int _currentHumanity; public int CurrentHumanity { get => _currentHumanity; set { _currentHumanity = value; OnPropertyChanged(); UpdateEmp(); } }
        private int _maxHumanity; public int MaxHumanity { get => _maxHumanity; set { _maxHumanity = value; OnPropertyChanged(); } }

        // --- СВОЙСТВА ЗДОРОВЬЯ И ТРАВМ ---
        private int _currentHP; public int CurrentHP { get => _currentHP; set { _currentHP = value; OnPropertyChanged(); RecalculatePenalties(); } }
        private int _maxHP; public int MaxHP { get => _maxHP; set { _maxHP = value; OnPropertyChanged(); RecalculatePenalties(); } }
        private int _baseDeathSave; public int BaseDeathSave { get => _baseDeathSave; set { _baseDeathSave = value; OnPropertyChanged(); RecalculatePenalties(); } }
        private int _currentDeathSave; public int CurrentDeathSave { get => _currentDeathSave; set { _currentDeathSave = value; OnPropertyChanged(); } }
        private string _woundStateText; public string WoundStateText { get => _woundStateText; set { _woundStateText = value; OnPropertyChanged(); } }
        private string _woundTooltipText; public string WoundTooltipText { get => _woundTooltipText; set { _woundTooltipText = value; OnPropertyChanged(); } }

        public ObservableCollection<CriticalInjuryItem> CriticalInjuriesList { get; set; }
        public List<string> AvailableInjuries => CoreDataBase.CriticalInjuries.Select(x => x.Name).ToList();
        private string _selectedInjuryToAdd; public string SelectedInjuryToAdd { get => _selectedInjuryToAdd; set { _selectedInjuryToAdd = value; OnPropertyChanged(); } }
        private string _addictions; public string Addictions { get => _addictions; set { _addictions = value; OnPropertyChanged(); } }

        private string _notes; public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }
        private string _roleAbilityNotes; public string RoleAbilityNotes { get => _roleAbilityNotes; set { _roleAbilityNotes = value; OnPropertyChanged(); } }

        public ObservableCollection<GearRowItem> GearItems { get; set; }
        public ObservableCollection<CyberwareBlockItem> CyberwareBlocks { get; set; }

        private string _ammoValue; public string AmmoValue { get => _ammoValue; set { _ammoValue = value; OnPropertyChanged(); } }
        private string _cashValue; public string CashValue { get => _cashValue; set { _cashValue = value; OnPropertyChanged(); } }
        private string _styleNotes; public string StyleNotes { get => _styleNotes; set { _styleNotes = value; OnPropertyChanged(); } }
        private string _housing; public string Housing { get => _housing; set { _housing = value; OnPropertyChanged(); } }
        private string _rent; public string Rent { get => _rent; set { _rent = value; OnPropertyChanged(); } }
        private string _lifestyle; public string Lifestyle { get => _lifestyle; set { _lifestyle = value; OnPropertyChanged(); } }

        // Новые свойства для привязки к интерфейсу
        private int _regeneration; public int Regeneration { get => _regeneration; set { _regeneration = value; OnPropertyChanged(); } }
        private int _initiative; public int Initiative { get => _initiative; set { _initiative = value; OnPropertyChanged(); } }

        // Метод автоматического обновления на основе текущего состояния статов
        public void UpdateDerivedCombatStats()
        {
            var refStat = HexStats?.FirstOrDefault(s => s.Name == "РЕА" || s.Name == "REF");
            var bodyStat = HexStats?.FirstOrDefault(s => s.Name == "ТЕЛ" || s.Name == "BODY");

            if (refStat != null) Initiative = refStat.CurrentValue;
            if (bodyStat != null) Regeneration = bodyStat.CurrentValue;
        }

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

        public void RecalculatePenalties()
        {
            if (HexStats == null || CenterSkillCategories == null) return;

            int threshold = (int)Math.Ceiling(MaxHP / 2.0);
            int woundSkillPenalty = 0;
            int woundMovePenalty = 0;
            int dsPenalty = 0;

            int visualPenalty = 0;
            int audioPenalty = 0;

            if (CurrentHP >= MaxHP)
            {
                WoundStateText = "НЕТ";
                WoundTooltipText = "ЭФФЕКТ РАНЕНИЯ:\nПерсонаж полностью здоров.\n\nСЛОЖНОСТЬ СТАБИЛИЗАЦИИ:\nНе требуется.";
            }
            else if (CurrentHP > threshold)
            {
                WoundStateText = "ЛЕГКОЕ";
                WoundTooltipText = "ЭФФЕКТ РАНЕНИЯ:\nНет штрафов к характеристикам.\n\nСЛОЖНОСТЬ СТАБИЛИЗАЦИИ:\nСложность 10.";
            }
            else if (CurrentHP > 0)
            {
                WoundStateText = "ТЯЖЕЛОЕ";
                woundSkillPenalty += 2;
                WoundTooltipText = "ЭФФЕКТ РАНЕНИЯ:\n-2 ко всем действиям.\n\nСЛОЖНОСТЬ СТАБИЛИЗАЦИИ:\nСложность 13.";
            }
            else
            {
                WoundStateText = "СМЕРТЕЛЬНОЕ";
                woundSkillPenalty += 4;
                woundMovePenalty += 6; // В SheetStat сработает ограничение "минимум 1"
                WoundTooltipText = "ЭФФЕКТ РАНЕНИЯ:\n-4 ко всем действиям. СКО снижено на 6 (минимум 1).\nКаждый ход необходимо проходить ИСПЫТАНИЕ СМЕРТИ.\n\nСЛОЖНОСТЬ СТАБИЛИЗАЦИИ:\nСложность 15.";
            }

            if (CriticalInjuriesList != null)
            {
                foreach (var inj in CriticalInjuriesList)
                {
                    woundSkillPenalty += inj.AllActionsPenalty;
                    woundMovePenalty += inj.MovePenalty;
                    dsPenalty += inj.DeathSavePenalty;
                }
            }

            var allCats = new List<SheetSkillCategory>();
            allCats.AddRange(CenterSkillCategories);
            if (RightSkillCategories1 != null) allCats.AddRange(RightSkillCategories1);
            if (RightSkillCategories2 != null) allCats.AddRange(RightSkillCategories2);

            // 1. Сначала сбрасываем специфические моды и применяем штраф за состояние здоровья
            foreach (var cat in allCats)
            {
                foreach (var skill in cat.Skills)
                {
                    skill.WoundPenalty = -woundSkillPenalty;
                    skill.EquipmentMod = 0;
                    skill.VisualMod = 0;
                    skill.AudioMod = 0;
                }
            }

            // 2. Собираем ВСЕ модификаторы из травм и имплантов в единый список
            var activeModifiers = new List<SkillModifierDef>();

            if (CriticalInjuriesList != null)
            {
                foreach (var inj in CriticalInjuriesList)
                    activeModifiers.AddRange(inj.SkillModifiers);
            }

            if (CyberwareBlocks != null)
            {
                foreach (var block in CyberwareBlocks)
                {
                    if (block.InstalledItems != null)
                    {
                        foreach (var cw in block.InstalledItems)
                            activeModifiers.AddRange(cw.SkillModifiers);
                    }
                }
            }

            // 3. Автоматически распределяем их по навыкам
            foreach (var mod in activeModifiers)
            {
                foreach (var cat in allCats)
                {
                    foreach (var skill in cat.Skills)
                    {
                        if (skill.Name == mod.SkillName)
                        {
                            if (mod.ModType == "Visual") skill.VisualMod += mod.Value;
                            else if (mod.ModType == "Audio") skill.AudioMod += mod.Value;
                            else skill.EquipmentMod += mod.Value; // Обычные баффы от имплантов
                        }
                    }
                }
            }

            var moveStat = HexStats.FirstOrDefault(s => s.Name == "СКО" || s.Name == "MOVE");
            if (moveStat != null)
            {
                moveStat.WoundPenalty = -woundMovePenalty;
                ApplyStatPenalty("СКО", "MOVE", moveStat.ArmorPenalty);
            }
            CurrentDeathSave = Math.Max(0, BaseDeathSave - dsPenalty);
        }

        private string _overlayTitle; public string OverlayTitle { get => _overlayTitle; set { _overlayTitle = value; OnPropertyChanged(); } }
        public ObservableCollection<CyberwareDef> AvailableCyberware { get; set; } = new ObservableCollection<CyberwareDef>();

        // ВОТ ЭТО ИСПРАВЛЯЕТ БАГ С ПУСТЫМ МЕНЮ:
        private CyberwareBlockItem _currentCyberBlock;
        public CyberwareBlockItem CurrentCyberBlock { get => _currentCyberBlock; set { _currentCyberBlock = value; OnPropertyChanged(); } }

        // Поля формы кастома
        private string _newCyberName; public string NewCyberName { get => _newCyberName; set { _newCyberName = value; OnPropertyChanged(); } }
        private string _newCyberDesc; public string NewCyberDesc { get => _newCyberDesc; set { _newCyberDesc = value; OnPropertyChanged(); } }
        private string _newCyberHL; public string NewCyberHL { get => _newCyberHL; set { _newCyberHL = value; OnPropertyChanged(); } }
        private string _newCyberSlots = "1"; public string NewCyberSlots { get => _newCyberSlots; set { _newCyberSlots = value; OnPropertyChanged(); } }
        private bool _newCyberIsFoundation; public bool NewCyberIsFoundation { get => _newCyberIsFoundation; set { _newCyberIsFoundation = value; OnPropertyChanged(); } }
        private string _newCyberRequires; public string NewCyberRequires { get => _newCyberRequires; set { _newCyberRequires = value; OnPropertyChanged(); } }

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
                        RoleAbilityNotes = _originalData.RoleAbilityNotes ?? "",

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

                    viewModel.MaxHP = _originalData.SystemStats != null && _originalData.SystemStats.ContainsKey("HP") ? _originalData.SystemStats["HP"] : 40;
                    viewModel.CurrentHP = _originalData.SystemStats != null && _originalData.SystemStats.ContainsKey("CurrentHP") ? _originalData.SystemStats["CurrentHP"] : viewModel.MaxHP;
                    viewModel.BaseDeathSave = _originalData.SystemStats != null && _originalData.SystemStats.ContainsKey("DeathSave") ? _originalData.SystemStats["DeathSave"] : 0;

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
                                    SkillModifiers = def.SkillModifiers // ДОБАВЛЕНО ЗДЕСЬ
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
                        // Строгий порядок: 3 колонки, 4 строки (слева-направо, сверху-вниз)
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

                        // ДОБАВЛЕНО ЗДЕСЬ: Слушаем изменения статов (включая штрафы от брони/ран)
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

                    viewModel.RecalculatePenalties();

                    // ДОБАВЛЕНО ЗДЕСЬ: Первичный расчет при загрузке файла персонажа
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
                        SkillModifiers = def.SkillModifiers // ДОБАВЛЕНО ЗДЕСЬ
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
                    _originalData.SystemStats["HP"] = vm.MaxHP;
                    _originalData.SystemStats["CurrentHP"] = vm.CurrentHP;
                    _originalData.SystemStats["DeathSave"] = vm.BaseDeathSave;

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

        private CyberwareBlockItem _currentCyberBlock;

        private void BtnOpenCyberwareManager_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is CyberwareBlockItem block)
            {
                var vm = this.DataContext as SheetViewModel;
                vm.CurrentCyberBlock = block; // Жесткая привязка для UI (исправляет баг)
                vm.OverlayTitle = $"УПРАВЛЕНИЕ: {block.Name}";
                vm.AvailableCyberware.Clear();

                foreach (var def in CoreDataBase.AllCyberware.Where(c => c.Category == block.Name))
                    vm.AvailableCyberware.Add(def);

                if (_originalData.CustomCyberwareList != null)
                {
                    foreach (var def in _originalData.CustomCyberwareList.Where(c => c.Category == block.Name))
                        vm.AvailableCyberware.Add(def);
                }

                CyberwareOverlay.Visibility = Visibility.Visible;
            }
        }

        private void BtnCloseCyberwareManager_Click(object sender, RoutedEventArgs e) => CyberwareOverlay.Visibility = Visibility.Collapsed;

        private void BtnInstallCyber_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as SheetViewModel;
            if (vm.CurrentCyberBlock == null) return;

            if ((sender as Button)?.Tag is CyberwareDef def)
            {
                // Проверка зависимостей!
                if (!string.IsNullOrWhiteSpace(def.Requires) && !vm.CurrentCyberBlock.InstalledItems.Any(i => i.Name == def.Requires))
                {
                    MessageBox.Show($"Для установки этого импланта сначала требуется установить: {def.Requires}!", "ОШИБКА СОВМЕСТИМОСТИ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (vm.CurrentCyberBlock.UsedSlots + def.Slots > vm.CurrentCyberBlock.MaxSlots)
                {
                    MessageBox.Show("В этом блоке не осталось свободных слотов!", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                vm.CurrentCyberBlock.InstalledItems.Add(def);
                vm.CurrentCyberBlock.UsedSlots += def.Slots;
            }
        }

        private void BtnUninstallCyber_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as SheetViewModel;
            if ((sender as Button)?.Tag is CyberwareDef def)
            {
                // Удаление с главной страницы или из меню
                foreach (var block in vm.CyberwareBlocks)
                {
                    if (block.InstalledItems.Contains(def))
                    {
                        if (def.IsFoundation) MessageBox.Show("Внимание: Вы удаляете базовый имплант. Проверьте слоты зависимых опций!", "СИСТЕМНОЕ УВЕДОМЛЕНИЕ", MessageBoxButton.OK, MessageBoxImage.Information);

                        block.InstalledItems.Remove(def);
                        block.UsedSlots -= def.Slots;
                        break;
                    }
                }
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
                Requires = vm.NewCyberRequires ?? "",
                IsCustom = true
            };

            if (_originalData.CustomCyberwareList == null) _originalData.CustomCyberwareList = new List<CyberwareDef>();
            _originalData.CustomCyberwareList.Add(newItem);
            vm.AvailableCyberware.Add(newItem);

            // Очищаем форму
            vm.NewCyberName = ""; vm.NewCyberDesc = ""; vm.NewCyberHL = ""; vm.NewCyberSlots = "1"; vm.NewCyberIsFoundation = false; vm.NewCyberRequires = "";
        }

        private void BtnBackToMenu_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}