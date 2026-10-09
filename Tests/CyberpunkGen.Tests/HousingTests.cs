using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class HousingTests
    {
        [Fact]
        public void EachTypeHasTenNumberedPlaces()
        {
            AppSettings.Data.DisabledPacks.Clear();
            foreach (var t in Housing.Types)
            {
                var l = Housing.Of(t);
                Assert.Equal(10, l.Count);
                Assert.Equal(Enumerable.Range(1, 10).Select(i => i.ToString()), l.Select(p => p.Get("roll")));
                for (int d = 1; d <= 10; d++) Assert.NotNull(Housing.Roll(t, d));
            }
        }

        [Fact]
        public void RentsFollowCoreHousingTable()
        {
            AppSettings.Data.DisabledPacks.Clear();
            Assert.Equal("500", Housing.RentOf(Housing.Of("Кубический отель")[0]));
            Assert.Equal("1000", Housing.RentOf(Housing.Of("Грузовой контейнер")[0]));
            Assert.Equal("1500", Housing.RentOf(Housing.Of("Квартира-студия")[0]));
            Assert.Equal("0", Housing.RentOf(Housing.Of("Корпоративный конапт")[0]));
        }

        [Fact]
        public void BookTableLookups()
        {
            AppSettings.Data.DisabledPacks.Clear();
            Assert.Contains("Куб-А-Рама", Housing.Roll("Кубический отель", 4).Name);
            Assert.Equal("КК8", Housing.Roll("Корпоративный конапт", 8).Get("code"));
            Assert.Equal("Бриджтаун", Housing.Roll("Грузовой контейнер", 9).Name);
        }

        [Fact]
        public void DisabledPack_HidesPlaces()
        {
            AppSettings.Data.DisabledPacks.Clear();
            AppSettings.Data.DisabledPacks.Add("dlc08");
            try { Assert.Empty(Housing.Of("Кубический отель")); }
            finally { AppSettings.Data.DisabledPacks.Clear(); }
        }
    }
}
