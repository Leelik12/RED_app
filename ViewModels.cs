using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CyberpunkRED_Generator
{
    public class SheetViewModel : INotifyPropertyChanged
    {
        public string Name { get; set; }

        // --- МУЛЬТИКЛАССИРОВАНИЕ ---
        public ObservableCollection<RoleData> ActiveRoles { get; set; } = new ObservableCollection<RoleData>();
        private int _currentRoleIndex = 0;

        public RoleData CurrentActiveRole => ActiveRoles.Count > 0 ? ActiveRoles[_currentRoleIndex] : null;

        // Поле Role теперь возвращает все роли через слеш, например "Соло/Фиксер"
        public string Role => ActiveRoles.Count > 0 ? string.Join("/", ActiveRoles.Select(r => r.Name)) : "";

        // Проверка роли и её ранга с учётом мультикласса (Role может быть "Соло/Фиксер")
        public bool HasRole(string roleName) => ActiveRoles.Any(r => r.Name == roleName);
        public int GetRoleRank(string roleName) => ActiveRoles.FirstOrDefault(r => r.Name == roleName)?.Rank ?? 0;

        public int RoleRank
        {
            get => CurrentActiveRole?.Rank ?? 0;
            set
            {
                if (CurrentActiveRole != null)
                {
                    CurrentActiveRole.Rank = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SoloPointsText));
                    OnPropertyChanged(nameof(TechPointsText));
                    OnPropertyChanged(nameof(MedtechPointsText));
                    OnPropertyChanged(nameof(NetrunnerTotal));
                    OnPropertyChanged(nameof(NetrunnerActions));
                    RecalculatePenalties();
                }
            }
        }

        public string RoleDescription
        {
            get
            {
                switch (CurrentActiveRole?.Name)
                {
                    case "Рокербой": return "ХАРИЗМАТИЧЕСКОЕ ВЛИЯНИЕ\nВлияй на окружающих силой личности. Превращай людей в фанатов (Бросок: СЛ8 — 1 чел, СЛ10 — до 6 чел, СЛ12 — толпа).\n\nРанги 1-2 (Местные клубы): Убедить фаната на мелкую услугу (купить еду, подвезти).\nРанги 3-4 (Известные клубы): Фанат окажет крупную услугу. Группа будет тусоваться с тобой.\nРанги 5-6 (Крупные клубы): Фанат совершит мелкое преступление (кража, драка). Группа станет личной свитой.\nРанги 7-8 (Концертные залы): Фанат рискнёт жизнью. Группа пойдёт на мелкое преступление.\nРанг 9 (ТВ/Крупные залы): Фанат и группа до 6 человек пойдут на тяжкое преступление (кража дорогой вещи, избиение). Толпа фанатов устроит беспорядки, разрушит имущество и даже убьёт.\nРанг 10 (Стадионы): Фанаты пожертвуют собой без вопросов. Группа рискует жизнями как твоя охрана. Частная армия.";
                    case "Нетраннер": return "ИНТЕРФЕЙС\nПозволяет заниматься нетраннингом, определяет количество сетевых действий в ход и даёт доступ к программам.\n\nСЕТЕВЫЕ ДЕЙСТВИЯ ЗА ХОД:\nРанг 1–3: 2 действия\nРанг 4–6: 3 действия\nРанг 7–9: 4 действия\nРанг 10: 5 действий";
                    case "Медиа": return "АВТОРИТЕТНОСТЬ\nДоступ к источникам и убеждение аудитории. Ты пассивно подхватываешь слухи (СЛ 7-13).\n\nУбедительность: Шанс того, что тебе поверят (бросок 1d10 <= Убедительность). Одно понятное проверяемое доказательство даёт +1, больше 4 — +2; бонусы складываются (до +3). УДЧ тратить нельзя.\nРанги 1-2: Местные связи. Убедительность 2/10. Влияние на мелких злодеев.\nРанги 3-4: Городские лидеры. Убедительность 3/10. Торжество локальной справедливости.\nРанги 5-6: Крупные игроки. Убедительность 4/10. Влияние на весь город.\nРанги 7-8: Президенты корп. Убедительность 5/10. Влияние на штат.\nРанг 9: Политики штата. Убедительность 6/10. Влияние на страну.\nРанг 10: Мировые лидеры. Убедительность 7/10. Свержение мегакорпораций и мировых правительств.";
                    case "Законник": return "ПОДКРЕПЛЕНИЕ\nПризыв сослуживцев. Действием брось 1d10 <= ранга. При успехе брось 1d6 — столько раундов до прибытия. Выпала 6 — прибудет отряд следующего уровня (на 10 ранге — две группы).\n\nБП = боевой показатель, СКО/ТЕЛ — оба значения.\nРанги 1-2: 4 наёмных копа пешком (БП 8, ОС 7, ПЗ 20, СКО/ТЕЛ 4).\nРанги 3-4: 4 патрульных на двух авто (БП 10, ОС 7, ПЗ 25, СКО/ТЕЛ 5).\nРанги 5-7: 2 шерифа на скоростном авто, тяж. бронекуртка (БП 14, ОС 13, ПЗ 35, СКО/ТЕЛ 4).\nРанг 8: Маршал Зоны на супербайке (БП 16, ОС 15, ПЗ 50, СКО/ТЕЛ 6, гранатомёт).\nРанг 9: C-SWAT на AV-4 (БП 15, ОС 18, ПЗ 35, СКО/ТЕЛ 4, ракетницы).\nРанг 10: Агенты ФБР/Интерпола на AV-4 (БП 14, ОС 11, ПЗ 35, СКО/ТЕЛ 6). Остаются помогать в расследовании дела.";
                    case "Корпорат": return "КОМАНДНАЯ РАБОТА\nКорпоративные блага и команда подчиненных (проверка лояльности подчинённого: 1d6 меньше его лояльности).\n\nРанг 1: Комплект элитной одежды.\nРанг 2: Бесплатный корпоративный конапт.\nРанг 3: 1 член команды (подчинённый).\nРанг 5: 2 члена команды.\nРанг 6: Медстраховка Trauma Team Silver.\nРанг 7: Улучшение жилья до дома в бивервилле.\nРанг 8: Медстраховка Trauma Team Executive.\nРанг 9: 3 члена команды.\nРанг 10: Люксовый пентхаус или роскошный особняк.";
                    case "Фиксер": return "ДЕЛОВАЯ ХВАТКА\nСвязи, сделки и культурная гибкость.\n\nОХВАТ (высшая категория предметов, которую всегда можно достать поштучно):\nРанги 1-2: Дёшево и повседневно.\nРанги 3-4: До «дорого».\nРанги 5-6: Раз в месяц, вместе с фиксерами своего ранга, можно организовать Ночной рынок: на нём доступно всё до «супер роскоши».\nРанги 7-8: До «очень дорого».\nРанг 9: До «роскоши» + Полуночный рынок внутри Ночного (верхушка криминального мира).\nРанг 10: До «супер роскоши».\n\nТОРГ (КРУТ + Торговля + ранг + 1d10; при успехе одна сделка уровня твоего ранга или ниже):\nРанги 1-2: ±10% к рыночной цене при покупке и продаже.\nРанги 3-4: При покупке 5 и более одинаковых предметов — ещё один бесплатно.\nРанги 5-6: +20% к оплате за работу на человека.\nРанги 7-8: Роскошь и супер роскошь — половина сейчас, половина через месяц.\nРанг 9: ±20% к рыночной цене.\nРанг 10: Двойная оплата за опасную работу.\n\nГИБКОСТЬ (культуры и языки):\nРанги 1-2: Твой район и все местные банды.\nРанги 3-4: 1 культура + её язык на уровне 4.\nРанги 5-6: 3 культуры + их языки на уровне 4.\nРанги 7-8: 6 культур + их языки на уровне 4.\nРанг 9: Ещё и корпорации и правительственные структуры.\nРанг 10: Любые группы, даже закрытые (тайные общества, секты).";
                    case "Кочевник": return "МОТО\nСемья на колёсах. Добавляет ранг Мото к проверкам: Вождение, Пилотирование, Судовождение, Авиатехника, Автомеханика, Судоремонт.\n\nСЕМЕЙНЫЙ АВТОПАРК:\nКаждый раз повышая ранг, выбери одно:\n1) Добавить 1 стандартный транспорт (ранга Мото или ниже) в свой пул.\n2) Установить 1 улучшение (броню, оружие, NOS и т.д.) на уже доступный транспорт.\n\nКочевник использует 1 семейный транспорт за раз (замена к следующему утру). Семья чинит разбитый транспорт за неделю (и штраф 500eb). На 10 ранге можно выводить весь свой автопарк одновременно.";
                    default: return "";
                }
            }
        }

        public Visibility IsSoloVis => CurrentActiveRole?.Name == "Соло" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsTechVis => CurrentActiveRole?.Name == "Техник" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsMedtechVis => CurrentActiveRole?.Name == "Медтехник" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsNetrunnerVis => CurrentActiveRole?.Name == "Нетраннер" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsTextRoleVis => (CurrentActiveRole?.Name != "Соло" && CurrentActiveRole?.Name != "Техник" && CurrentActiveRole?.Name != "Медтехник" && CurrentActiveRole?.Name != "Нетраннер") ? Visibility.Visible : Visibility.Collapsed;

        public void ChangeRoleIndex(int newIndex)
        {
            if (ActiveRoles.Count == 0) return;
            _currentRoleIndex = newIndex;
            if (_currentRoleIndex < 0) _currentRoleIndex = ActiveRoles.Count - 1;
            if (_currentRoleIndex >= ActiveRoles.Count) _currentRoleIndex = 0;

            OnPropertyChanged(nameof(CurrentActiveRole));
            OnPropertyChanged(nameof(Role));
            OnPropertyChanged(nameof(RoleRank));
            OnPropertyChanged(nameof(RoleDescription));
            OnPropertyChanged(nameof(IsSoloVis));
            OnPropertyChanged(nameof(IsTechVis));
            OnPropertyChanged(nameof(IsMedtechVis));
            OnPropertyChanged(nameof(IsNetrunnerVis));
            OnPropertyChanged(nameof(IsTextRoleVis));

            // Обновляем счетчики очков конкретной роли
            OnPropertyChanged(nameof(SoloPointsText));
            OnPropertyChanged(nameof(TechPointsText));
            OnPropertyChanged(nameof(MedtechPointsText));

            // ИСПРАВЛЕНИЕ: Обновляем статы Нетраннера при переключении
            OnPropertyChanged(nameof(NetrunnerTotal));
            OnPropertyChanged(nameof(NetrunnerActions));
        }

        public void RemoveCurrentRole()
        {
            if (ActiveRoles.Count <= 1 || CurrentActiveRole == null) return;

            string roleToRemove = CurrentActiveRole.Name;

            // Обнуляем способности удаляемой роли, чтобы скрытые статы не влияли на математику
            if (roleToRemove == "Соло")
            {
                SoloThreat = 0; SoloAwareness = 0; SoloDeflection = 0;
                SoloFumble = 0; SoloInitiative = 0; SoloPrecision = 0;
            }
            else if (roleToRemove == "Техник")
            {
                TechField = 0; TechUpgrade = 0; TechFab = 0; TechInvent = 0;
            }
            else if (roleToRemove == "Медтехник")
            {
                MedSurgery = 0; MedPharma = 0; MedCryo = 0;
            }

            // Удаляем роль
            ActiveRoles.RemoveAt(_currentRoleIndex);

            // Переключаемся на предыдущую роль (чтобы индекс не вышел за пределы списка)
            ChangeRoleIndex(Math.Max(0, _currentRoleIndex - 1));

            // Пересчитываем скиллы, так как пассивные бонусы от роли могли пропасть
            RecalculatePenalties();
        }

        // --- УПРОЩЕННОЕ СНАРЯЖЕНИЕ ---
        private bool _isGearSimpleMode;
        public bool IsGearSimpleMode { get => _isGearSimpleMode; set { _isGearSimpleMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(GearListVis)); OnPropertyChanged(nameof(GearSimpleVis)); } }
        public Visibility GearListVis => IsGearSimpleMode ? Visibility.Collapsed : Visibility.Visible;
        public Visibility GearSimpleVis => IsGearSimpleMode ? Visibility.Visible : Visibility.Collapsed;

        private string _gearSimpleNames; public string GearSimpleNames { get => _gearSimpleNames; set { _gearSimpleNames = value; OnPropertyChanged(); } }
        private string _gearSimpleNotes; public string GearSimpleNotes { get => _gearSimpleNotes; set { _gearSimpleNotes = value; OnPropertyChanged(); } }

        // --- Вкладка NPC ---
        public ObservableCollection<CombatantViewModel> NPCs { get; set; } = new ObservableCollection<CombatantViewModel>();
        public ObservableCollection<EnemySaveData> AvailableEnemies { get; set; } = new ObservableCollection<EnemySaveData>();

        // --- СОЛО ---
        private int _soloThreat; public int SoloThreat { get => _soloThreat; set { _soloThreat = value; OnPropertyChanged(); OnPropertyChanged(nameof(SoloPointsText)); RecalculatePenalties(); } }
        private int _soloAwareness; public int SoloAwareness { get => _soloAwareness; set { _soloAwareness = value; OnPropertyChanged(); OnPropertyChanged(nameof(SoloPointsText)); } }
        private int _soloDeflection; public int SoloDeflection { get => _soloDeflection; set { _soloDeflection = value; OnPropertyChanged(); OnPropertyChanged(nameof(SoloPointsText)); } }
        private int _soloFumble; public int SoloFumble { get => _soloFumble; set { _soloFumble = value; OnPropertyChanged(); OnPropertyChanged(nameof(SoloPointsText)); } }
        private int _soloInitiative; public int SoloInitiative { get => _soloInitiative; set { _soloInitiative = value; OnPropertyChanged(); OnPropertyChanged(nameof(SoloPointsText)); RecalculatePenalties(); } }
        private int _soloPrecision; public int SoloPrecision { get => _soloPrecision; set { _soloPrecision = value; OnPropertyChanged(); OnPropertyChanged(nameof(SoloPointsText)); } }
        public int SoloPointsUsed => SoloThreat * 1 + SoloAwareness * 1 + SoloDeflection * 2 + SoloFumble * 4 + SoloInitiative * 1 + SoloPrecision * 3;
        public string SoloPointsText => $"СВОБОДНЫХ ОЧКОВ: {RoleRank - SoloPointsUsed}";

        // --- ТЕХНИК ---
        private int _techField; public int TechField { get => _techField; set { _techField = value; OnPropertyChanged(); OnPropertyChanged(nameof(TechPointsText)); RecalculatePenalties(); } }
        private int _techUpgrade; public int TechUpgrade { get => _techUpgrade; set { _techUpgrade = value; OnPropertyChanged(); OnPropertyChanged(nameof(TechPointsText)); } }
        private int _techFab; public int TechFab { get => _techFab; set { _techFab = value; OnPropertyChanged(); OnPropertyChanged(nameof(TechPointsText)); } }
        private int _techInvent; public int TechInvent { get => _techInvent; set { _techInvent = value; OnPropertyChanged(); OnPropertyChanged(nameof(TechPointsText)); } }
        public int TechPointsUsed => TechField + TechUpgrade + TechFab + TechInvent;
        // за каждый ранг Создателя — по 1 очку в две разные специализации (стр. 148)
        public int TechPointsTotal => RoleRank * 2;
        public string TechPointsText => $"СВОБОДНЫХ ОЧКОВ: {TechPointsTotal - TechPointsUsed}";

        // --- МЕДТЕХНИК ---
        private int _medSurgery; public int MedSurgery { get => _medSurgery; set { _medSurgery = value; OnPropertyChanged(); OnPropertyChanged(nameof(MedtechPointsText)); OnPropertyChanged(nameof(MedSurgeryTotal)); RecalculatePenalties(); } }
        private int _medPharma; public int MedPharma { get => _medPharma; set { _medPharma = value; OnPropertyChanged(); OnPropertyChanged(nameof(MedtechPointsText)); OnPropertyChanged(nameof(MedicalTechTotal)); RecalculatePenalties(); } }
        private int _medCryo; public int MedCryo { get => _medCryo; set { _medCryo = value; OnPropertyChanged(); OnPropertyChanged(nameof(MedtechPointsText)); OnPropertyChanged(nameof(MedicalTechTotal)); RecalculatePenalties(); } }

        public int MedtechPointsUsed => MedSurgery + MedPharma + MedCryo;
        public string MedtechPointsText => $"СВОБОДНЫХ ОЧКОВ: {RoleRank - MedtechPointsUsed}";

        public int BaseTech => HexStats?.FirstOrDefault(s => s.Name == "ТЕХ" || s.Name == "TECH")?.CurrentValue ?? 0;

        // Итоговые значения навыков по правилам:
        public int MedSurgeryTotal => BaseTech + (MedSurgery * 2);
        public int MedicalTechTotal => BaseTech + MedPharma + MedCryo;
        // --- НЕТРАННЕР ---
        // проверки нетраннинга: Интерфейс + 1d10, без характеристики (стр. 199)
        public int NetrunnerTotal => RoleRank;
        public int NetrunnerActions
        {
            get
            {
                if (RoleRank >= 10) return 5;
                if (RoleRank >= 7) return 4;
                if (RoleRank >= 4) return 3;
                return 2;
            }
        }

        // подсвечивает навыки, подходящие под строку поиска, остальные делает тусклыми
        public void ApplySkillSearch(string text)
        {
            string q = (text ?? "").Trim();
            var cats = new List<SheetSkillCategory>();
            if (CenterSkillCategories != null) cats.AddRange(CenterSkillCategories);
            if (RightSkillCategories1 != null) cats.AddRange(RightSkillCategories1);
            if (RightSkillCategories2 != null) cats.AddRange(RightSkillCategories2);
            foreach (var cat in cats)
                foreach (var sk in cat.Skills)
                {
                    bool match = q.Length == 0
                        || (sk.Name ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                        || (sk.SubName ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
                    sk.SearchOpacity = match ? 1.0 : 0.2;
                }
        }

        // Вспомогательный метод для добавления бонусов роли к скиллам
        public void ApplyRoleModToSkill(string skillName, int value)
        {
            var allCats = new List<SheetSkillCategory>();
            if (CenterSkillCategories != null) allCats.AddRange(CenterSkillCategories);
            if (RightSkillCategories1 != null) allCats.AddRange(RightSkillCategories1);
            if (RightSkillCategories2 != null) allCats.AddRange(RightSkillCategories2);

            foreach (var cat in allCats)
                foreach (var skill in cat.Skills)
                    if (skill.Name == skillName || skill.BaseName == skillName)
                        skill.RoleMod += value;
        }

        public List<SheetStat> HexStats { get; set; }
        public Dictionary<string, int> Stats { get; set; }
        public Dictionary<string, int> SystemStats { get; set; }

        private int _currentHumanity;
        public int CurrentHumanity
        {
            get => _currentHumanity;
            set
            {
                int val = value;
                if (val < -777) val = -777;
                if (val > MaxHumanity) val = MaxHumanity;
                _currentHumanity = val;
                OnPropertyChanged();
                UpdateEmp();
            }
        }

        private int _maxHumanity;
        public int MaxHumanity
        {
            get => _maxHumanity;
            set
            {
                int baseEmp = Stats != null && Stats.ContainsKey("EMP") ? Stats["EMP"] : (Stats != null && Stats.ContainsKey("ЭМП") ? Stats["ЭМП"] : 5);
                int limit = baseEmp * 10;
                int val = Math.Max(0, Math.Min(limit, value));

                _maxHumanity = val;
                if (_currentHumanity > _maxHumanity) CurrentHumanity = _maxHumanity;
                OnPropertyChanged();
            }
        }

        // здоровье и травмы
        private int _currentHP;
        public int CurrentHP
        {
            get => _currentHP;
            set
            {
                int val = Math.Max(0, Math.Min(MaxHP, value));
                if (_currentHP == val) return;
                _currentHP = val;
                OnPropertyChanged();
                RecalculatePenalties();
            }
        }

        private int _maxHP;
        public int MaxHP
        {
            get => _maxHP;
            set
            {
                if (_maxHP == value) return;
                _maxHP = value;
                // текущие ПЗ не могут быть выше максимума (например, после снятия импланта, дающего ТЕЛ)
                if (_currentHP > _maxHP) { _currentHP = _maxHP; OnPropertyChanged(nameof(CurrentHP)); }
                OnPropertyChanged();
                RecalculatePenalties();
            }
        }

        private int _baseDeathSave;
        public int BaseDeathSave { get => _baseDeathSave; set { if (_baseDeathSave == value) return; _baseDeathSave = value; OnPropertyChanged(); RecalculatePenalties(); } }

        private int _currentDeathSave; public int CurrentDeathSave { get => _currentDeathSave; set { _currentDeathSave = value; OnPropertyChanged(); } }
        private string _woundStateText; public string WoundStateText { get => _woundStateText; set { _woundStateText = value; OnPropertyChanged(); } }
        private string _woundTooltipText; public string WoundTooltipText { get => _woundTooltipText; set { _woundTooltipText = value; OnPropertyChanged(); } }

        // --- Состояния чекбоксов для спасбросков ---
        private bool _ds0; public bool DS0 { get => _ds0; set { _ds0 = value; OnPropertyChanged(); RecalculatePenalties(); } }
        private bool _ds1; public bool DS1 { get => _ds1; set { _ds1 = value; OnPropertyChanged(); RecalculatePenalties(); } }
        private bool _ds2; public bool DS2 { get => _ds2; set { _ds2 = value; OnPropertyChanged(); RecalculatePenalties(); } }
        private bool _ds3; public bool DS3 { get => _ds3; set { _ds3 = value; OnPropertyChanged(); RecalculatePenalties(); } }
        private bool _ds4; public bool DS4 { get => _ds4; set { _ds4 = value; OnPropertyChanged(); RecalculatePenalties(); } }
        private bool _ds5; public bool DS5 { get => _ds5; set { _ds5 = value; OnPropertyChanged(); RecalculatePenalties(); } }
        private bool _ds6; public bool DS6 { get => _ds6; set { _ds6 = value; OnPropertyChanged(); RecalculatePenalties(); } }
        private bool _ds7; public bool DS7 { get => _ds7; set { _ds7 = value; OnPropertyChanged(); RecalculatePenalties(); } }
        private bool _ds8; public bool DS8 { get => _ds8; set { _ds8 = value; OnPropertyChanged(); RecalculatePenalties(); } }
        private bool _ds9; public bool DS9 { get => _ds9; set { _ds9 = value; OnPropertyChanged(); RecalculatePenalties(); } }
        // ------------------------------------------

        public ObservableCollection<CriticalInjuryItem> CriticalInjuriesList { get; set; }
        public List<string> AvailableInjuries => CoreDataBase.CriticalInjuries.Select(x => x.Name).ToList();
        private string _selectedInjuryToAdd; public string SelectedInjuryToAdd { get => _selectedInjuryToAdd; set { _selectedInjuryToAdd = value; OnPropertyChanged(); } }
        private string _addictions; public string Addictions { get => _addictions; set { _addictions = value; OnPropertyChanged(); } }

        private string _chassis;
        public string Chassis { get => _chassis; set { _chassis = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsBorg)); } }
        public bool IsBorg => !string.IsNullOrEmpty(_chassis);

        private string _notes; public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }
        private string _roleAbilityNotes; public string RoleAbilityNotes { get => _roleAbilityNotes; set { _roleAbilityNotes = value; OnPropertyChanged(); } }

        public ObservableCollection<GearRowItem> GearItems { get; set; }
        public ObservableCollection<CyberwareBlockItem> CyberwareBlocks { get; set; }

        private string _ammoValue; public string AmmoValue { get => _ammoValue; set { _ammoValue = value; OnPropertyChanged(); } }
        private string _cashValue; public string CashValue { get => _cashValue; set { _cashValue = value; OnPropertyChanged(); } }
        private string _styleNotes; public string StyleNotes { get => _styleNotes; set { _styleNotes = value; OnPropertyChanged(); } }
        private int _improvementPoints; public int ImprovementPoints { get => _improvementPoints; set { _improvementPoints = value; OnPropertyChanged(); } }
        private int _reputation; public int Reputation { get => _reputation; set { _reputation = value; OnPropertyChanged(); } }
        private string _reputationEvents; public string ReputationEvents { get => _reputationEvents; set { _reputationEvents = value; OnPropertyChanged(); } }
        private string _housing; public string Housing { get => _housing; set { _housing = value; OnPropertyChanged(); } }
        private string _rent; public string Rent { get => _rent; set { _rent = value; OnPropertyChanged(); } }
        private string _lifestyle; public string Lifestyle { get => _lifestyle; set { _lifestyle = value; OnPropertyChanged(); } }

        private int _regeneration; public int Regeneration { get => _regeneration; set { _regeneration = value; OnPropertyChanged(); } }
        private string _initiative; public string Initiative { get => _initiative; set { _initiative = value; OnPropertyChanged(); } }
        public void UpdateDerivedCombatStats()
        {
            var refStat = HexStats?.FirstOrDefault(s => s.Name == "РЕА" || s.Name == "REF");
            var bodyStat = HexStats?.FirstOrDefault(s => s.Name == "ТЕЛ" || s.Name == "BODY");

            if (bodyStat != null)
            {
                bool hasAntibodies = CyberwareBlocks?.Any(b => b.InstalledItems.Any(i => i.Name == "Усиленные антитела")) == true;
                Regeneration = hasAntibodies ? bodyStat.CurrentValue * 2 : bodyStat.CurrentValue;
            }

            if (refStat != null)
            {
                int baseInit = refStat.CurrentValue;
                if (HasRole("Соло")) baseInit += SoloInitiative;
                bool hasKerenzikov = CyberwareBlocks?.Any(b => b.InstalledItems.Any(i => i.Name == "Керензиков")) == true;
                bool hasSandevistan = CyberwareBlocks?.Any(b => b.InstalledItems.Any(i => i.Name == "Сандевистан")) == true;

                if (hasKerenzikov) baseInit += 2;

                if (hasSandevistan)
                {
                    Initiative = $"{baseInit} / {baseInit + 3} акт";
                }
                else
                {
                    Initiative = baseInit.ToString();
                }
            }
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
        public NetrunData Net { get; set; } = new NetrunData();
        public FinanceData Finance { get; set; } = new FinanceData();
        public AgentState Agent { get; set; } = new AgentState();
        public HqData Hq { get; set; } = new HqData();
        public JournalData Journal { get; set; } = new JournalData();
        public EffectsData Effects { get; set; } = new EffectsData();
        public ObservableCollection<OwnedVehicle> Vehicles { get; set; } = new ObservableCollection<OwnedVehicle>();
        public List<SheetSkillCategory> CenterSkillCategories { get; set; }
        public List<SheetSkillCategory> RightSkillCategories1 { get; set; }
        public List<SheetSkillCategory> RightSkillCategories2 { get; set; }

        public ObservableCollection<LifepathItem> Lifepath { get; set; }
        public ObservableCollection<LifepathItem> RoleLifepath { get; set; }
        public ObservableCollection<StringItem> Friends { get; set; }
        public ObservableCollection<StringItem> TragicLoves { get; set; }
        public List<EnemyData> Enemies { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void Armor_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != null && e.PropertyName.EndsWith("Penalty")) UpdateArmorPenalty();
        }

        private void UpdateArmorPenalty()
        {
            if (HexStats == null) return;
            int hp = 0, bp = 0;
            string headStr = Armor?.HeadPenalty?.Replace("-", "")?.Trim();
            string bodyStr = Armor?.BodyPenalty?.Replace("-", "")?.Trim();

            int.TryParse(headStr, out hp);
            int.TryParse(bodyStr, out bp);

            // штраф даёт только броня на голове и теле, у щита штрафа нет (стр. 183)
            int maxPenalty = Math.Max(hp, bp);

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

            var installedCyberware = new List<CyberwareDef>();
            if (CyberwareBlocks != null)
            {
                foreach (var block in CyberwareBlocks)
                    installedCyberware.AddRange(block.InstalledItems);
            }

            // --- РАСЧЕТ ТЕЛО И ПЗ ---
            var bodyStat = HexStats.FirstOrDefault(s => s.Name == "ТЕЛ" || s.Name == "BODY");
            if (bodyStat != null)
            {
                int baseBody = bodyStat.BaseValue;
                int muscleLaceCount = installedCyberware.Count(c => c.Name == "Искусственные мышцы и усиленные кости");
                int effectiveBody = baseBody + (muscleLaceCount * 2);
                if (effectiveBody > 10) effectiveBody = 10;

                if (installedCyberware.Any(c => c.Name == "Эндоскелет ∑ (Сигма)")) effectiveBody = 12;
                if (installedCyberware.Any(c => c.Name == "Эндоскелет ß (Бета)")) effectiveBody = 14;
                if (installedCyberware.Any(c => c.Name == "Эндоскелет Ω (Омега)")) effectiveBody = 16;
                var borgChassis = Borg.Find(Chassis);
                if (borgChassis != null) effectiveBody = Math.Max(borgChassis.EndoBody, effectiveBody);

                bodyStat.ImplantModifier = effectiveBody - baseBody;

                int will = HexStats.FirstOrDefault(s => s.Name == "ВОЛЯ" || s.Name == "WILL")?.CurrentValue ?? 5;
                MaxHP = 10 + (5 * (int)Math.Ceiling((effectiveBody + will) / 2.0));
                BaseDeathSave = effectiveBody;
            }

            int baseEmp = Stats != null && Stats.ContainsKey("EMP") ? Stats["EMP"] : (Stats != null && Stats.ContainsKey("ЭМП") ? Stats["ЭМП"] : 5);
            int maxHum = baseEmp * 10;

            var chassisInfo = Borg.Find(Chassis);
            if (IsBorg)
            {
                maxHum -= Borg.BiosystemMaxHumanityLoss;
                if (chassisInfo != null) maxHum -= chassisInfo.MaxHumanityLoss;
            }
            foreach (var cw in installedCyberware)
            {
                if (IsBorg && Borg.IsFreeInBorg(cw)) continue;
                if (cw.HumanityLoss != "0" && cw.HumanityLoss != "0 (—)")
                {
                    maxHum -= cw.Category.Contains("БОРГИРОВАНИЕ") ? 4 : 2;
                }
            }
            MaxHumanity = maxHum;
            if (CurrentHumanity > MaxHumanity) CurrentHumanity = MaxHumanity;

            var audioBlock = CyberwareBlocks?.FirstOrDefault(b => b.Name == "КИБЕРАУДИО (CYBERAUDIO)");
            if (audioBlock != null)
            {
                bool hasArray = installedCyberware.Any(c => c.Name == "Сенсорный массив");
                audioBlock.MaxSlots = (hasArray ? 8 : 3) * (audioBlock.BorgDoubled ? 2 : 1);
            }

            int threshold = (int)Math.Ceiling(MaxHP / 2.0);
            int woundSkillPenalty = 0;
            int woundMovePenalty = 0;
            int dsPenalty = 0;

            if (CurrentHP >= MaxHP) { WoundStateText = "НЕТ"; WoundTooltipText = "Персонаж полностью здоров."; }
            else if (CurrentHP >= threshold) { WoundStateText = "ЛЕГКОЕ"; WoundTooltipText = "СЛОЖНОСТЬ СТАБИЛИЗАЦИИ: 10"; }
            else if (CurrentHP > 0)
            {
                WoundStateText = "ТЯЖЕЛОЕ";
                if (installedCyberware.Any(c => c.Name.Contains("Подавитель боли")))
                {
                    WoundTooltipText = "ЭФФЕКТ: -2 ко всем действиям (ИГНОРИРУЕТСЯ ПОДАВИТЕЛЕМ БОЛИ).\nСЛОЖНОСТЬ СТАБИЛИЗАЦИИ: 13";
                }
                else
                {
                    woundSkillPenalty += 2;
                    WoundTooltipText = "ЭФФЕКТ: -2 ко всем действиям.\nСЛОЖНОСТЬ СТАБИЛИЗАЦИИ: 13";
                }
            }
            else
            {
                WoundStateText = "СМЕРТЕЛЬНОЕ"; woundSkillPenalty += 4; woundMovePenalty += 6;
                WoundTooltipText = "ЭФФЕКТ: -4 к действиям, -6 СКО.\nСЛОЖНОСТЬ СТАБИЛИЗАЦИИ: 15";
            }

            // ПКТ: иммунитет к штрафам навыков и статов от состояний ранения (спасброски остаются)
            if (IsBorg)
            {
                if (woundSkillPenalty > 0 || woundMovePenalty > 0) WoundTooltipText = "ПКТ: штрафы состояния ранения не действуют.\n" + WoundTooltipText;
                woundSkillPenalty = 0;
                woundMovePenalty = 0;
            }

            // ПКТ: внутренняя гидравлика поднимает РЕА, ЛВК и СКО до 4/6/8
            int hydraulics = IsBorg ? Math.Max(Borg.Find(Chassis)?.HydraulicFloor ?? 0, Borg.HydraulicFloor(installedCyberware.Select(c => c.Name))) : 0;
            foreach (var pair in new[] { new[] { "РЕА", "REF" }, new[] { "ЛВК", "DEX" }, new[] { "СКО", "MOVE" } })
            {
                var st = HexStats.FirstOrDefault(s => s.Name == pair[0] || s.Name == pair[1]);
                if (st != null) st.ImplantModifier = hydraulics > st.BaseValue ? hydraulics - st.BaseValue : 0;
            }

            // === ПЕРЕМЕЩЕННАЯ ЛОГИКА ТРАВМ (Считаем ВСЕ штрафы до применения к навыкам) ===
            var activeModifiers = new List<SkillModifierDef>();

            if (CriticalInjuriesList != null)
            {
                foreach (var inj in CriticalInjuriesList)
                {
                    if (inj.SkillModifiers != null)
                        activeModifiers.AddRange(inj.SkillModifiers);
                    dsPenalty += inj.DeathSavePenalty;
                    woundMovePenalty += inj.MovePenalty;
                    woundSkillPenalty += inj.AllActionsPenalty;
                }
            }
            // =================================

            var allCats = new List<SheetSkillCategory>();
            allCats.AddRange(CenterSkillCategories);
            if (RightSkillCategories1 != null) allCats.AddRange(RightSkillCategories1);
            if (RightSkillCategories2 != null) allCats.AddRange(RightSkillCategories2);

            foreach (var cat in allCats)
            {
                foreach (var skill in cat.Skills)
                {
                    skill.WoundPenalty = -woundSkillPenalty;
                    skill.EquipmentMod = 0;
                    skill.VisualMod = 0;
                    skill.AudioMod = 0;
                    skill.RoleMod = 0;
                }
            }

            // <--- СРАЗУ ПОСЛЕ ЦИКЛА ДОБАВЛЯЕМ ЛОГИКУ БОНУСОВ РОЛЕЙ --->
            if (HasRole("Фиксер"))
            {
                // по книге ранг Деловой хватки добавляется только в бросок торга (КРУТ + Торговля + ранг), показываем его в Торговле
                ApplyRoleModToSkill("Торговля", GetRoleRank("Фиксер"));
            }
            if (HasRole("Кочевник"))
            {
                int motoRank = GetRoleRank("Кочевник");
                ApplyRoleModToSkill("Авиационные технологии", motoRank);
                ApplyRoleModToSkill("Автомеханика", motoRank);
                ApplyRoleModToSkill("Морские технологии", motoRank);

                ApplyRoleModToSkill("Вождение", motoRank);
                ApplyRoleModToSkill("Пилотирование (x2)", motoRank);
                ApplyRoleModToSkill("Судоходство", motoRank);
            }
            if (HasRole("Соло"))
            {
                ApplyRoleModToSkill("Внимательность", SoloThreat);
            }
            if (HasRole("Техник"))
            {
                ApplyRoleModToSkill("Знание техники", TechField);
                ApplyRoleModToSkill("Кибертехника", TechField);
                ApplyRoleModToSkill("Электроника/Безопасность (x2)", TechField);
                ApplyRoleModToSkill("Автомеханика", TechField);
                ApplyRoleModToSkill("Морские технологии", TechField);
                ApplyRoleModToSkill("Авиационные технологии", TechField);
                ApplyRoleModToSkill("Оружейник", TechField);
            }

            var appliedCyberwareMods = new HashSet<string>();
            foreach (var cw in installedCyberware)
            {
                if (!appliedCyberwareMods.Contains(cw.Name))
                {
                    if (cw.SkillModifiers != null)
                        activeModifiers.AddRange(cw.SkillModifiers);
                    appliedCyberwareMods.Add(cw.Name);
                }
            }

            // Удаляем бонус Светотату, который мог автоматически подтянуться из БД (чтобы не было дублирования)
            activeModifiers.RemoveAll(m => m.SkillName == "Гардероб и стиль" && installedCyberware.Any(c => c.Name == "Светотату" && c.SkillModifiers != null && c.SkillModifiers.Contains(m)));

            // Добавляем бонус только если установлено 3 и более штук
            if (installedCyberware.Count(c => c.Name == "Светотату") >= 3)
            {
                activeModifiers.Add(new SkillModifierDef { SkillName = "Гардероб и стиль", Value = 2 });
            }

            if (installedCyberware.Any(c => c.Name == "Химкожа") && installedCyberware.Any(c => c.Name == "Техноволосы"))
                activeModifiers.Add(new SkillModifierDef { SkillName = "Уход за собой", Value = 2 });

            foreach (var mod in activeModifiers)
            {
                foreach (var cat in allCats)
                {
                    foreach (var skill in cat.Skills)
                    {
                        if (skill.Name == mod.SkillName || skill.BaseName == mod.SkillName)
                        {
                            if (mod.ModType == "Visual") skill.VisualMod += mod.Value;
                            else if (mod.ModType == "Audio") skill.AudioMod += mod.Value;
                            else skill.EquipmentMod += mod.Value;
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

            // --- РАСЧЕТ ИТОГОВОГО СПАСБРОСКА ОТ СМЕРТИ (Включая чекбоксы ручных штрафов) ---
            int manualDsPenalty = (DS0 ? 1 : 0) + (DS1 ? 1 : 0) + (DS2 ? 1 : 0) + (DS3 ? 1 : 0) + (DS4 ? 1 : 0) + (DS5 ? 1 : 0) + (DS6 ? 1 : 0) + (DS7 ? 1 : 0) + (DS8 ? 1 : 0) + (DS9 ? 1 : 0);
            CurrentDeathSave = Math.Max(0, BaseDeathSave - dsPenalty - manualDsPenalty);

            OnPropertyChanged(nameof(MedSurgeryTotal));
            OnPropertyChanged(nameof(MedicalTechTotal));

            OnPropertyChanged(nameof(NetrunnerTotal));
            OnPropertyChanged(nameof(NetrunnerActions));

            UpdateDerivedCombatStats();
        }

        // управление имплантами
        private string _overlayTitle; public string OverlayTitle { get => _overlayTitle; set { _overlayTitle = value; OnPropertyChanged(); } }
        public ObservableCollection<CyberwareDef> AvailableCyberware { get; set; } = new ObservableCollection<CyberwareDef>();

        private CyberwareBlockItem _currentCyberBlock;
        public CyberwareBlockItem CurrentCyberBlock { get => _currentCyberBlock; set { _currentCyberBlock = value; OnPropertyChanged(); } }

        // кастомка имплантов
        public List<string> AllSkillNames => CoreDataBase.AllSkills.Select(s => s.Name).OrderBy(n => n).ToList();
        public List<string> AllModTypes => new List<string> { "Обычный (Normal)", "Зрение (Visual)", "Слух (Audio)" };
        public List<string> AvailableRequires => CoreDataBase.AllCyberware.Where(c => c.IsFoundation).Select(c => c.Name).Distinct().ToList();

        private string _newCyberName; public string NewCyberName { get => _newCyberName; set { _newCyberName = value; OnPropertyChanged(); } }
        private string _newCyberDesc; public string NewCyberDesc { get => _newCyberDesc; set { _newCyberDesc = value; OnPropertyChanged(); } }
        private string _newCyberHL; public string NewCyberHL { get => _newCyberHL; set { _newCyberHL = value; OnPropertyChanged(); } }
        private string _newCyberSlots = "1"; public string NewCyberSlots { get => _newCyberSlots; set { _newCyberSlots = value; OnPropertyChanged(); } }
        private bool _newCyberIsFoundation; public bool NewCyberIsFoundation { get => _newCyberIsFoundation; set { _newCyberIsFoundation = value; OnPropertyChanged(); } }
        private bool _newCyberIsPaired; public bool NewCyberIsPaired { get => _newCyberIsPaired; set { _newCyberIsPaired = value; OnPropertyChanged(); } }
        private string _newCyberRequires; public string NewCyberRequires { get => _newCyberRequires; set { _newCyberRequires = value; OnPropertyChanged(); } }

        // модификаторы для имплантов
        public ObservableCollection<SkillModifierDef> NewCyberModifiers { get; set; } = new ObservableCollection<SkillModifierDef>();

        private string _selectedSkillForMod; public string SelectedSkillForMod { get => _selectedSkillForMod; set { _selectedSkillForMod = value; OnPropertyChanged(); } }
        private int _selectedValueForMod; public int SelectedValueForMod { get => _selectedValueForMod; set { _selectedValueForMod = value; OnPropertyChanged(); } }
        private string _selectedTypeForMod = "Обычный (Normal)"; public string SelectedTypeForMod { get => _selectedTypeForMod; set { _selectedTypeForMod = value; OnPropertyChanged(); } }


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
        public ObservableCollection<SkillSaveData> NewEnemySkills { get; set; } = new ObservableCollection<SkillSaveData>();
        public ObservableCollection<string> NewEnemyCyberware { get; set; } = new ObservableCollection<string>();
        public List<string> AllCyberwareNames => CoreDataBase.AllCyberware.Select(c => c.Name).Distinct().OrderBy(n => n).ToList();
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

            // ско не опускается ниже 1 из-за ранений и травм, а броня может довести его до 0 (стр. 183)
            if (Name == "СКО" || Name == "MOVE")
            {
                val = BaseValue + ImplantModifier + WoundPenalty;
                if (WoundPenalty < 0 && val < 1) val = 1;
                val = Math.Max(0, val - ArmorPenalty);
            }

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
        public int Level
        {
            get => _level;
            set
            {
                int val = Math.Max(0, Math.Min(777, value));
                if (_level == val) return;
                _level = val;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Base));
                OnPropertyChanged(nameof(DisplayModifier));
                OnPropertyChanged(nameof(DisplayBase));
            }
        }

        private int _woundPenalty;
        public int WoundPenalty { get => _woundPenalty; set { _woundPenalty = value; OnPropertyChanged(); OnPropertyChanged(nameof(Base)); OnPropertyChanged(nameof(DisplayModifier)); OnPropertyChanged(nameof(DisplayBase)); } }

        private int _equipmentMod;
        public int EquipmentMod { get => _equipmentMod; set { _equipmentMod = value; OnPropertyChanged(); OnPropertyChanged(nameof(Base)); OnPropertyChanged(nameof(DisplayModifier)); OnPropertyChanged(nameof(DisplayBase)); } }

        private int _visualMod;
        public int VisualMod { get => _visualMod; set { _visualMod = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayModifier)); OnPropertyChanged(nameof(DisplayBase)); } }

        private int _audioMod;
        public int AudioMod { get => _audioMod; set { _audioMod = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayModifier)); OnPropertyChanged(nameof(DisplayBase)); } }

        // ДОБАВЛЯЕМ ЭТО:
        private int _roleMod;
        public int RoleMod { get => _roleMod; set { _roleMod = value; OnPropertyChanged(); OnPropertyChanged(nameof(Base)); OnPropertyChanged(nameof(DisplayModifier)); OnPropertyChanged(nameof(DisplayBase)); } }

        // общий численный модификатор травмы + броня + импланты
        public int GeneralMod => WoundPenalty + EquipmentMod + RoleMod;

        // строка для колонки "ДОП"
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

        // число используется для сохранения в json
        public int Base => StatValue + Level + GeneralMod;

        // cтрока для колонки сумм ур+стат+доп
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

        // прозрачность строки при поиске по навыкам (1 — подходит или поиск пуст)
        private double _searchOpacity = 1;
        public double SearchOpacity { get => _searchOpacity; set { _searchOpacity = value; OnPropertyChanged(); } }

        public string Description { get; set; }
        public bool CanAddMultiple { get; set; }
        public bool IsVariant { get; set; }
        public string BaseName { get; set; }
        private string _subName; public string SubName { get => _subName; set { _subName = value; OnPropertyChanged(); } }

        public Visibility AddBtnVis => CanAddMultiple ? Visibility.Visible : Visibility.Collapsed;
        public Visibility RemoveBtnVis => IsVariant ? Visibility.Visible : Visibility.Collapsed;
        public Visibility SubNameVis => (IsVariant || BaseName == "Язык (Родной)") ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BaseNameVis => !(IsVariant || BaseName == "Язык (Родной)") ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BaseVariantLabelVis => (IsVariant || BaseName == "Язык (Родной)") ? Visibility.Visible : Visibility.Collapsed;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class SkillRow : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string StatName { get; set; }
        public bool IsBasic { get; set; }
        public bool IsX2 { get; set; }
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
        public string BaseName { get; set; } //

        // === ИСПРАВЛЕННЫЕ СВОЙСТВА ВИДИМОСТИ ===
        public Visibility NormalControlsVisibility => CanAddMultiple ? Visibility.Collapsed : Visibility.Visible;
        public Visibility AddBtnVis => CanAddMultiple ? Visibility.Visible : Visibility.Collapsed;

        // Крестик удаления показываем ТОЛЬКО у вариантов (у базового Родного языка его быть не должно)
        public Visibility RemoveBtnVis => IsVariant ? Visibility.Visible : Visibility.Collapsed;

        // Поле ввода показываем, если это вариант ИЛИ если это базовый "Язык (Родной)"
        public Visibility SubNameVis => (IsVariant || BaseName == "Язык (Родной)") ? Visibility.Visible : Visibility.Collapsed;

        // Прячем обычное имя, если включилось поле ввода
        public Visibility BaseNameVis => !(IsVariant || BaseName == "Язык (Родной)") ? Visibility.Visible : Visibility.Collapsed;

        // Показываем имя с двоеточием (например, "Язык (Родной): ")
        public Visibility BaseVariantLabelVis => (IsVariant || BaseName == "Язык (Родной)") ? Visibility.Visible : Visibility.Collapsed;
        // ========================================

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

        public string Source { get; set; } = Packs.CoreName;   // «Корник» или название DLC
        public bool IsCustom { get; set; } = false;
        public bool IsFoundation { get; set; } = false;
        public bool IsPaired { get; set; } = false;
        public string Requires { get; set; } = "";

        public List<SkillModifierDef> SkillModifiers { get; set; } = new List<SkillModifierDef>();

        public string TooltipText => $"{Description}\n\nПЧ: {HumanityLoss} | Слотов: {Slots}" +
                                     (string.IsNullOrEmpty(Requires) ? "" : $"\nТРЕБУЕТ: {Requires}") +
                                     (IsPaired ? "\n[СОПРЯЖЕННЫЙ ИМПЛАНТ: Ставится в обе конечности]" : "");

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class CyberwareBlockItem : INotifyPropertyChanged
    {
        private string _name; public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        public bool IsInstalled => InstalledItems.Any(i => i.IsFoundation);

        private int _usedSlots; public int UsedSlots { get => _usedSlots; set { _usedSlots = value; OnPropertyChanged(); OnPropertyChanged(nameof(SlotsText)); } }
        private int _maxSlots; public int MaxSlots { get => _maxSlots; set { _maxSlots = value; OnPropertyChanged(); OnPropertyChanged(nameof(SlotsText)); } }
        public bool BorgDoubled { get; set; }   // слоты удвоены при переходе в корпус ПКТ
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
            OnPropertyChanged(nameof(IsInstalled)); // чекбоксик если ставим типа нейралинка или киберруки
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
        // названия надетой брони (из каталога или вручную)
        private string _headName; public string HeadName { get => _headName; set { _headName = value; OnPropertyChanged(); } }
        private string _bodyName; public string BodyName { get => _bodyName; set { _bodyName = value; OnPropertyChanged(); } }
        private string _shieldName; public string ShieldName { get => _shieldName; set { _shieldName = value; OnPropertyChanged(); } }

        private int _headSpCurrent; public int HeadSpCurrent { get => _headSpCurrent; set { _headSpCurrent = value; OnPropertyChanged(); } }
        private int _headSpMax; public int HeadSpMax { get => _headSpMax; set { _headSpMax = value; OnPropertyChanged(); } }
        private string _headPenalty; public string HeadPenalty { get => _headPenalty; set { _headPenalty = value; OnPropertyChanged(); } }

        private int _bodySpCurrent; public int BodySpCurrent { get => _bodySpCurrent; set { _bodySpCurrent = value; OnPropertyChanged(); } }
        private int _bodySpMax; public int BodySpMax { get => _bodySpMax; set { _bodySpMax = value; OnPropertyChanged(); } }
        private string _bodyPenalty; public string BodyPenalty { get => _bodyPenalty; set { _bodyPenalty = value; OnPropertyChanged(); } }

        private int _shieldSpCurrent; public int ShieldSpCurrent { get => _shieldSpCurrent; set { _shieldSpCurrent = value; OnPropertyChanged(); } }
        private int _shieldSpMax; public int ShieldSpMax { get => _shieldSpMax; set { _shieldSpMax = value; OnPropertyChanged(); } }
        private string _shieldPenalty; public string ShieldPenalty { get => _shieldPenalty; set { _shieldPenalty = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class WeaponData : INotifyPropertyChanged
    {
        private string _name; public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        private string _damage; public string Damage { get => _damage; set { _damage = value; OnPropertyChanged(); } }
        private string _ammoCurrent; public string AmmoCurrent { get => _ammoCurrent; set { _ammoCurrent = value; OnPropertyChanged(); } }
        private string _ammoMax; public string AmmoMax { get => _ammoMax; set { _ammoMax = value; OnPropertyChanged(); } }
        private string _ammoTotal; public string AmmoTotal { get => _ammoTotal; set { _ammoTotal = value; OnPropertyChanged(); } }
        private string _rof; public string Rof { get => _rof; set { _rof = value; OnPropertyChanged(); } }
        private string _notes; public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class StringItem : INotifyPropertyChanged
    {
        private string _value;
        public string Value { get => _value; set { _value = value; OnPropertyChanged(); } }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class LifepathItem : INotifyPropertyChanged
    {
        private string _key;
        public string Key { get => _key; set { _key = value; OnPropertyChanged(); } }

        private string _value;
        public string Value { get => _value; set { _value = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class EditableString : INotifyPropertyChanged
    {
        private string _value;
        public string Value { get => _value; set { _value = value; OnPropertyChanged(); } }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class EditableKeyValue : INotifyPropertyChanged
    {
        public string Key { get; set; }
        private string _value;
        public string Value { get => _value; set { _value = value; OnPropertyChanged(); } }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}