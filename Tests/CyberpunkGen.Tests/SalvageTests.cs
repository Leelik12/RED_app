using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class SalvageTests
    {
        [Theory]
        [InlineData(8, false, 0)] [InlineData(9, false, 10)] [InlineData(13, false, 20)] [InlineData(16, false, 50)]
        [InlineData(24, false, 500)] [InlineData(25, false, 500)] [InlineData(29, false, 1000)]
        [InlineData(12, true, 0)] [InlineData(13, true, 10)] [InlineData(21, true, 100)] [InlineData(29, true, 500)] [InlineData(40, true, 500)]
        public void ValueMatchesBookTable(int total, bool specific, int value) => Assert.Equal(value, Salvage.Value(total, specific));

        [Fact]
        public void BookExample_Total25_GivesFiveHundredAnyCategory() => Assert.Equal(500, Salvage.Value(25, false));

        [Fact]
        public void Categories_FollowD6Table()
        {
            Assert.Equal("Киберснаряжение", Salvage.Category(1));
            Assert.Equal("Оружие", Salvage.Category(2));
            Assert.Equal("Стандартное снаряжение", Salvage.Category(3));
            Assert.Equal("Стандартное снаряжение", Salvage.Category(4));
            Assert.Equal("Броня", Salvage.Category(5));
            Assert.Equal("Кибердеки и программы", Salvage.Category(6));
            Assert.Equal(6, Salvage.Hazards.Length);
        }

        [Fact]
        public void Eligible_RespectsValue_AndCategory()
        {
            AppSettings.Data.DisabledPacks.Clear();
            Packs.Apply();
            Assert.All(Salvage.Eligible(2, 100), p => Assert.True(p.Value <= 100));
            Assert.NotEmpty(Salvage.Eligible(5, 500));
            Assert.All(Salvage.Eligible(6, 1000), p => Assert.Contains("ибердек", p.Key, System.StringComparison.OrdinalIgnoreCase) );
        }
    }
}
