using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace CyberpunkRED_Generator
{
    // программа или чёрный лёд на кибердеке
    public class NetProgram : INotifyPropertyChanged
    {
        private string _name; public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        private string _class; public string Class { get => _class; set { _class = value; OnPropertyChanged(); } }
        private int _atk; public int Atk { get => _atk; set { _atk = value; OnPropertyChanged(); } }
        private int _def; public int Def { get => _def; set { _def = value; OnPropertyChanged(); } }
        private int _rezMax; public int RezMax { get => _rezMax; set { _rezMax = value; OnPropertyChanged(); } }
        private int _rezCurrent; public int RezCurrent { get => _rezCurrent; set { _rezCurrent = value; OnPropertyChanged(); } }
        private int _slots = 1; public int Slots { get => _slots; set { _slots = value; OnPropertyChanged(); } }
        private int _per; public int Per { get => _per; set { _per = value; OnPropertyChanged(); } }
        private int _spd; public int Spd { get => _spd; set { _spd = value; OnPropertyChanged(); } }
        private bool _isBlackIce; public bool IsBlackIce { get => _isBlackIce; set { _isBlackIce = value; OnPropertyChanged(); } }
        private string _effect; public string Effect { get => _effect; set { _effect = value; OnPropertyChanged(); } }
        private bool _active; public bool Active { get => _active; set { _active = value; OnPropertyChanged(); } }

        public bool IsAttacker => (Class ?? "").StartsWith("Атакующая") || IsBlackIce;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // железо кибердеки
    public class NetHardware : INotifyPropertyChanged
    {
        private string _name; public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        private int _slots = 1; public int Slots { get => _slots; set { _slots = value; OnPropertyChanged(); } }
        private string _effect; public string Effect { get => _effect; set { _effect = value; OnPropertyChanged(); } }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // всё, что относится к нетраннингу персонажа (хранится в файле сохранения)
    public class NetrunData : INotifyPropertyChanged
    {
        private string _deckName; public string DeckName { get => _deckName; set { _deckName = value; OnPropertyChanged(); } }
        private int _deckSlots; public int DeckSlots { get => _deckSlots; set { _deckSlots = value; OnPropertyChanged(); OnPropertyChanged(nameof(SlotsTotal)); } }
        private bool _hasDeckArm; public bool HasDeckArm { get => _hasDeckArm; set { _hasDeckArm = value; OnPropertyChanged(); OnPropertyChanged(nameof(SlotsTotal)); } }
        private bool _hasBodyweight; public bool HasBodyweight { get => _hasBodyweight; set { _hasBodyweight = value; OnPropertyChanged(); OnPropertyChanged(nameof(SlotsTotal)); } }
        private string _notes; public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(); } }

        public ObservableCollection<NetProgram> Programs { get; set; } = new ObservableCollection<NetProgram>();
        public ObservableCollection<NetHardware> Hardware { get; set; } = new ObservableCollection<NetHardware>();

        [JsonIgnore] public int SlotsTotal => DeckSlots + (HasDeckArm ? 1 : 0) + (HasBodyweight ? 1 : 0);
        [JsonIgnore] public int SlotsUsed => Programs.Sum(p => p.Slots) + Hardware.Sum(h => h.Slots);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class NetItemDef
    {
        public string Source;  // пакет DLC, из которого пришёл предмет (пусто — корник)
        public string Kind;    // Программа / Чёрный лёд / Железо / Кибердека
        public string Name, Class, Effect;
        public int Atk, Def, Rez, Per, Spd, Slots = 1, Cost;
    }

    public class InterfaceAbilityDef
    {
        public string Name, Description;
        public bool IsMeat;    // единственная способность, не требующая сетевого действия
    }

    // каталог нетраннинга из книги (глава «Нетраннинг»)
    public static class NetCatalog
    {
        public const string ClsBoost = "Усиливающая";
        public const string ClsDef = "Защитная";
        public const string ClsAtkProg = "Атакующая: против программ";
        public const string ClsAtkUser = "Атакующая: против нетраннера";
        public const string ClsIceUser = "Чёрный лёд: против нетраннера";
        public const string ClsIceProg = "Чёрный лёд: против программ";

        private const string OncePerNetrun = " Одновременно запущена только 1 копия; каждую копию можно использовать раз за нетран.";

        private static NetItemDef P(string name, string cls, int atk, int rez, int cost, string effect)
            => new NetItemDef { Kind = "Программа", Name = name, Class = cls, Atk = atk, Rez = rez, Cost = cost, Effect = effect };
        private static NetItemDef Ice(string name, string cls, int per, int spd, int atk, int def, int rez, int cost, string effect)
            => new NetItemDef { Kind = "Чёрный лёд", Name = name, Class = cls, Per = per, Spd = spd, Atk = atk, Def = def, Rez = rez, Slots = 2, Cost = cost, Effect = effect };
        private static NetItemDef Hw(string name, int slots, int cost, string effect)
            => new NetItemDef { Kind = "Железо", Name = name, Slots = slots, Cost = cost, Effect = effect };
        private static NetItemDef Deck(string name, int slots, int cost)
            => new NetItemDef { Kind = "Кибердека", Name = name, Slots = slots, Cost = cost, Effect = slots + " слотов для программ и железа. Нужны интерфейсный разъём и нейролинк." };

        public static readonly List<NetItemDef> Decks = new List<NetItemDef>
        {
            Deck("Кибердека низкого качества", 5, 100),
            Deck("Кибердека", 7, 500),
            Deck("Кибердека отличного качества", 9, 1000),
        };

        public static readonly List<NetItemDef> Programs = new List<NetItemDef>
        {
            P("Ластик", ClsBoost, 0, 7, 20, "+2 ко всем проверкам плаща, пока программа целостна."),
            P("Увидимся", ClsBoost, 0, 7, 20, "+2 ко всем проверкам следопыта, пока программа целостна."),
            P("Быстрый Гонзалес", ClsBoost, 0, 7, 100, "+2 к реакции (СКОР в сети), пока программа целостна."),
            P("Червь", ClsBoost, 0, 7, 50, "+2 ко всем проверкам бэкдора, пока программа целостна."),
            P("Доспехи", ClsDef, 0, 7, 50, "Весь урон по мозгу снижается на 4, пока программа целостна." + OncePerNetrun),
            P("Зенитный огонь", ClsDef, 0, 7, 50, "АТК всех атакующих программ (кроме чёрного льда) против тебя равна 0, пока программа целостна." + OncePerNetrun),
            P("Щит", ClsDef, 0, 7, 20, "Блокирует первый успешный эффект программы (не ЧЛ), наносящий урон по мозгу, после чего становится нецелостным." + OncePerNetrun),
            P("Адская стрела", ClsAtkUser, 2, 0, 100, "2d6 урона по мозгу. Без изоляции кибердека и одежда горят: 2 урона по ПЗ в конце хода, пока не потушишь (мясное действие)."),
            P("Банхаммер", ClsAtkProg, 1, 0, 50, "3d6 ЦЕЛ программе (не ЧЛ) или 2d6 ЦЕЛ программе чёрного льда."),
            P("ДекKRASH", ClsAtkUser, 0, 0, 100, "Вражеского нетраннера небезопасно выбрасывает из архитектуры (получает эффект всего целостного ЧЛ)."),
            P("Меч", ClsAtkProg, 1, 0, 50, "3d6 ЦЕЛ программе чёрного льда или 2d6 ЦЕЛ программе (не ЧЛ)."),
            P("Нейроразряд", ClsAtkUser, 1, 0, 50, "1d6 урона по мозгу; сетевых действий в его следующий ход на 1 меньше (минимум 2)."),
            P("Нервотрёпка", ClsAtkUser, 0, 0, 100, "ИНТ, РЕФ и ЛВК цели −1d6 на час (минимум 1)."),
            P("Обнуляющий яд", ClsAtkUser, 0, 0, 100, "Уничтожает одну случайную программу (не ЧЛ) в кибердеке цели."),
            P("Суперклей", ClsAtkUser, 2, 0, 100, "Цель не может идти глубже или безопасно выйти 1d6 раундов (небезопасный выход возможен). Раз за нетран."),
        };

        public static readonly List<NetItemDef> BlackIce = new List<NetItemDef>
        {
            Ice("Аспид", ClsIceUser, 4, 6, 2, 2, 15, 100, "Уничтожает одну случайную программу в кибердеке врага."),
            Ice("Блуждающий огонёк", ClsIceUser, 4, 4, 4, 2, 15, 50, "1d6 по мозгу и −1 сетевое действие в следующий ход (минимум 2)."),
            Ice("Великан", ClsIceUser, 2, 2, 8, 4, 25, 1000, "3d6 по мозгу, небезопасный выход из нетрана (эффект всего прочего целостного ЧЛ, кроме великана)."),
            Ice("Ворон", ClsIceUser, 6, 4, 4, 2, 15, 50, "Делает нецелостной случайную активную защитную программу, затем 1d6 по мозгу."),
            Ice("Адская гончая", ClsIceUser, 6, 6, 6, 2, 20, 500, "2d6 по мозгу. Без изоляции кибердека и одежда горят: 2 урона по ПЗ в конце хода."),
            Ice("Кракен", ClsIceUser, 6, 2, 8, 4, 30, 1000, "3d6 по мозгу; до конца следующего хода цель не идёт глубже и не выходит безопасно."),
            Ice("Лич", ClsIceUser, 8, 2, 6, 2, 25, 500, "ИНТ, РЕФ и ЛВК цели −1d6 на час (минимум 1)."),
            Ice("Скорпион", ClsIceUser, 2, 6, 2, 2, 15, 100, "СКО цели −1d6 на час (минимум 1). В RU-книге ошибочно «РЕА», в EN — MOVE."),
            Ice("Скунс", ClsIceUser, 2, 4, 4, 2, 10, 500, "Цель получает −2 ко всем проверкам подката, пока скунс цел. Эффекты нескольких скунсов суммируются."),
            Ice("Дракон", ClsIceProg, 6, 4, 6, 6, 30, 1000, "6d6 программе; если хватает на «нецелостная», программа уничтожается."),
            Ice("Саблезубый", ClsIceProg, 8, 6, 6, 2, 25, 1000, "6d6 программе; если хватает на «нецелостная», программа уничтожается."),
            Ice("Убийца", ClsIceProg, 4, 8, 6, 2, 20, 500, "4d6 программе; если хватает на «нецелостная», программа уничтожается."),
        };

        public static readonly List<NetItemDef> Hardware = new List<NetItemDef>
        {
            Hw("Резервный диск", 2, 100, "Сохраняет уничтоженные программы (не ЧЛ); мясное действие — переустановить, если есть слоты."),
            Hw("ДНК-замок", 2, 100, "Деку нельзя открыть без биоключа или проверки Электроника/Безопасность СЛ17."),
            Hw("Закалённая схема", 1, 100, "Дека не выводится из строя ЭМИ и эффектами программ (не ЧЛ)."),
            Hw("Изолированная проводка", 1, 100, "Дека не загорается от эффектов программ."),
            Hw("KRASH барьер", 2, 100, "Иммунитет к эффектам, принуждающим к отключению."),
            Hw("Увеличение дистанции", 1, 100, "Подключение к точке доступа с расстояния до 8 м."),
        };

        public static readonly List<InterfaceAbilityDef> Abilities = new List<InterfaceAbilityDef>
        {
            new InterfaceAbilityDef { Name = "Сканер", IsMeat = true, Description = "Мясное действие: найти точки доступа к архитектурам сети в области. Чем выше результат, тем больше найдёшь." },
            new InterfaceAbilityDef { Name = "Бэкдор", Description = "Взломать пароль в архитектуре. Если пароль известен — проходишь автоматически." },
            new InterfaceAbilityDef { Name = "Плащ", Description = "Скрыть свои следы и вирус. СЛ следопыта для другого нетраннера равна твоей проверке плаща." },
            new InterfaceAbilityDef { Name = "Контроль", Description = "Захватить узел управления (камеры, турели, лифты). Каждое устройство узла — отдельное сетевое действие, узел — раз за ход." },
            new InterfaceAbilityDef { Name = "Идентификация", Description = "Узнать, что за данные или файл и сколько они стоят." },
            new InterfaceAbilityDef { Name = "Следопыт", Description = "Частично раскрыть карту: столько этажей, сколько результат, но до первого препятствия с СЛ выше результата." },
            new InterfaceAbilityDef { Name = "Подкат", Description = "Уйти от одного не-демонического ЧЛ против ВСП ЧЛ + 1d10, на соседний этаж. Раз за ход, заранее нельзя." },
            new InterfaceAbilityDef { Name = "Вирус", Description = "На нижнем этаже оставить вирус (до 2 изменений). СЛ и число действий назначает Рефери. СЛ уничтожения = твоя проверка." },
            new InterfaceAbilityDef { Name = "Разряд", Description = "Атака сетевым действием против программы (ЗАЩ + 1d10) или нетраннера (Интерфейс + 1d10): 1d6 по ЦЕЛ или по мозгу." },
        };

        // число сетевых действий по рангу Интерфейса
        public static int NetActions(int interfaceRank)
            => interfaceRank <= 0 ? 0 : interfaceRank <= 3 ? 2 : interfaceRank <= 6 ? 3 : interfaceRank <= 9 ? 4 : 5;

        public static NetProgram ToProgram(NetItemDef d) => new NetProgram
        {
            Name = d.Name, Class = d.Class, Atk = d.Atk, Def = d.Def, RezMax = d.Rez, RezCurrent = d.Rez, Slots = d.Slots,
            Per = d.Per, Spd = d.Spd, IsBlackIce = d.Kind == "Чёрный лёд", Effect = d.Effect
        };
    }
}
