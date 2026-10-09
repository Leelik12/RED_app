using System.Collections.Generic;
using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class NpcTests
    {
        static readonly string[] StatKeys = { "INT", "REF", "DEX", "TECH", "COOL", "WILL", "LUCK", "MOVE", "BODY", "EMP" };

        [Fact]
        public void Npcs_AreWellFormed()
        {
            Assert.NotEmpty(Npcs.All);
            Assert.Equal(Npcs.All.Count, Npcs.All.Select(n => n.Id).Distinct().Count());
            foreach (var n in Npcs.All)
            {
                Assert.False(string.IsNullOrWhiteSpace(n.Name), n.Id);
                Assert.Contains(Packs.All, p => p.Id == n.Pack);
                Assert.All(StatKeys, k => Assert.True(n.Stats.ContainsKey(k), n.Id + " " + k));
                Assert.True(n.Hp > 0 && n.Wound > 0 && n.Wound <= n.Hp, n.Id);
                Assert.True(n.Ds >= 1 && n.Ds <= 14, n.Id);
                Assert.NotEmpty(n.Skills);
            }
        }

        [Fact]
        public void Presets_AreBuiltIn_AndRespectDisabledPacks()
        {
            AppSettings.Data.DisabledPacks.Clear();
            var all = Npcs.Presets();
            Assert.Equal(Npcs.All.Count, all.Count);
            Assert.All(all, e => Assert.True(e.IsBuiltIn));
            string pack = Npcs.All[0].Pack;
            AppSettings.Data.DisabledPacks.Add(pack);
            try { Assert.True(Npcs.Presets().Count < all.Count); }
            finally { AppSettings.Data.DisabledPacks.Clear(); }
        }

        [Fact]
        public void AddTo_KeepsUserPresetWithSameName()
        {
            AppSettings.Data.DisabledPacks.Clear();
            var first = Npcs.All[0];
            var mine = new EnemySaveData { Name = first.Name.ToUpper(), HP = 1 };
            var list = new List<EnemySaveData> { mine };
            Npcs.AddTo(list);
            Assert.Single(list, e => e.Name.ToUpper() == first.Name.ToUpper());
            Assert.Equal(1, list.First(e => e.Name.ToUpper() == first.Name.ToUpper()).HP);
        }

        [Fact]
        public void Clone_KeepsSource_ButDoesNotSerializeIt()
        {
            var e = Npcs.ToEnemy(Npcs.All[0]);
            Assert.Equal(e.Source, e.Clone().Source);
            Assert.DoesNotContain("Source", System.Text.Json.JsonSerializer.Serialize(e));
        }
    }
}
