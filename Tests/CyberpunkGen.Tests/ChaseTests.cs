using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class ChaseTests
    {
        [Theory]
        [InlineData(60, 13)] [InlineData(40, 15)] [InlineData(20, 17)] [InlineData(15, 21)] [InlineData(10, 24)] [InlineData(8, 29)]
        public void PositionDc_ByCombatSpeed(int speed, int dc) => Assert.Equal(dc, Chase.PositionDc(speed, 50, 50));

        [Fact]
        public void BookExample_DamagedTruck20Becomes21()
        {
            Assert.Equal(21, Chase.PositionDc(20, 20, 50));   // меньше половины ПЗТ: как 15 СКО
            Assert.Equal(17, Chase.PositionDc(20, 50, 50));
            Assert.Equal(17, Chase.PositionDc(20, 25, 50));   // ровно половина — ещё не повреждён
        }

        [Fact]
        public void Pit_LowersSpeedOneTier_AndCrashesBelow8()
        {
            Assert.Equal(24, Chase.PositionDc(15, 50, 50, 1));
            Assert.Equal(29, Chase.PositionDc(10, 50, 50, 1));
            Assert.Null(Chase.PositionDc(8, 50, 50, 1));
            Assert.Null(Chase.PositionDc(10, 10, 50, 1));
            Assert.Equal(0, Chase.EffectiveSpeed(8, 50, 50, 1));
        }

        [Fact]
        public void IntermediateSpeedsFallToLowerTier()
        {
            Assert.Equal(17, Chase.PositionDc(25, 50, 50));
            Assert.Equal(29, Chase.PositionDc(5, 50, 50));
        }

        [Theory]
        [InlineData(1, "0–6 м")] [InlineData(2, "7–12 м")] [InlineData(3, "13–25 м")] [InlineData(8, "401–800 м")] [InlineData(0, "0–6 м")]
        public void RangeBands(int gap, string text) => Assert.Equal(text, Chase.RangeText(gap));

        [Fact]
        public void ChaseEndsPastEightCells()
        {
            Assert.False(Chase.IsOver(new[] { 0, 8 }));
            Assert.True(Chase.IsOver(new[] { 0, 9 }));
            Assert.False(Chase.IsOver(new[] { 5 }));
        }

        [Fact]
        public void OperatorNeedsNineToAct()
        {
            Assert.False(Chase.CanAct(5, 3, 0));
            Assert.True(Chase.CanAct(5, 4, 0));
            Assert.True(Chase.CanAct(5, 2, 2));
        }

        [Fact]
        public void Maneuvers_MatchBook()
        {
            Assert.Equal(new[] { 13, 15, 17, 13, 17 }, Chase.Maneuvers.Select(m => m.Dc).ToArray());
        }
    }

    public class VehicleTests
    {
        [Fact]
        public void CoreTableMatchesBook()
        {
            Assert.Equal(14, Vehicles.Core.Count);
            var av4 = Vehicles.Core.Single(v => v.Name.Contains("AV-4"));
            Assert.Equal(100, av4.Sdp); Assert.Equal(6, av4.Seats); Assert.Equal(40, av4.Combat);
            Assert.Equal(60, Vehicles.Core.Single(v => v.Name == "Суперкар").Combat);
        }

        [Fact]
        public void All_IncludesPackVehicles_AndOwnCopyIsIndependent()
        {
            AppSettings.Data.DisabledPacks.Clear();
            Assert.Contains(Vehicles.All(), v => v.Name.Contains("Zonda") && v.Sdp == 25);
            var d = Vehicles.Core[0];
            var o = Vehicles.Own(d);
            o.Sdp = 3;
            Assert.Equal(35, d.Sdp);
            Assert.Equal("повреждён (меньше половины ПСП)", o.State);
            o.Sdp = 0;
            Assert.Equal("уничтожен", o.State);
        }

        [Fact]
        public void OwnedVehicle_SurvivesJsonRoundTrip()
        {
            var o = Vehicles.Own(Vehicles.Core[3]);
            o.Notes = "Тюнинг: NOS";
            var json = System.Text.Json.JsonSerializer.Serialize(o);
            Assert.DoesNotContain("State", json);
            var back = System.Text.Json.JsonSerializer.Deserialize<OwnedVehicle>(json);
            Assert.Equal(o.Name, back.Name); Assert.Equal(40, back.Combat); Assert.Equal("Тюнинг: NOS", back.Notes);
        }
    }
}
