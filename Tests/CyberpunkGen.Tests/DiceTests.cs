using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class DiceTests
    {
        [Theory]
        [InlineData("2d6 (7)", 1, 2, 12)]
        [InlineData("1d10+2", 1, 3, 12)]
        [InlineData("1d6/2", 1, 1, 3)]
        [InlineData("3d6", 2, 6, 36)]
        public void Roll_StaysInRange(string text, int times, int min, int max)
        {
            for (int i = 0; i < 300; i++)
            {
                Assert.True(DiceRoller.TryRoll(text, times, out var r));
                Assert.InRange(r.Total, min, max);
            }
        }

        [Fact]
        public void FixedNumber_IsMultiplied()
        {
            Assert.True(DiceRoller.TryRoll("5", 3, out var r));
            Assert.Equal(15, r.Total);
        }

        [Theory]
        [InlineData("")] [InlineData("abc")] [InlineData("0d6")] [InlineData("1d1")] [InlineData("101d6")]
        public void BadFormulas_AreRejected(string text)
            => Assert.False(DiceRoller.TryRoll(text, 1, out _));

        [Fact]
        public void D10Check_OnlyNaturalExtremesChain()
        {
            for (int i = 0; i < 2000; i++)
            {
                int v = DiceRoller.D10Check(out string detail);
                Assert.InRange(v, -9, 20);
                Assert.False(string.IsNullOrEmpty(detail));
            }
        }
    }
}
