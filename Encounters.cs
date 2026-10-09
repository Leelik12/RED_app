using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberpunkRED_Generator
{
    public class EncounterSlot
    {
        public EnemySaveData Enemy;
        public string Tier;
        public int Count;
    }

    // Подбор столкновения для ГМ-экрана. Пропорции взяты из DLC «Прожжённые»: шестёрка на каждого эджраннера,
    // лейтенант на двоих (стоит двух шестёрок), мини-босс на троих. Сложность двигает долю бойцов; это помощник ГМа.
    public static class Encounters
    {
        public const string Six = "Шестёрка", Lieutenant = "Лейтенант", MiniBoss = "Мини-босс", Boss = "Босс";
        public static readonly string[] Difficulties = { "Лёгкая", "Обычная", "Тяжёлая" };

        // вид противника по пояснению пресета DLC («Прожжённый лейтенант …»); у пользовательских пресетов вида нет — считаем шестёркой
        public static string TierOf(EnemySaveData e)
        {
            string info = (e?.Info ?? "").ToLowerInvariant();
            if (info.StartsWith("прожжённый мини-босс") || info.StartsWith("мини-босс")) return MiniBoss;
            if (info.StartsWith("прожжённый босс") || info.StartsWith("босс")) return Boss;
            if (info.Contains("лейтенант")) return Lieutenant;
            return Six;
        }

        // сколько шестёрок-эквивалентов давать на игрока
        private static double Factor(int difficulty) => difficulty <= 0 ? 0.5 : difficulty == 1 ? 1.0 : 1.5;

        // бюджет: 1 шестёрка = 1, лейтенант = 2, мини-босс = 3, босс = 5 (по тому, как DLC уравнивает их друг с другом)
        public static int Cost(string tier) => tier == Boss ? 5 : tier == MiniBoss ? 3 : tier == Lieutenant ? 2 : 1;

        public static List<EncounterSlot> Generate(IList<EnemySaveData> pool, int players, int difficulty, Random rng, bool allowBoss = false)
        {
            var result = new List<EncounterSlot>();
            if (pool == null || pool.Count == 0 || players < 1) return result;
            int budget = (int)Math.Round(players * Factor(difficulty), MidpointRounding.AwayFromZero);
            budget = Math.Max(1, budget);
            var byTier = pool.GroupBy(TierOf).ToDictionary(g => g.Key, g => g.ToList());

            // сперва один «командир» самого крупного вида, который влезает в бюджет и есть в пуле
            var order = new List<string>();
            if (allowBoss) order.Add(Boss);
            order.AddRange(new[] { MiniBoss, Lieutenant });
            foreach (var tier in order)
            {
                if (budget < Cost(tier) || !byTier.ContainsKey(tier)) continue;
                if (tier == MiniBoss && players < 3) continue;       // мини-босс — на троих
                if (tier == Lieutenant && players < 2) continue;     // лейтенант — на двоих
                AddSlot(result, byTier[tier][rng.Next(byTier[tier].Count)]);
                budget -= Cost(tier);
                break;
            }
            // остаток — шестёрки (если их нет в пуле, берём что есть)
            var sixes = byTier.ContainsKey(Six) ? byTier[Six] : pool.ToList();
            while (budget > 0)
            {
                AddSlot(result, sixes[rng.Next(sixes.Count)]);
                budget--;
            }
            return result.OrderByDescending(s => Cost(s.Tier)).ThenBy(s => s.Enemy.Name).ToList();
        }

        private static void AddSlot(List<EncounterSlot> list, EnemySaveData e)
        {
            var slot = list.FirstOrDefault(s => s.Enemy == e);
            if (slot != null) slot.Count++;
            else list.Add(new EncounterSlot { Enemy = e, Tier = TierOf(e), Count = 1 });
        }

        public static int TotalCount(IEnumerable<EncounterSlot> slots) => slots.Sum(s => s.Count);

        // добыча: то, что враги несут на себе, плюс случайная вещь из каталога по потолку цены вида
        public static int LootCap(string tier) => tier == Boss ? 5000 : tier == MiniBoss ? 1000 : tier == Lieutenant ? 500 : 100;

        public static List<string> Loot(IEnumerable<EncounterSlot> slots, Random rng)
        {
            var lines = new List<string>();
            foreach (var s in slots)
            {
                var carried = new[] { s.Enemy.Weapons, s.Enemy.Gear, s.Enemy.Cyberware }.Where(t => !string.IsNullOrWhiteSpace(t));
                lines.Add($"{s.Enemy.Name} ×{s.Count}: " + (carried.Any() ? string.Join("; ", carried) : "ничего особенного"));
                int cap = LootCap(s.Tier);
                var pool = Packs.Items.Where(i => Packs.IsEnabled(i.Pack) && i.Cost > 0 && i.Cost <= cap && (i.Kind == "gear" || i.Kind == "weapon" || i.Kind == "armor" || i.Kind == "drug")).ToList();
                if (pool.Count > 0)
                {
                    var it = pool[rng.Next(pool.Count)];
                    lines.Add($"   случайная находка: {it.Name} ({it.Cost:N0} eb, {Packs.PackName(it.Pack)})");
                }
            }
            return lines;
        }
    }
}
