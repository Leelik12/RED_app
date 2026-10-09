using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class PackTests
    {
        static readonly string[] Kinds = { "weapon", "armor", "gear", "drug", "ammo", "cyber", "vehicle", "property", "npc", "other", "deck", "netware" };

        [Fact]
        public void Items_AreWellFormed()
        {
            Assert.NotEmpty(Packs.Items);
            Assert.Equal(Packs.Items.Count, Packs.Items.Select(i => i.Id).Distinct().Count());
            foreach (var i in Packs.Items)
            {
                Assert.False(string.IsNullOrWhiteSpace(i.Name), i.Id);
                Assert.Contains(i.Kind, Kinds);
                Assert.Contains(Packs.All, p => p.Id == i.Pack);
                Assert.True(i.Cost >= 0, i.Id);
                if (i.Kind == "weapon") { Assert.False(string.IsNullOrEmpty(i.Skill), i.Id); Assert.False(string.IsNullOrEmpty(i.Damage), i.Id); }
                if (i.Kind == "cyber") { Assert.False(string.IsNullOrEmpty(i.Category), i.Id); Assert.False(string.IsNullOrEmpty(i.HL), i.Id + " " + i.Name); }
            }
        }

        [Fact]
        public void Apply_AddsAndRemovesItems_Idempotently()
        {
            // базовое состояние каталога — без DLC (другие тесты могли оставить их встроенными)
            foreach (var p in Packs.All.Where(p => p.Kind != "core")) AppSettings.Data.DisabledPacks.Add(p.Id);
            Packs.Apply();
            AppSettings.Data.DisabledPacks.Clear();
            int w0 = GearCatalog.Weapons.Count, a0 = GearCatalog.Armor.Count, g0 = GearCatalog.Gear.Count, c0 = CoreDataBase.AllCyberware.Count;
            Packs.Apply();
            int w1 = GearCatalog.Weapons.Count, c1 = CoreDataBase.AllCyberware.Count;
            Assert.True(w1 > w0 && c1 > c0);
            Packs.Apply();
            Assert.Equal(w1, GearCatalog.Weapons.Count);
            Assert.Equal(c1, CoreDataBase.AllCyberware.Count);

            foreach (var p in Packs.All.Where(p => p.Kind != "core")) AppSettings.Data.DisabledPacks.Add(p.Id);
            Packs.Apply();
            Assert.Equal(w0, GearCatalog.Weapons.Count); Assert.Equal(a0, GearCatalog.Armor.Count);
            Assert.Equal(g0, GearCatalog.Gear.Count); Assert.Equal(c0, CoreDataBase.AllCyberware.Count);
            AppSettings.Data.DisabledPacks.Clear();
        }

        [Fact]
        public void DlcItems_AreTaggedWithTheirSource()
        {
            AppSettings.Data.DisabledPacks.Clear();
            Packs.Apply();
            Assert.All(GearCatalog.Weapons.Where(w => w.Source != Packs.CoreName), w => Assert.True(w.FixedQuality));
            Assert.Contains(GearCatalog.Weapons, w => w.Source == "12 дней оруждества");
            Assert.All(CoreDataBase.AllCyberware.Where(c => c.Source == Packs.CoreName), c => Assert.False(string.IsNullOrEmpty(c.Category)));
            AppSettings.Data.DisabledPacks.Clear();
            Packs.Apply();
        }

        [Fact]
        public void PairedLimbOptions_AreOfferedForBothSides()
        {
            var item = new PackItem { Name = "X", Kind = "cyber", Category = "ПРАВАЯ КИБЕРРУКА (CYBERARM R)", HL = "3 (1d6)", Slots = 1 };
            var defs = Packs.ToCyber(item, "T").ToList();
            Assert.Equal(2, defs.Count);
            Assert.Contains(defs, d => d.Category == "ЛЕВАЯ КИБЕРРУКА (CYBERARM L)");
        }
    }
}
