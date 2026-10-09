using System;
using System.IO;
using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class MapTests
    {
        [Fact]
        public void Districts_Have_Positions_On_The_Raster_Map()
        {
            foreach (var d in CityMap.Districts)
            {
                Assert.InRange(d.MX, 1, 2749);
                Assert.InRange(d.MY, 1, 4249);
            }
        }

        [Fact]
        public void All_24_Districts_Loaded_With_Unique_Letters()
        {
            Assert.Equal(24, CityMap.Districts.Count);
            Assert.Equal(24, CityMap.Districts.Select(d => d.Letter).Distinct().Count());
            foreach (var d in CityMap.Districts)
            {
                Assert.False(string.IsNullOrWhiteSpace(d.Name), d.Letter);
                Assert.False(string.IsNullOrWhiteSpace(d.Gangs), d.Letter);
                Assert.True(d.X > 0 && d.Y > 0, d.Letter);
                Assert.NotEqual("", CityMap.KindName(d.Kind));
            }
        }

        [Fact]
        public void Places_Belong_To_Their_District()
        {
            foreach (var d in CityMap.Districts)
                foreach (var p in d.Places)
                {
                    Assert.StartsWith(d.Letter, p.Code);
                    Assert.False(string.IsNullOrWhiteSpace(p.Name), p.Code);
                    Assert.False(string.IsNullOrWhiteSpace(p.Text), p.Code);
                }
            Assert.True(CityMap.Districts.Sum(d => d.Places.Count) > 100);
        }

        [Fact]
        public void Text_Has_No_Broken_Words_Or_Dangling_Headers()
        {
            foreach (var d in CityMap.Districts)
            {
                Assert.DoesNotContain("вос становленными", d.Manager + d.Gangs);
                foreach (var p in d.Places) Assert.DoesNotContain("  ", p.Text);
            }
            Assert.Equal("нет", CityMap.Get("R").Gangs);
        }

        [Theory]
        [InlineData("Курорт Пасифика", "W")]
        [InlineData("Новый Вестбрук", "P")]
        [InlineData("Хейвуд / Санто-Доминго", "U,V")]
        [InlineData("Южный Найт-Сити (доки)", "I")]
        [InlineData("Университетский район / Глен", "F,G")]
        [InlineData("Ранчо Коронадо / Пасифика", "X,W")]
        [InlineData("Морро-Рок", "Морро-Рок")]
        [InlineData("где-то", "")]
        public void Zone_Names_Map_To_Districts(string zone, string expected)
            => Assert.Equal(expected, string.Join(",", CityMap.ZoneLetters(zone)));

        [Fact]
        public void Every_Night_Market_Is_Placed_On_An_Existing_District()
        {
            foreach (var m in NightMarket.All)
            {
                Assert.True(CityMap.MarketDistrict.ContainsKey(m.Id), m.Id);
                Assert.NotNull(CityMap.Get(CityMap.MarketDistrict[m.Id]));
            }
        }

        [Fact]
        public void Search_Finds_Gangs_And_Places()
        {
            var hits = CityMap.Districts.Where(d => CityMap.Matches(d, "Мальстрём")).Select(d => d.Letter).ToList();
            Assert.Contains("B", hits);
            Assert.Contains("D", hits);
            Assert.DoesNotContain("R", hits);
            Assert.Equal(24, CityMap.Districts.Count(d => CityMap.Matches(d, "  ")));
        }

        private static readonly string Original = AppSettings.FilePath;

        private static string Isolate()
        {
            string path = Path.Combine(Path.GetTempPath(), "map_settings_" + Guid.NewGuid().ToString("N") + ".json");
            AppSettings.Reload(path);
            return path;
        }

        [Fact]
        public void Layers_Hide_Properties_And_Markets()
        {
            string path = Isolate();
            try
            {
                int all = CityMap.Properties("W").Count;
                Assert.True(all > 0);
                Assert.Equal(all, CityMap.VisibleProperties("W").Count);
                Assert.Single(CityMap.VisibleMarkets("A"));

                CityMap.SetLayer("cube", false);
                CityMap.SetLayer("market", false);
                Assert.True(CityMap.VisibleProperties("W").Count < all);
                Assert.All(CityMap.VisibleProperties("W"), i => Assert.NotEqual("cube", i.Get("type")));
                Assert.Empty(CityMap.VisibleMarkets("A"));
                Assert.False(CityMap.LayerOn("cube"));

                CityMap.SetLayer("cube", true);
                Assert.True(CityMap.LayerOn("cube"));
            }
            finally { AppSettings.Reload(Original); if (File.Exists(path)) File.Delete(path); }
        }

        [Fact]
        public void Custom_Pins_Are_Saved_And_Removed()
        {
            string path = Isolate();
            try
            {
                var pin = CityMap.AddPin(100.4, 250.6);
                pin.Title = "Склад Фикси";
                AppSettings.Save();
                Assert.Equal(100, pin.X);
                Assert.Equal(251, pin.Y);

                AppSettings.Reload(path);   // перечитать с диска
                var back = AppSettings.Data.MapPins.Single();
                Assert.Equal("Склад Фикси", back.Title);
                Assert.Equal(pin.Id, back.Id);

                CityMap.RemovePin(pin.Id);
                AppSettings.Reload(path);
                Assert.Empty(AppSettings.Data.MapPins);
            }
            finally { AppSettings.Reload(Original); if (File.Exists(path)) File.Delete(path); }
        }
    }
}
