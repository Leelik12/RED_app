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
        public string Role { get; set; }

        public string RoleDescription
        {
            get
            {
                switch (Role)
                {
                    case "Рокербой": return "ХАРИЗМАТИЧЕСКОЕ ВЛИЯНИЕ\nВлияй на окружающих силой личности. Превращай людей в фанатов (Бросок: СЛ8 — 1 чел, СЛ10 — до 6 чел, СЛ12 — толпа).\n\nРанги 1-2 (Местные клубы): Убедить фаната на мелкую услугу (купить еду, подвезти).\nРанги 3-4 (Известные клубы): Фанат окажет крупную услугу. Группа будет тусоваться с тобой.\nРанги 5-6 (Крупные клубы): Фанат совершит мелкое преступление (кража, драка). Группа станет личной свитой.\nРанги 7-8 (Концертные залы): Фанат рискнёт жизнью. Группа пойдёт на мелкое преступление.\nРанг 9 (ТВ/Крупные залы): Фанат пойдёт на тяжкое преступление (избиение, грабеж). Группа устроит беспорядки.\nРанг 10 (Стадионы): Фанаты пожертвуют собой без вопросов. Группа рискует жизнями как твоя охрана. Частная армия.";
                    case "Нетраннер": return "ИНТЕРФЕЙС\nПозволяет заниматься нетраннингом, определяет количество сетевых действий в ход и даёт доступ к программам.\n\nСЕТЕВЫЕ ДЕЙСТВИЯ ЗА ХОД:\nРанг 1–3: 2 действия\nРанг 4–6: 3 действия\nРанг 7–9: 4 действия\nРанг 10: 5 действий";
                    case "Медиа": return "АВТОРИТЕТНОСТЬ\nДоступ к источникам и убеждение аудитории. Ты пассивно подхватываешь слухи (СЛ 7-13).\n\nУбедительность: Шанс того, что тебе поверят (бросок 1d10 <= Убедительность). Твердые доказательства дают +1/2.\nРанги 1-2: Местные связи. Убедительность 2/10. Влияние на мелких злодеев.\nРанги 3-4: Городские лидеры. Убедительность 3/10. Торжество локальной справедливости.\nРанги 5-6: Крупные игроки. Убедительность 4/10. Влияние на весь город.\nРанги 7-8: Президенты корп. Убедительность 5/10. Влияние на штат.\nРанг 9: Политики штата. Убедительность 6/10. Влияние на страну.\nРанг 10: Мировые лидеры. Убедительность 7/10. Свержение мегакорпораций и мировых правительств.";
                    case "Законник": return "ПОДКРЕПЛЕНИЕ\nПризыв сослуживцев. Брось 1d10 <= ранга. При успехе брось 1d6 на раунды до прибытия (выпадет 6 — прибудет отряд выше рангом).\n\nРанги 1-2: 4 наемных копа (ОС 7, ПЗ 20).\nРанги 3-4: 4 патрульных на авто (ОС 7, ПЗ 25).\nРанги 5-7: 2 шерифа/штурмовика в тяж. броне (ОС 13, ПЗ 35).\nРанг 8: Маршал Зоны на супербайке (ОС 15, ПЗ 50, гранатомёт).\nРанг 9: C-SWAT/Психоотряд на AV-4 (ОС 18, ПЗ 35, ракетницы).\nРанг 10: Агенты ФБР/Интерпола на AV-4. Остаются помогать в расследовании дела.";
                    case "Корпорат": return "КОМАНДНАЯ РАБОТА\nКорпоративные блага и команда подчиненных (проверка лояльности подчиненных 1d6 <= их лояльности).\n\nРанг 1: Комплект элитной одежды.\nРанг 2: Бесплатный корпоративный конапт.\nРанг 3: 1 член команды (подчинённый).\nРанг 5: 2 члена команды.\nРанг 6: Медстраховка Trauma Team Silver.\nРанг 7: Улучшение жилья до дома в бивервилле.\nРанг 8: Медстраховка Trauma Team Executive.\nРанг 9: 3 члена команды.\nРанг 10: Люксовый пентхаус или роскошный особняк.";
                    case "Фиксер": return "ДЕЛОВАЯ ХВАТКА\nСвязи, сделки и культурная гибкость.\n\nОХВАТ РЫНКА (доступ к скрытым товарам):\nРанги 1-2: Повседневные вещи.\nРанги 3-4: Дорогие предметы.\nРанги 5-6: Очень дорогие + организация Ночного Рынка.\nРанги 7-8: Роскошь.\nРанг 9: Супер роскошь + тайный рынок для боссов криминала.\nРанг 10: Уникальные товары.\n\nТОРГ: Наценки/скидки (10-20%), бонусный предмет при покупке 5-и одинаковых.\nГИБКОСТЬ: Автоматическое знание языков и культур местных банд/элит.";
                    case "Кочевник": return "МОТО\nСемья на колёсах. Добавляет ранг Мото к проверкам: Вождение, Пилотирование, Судовождение, Авиатехника, Автомеханика, Судоремонт.\n\nСЕМЕЙНЫЙ АВТОПАРК:\nКаждый раз повышая ранг, выбери одно:\n1) Добавить 1 стандартный транспорт (ранга Мото или ниже) в свой пул.\n2) Установить 1 улучшение (броню, оружие, NOS и т.д.) на уже доступный транспорт.\n\nКочевник использует 1 семейный транспорт за раз (замена к следующему утру). Семья чинит разбитый транспорт за неделю (и штраф 500eb). На 10 ранге можно выводить весь свой автопарк одновременно.";
                    default: return "";
                }
            }
        }

        private int _roleRank = 4;
        public int RoleRank
        {
            get => _roleRank;
            set
            {
                _roleRank = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SoloPointsText));
                OnPropertyChanged(nameof(TechPointsText));
                OnPropertyChanged(nameof(MedtechPointsText));
                OnPropertyChanged(nameof(NetrunnerTotal)); // <--- Добавили
                OnPropertyChanged(nameof(NetrunnerActions)); // <--- Добавили
                RecalculatePenalties();
            }
        }
        public Visibility IsSoloVis => Role == "Соло" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsTechVis => Role == "Техник" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsMedtechVis => Role == "Медтехник" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsNetrunnerVis => Role == "Нетраннер" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsTextRoleVis => (Role != "Соло" && Role != "Техник" && Role != "Медтехник" && Role != "Нетраннер") ? Visibility.Visible : Visibility.Collapsed;

        // --- СОЛО ---
        private int _soloThreat; public int SoloThreat { get => _soloThreat; set { _soloThreat = value; OnPropertyChanged(); OnPropertyChanged(nameof(SoloPointsText)); RecalculatePenalties(); } }
        private int _soloAwareness; public int SoloAwareness { get => _soloAwareness; set { _soloAwareness = value; OnPropertyChanged(); OnPropertyChanged(nameof(SoloPointsText)); } }
        private int _soloDeflection; public int SoloDeflection { get => _soloDeflection; set { _soloDeflection = value; OnPropertyChanged(); OnPropertyChanged(nameof(SoloPointsText)); } }
        private int _soloFumble; public int SoloFumble { get => _soloFumble; set { _soloFumble = value; OnPropertyChanged(); OnPropertyChanged(nameof(SoloPointsText)); } }
        private int _soloInitiative; public int SoloInitiative { get => _soloInitiative; set { _soloInitiative = value; OnPropertyChanged(); OnPropertyChanged(nameof(SoloPointsText)); RecalculatePenalties(); } }
        private int _soloPrecision; public int SoloPrecision { get => _soloPrecision; set { _soloPrecision = value; OnPropertyChanged(); OnPropertyChanged(nameof(SoloPointsText)); } }
        public int SoloPointsUsed => SoloThreat * 1 + SoloAwareness * 1 + SoloDeflection * 2 + SoloFumble * 1 + SoloInitiative * 1 + SoloPrecision * 3;
        public string SoloPointsText => $"СВОБОДНЫХ ОЧКОВ: {RoleRank - SoloPointsUsed}";

        // --- ТЕХНИК ---
        private int _techField; public int TechField { get => _techField; set { _techField = value; OnPropertyChanged(); OnPropertyChanged(nameof(TechPointsText)); RecalculatePenalties(); } }
        private int _techUpgrade; public int TechUpgrade { get => _techUpgrade; set { _techUpgrade = value; OnPropertyChanged(); OnPropertyChanged(nameof(TechPointsText)); } }
        private int _techFab; public int TechFab { get => _techFab; set { _techFab = value; OnPropertyChanged(); OnPropertyChanged(nameof(TechPointsText)); } }
        private int _techInvent; public int TechInvent { get => _techInvent; set { _techInvent = value; OnPropertyChanged(); OnPropertyChanged(nameof(TechPointsText)); } }
        public int TechPointsUsed => TechField + TechUpgrade + TechFab + TechInvent;
        public string TechPointsText => $"СВОБОДНЫХ ОЧКОВ: {RoleRank - TechPointsUsed}";

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
        public int NetrunnerTotal => (HexStats?.FirstOrDefault(s => s.Name == "ИНТ" || s.Name == "INT")?.CurrentValue ?? 0) + RoleRank;
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
        public int MaxHP { get => _maxHP; set { if (_maxHP == value) return; _maxHP = value; OnPropertyChanged(); RecalculatePenalties(); } }

        private int _baseDeathSave;
        public int BaseDeathSave { get => _baseDeathSave; set { if (_baseDeathSave == value) return; _baseDeathSave = value; OnPropertyChanged(); RecalculatePenalties(); } }

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
                if (Role == "Соло") baseInit += SoloInitiative;
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

                bodyStat.ImplantModifier = effectiveBody - baseBody;

                int will = HexStats.FirstOrDefault(s => s.Name == "ВОЛЯ" || s.Name == "WILL")?.CurrentValue ?? 5;
                MaxHP = 10 + (5 * (int)Math.Ceiling((effectiveBody + will) / 2.0));
                BaseDeathSave = effectiveBody;
            }

            int baseEmp = Stats != null && Stats.ContainsKey("EMP") ? Stats["EMP"] : (Stats != null && Stats.ContainsKey("ЭМП") ? Stats["ЭМП"] : 5);
            int maxHum = baseEmp * 10;

            foreach (var cw in installedCyberware)
            {
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
                audioBlock.MaxSlots = hasArray ? 8 : 3;
            }

            int threshold = (int)Math.Ceiling(MaxHP / 2.0);
            int woundSkillPenalty = 0;
            int woundMovePenalty = 0;
            int dsPenalty = 0;

            if (CurrentHP >= MaxHP) { WoundStateText = "НЕТ"; WoundTooltipText = "Персонаж полностью здоров."; }
            else if (CurrentHP > threshold) { WoundStateText = "ЛЕГКОЕ"; WoundTooltipText = "СЛОЖНОСТЬ СТАБИЛИЗАЦИИ: 10"; }
            else if (CurrentHP > 0)
            {
                WoundStateText = "ТЯЖЕЛОЕ"; woundSkillPenalty += 2; WoundTooltipText = "ЭФФЕКТ: -2 ко всем действиям.\nСЛОЖНОСТЬ СТАБИЛИЗАЦИИ: 13";
            }
            else
            {
                WoundStateText = "СМЕРТЕЛЬНОЕ"; woundSkillPenalty += 4; woundMovePenalty += 6;
                WoundTooltipText = "ЭФФЕКТ: -4 к действиям, -6 СКО.\nСЛОЖНОСТЬ СТАБИЛИЗАЦИИ: 15";
            }

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
            if (Role == "Фиксер")
            {
                ApplyRoleModToSkill("Торговля", RoleRank);
                ApplyRoleModToSkill("Знаток Улиц", RoleRank);
            }
            else if (Role == "Кочевник")
            {
                ApplyRoleModToSkill("Авиационные технологии", RoleRank);
                ApplyRoleModToSkill("Автомеханика", RoleRank);
                ApplyRoleModToSkill("Морские технологии", RoleRank);

                ApplyRoleModToSkill("Вождение", RoleRank);
                ApplyRoleModToSkill("Пилотирование (x2)", RoleRank);
                ApplyRoleModToSkill("Судоходство", RoleRank);
            }
            else if (Role == "Соло")
            {
                ApplyRoleModToSkill("Внимательность", SoloThreat);
            }
            else if (Role == "Техник")
            {
                ApplyRoleModToSkill("Знание техники", TechField);
                ApplyRoleModToSkill("Кибертехника", TechField);
                ApplyRoleModToSkill("Автомеханика", TechField);
                ApplyRoleModToSkill("Морские технологии", TechField);
                ApplyRoleModToSkill("Авиационные технологии", TechField);
                ApplyRoleModToSkill("Оружейник", TechField);
            }
            var activeModifiers = new List<SkillModifierDef>();

            if (CriticalInjuriesList != null)
            {
                foreach (var inj in CriticalInjuriesList)
                {
                    activeModifiers.AddRange(inj.SkillModifiers);
                    dsPenalty += inj.DeathSavePenalty;
                    woundMovePenalty += inj.MovePenalty;
                    woundSkillPenalty += inj.AllActionsPenalty;
                }
            }

            var appliedCyberwareMods = new HashSet<string>();
            foreach (var cw in installedCyberware)
            {
                if (!appliedCyberwareMods.Contains(cw.Name))
                {
                    activeModifiers.AddRange(cw.SkillModifiers);
                    appliedCyberwareMods.Add(cw.Name);
                }
            }

            if (installedCyberware.Count(c => c.Name == "Светотату") >= 3)
                activeModifiers.Add(new SkillModifierDef { SkillName = "Гардероб и стиль", Value = 2 });

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
            CurrentDeathSave = Math.Max(0, BaseDeathSave - dsPenalty);

            OnPropertyChanged(nameof(MedSurgeryTotal));
            OnPropertyChanged(nameof(MedicalTechTotal));

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

            // ско не может опуститься ниже 1 из-за ранений/брони
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
