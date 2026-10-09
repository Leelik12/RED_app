using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberpunkRED_Generator
{
    public class EffectItem
    {
        public string Name { get; set; }
        public string Note { get; set; }
        public int RoundsLeft { get; set; }     // -1 — действует, пока не снимут вручную
        public int RoundsTotal { get; set; }
    }

    // активные эффекты персонажа (наркотики, яды, баффы) с таймерами в раундах; 1 раунд = 3 секунды
    public class EffectsData
    {
        public const int RoundsPerMinute = 20;
        public const int RoundsPerHour = 1200;

        public List<EffectItem> Items { get; set; } = new List<EffectItem>();
        public Action Changed;   // поле, в файл не попадает

        public static int ToRounds(int amount, string unit)
        {
            if (amount <= 0) return -1;
            switch (unit)
            {
                case "минут": return amount * RoundsPerMinute;
                case "часов": return amount * RoundsPerHour;
                default: return amount;
            }
        }

        public static string Describe(int rounds)
        {
            if (rounds < 0) return "до отмены";
            if (rounds >= RoundsPerHour && rounds % RoundsPerHour == 0) return rounds / RoundsPerHour + " ч";
            if (rounds >= RoundsPerMinute)
                return rounds / RoundsPerMinute + " мин" + (rounds % RoundsPerMinute > 0 ? " " + rounds % RoundsPerMinute + " р." : "");
            return rounds + " р.";
        }

        public EffectItem Add(string name, string note, int rounds)
        {
            var e = new EffectItem { Name = name, Note = note, RoundsLeft = rounds, RoundsTotal = rounds };
            Items.Add(e);
            Changed?.Invoke();
            return e;
        }

        public void Remove(EffectItem e) { if (Items.Remove(e)) Changed?.Invoke(); }

        // проходит n раундов; возвращает эффекты, срок которых вышел (они удаляются)
        public List<EffectItem> Tick(int n = 1)
        {
            var expired = new List<EffectItem>();
            foreach (var e in Items.ToList())
            {
                if (e.RoundsLeft < 0) continue;
                e.RoundsLeft = Math.Max(0, e.RoundsLeft - n);
                if (e.RoundsLeft == 0) { expired.Add(e); Items.Remove(e); }
            }
            Changed?.Invoke();
            return expired;
        }
    }
}
