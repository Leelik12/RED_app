using System.Collections.Generic;
using System.IO;
using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class LocTests
    {
        private static Dictionary<string, string> Sample() => new Dictionary<string, string>
        {
            ["Атлетика"] = "Athletics",
            ["ПЗ: {#}"] = "HP: {0}",
            ["(свободно {#}/{#})"] = "({0}/{1} free)",
            ["{*} — {#}eb в месяц"] = "{t0} — {1}eb per month",
            ["На киббле"] = "On kibble",
            ["Малая Европа"] = "Little Europe",
            ["(положение приблизительно)"] = "(position approximate)",
            ["Остров"] = "The Island",
            ["Восстанавливающийся центр"] = "Recovering Downtown",
            ["Здоровье (ПЗ):"] = "Health (HP):",
            ["ТРЕБУЕТ: {*}"] = "REQUIRES: {t0}",
            ["Киберглаз"] = "Cybereye",
        };

        private static string Tr(string ru)
        {
            Loc.SetForTests(Sample());
            return Loc.Translate(ru);
        }

        [Fact]
        public void Exact_And_Unknown()
        {
            Assert.Equal("Athletics", Tr("Атлетика"));
            Assert.Equal("Бег", Tr("Бег"));
            Assert.Equal("", Tr(""));
            Assert.Equal("100", Tr("100"));
        }

        [Fact]
        public void Whitespace_Is_Preserved()
        {
            Assert.Equal("  Athletics ", Tr("  Атлетика "));
        }

        [Fact]
        public void Patterns_Substitute_Values()
        {
            Assert.Equal("HP: 25", Tr("ПЗ: 25"));
            Assert.Equal("(2/3 free)", Tr("(свободно 2/3)"));
            Assert.Equal("On kibble — 500eb per month", Tr("На киббле — 500eb в месяц"));
            Assert.Equal("REQUIRES: Cybereye", Tr("ТРЕБУЕТ: Киберглаз"));
            Assert.Equal("REQUIRES: Нечто", Tr("ТРЕБУЕТ: Нечто"));
        }

        [Fact]
        public void Structural_Rules()
        {
            Assert.Equal("Athletics [26]", Tr("Атлетика [26]"));
            Assert.Equal("A — Little Europe", Tr("A — Малая Европа"));
            Assert.Equal("A · Little Europe", Tr("A · Малая Европа"));
            Assert.Equal("Little Europe (position approximate)", Tr("Малая Европа (положение приблизительно)"));
            Assert.Equal("The Island · Recovering Downtown", Tr("Остров · Восстанавливающийся центр"));
            Assert.Equal("Health (HP): 12", Tr("Здоровье (ПЗ): 12"));
        }

        [Fact]
        public void Disabled_When_Russian()
        {
            Loc.SetForTests(Sample());
            string was = AppSettings.Data.Language;
            try
            {
                AppSettings.Data.Language = "ru";
                Assert.Equal("Атлетика", Loc.T("Атлетика"));
                AppSettings.Data.Language = "en";
                Assert.Equal("Athletics", Loc.T("Атлетика"));
            }
            finally { AppSettings.Data.Language = was; }
        }

        [Fact]
        public void Multiline_Tooltips_Translate_Per_Line()
        {
            Assert.Equal("Athletics"+"\n"+"HP: 7", Tr("Атлетика\nПЗ: 7"));
            Assert.Equal("Книга"+"\n"+"Athletics", Tr("Книга\nАтлетика"));
        }

        [Fact]
        public void Shipped_Dictionary_Translates_Catalog_Names()
        {
            Loc.SetForTests(null);
            Assert.Equal("Combat Knife (Light)", Loc.Translate("Боевой нож (лёгкое)"));
            Assert.Equal("Chainsaw (Very Heavy)", Loc.Translate("Бензопила (очень тяжёлое)"));
            Assert.Equal("Cyberdeck (poor quality)", Loc.Translate("Кибердека (низкого качества)"));
            Assert.Equal("Kevlar® — SP 7", Loc.Translate("Кевлар®  —  ОС 7"));
            Assert.Equal("500eb (Expensive)", Loc.Translate("500eb (Дорого)"));
            Assert.Equal("Black Chrome (173)", Loc.Translate("Чёрный хром  (173)"));
        }

        // ---- словарь в репозитории
        [Fact]
        public void Shipped_Dictionary_Is_Sound()
        {
            Loc.SetForTests(null);
            var map = Loc.Map;
            Assert.True(map.Count > 500, "словарь пуст или не встроен");
            foreach (var kv in map)
            {
                Assert.False(string.IsNullOrWhiteSpace(kv.Key));
                Assert.False(string.IsNullOrWhiteSpace(kv.Value), kv.Key);
                // перевод не должен содержать кириллицу, кроме шаблонных подстановок
                if (!kv.Value.Contains("{")) Assert.False(Loc.HasCyrillic(kv.Value), kv.Key + " => " + kv.Value);
                // все {N}/{tN} в переводе должны быть среди найденных в ключе значений
                int slots = kv.Key.Split(new[] { "{#}", "{*}" }, System.StringSplitOptions.None).Length - 1;
                for (int i = slots; i < 6; i++)
                {
                    Assert.DoesNotContain("{" + i + "}", kv.Value);
                    Assert.DoesNotContain("{t" + i + "}", kv.Value);
                }
            }
        }

        [Fact]
        public void Shipped_Dictionary_Translates_Core_Chrome()
        {
            Loc.SetForTests(null);
            Assert.Equal("SAVE", Loc.Translate("СОХРАНИТЬ"));
            Assert.Equal("Athletics", Loc.Translate("Атлетика"));
            Assert.Equal("Athletics [26]", Loc.Translate("Атлетика [26]"));
            Assert.Equal("Brawling [12]", Loc.Translate("Рукопашный бой [12]"));
            Assert.Equal("HP: 40", Loc.Translate("ПЗ: 40"));
            Assert.Equal("A — Little Europe", Loc.Translate("A — Малая Европа"));
            Assert.Equal("Local Expert (Glitch City) [8]", Loc.Translate("Знание местности (Глитч-Сити) [8]").Replace("Глитч-Сити", "Glitch City"));
        }

        [Fact]
        public void Every_Skill_And_Category_Has_A_Translation()
        {
            Loc.SetForTests(null);
            var missing = new List<string>();
            foreach (var sk in CoreDataBase.AllSkills)
            {
                if (Loc.HasCyrillic(Loc.Translate(sk.Category))) missing.Add(sk.Category);
                if (Loc.HasCyrillic(Loc.Translate(sk.Name))) missing.Add(sk.Name);
            }
            foreach (var r in CoreDataBase.Roles)
                if (Loc.HasCyrillic(Loc.Translate(r))) missing.Add(r);
            Assert.True(missing.Count == 0, "без перевода: " + string.Join("; ", missing));
        }

        [Fact]
        public void Every_District_And_Kind_Has_A_Translation()
        {
            Loc.SetForTests(null);
            var missing = new List<string>();
            foreach (var d in CityMap.Districts)
            {
                if (Loc.HasCyrillic(Loc.Translate(d.Name))) missing.Add(d.Name);
                if (Loc.HasCyrillic(Loc.Translate(d.Side))) missing.Add(d.Side);
                if (Loc.HasCyrillic(Loc.Translate(CityMap.KindName(d.Kind)))) missing.Add(CityMap.KindName(d.Kind));
            }
            Assert.True(missing.Count == 0, "без перевода: " + string.Join("; ", missing));
        }
    }
}
