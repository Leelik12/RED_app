using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
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

        // состояние NPC на листе персонажа (у пресета не заполняется)
        public int? CurrentHP { get; set; }
        public int? CurrentHeadArmor { get; set; }
        public int? CurrentBodyArmor { get; set; }

        // встроенный пресет из DLC: название пакета (пусто у пользовательских) и пояснение; в файлы персонажей не пишутся
        [System.Text.Json.Serialization.JsonIgnore] public string Source { get; set; }
        [System.Text.Json.Serialization.JsonIgnore] public string Info { get; set; }
        [System.Text.Json.Serialization.JsonIgnore] public bool IsBuiltIn => !string.IsNullOrEmpty(Source);

        // отдельная копия пресета для NPC, чтобы одинаковые враги не делили состояние
        public EnemySaveData Clone() => new EnemySaveData
        {
            Name = Name,
            HP = HP,
            HeadArmor = HeadArmor,
            BodyArmor = BodyArmor,
            IsCNMode = IsCNMode,
            CombatNumber = CombatNumber,
            BaseDeathSave = BaseDeathSave,
            BaseMove = BaseMove,
            Stats = new Dictionary<string, int>(Stats ?? new Dictionary<string, int>()),
            Skills = new Dictionary<string, int>(Skills ?? new Dictionary<string, int>()),
            Weapons = Weapons,
            Gear = Gear,
            Cyberware = Cyberware,
            Source = Source,
            Info = Info
        };

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
        public int Initiative
        {
            get => _initiative;
            set { _initiative = Math.Min(777, Math.Max(0, value)); OnPropertyChanged(); }
        }

        private string _name;
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        private int _maxHP;
        public int MaxHP
        {
            get => _maxHP;
            set
            {
                int val = Math.Min(777, Math.Max(1, value));
                if (_maxHP == val) return;
                _maxHP = val;
                if (_currentHP > _maxHP) CurrentHP = _maxHP;
                OnPropertyChanged();
                Recalculate();
            }
        }

        private int _currentHP;
        public int CurrentHP
        {
            get => _currentHP;
            set
            {
                int val = value;
                if (val > MaxHP) val = MaxHP;
                if (val < 0) val = 0;
                if (_currentHP == val) return;
                _currentHP = val;
                OnPropertyChanged();
                Recalculate();
            }
        }

        private int _headArmor;
        public int HeadArmor
        {
            get => _headArmor;
            set
            {
                int val = Math.Min(777, Math.Max(0, value));
                if (_headArmor == val) return;
                _headArmor = val;
                OnPropertyChanged();
            }
        }

        private int _bodyArmor;
        public int BodyArmor
        {
            get => _bodyArmor;
            set
            {
                int val = Math.Min(777, Math.Max(0, value));
                if (_bodyArmor == val) return;
                _bodyArmor = val;
                OnPropertyChanged();
            }
        }

        public string CombatStatsDisplay { get; set; }
        public ObservableCollection<string> DisplaySkills { get; set; } = new ObservableCollection<string>();

        // состояния боя («Оглушён, Горит») — заметка ГМа, правила к ним применяются вручную
        private string _conditions; public string Conditions { get => _conditions; set { _conditions = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasConditions)); } }
        public bool HasConditions => !string.IsNullOrWhiteSpace(_conditions);

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

        // штрафы от состояния ранения по ПЗ (стр. 187): тяжёлое — меньше половины ПЗ (округляя вверх), смертельное — меньше 1 ПЗ
        private int WoundActionPenalty => CurrentHP < 1 ? 4 : (CurrentHP < (int)Math.Ceiling(MaxHP / 2.0) ? 2 : 0);
        private int WoundMovePenalty => CurrentHP < 1 ? 6 : 0;

        public int CurrentMove => Math.Max(1, BaseMove - WoundMovePenalty - ActiveInjuries.Sum(i => i.MovePenalty));
        public int CurrentDeathSave => Math.Max(0, BaseDeathSave - ActiveInjuries.Sum(i => i.DeathSavePenalty));
        public int ActionPenalty => WoundActionPenalty + ActiveInjuries.Sum(i => i.AllActionsPenalty);
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
}
