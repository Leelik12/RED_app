using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class EconomicsTests
    {
        [Theory]
        [InlineData(499, null)]
        [InlineData(500, 15)]
        [InlineData(999, 15)]
        [InlineData(1000, 17)]
        [InlineData(5000, 21)]
        [InlineData(9999, 21)]
        [InlineData(10000, 24)]
        [InlineData(250000, 24)]
        public void Streetwise_Dv_By_Cost(int cost, int? dv) => Assert.Equal(dv, FinanceCatalog.StreetwiseDv(cost));

        [Fact]
        public void Every_Lifestyle_Has_Details_And_Costs_Match_Black_Chrome()
        {
            Assert.All(FinanceCatalog.Lifestyles, l => Assert.False(string.IsNullOrWhiteSpace(l.Details)));
            Assert.Equal(new[] { 100, 300, 600, 1500 }, FinanceCatalog.Lifestyles.ConvertAll(l => l.Cost).ToArray());
        }
    }
}
