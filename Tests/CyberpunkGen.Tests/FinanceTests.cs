using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class FinanceTests
    {
        [Theory]
        [InlineData("1500", 1500)]
        [InlineData("1 500", 1500)]
        [InlineData("1,500 eb", 1500)]
        [InlineData("-300", -300)]
        public void TryParseMoney_ParsesCommonForms(string text, int expected)
        {
            Assert.True(FinanceCatalog.TryParseMoney(text, out int v));
            Assert.Equal(expected, v);
        }

        [Theory] [InlineData("")] [InlineData("eb")] [InlineData(null)]
        public void TryParseMoney_RejectsNonNumbers(string text)
            => Assert.False(FinanceCatalog.TryParseMoney(text, out _));

        [Theory]
        [InlineData(1500, "1 500")]
        [InlineData(-20000, "−20 000")]
        [InlineData(0, "0")]
        [InlineData(999, "999")]
        public void FormatMoney_GroupsThousands(int v, string expected)
            => Assert.Equal(expected, FinanceCatalog.FormatMoney(v));

        [Fact]
        public void MonthlyBill_CountsRentOnlyWhenRented()
        {
            var life = new LifestyleDef { Cost = 1000 };
            var rented = new HousingDef { Rent = 500 };
            Assert.Equal(1500, FinanceCatalog.MonthlyBill(life, rented, false));
            Assert.Equal(1000, FinanceCatalog.MonthlyBill(life, rented, true));
            Assert.Equal(1000, FinanceCatalog.MonthlyBill(life, new HousingDef { Rent = 500, Provided = true }, false));
            Assert.Equal(0, FinanceCatalog.MonthlyBill(null, null, false));
        }

        [Fact]
        public void Catalogs_AreNotEmpty()
        {
            Assert.NotEmpty(FinanceCatalog.Lifestyles); Assert.NotEmpty(FinanceCatalog.Housing); Assert.NotEmpty(FinanceCatalog.Services);
        }
    }
}
