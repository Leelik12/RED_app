using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class BlackChromeTests
    {
        [Fact]
        public void VehiclesCarryTableStatsAndNomadAccess()
        {
            AppSettings.Data.DisabledPacks.Clear();
            var bc = Vehicles.All().Where(v => v.Source != null && v.Source.Contains("хром")).ToList();
            Assert.Equal(31, bc.Count);
            Assert.Equal(8, bc.Count(v => v.Kind == Vehicles.KindAir));
            var par = bc.Single(v => v.Name.Contains("Параллакс"));
            Assert.Equal(35, par.Sdp);
            Assert.Equal(2, par.Seats);
            Assert.Equal(60, par.Combat);
            Assert.Equal("483 км/ч", par.Narrative);
            Assert.Equal(110000, par.Cost);
            Assert.True(par.Nomad);
            var jet = bc.Single(v => v.Name.Contains("ДжетБой"));
            Assert.False(jet.Nomad);                                 // в оригинале «Nomad Access: N/A»
            Assert.Equal("48 км/ч", jet.Narrative);
            Assert.Contains("48 км/ч (30 миль/ч)", jet.Description);  // в русском издании 30 миль/ч записаны как 30 км/ч
        }

        [Fact]
        public void ArmorAndSuitsFollowTheList()
        {
            AppSettings.Data.DisabledPacks.Clear();
            var armor = Packs.Enabled("armor").Where(i => i.Pack == "bc").ToList();
            Assert.Equal(14, armor.Count);
            var lotos = armor.Single(i => i.Name.Contains("Лотос"));
            Assert.Equal(13, lotos.SP);
            Assert.Equal(2, lotos.Penalty);
            Assert.True(lotos.Suit);
            Assert.Equal(5000, lotos.Cost);
            var trench = armor.Single(i => i.Name.Contains("СкидРоу"));
            Assert.Equal(13, trench.SP);
            Assert.Equal(4, trench.Penalty);
            Assert.False(trench.Suit);
            Assert.Contains("арморджек", armor.Single(i => i.Name.Contains("Кортик")).Info);   // термины русского корника
        }

        [Fact]
        public void WeaponsKeepTableNumbersAndFlags()
        {
            AppSettings.Data.DisabledPacks.Clear();
            var w = Packs.Enabled("weapon").Where(i => i.Pack == "bc").ToList();
            Assert.Equal(44, w.Count);
            var katana = w.Single(i => i.Name.Contains("Плачущий"));
            Assert.Equal("4d6", katana.Damage);
            Assert.Equal("2", katana.Hands);
            Assert.True(katana.Melee);
            Assert.Equal(5000, katana.Cost);
            var bmg = w.Single(i => i.Name.Contains("BMG-500"));
            Assert.Equal(500, bmg.Mag);
            Assert.Equal("Оружие крупного калибра", bmg.Skill);
            var modfire = w.Single(i => i.Name.Contains("МодФайр"));
            Assert.Equal("3d6 / 2d6 / 5d6", modfire.Damage);
            Assert.All(w, i => Assert.False(string.IsNullOrWhiteSpace(i.Info)));
        }

        [Fact]
        public void EverythingHasInfoAndIsHiddenWithThePack()
        {
            AppSettings.Data.DisabledPacks.Clear();
            Packs.Apply();
            try
            {
                var all = Packs.Enabled().Where(i => i.Pack == "bc").ToList();
                Assert.True(all.Count >= 170);
                Assert.All(all, i => Assert.False(string.IsNullOrWhiteSpace(i.Info), i.Name));
                Assert.All(all, i => Assert.DoesNotContain("Забрать или посмотреть", i.Info));
                AppSettings.Data.DisabledPacks.Add("bc");
                Assert.DoesNotContain(Packs.Enabled(), i => i.Pack == "bc");
                Assert.DoesNotContain(Vehicles.All(), v => v.Source != null && v.Source.Contains("хром"));
            }
            finally { AppSettings.Data.DisabledPacks.Clear(); Packs.Apply(); }
        }
    }
}
