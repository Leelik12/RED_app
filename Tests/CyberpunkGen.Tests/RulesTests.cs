using System;
using System.Linq;
using System.Text.RegularExpressions;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class RulesTests
    {
        private static bool Cyr(string s) => s != null && Regex.IsMatch(s, "[А-Яа-яЁё]");

        [Fact]
        public void Blocks_Are_Unique_And_Filled()
        {
            Assert.True(Rules.Blocks.Count >= 14);
            Assert.Equal(Rules.Blocks.Count, Rules.Blocks.Select(b => b.Id).Distinct().Count());
            foreach (var b in Rules.Blocks)
            {
                Assert.False(string.IsNullOrWhiteSpace(b.TitleRu), b.Id);
                Assert.False(string.IsNullOrWhiteSpace(b.TitleEn), b.Id);
                Assert.NotEmpty(b.Items);
                Assert.False(string.IsNullOrWhiteSpace(b.Pages), b.Id);
            }
        }

        [Fact]
        public void English_Text_Has_No_Russian_Leftovers()
        {
            foreach (var b in Rules.Blocks)
            {
                Assert.False(Cyr(b.TitleEn), b.Id);
                Assert.False(Cyr(Rules.Pick(b.Pages, true)), b.Id);
                foreach (var it in b.Items)
                {
                    Assert.False(Cyr(it.En), b.Id + ": " + it.En);
                    if (it.BulletsEn != null) foreach (var s in it.BulletsEn) Assert.False(Cyr(s), b.Id + ": " + s);
                    if (it.Head != null)
                    {
                        foreach (var h in it.Head) Assert.False(Cyr(Rules.Pick(h, true)), b.Id + ": " + h);
                        foreach (var r in it.Rows) foreach (var c in r) Assert.False(Cyr(Rules.Pick(c, true)), b.Id + ": " + c);
                    }
                }
            }
        }

        [Fact]
        public void Russian_Text_Is_Present_And_Paragraphs_Pair_Up()
        {
            foreach (var b in Rules.Blocks)
                foreach (var it in b.Items)
                {
                    switch (it.Kind)
                    {
                        case RuleKind.Bullets:
                            Assert.Equal(it.BulletsRu.Length, it.BulletsEn.Length);
                            break;
                        case RuleKind.Table:
                            break;
                        default:
                            Assert.False(string.IsNullOrWhiteSpace(it.Ru), b.Id);
                            Assert.False(string.IsNullOrWhiteSpace(it.En), b.Id);
                            break;
                    }
                }
        }

        [Fact]
        public void Table_Rows_Match_Their_Headers_In_Both_Languages()
        {
            foreach (var b in Rules.Blocks)
                foreach (var it in b.Items.Where(i => i.Kind == RuleKind.Table))
                    foreach (var r in it.Rows)
                    {
                        Assert.True(r.Length == it.Head.Length, b.Id + ": " + string.Join(" / ", r));
                        foreach (var c in r)
                        {
                            Assert.False(string.IsNullOrWhiteSpace(Rules.Pick(c, false)), b.Id + ": " + c);
                            Assert.False(string.IsNullOrWhiteSpace(Rules.Pick(c, true)), b.Id + ": " + c);
                        }
                    }
        }

        [Fact]
        public void Price_Tokens_Expand_To_Categories()
        {
            Assert.Equal("50eb (Недёшево)", Rules.Pick("{50}", false));
            Assert.Equal(5000.ToString("N0") + "eb (Luxury)", Rules.Pick("{5000}", true));
            Assert.Equal("Дёшево", Rules.Pick("Дёшево|Cheap", false));
            Assert.Equal("Cheap", Rules.Pick("Дёшево|Cheap", true));
            Assert.Equal("13", Rules.Pick("13", true));
            foreach (var b in Rules.Blocks)
                Assert.DoesNotContain("{", Rules.Expand(Rules.SearchText(b), false));
        }

        [Fact]
        public void Find_Locates_Blocks_By_Term_In_Either_Language()
        {
            Assert.Equal("wounds", Rules.Find("спасбросок").Id);
            Assert.Equal("wounds", Rules.Find("death save").Id);
            Assert.Equal("check", Rules.Find("критический успех").Id);
            Assert.Equal("net", Rules.Find("Интерфейс").Id);
            Assert.Null(Rules.Find("абракадабра123"));
            Assert.True(Rules.Matches(Rules.Blocks.First(b => b.Id == "armor"), "броня ос"));
            Assert.False(Rules.Matches(Rules.Blocks.First(b => b.Id == "armor"), "нетраннер"));
        }

        // справочная таблица брони совпадает со снаряжением в каталоге (одни и те же числа книги)
        [Fact]
        public void Armor_Table_Matches_The_Gear_Catalog()
        {
            var armor = Rules.Blocks.First(b => b.Id == "armor").Items.First(i => i.Kind == RuleKind.Table && i.Head[0].StartsWith("Броня"));
            foreach (var r in armor.Rows)
            {
                int sp = int.Parse(r[1]);
                int cost = int.Parse(Regex.Match(r[3], @"\d+").Value);
                int penalty = r[2] == "—" ? 0 : int.Parse(Regex.Match(Rules.Pick(r[2], false), @"\d+").Value);
                Assert.Contains(GearCatalog.Armor, a => !a.IsShield && a.SP == sp && a.Cost == cost && a.Penalty == penalty);
            }
        }

        [Fact]
        public void Ranged_Table_Matches_The_Weapon_Catalog()
        {
            var table = Rules.Blocks.First(b => b.Id == "ranged").Items.First(i => i.Kind == RuleKind.Table && i.Head.Length == 8);
            Assert.Equal(11, table.Rows.Count);
            foreach (var r in table.Rows)
            {
                int cost = int.Parse(Regex.Match(r[7], @"\d+").Value);
                int rof = int.Parse(r[4]);
                Assert.Contains(GearCatalog.Weapons, w => !w.IsMelee && w.Damage == r[2] && w.Rof == rof && w.Cost == cost);
            }
        }

        [Fact]
        public void Hit_Point_Table_Follows_The_Formula()
        {
            var table = Rules.Blocks.First(b => b.Id == "stats").Items.First(i => i.Kind == RuleKind.Table);
            for (int row = 0; row < table.Rows.Count; row++)
            {
                int body = int.Parse(table.Rows[row][0]);
                for (int col = 1; col < table.Head.Length; col++)
                {
                    int will = int.Parse(table.Head[col]);
                    int hp = 10 + 5 * (int)Math.Ceiling((body + will) / 2.0);
                    Assert.Equal(hp, int.Parse(table.Rows[row][col]));
                }
            }
        }

        [Fact]
        public void Critical_Injury_Tables_Cover_Every_Roll_Once()
        {
            var crits = Rules.Blocks.First(b => b.Id == "crits").Items.Where(i => i.Kind == RuleKind.Table).ToList();
            Assert.Equal(2, crits.Count);
            foreach (var t in crits)
                Assert.Equal(Enumerable.Range(2, 11).Select(x => x.ToString()), t.Rows.Select(r => r[0]));
        }
    }
}
