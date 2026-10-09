using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class PunknautTests
    {
        private static PunknautBuild Base() { var b = new PunknautBuild(); b.Set("base", 1); return b; }

        [Fact]
        public void BookExample_BaseArmorThreeTurrets_Is11Days_Dc16()
        {
            // «Торч»: основа 7 + броня 1 + три турели 3 = 11 дней; СЛ 13 + 1 за броню + 2 за две турели найденные... в книге 16
            var b = Base(); b.Set("armor", 1); b.Set("turret", 2);
            Assert.Equal(7 + 1 + 2, b.TotalDays);
            Assert.Equal(16, b.BuildDc);
            b.Set("turret", 3);
            Assert.Equal(11, b.TotalDays);
        }

        [Fact]
        public void BaseStats()
        {
            var b = Base();
            Assert.Equal(60, b.Hp); Assert.Equal(20, b.Seats); Assert.Equal(15, b.CombatSpeed); Assert.Equal(97, b.NarrativeKmh);
            Assert.Equal(13, b.BuildDc);
            Assert.Equal(10, b.SteerSkillOver); Assert.Equal(5, b.SteerBodyOver);
        }

        [Fact]
        public void ExtraChassisAddsHpSeatsAndSteeringDemands()
        {
            var b = Base(); b.Set("chassis", 2); b.Set("av4", 2);
            Assert.Equal(100, b.Hp); Assert.Equal(40, b.Seats);
            Assert.Equal(25, b.CombatSpeed); Assert.Equal(161, b.NarrativeKmh);
            Assert.Equal(12, b.SteerSkillOver); Assert.Equal(7, b.SteerBodyOver);   // как в книге: шасси ×2 → выше 12 и ТЕЛ выше 7
            Assert.Equal(2, b.SteerPenalty);
        }

        [Fact]
        public void Validation()
        {
            var b = new PunknautBuild();
            Assert.NotEmpty(b.Validate());
            b.Set("base", 1);
            Assert.Empty(b.Validate());
            b.Set("av4", 1);
            Assert.NotEmpty(b.Validate());
            b.Set("chassis", 1);
            Assert.Empty(b.Validate());
            b.Set("armor", 3);
            Assert.NotEmpty(b.Validate());
            b.Set("armor", 2); b.Set("turret", 7);
            Assert.NotEmpty(b.Validate());
            b.Set("turret", 6);
            Assert.Empty(b.Validate());
        }

        [Fact]
        public void HoursAreMarginOverDc()
        {
            Assert.Equal(1, Punknaut.Hours(17, 16));
            Assert.Null(Punknaut.Hours(15, 16));
            Assert.Equal(0, Punknaut.Hours(16, 16));
        }

        [Fact]
        public void PartsTableMatchesBook()
        {
            Assert.Equal(11, Punknaut.Parts.Count);
            var av = Punknaut.Parts.Single(p => p.Id == "av4");
            Assert.Equal(2, av.Days); Assert.Equal(15, av.GatherDc); Assert.Equal(2, av.Mod);
            Assert.Equal(9, Punknaut.Parts.Single(p => p.Id == "saw").GatherDc);
            Assert.Equal(6, Punknaut.Doom.Length);
        }
    }
}
