using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberpunkRED_Generator
{
    // правила погони (DLC «Hot Pursuit»): сетка погони, СЛ позиционирования по боевой скорости, манёвры
    public static class Chase
    {
        // боевая скорость (СКО) → СЛ проверки позиционирования
        public static readonly int[] SpeedTiers = { 60, 40, 20, 15, 10, 8 };
        public static readonly int[] PositionDcs = { 13, 15, 17, 21, 24, 29 };

        // клетка сетки → полоса дальности, м (от, до)
        public static readonly int[][] RangeBands =
        {
            new[] { 0, 6 }, new[] { 7, 12 }, new[] { 13, 25 }, new[] { 26, 50 },
            new[] { 51, 100 }, new[] { 101, 200 }, new[] { 201, 400 }, new[] { 401, 800 }
        };

        public const int MaxGap = 8;

        public class Maneuver
        {
            public string Name; public int Dc; public bool NeedsAdjacent; public string Text;
        }

        public static readonly Maneuver[] Maneuvers =
        {
            new Maneuver { Name = "NOS", Dc = 13, Text = "Стандартное действие: транспорт сразу уходит на 1 клетку вперёд. Проверка — удержать контроль при резком ускорении." },
            new Maneuver { Name = "Полицейский манёвр PIT", Dc = 15, NeedsAdjacent = true, Text = "Лёгкий толчок без урона. В конце раунда оператор цели делает проверку позиционирования как при боевой скорости на категорию ниже; ниже СКО 8 — потеря управления и авария." },
            new Maneuver { Name = "Выход вперёд", Dc = 17, NeedsAdjacent = true, Text = "Успех: ваш транспорт на 1 клетку впереди цели. Провал — потеря контроля или столкновение с препятствием." },
            new Maneuver { Name = "Подтянуться ближе", Dc = 13, Text = "До конца раунда транспорт у нижнего края текущей полосы дальности (7 м вместо 12 м): проще прыгать между машинами и бить в ближнем бою." },
            new Maneuver { Name = "Таран", Dc = 17, NeedsAdjacent = true, Text = "Не происходит автоматически при входе в ту же клетку: нужна намеренная попытка при сохранении контроля. Дальше — обычные правила тарана (CP:R с. 192)." },
        };

        public const string Rules =
            "• ГМ задаёт сетку: расстояние в клетках между всеми транспортами (1 клетка = 0–6 м, 2 = 7–12, 3 = 13–25, 4 = 26–50, 5 = 51–100, 6 = 101–200, 7 = 201–400, 8 = 401–800 м). Все, включая операторов, бросают инициативу.\n" +
            "• В раунд каждый делает действие перемещения и стандартное действие. Транспорт двигает не действие, а проверка позиционирования в конце раунда.\n" +
            "• Позиционирование: оператор бросает навык вождения против СЛ по боевой скорости; успех — на 1 клетку вперёд, провал — на месте. Можно держать скорость или замедлиться на 1 клетку назад без проверки.\n" +
            "• Транспорт с ПЗТ меньше половины проверяет позиционирование как при скорости на категорию ниже; ниже СКО 8 — потеря управления и авария.\n" +
            "• Оператор с РЕА + навык + бонусы меньше 9 не может делать стандартное действие: все силы уходят на управление.\n" +
            "• Прыжок между машинами — действие перемещения, дальность считается по дальнему краю полосы (если оператор не «подтянулся ближе»); возможность зависит от СКО.\n" +
            "• Ближний бой между транспортами — только на соседних клетках после «Подтянуться ближе».\n" +
            "• Погоня кончается, когда транспорт не может ехать или расстояние больше 8 клеток.";

        // индекс категории скорости: наибольшая категория, не превышающая скорость (скорость 25 → 20)
        public static int TierIndex(int combatSpeed)
        {
            for (int i = 0; i < SpeedTiers.Length; i++) if (combatSpeed >= SpeedTiers[i]) return i;
            return SpeedTiers.Length - 1;
        }

        // половина максимума и ниже — штраф в одну категорию
        public static bool Damaged(int hp, int maxHp) => maxHp > 0 && hp * 2 < maxHp;

        // СЛ позиционирования; stepsDown — дополнительное снижение скорости (PIT); null — транспорт потерял управление
        public static int? PositionDc(int combatSpeed, int hp, int maxHp, int stepsDown = 0)
        {
            int idx = TierIndex(combatSpeed) + stepsDown + (Damaged(hp, maxHp) ? 1 : 0);
            return idx >= SpeedTiers.Length ? (int?)null : PositionDcs[idx];
        }

        // эффективная боевая скорость с учётом повреждений; 0 — потеря управления
        public static int EffectiveSpeed(int combatSpeed, int hp, int maxHp, int stepsDown = 0)
        {
            int idx = TierIndex(combatSpeed) + stepsDown + (Damaged(hp, maxHp) ? 1 : 0);
            return idx >= SpeedTiers.Length ? 0 : SpeedTiers[idx];
        }

        // «0–6 м» и т. п. для расстояния в клетках; 0 клеток считаем одной (борт о борт)
        public static string RangeText(int gapCells)
        {
            if (gapCells > MaxGap) return "больше 800 м — погоня окончена";
            var b = RangeBands[Math.Max(1, gapCells) - 1];
            return b[0] + "–" + b[1] + " м";
        }

        public static bool Adjacent(int gapCells) => gapCells <= 1;

        // погоня окончена, если крайние участники разошлись больше чем на 8 клеток
        public static bool IsOver(IEnumerable<int> positions)
        {
            var p = positions.ToList();
            return p.Count > 1 && p.Max() - p.Min() > MaxGap;
        }

        // сколько нужно для действий: РЕА + навык + бонусы < 9 — стандартного действия нет
        public static bool CanAct(int rea, int skill, int bonus) => rea + skill + bonus >= 9;
    }
}
