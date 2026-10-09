using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CyberpunkRED_Generator
{
    // DLC «Переходи на Металл»: полная конверсия тела (ПКТ). Корпуса лежат в Data/items.json (пакет dlc14, kind=other, Group «Корпус ПКТ»).
    public class BorgKitItem
    {
        public string Name;
        public int Count = 1;
        public bool Borgware;        // боргирование: −4 к максимуму человечности вместо −2
        public override string ToString() => Name + (Count > 1 ? " ×" + Count : "");
    }

    public class BorgChassis
    {
        public string Name, Maker, Tier, Info;
        public int Price;
        public int HlAvg;            // «ПЧ за комплект» по книге (среднее)
        public string Half;          // кубы, делимые на 2 с округлением вверх («3d6»), может быть пусто
        public string Dice;          // основные кубы («18d6»)
        public int EndoBody;         // 12 / 14 / 16
        public string EndoName;
        public string[] Free;
        public List<BorgKitItem> Kit = new List<BorgKitItem>();

        public string FullName => Maker + " «" + Name + "»";
        public string HlFormula => (string.IsNullOrEmpty(Half) ? "" : "[" + Half + "/2, вверх] + ") + Dice;

        // потеря максимума человечности от набора с ПЧ (2 за имплант, 4 за боргирование)
        public int MaxHumanityLoss => Kit.Sum(k => k.Count * (k.Borgware ? 4 : 2));

        // минимальное значение РЕА/ЛВК/СКО от внутренней гидравлики в наборе (0 — нет)
        public int HydraulicFloor => Borg.HydraulicFloor(Kit.Select(k => k.Name));
    }

    public static class Borg
    {
        public const string BiosystemName = "Биосистема";
        public const int BiosystemMaxHumanityLoss = 4;   // боргирование
        public const int BiosystemHlAvg = 14;            // 4d6

        public static readonly string Rules =
            "• ИНТ, ХАР, ТЕХ, ВОЛЯ, УДЧ и ЭМП остаются как были. ТЕЛ, ПЗ и спасбросок от смерти зависят от эндоскелета корпуса (Сигма 12, Бета 14, Омега 16). РЕА, ЛВК и СКО меняются от имплантов корпуса.\n" +
            "• Киберруки, кибераудио, киберглаза, киберноги, киберчереп, эндоскелет и нейролинк в корпусе дают 0 ПЧ и не снижают максимум человечности.\n" +
            "• Слоты в основных, внутренних и внешних имплантах удваиваются. Искусственные мышцы и усиленные кости ставить не нужно.\n" +
            "• Урон получает корпус. При образе жизни «Хороший полуфабрикат» (600eb/мес) и выше ПЗ восстанавливаются штатно; ремонт: Кибертехника СЛ15, 4 часа (провал — половина ПЗ).\n" +
            "• Иммунитет к штрафам навыков и статов от всех состояний ранения (спасброски от смерти остаются) и к критическим: Перелом челюсти, Разрыв лёгкого, Сотрясение мозга, Перелом черепа, Травма трахеи, Оторванное ухо, Травма позвоночника, Разрыв мышцы, Травма шеи.\n" +
            "• Иммунитет к болезням, ядам, препаратам и уличным наркотикам (кроме введённых аэрогиппо прямо в биосистему), радиации; не нужны дыхание, еда и вода. Раз в месяц — раствор для мозга (100eb; бесплатно при «Хорошем полуфабрикате»): без него месяц −2 ко всем проверкам, два месяца — смерть.\n" +
            "• Лечебные приспособления для мяса (криокамера, крионасос, СпидХил) не действуют. Сон нужен, но штрафов от неудобного сна нет.\n" +
            "• Смена корпуса: Кибертехника СЛ17 или Хирургия, 4 часа, можно повторять. Импланты прежнего корпуса отключаются, за новый корпус — новая ПЧ (можно брать среднее).";

        private static List<BorgChassis> _all;
        private static readonly object Lock = new object();

        public static IReadOnlyList<BorgChassis> All
        {
            get { lock (Lock) { if (_all == null) _all = Build(); return _all; } }
        }

        public static void Reset() { lock (Lock) _all = null; }

        public static BorgChassis Find(string name)
            => string.IsNullOrEmpty(name) ? null : All.FirstOrDefault(c => c.Name == name || c.FullName == name);

        private static List<BorgChassis> Build()
        {
            var list = new List<BorgChassis>();
            foreach (var i in Packs.Items.Where(x => x.Kind == "other" && x.Group == "Корпус ПКТ"))
            {
                string endo = i.Get("endo");
                var c = new BorgChassis
                {
                    Name = i.Name, Maker = i.Get("maker"), Tier = i.Get("tier"), Info = i.Info, Price = i.Cost,
                    HlAvg = int.TryParse(i.Get("hl"), out var hl) ? hl : 0,
                    Half = i.Get("half") ?? "", Dice = i.Get("dice") ?? "",
                    EndoBody = endo == "omega" ? 16 : endo == "beta" ? 14 : 12,
                    EndoName = endo == "omega" ? "Эндоскелет Ω (Омега)" : endo == "beta" ? "Эндоскелет ß (Бета)" : "Эндоскелет ∑ (Сигма)",
                    Free = (i.Get("free") ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray()
                };
                foreach (var part in (i.Get("kit") ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int colon = part.LastIndexOf(':');
                    string name = (colon > 0 ? part.Substring(0, colon) : part).Trim();
                    int count = colon > 0 && int.TryParse(part.Substring(colon + 1), out var n) ? n : 1;
                    bool borg = name.EndsWith("*");
                    c.Kit.Add(new BorgKitItem { Name = name.TrimEnd('*'), Count = count, Borgware = borg });
                }
                list.Add(c);
            }
            return list;
        }

        // «Тюнингованная»/«Совершенная» версии гидравлики: 6 / 8, обычная: 4; берётся наибольшая
        public static int HydraulicFloor(IEnumerable<string> names)
        {
            int best = 0;
            foreach (var n in names)
            {
                if (n == null || n.IndexOf("модернизация внутренней гидравлики", StringComparison.OrdinalIgnoreCase) < 0) continue;
                int v = n.StartsWith("Тюнинг", StringComparison.OrdinalIgnoreCase) ? 6 : n.StartsWith("Совершенн", StringComparison.OrdinalIgnoreCase) ? 8 : 4;
                best = Math.Max(best, v);
            }
            return best;
        }

        // имплант в корпусе ПКТ не снижает максимум человечности и не даёт потери ПЧ при установке
        public static bool IsFreeInBorg(CyberwareDef c)
        {
            if (c == null) return false;
            if (c.Name.StartsWith("Нейролинк") || c.Name.StartsWith("Эндоскелет") || c.Name == "Киберчереп" || c.Name == BiosystemName) return true;
            string cat = c.Category ?? "";
            return c.IsFoundation && (cat.Contains("КИБЕРРУКА") || cat.Contains("КИБЕРНОГА") || cat.Contains("КИБЕРГЛАЗ") || cat.Contains("КИБЕРАУДИО"));
        }

        private static readonly Regex Dice = new Regex(@"^(\d+)d(\d+)$", RegexOptions.IgnoreCase);

        private static int RollDice(string expr, Func<int, int> die)
        {
            var m = Dice.Match(expr ?? "");
            if (!m.Success) return 0;
            int total = 0;
            for (int i = 0, n = int.Parse(m.Groups[1].Value); i < n; i++) total += die(int.Parse(m.Groups[2].Value));
            return total;
        }

        // ПЧ за комплект: ceil(половинные кубы / 2) + основные кубы
        public static int RollKitHl(BorgChassis c, Func<int, int> die = null)
        {
            die = die ?? DiceRoller.Die;
            int half = string.IsNullOrEmpty(c.Half) ? 0 : (RollDice(c.Half, die) + 1) / 2;
            return half + RollDice(c.Dice, die);
        }

        // 4d6 за биосистему
        public static int RollBiosystemHl(Func<int, int> die = null) => RollDice("4d6", die ?? DiceRoller.Die);
    }
}
