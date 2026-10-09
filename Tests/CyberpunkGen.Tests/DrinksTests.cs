using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class DrinksTests
    {
        [Fact]
        public void HasAll26Drinks() => Assert.Equal(26, Drinks.All().Count);

        [Fact]
        public void MatchBadTouchAndBeer()
        {
            var all = Drinks.All();
            Assert.Equal("Плохое Прикосновение", Drinks.Match(all, new[] { 0, 2, 2, 2, 4 }, true, false, false)?.Name);
            Assert.Equal("Пиво", Drinks.Match(all, new[] { 1, 2, 1, 2, 4 }, false, false, false)?.Name);
            Assert.Null(Drinks.Match(all, new[] { 1, 2, 1, 2, 4 }, true, false, false));
        }

        [Fact]
        public void OptionalKarmotrineAcceptsAnyAmount()
        {
            var all = Drinks.All();
            Assert.Equal("Голубая фея", Drinks.Match(all, new[] { 4, 0, 0, 1, 0 }, false, true, false)?.Name);
            Assert.Equal("Голубая фея", Drinks.Match(all, new[] { 4, 0, 0, 1, 3 }, false, true, false)?.Name);
        }

        [Fact]
        public void SparkleStarDiffersFromSugarRushByAging()
        {
            var all = Drinks.All();
            Assert.Equal("Сверкающая Звезда", Drinks.Match(all, new[] { 2, 0, 1, 0, 0 }, false, true, false)?.Name);
            Assert.Equal("Сахарная Лихорадка", Drinks.Match(all, new[] { 2, 0, 1, 0, 0 }, false, false, false)?.Name);
        }

        [Fact]
        public void EveryRecipeMatchesItself()
        {
            var all = Drinks.All();
            foreach (var d in all)
                Assert.Same(d, Drinks.Match(all, d.Chem, d.Ice, d.Aged, d.Blended));
        }

        [Theory]
        [InlineData(0, 0)] [InlineData(7, 0)] [InlineData(8, 11)] [InlineData(16, 11)] [InlineData(17, 13)]
        public void Intoxication(int karma, int dc) => Assert.Equal(dc, Drinks.IntoxicationDc(karma));
    }
}
