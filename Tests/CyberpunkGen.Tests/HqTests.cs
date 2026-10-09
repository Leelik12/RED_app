using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class HqTests
    {
        public HqTests() { AppSettings.Data.DisabledPacks.Clear(); }

        [Fact]
        public void PackHasTwelveUpgradesAndTenMoralBenefits()
        {
            Assert.Equal(12, Hq.Upgrades().Count);
            Assert.Equal(10, Hq.MoralTable().Count);
            Assert.NotNull(Hq.FactionRule());
        }

        [Fact]
        public void BuyingNeedsPointsAndCostsForty()
        {
            var h = new HqData { Earned = 40 };
            Assert.True(Hq.Buy(h, "garage"));
            Assert.Equal(0, Hq.Remaining(h));
            Assert.False(Hq.Buy(h, "cell"));
            Assert.False(Hq.Mod(h, "garage"));
            Hq.AddPoints(h, 40, "миссия");
            Assert.True(Hq.Mod(h, "garage"));
            Assert.Equal(80, Hq.Spent(h));
            Assert.False(Hq.Mod(h, "garage"));
        }

        [Fact]
        public void ModNeedsUpgradeAndRemovalDropsMods()
        {
            var h = new HqData { Earned = 200 };
            Assert.False(Hq.Mod(h, "server"));
            Hq.Buy(h, "server"); Hq.Mod(h, "server");
            Hq.Remove(h, "server");
            Assert.Equal(0, Hq.Spent(h));
        }

        [Fact]
        public void RentReductionLimitedByBeds()
        {
            var h = new HqData { Earned = 1000, BaseBeds = 2 };
            Hq.Buy(h, "rent");
            Assert.True(Hq.Mod(h, "rent")); Assert.True(Hq.Mod(h, "rent"));
            Assert.False(Hq.Mod(h, "rent"));
            Assert.Equal(4, Hq.Beds(h));
        }

        [Fact]
        public void MoralBoostReplacementsFollowTable()
        {
            Assert.Equal(new[] { 1, 2, 3 }, Hq.ActiveMoral(3));
            Assert.Equal(new[] { 2, 3, 4, 5, 6, 7 }.Where(x => true), Hq.ActiveMoral(7).Where(x => x != 1));
            Assert.DoesNotContain(1, Hq.ActiveMoral(4));
            Assert.DoesNotContain(6, Hq.ActiveMoral(8));
            Assert.DoesNotContain(4, Hq.ActiveMoral(9));
            Assert.Contains(9, Hq.ActiveMoral(9));
            Assert.Equal(7, Hq.ActiveMoral(10).Count);
        }

        [Fact]
        public void MoralBoostGivesLuckHealAndLifestyle()
        {
            var h = new HqData { Earned = 1000 };
            Hq.Buy(h, "moral"); Hq.Buy(h, "medbay");
            Assert.Equal(50, Hq.LifestyleDiscount(h));
            Assert.Equal(2, Hq.HealBodyBonus(h));
            for (int i = 0; i < 7; i++) Hq.Mod(h, "moral");
            Assert.Equal(3, Hq.HealBodyBonus(h));
            Assert.Equal(2, Hq.LuckBonus(h));
        }

        [Fact]
        public void Cyber6ExampleSpendsExactlyWhatBookSays()
        {
            var h = Hq.SampleCyber6();
            Assert.Equal(400, Hq.Spent(h));
            Assert.Equal(0, Hq.Remaining(h));
        }
    }
}
