using System;
using System.Collections.Generic;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class WeatherTests
    {
        // последовательность заранее заданных бросков
        private static Func<int, int> Seq(params int[] v)
        {
            var q = new Queue<int>(v);
            return n => q.Dequeue();
        }

        [Theory]
        [InlineData(12, 0)] [InlineData(1, 0)] [InlineData(2, 0)]
        [InlineData(3, 1)] [InlineData(5, 1)]
        [InlineData(6, 2)] [InlineData(8, 2)]
        [InlineData(9, 3)] [InlineData(11, 3)]
        public void SeasonByMonth(int month, int season) => Assert.Equal(season, Weather.SeasonOf(month));

        [Fact]
        public void WinterTableMatchesBook()
        {
            var r = Weather.Roll(1, Seq(1, 2));
            Assert.Equal("Холодно (около 2°C)", r.Temperature);
            Assert.Equal("Небольшой дождь/Слякоть", r.Condition);
            Assert.Null(r.Strange);
            Assert.Contains("экспозиции", r.Rules);
        }

        [Fact]
        public void StrangeWeatherRollsStateAndDuration()
        {
            // лето: темп 4 (Жарко 27), условия 6 (странная), d10 = 5 (кислотный дождь), длительность 1d6 = 3
            var r = Weather.Roll(7, Seq(4, 6, 5, 3));
            Assert.Equal("Кислотный дождь", r.Strange);
            Assert.Equal("3 часа", r.Duration);
            Assert.Contains("кислотный дождь (3 часа)", Weather.Summary(r));
        }

        [Fact]
        public void EightIsColdSnapWhenCool()
        {
            var r = Weather.Roll(1, Seq(5, 6, 8, 2));
            Assert.Equal(Weather.ColdSnap, r.Strange);
            Assert.Equal("Ниже нуля", r.Temperature);
            Assert.Equal("2 дня", r.Duration);
        }

        [Fact]
        public void EightIsExtremeHeatWhenWarm()
        {
            var r = Weather.Roll(7, Seq(5, 6, 8, 1));
            Assert.Equal(Weather.ExtremeHeat, r.Strange);
            Assert.Equal("40–45°C", r.Temperature);
            Assert.Equal("1 день", r.Duration);
        }

        [Fact]
        public void StormDurationIsInTensOfMinutes()
        {
            var r = Weather.Roll(4, Seq(2, 6, 6, 4));
            Assert.Equal("Смертоносная гроза", r.Strange);
            Assert.Equal("40 мин", r.Duration);
        }

        [Fact]
        public void EveryRollAndMonthProducesRules()
        {
            for (int m = 1; m <= 12; m++)
                for (int t = 1; t <= 6; t++)
                    for (int c = 1; c <= 6; c++)
                        for (int s = 1; s <= (c == 6 ? 10 : 1); s++)
                        {
                            var r = Weather.Roll(m, Seq(t, c, s, 1));
                            Assert.False(string.IsNullOrEmpty(r.Temperature));
                            Assert.False(string.IsNullOrEmpty(r.Rules), $"m{m} t{t} c{c} s{s}");
                        }
        }

        [Fact]
        public void RealDiceStayInRange()
        {
            for (int i = 0; i < 500; i++) Assert.NotNull(Weather.Roll(1 + i % 12).Rules);
        }
    }
}
