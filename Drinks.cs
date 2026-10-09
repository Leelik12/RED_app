using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberpunkRED_Generator
{
    // напитки Drink Master 5000 (DLC «Мешая напитки — верши судьбы»); рецепты лежат в Data/items.json (пакет dlc12, Extra.recipe)
    public class Drink
    {
        public string Name;
        public int Price;
        public string Taste;
        public int[] Chem;            // адельгид, бронсон, дельта, фланергид, кармотрин
        public bool KarmaOptional;
        public bool Ice, Aged, Blended;

        public string How => string.Join(", ", new[] { Aged ? "Выдержанный" : null, Ice ? "Со льдом" : null, Blended ? "Взбитый" : "Смешанный" }.Where(s => s != null));
    }

    public static class Drinks
    {
        public static readonly string[] Chemicals = { "Адельгид (сладкий)", "Экстракт Бронсона (горький)", "Порошковая дельта (кислый)", "Фланергид (острый)", "Кармотрин (алкоголь)" };

        public static List<Drink> All()
        {
            var list = new List<Drink>();
            foreach (var i in Packs.Items)
            {
                string r = i.Get("recipe");
                if (r == null) continue;
                var p = r.Split(',').Select(int.Parse).ToArray();
                string how = i.Get("how") ?? "";
                list.Add(new Drink
                {
                    Name = i.Name, Price = i.Cost, Taste = i.Get("taste"), Chem = p, KarmaOptional = i.Get("opt") == "1",
                    Ice = how.Contains("Со льдом"), Aged = how.Contains("Выдержанный"), Blended = how.Contains("Взбитый")
                });
            }
            return list;
        }

        // подходящий напиток для смеси или null
        public static Drink Match(IList<Drink> all, int[] chem, bool ice, bool aged, bool blended)
            => all.FirstOrDefault(d => d.Ice == ice && d.Aged == aged && d.Blended == blended
                && Enumerable.Range(0, 4).All(k => d.Chem[k] == chem[k])
                && (d.KarmaOptional ? true : d.Chem[4] == chem[4]));

        // «хорошее эмпирическое правило» книги: СЛ11 от 8–10 единиц кармотрина, +2 за каждые следующие 8–10
        public static int IntoxicationDc(int karmotrine)
            => karmotrine < 8 ? 0 : 11 + 2 * ((karmotrine - 8) / 9);
    }
}
