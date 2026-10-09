using System;
using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class NightMarketTests
    {
        [Fact]
        public void Six_Markets_With_Descriptions()
        {
            Assert.Equal(6, NightMarket.All.Count);
            foreach (var m in NightMarket.All)
            {
                Assert.False(string.IsNullOrWhiteSpace(m.Name));
                Assert.False(string.IsNullOrWhiteSpace(m.Sells));
                Assert.False(string.IsNullOrWhiteSpace(m.Invite));
            }
            Assert.Equal(6, NightMarket.All.Select(m => m.Id).Distinct().Count());
        }

        [Fact]
        public void Every_Market_Has_Stock_In_The_Catalog()
        {
            foreach (var m in NightMarket.All)
                Assert.True(NightMarket.Pool(m).Count >= m.MinItems, m.Id + " pool=" + NightMarket.Pool(m).Count);
        }

        [Fact]
        public void Generate_Is_Unique_Bounded_And_Matches_Filter()
        {
            var rng = new Random(7);
            foreach (var m in NightMarket.All)
                for (int k = 0; k < 10; k++)
                {
                    var stock = NightMarket.Generate(m, rng);
                    Assert.InRange(stock.Count, m.MinItems, m.MaxItems);
                    Assert.Equal(stock.Count, stock.Select(i => i.Id).Distinct().Count());
                    Assert.All(stock, i => Assert.True(m.Filter(i)));
                }
        }

        [Fact]
        public void Kernaghan_Sells_Only_Super_Luxury()
        {
            var k = NightMarket.Find("kernaghan");
            var stock = NightMarket.Generate(k, new Random(1));
            Assert.All(stock, i => Assert.True(i.Cost >= 10000));
        }
    }
}
