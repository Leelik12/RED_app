using System.Text.Json;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class EffectsTests
    {
        [Theory]
        [InlineData(5, "раундов", 5)]
        [InlineData(2, "минут", 40)]
        [InlineData(1, "часов", 1200)]
        [InlineData(0, "раундов", -1)]
        public void ToRounds_Converts_Units(int amount, string unit, int expected)
            => Assert.Equal(expected, EffectsData.ToRounds(amount, unit));

        [Fact]
        public void Tick_Counts_Down_And_Removes_Expired()
        {
            var fx = new EffectsData();
            fx.Add("Стимулятор", "", 2);
            fx.Add("Бессрочный", "", -1);
            Assert.Empty(fx.Tick());
            var gone = fx.Tick();
            Assert.Single(gone);
            Assert.Equal("Стимулятор", gone[0].Name);
            Assert.Single(fx.Items);
            Assert.Empty(fx.Tick(1000));
            Assert.Equal(-1, fx.Items[0].RoundsLeft);
        }

        [Fact]
        public void Describe_And_RoundTrip()
        {
            Assert.Equal("до отмены", EffectsData.Describe(-1));
            Assert.Equal("3 р.", EffectsData.Describe(3));
            Assert.Equal("1 мин 5 р.", EffectsData.Describe(25));
            Assert.Equal("2 ч", EffectsData.Describe(2400));
            var d = new CharacterSaveData();
            d.Effects.Add("Яд", "СЛ 15", 10);
            var back = JsonSerializer.Deserialize<CharacterSaveData>(JsonSerializer.Serialize(d));
            Assert.Equal(10, back.Effects.Items[0].RoundsLeft);
            Assert.Empty(JsonSerializer.Deserialize<CharacterSaveData>("{}").Effects.Items);
        }
    }
}
