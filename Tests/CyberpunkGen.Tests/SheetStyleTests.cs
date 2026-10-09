using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class SheetStyleTests
    {
        [Fact]
        public void Default_Style_Is_Minimal()
        {
            var saved = AppSettings.Data.UiStyle;
            try
            {
                AppSettings.Data.UiStyle = new SettingsData().UiStyle;
                Assert.Equal("minimal", AppSettings.Data.UiStyle);
                Assert.False(AppSettings.DetailedStyle);
            }
            finally { AppSettings.Data.UiStyle = saved; }
        }

        [Theory]
        [InlineData("detailed", true)]
        [InlineData("detailed2", true)]
        [InlineData("DETAILED", true)]
        [InlineData("minimal", false)]
        [InlineData("что-то другое", false)]
        [InlineData(null, false)]
        public void Detailed_Flag_Follows_The_Setting(string value, bool detailed)
        {
            var saved = AppSettings.Data.UiStyle;
            try
            {
                AppSettings.Data.UiStyle = value;
                Assert.Equal(detailed, AppSettings.DetailedStyle);
            }
            finally { AppSettings.Data.UiStyle = saved; }
        }

        [Theory]
        [InlineData("detailed", "detailed")]
        [InlineData("DETAILED2", "detailed2")]
        [InlineData("minimal", "minimal")]
        [InlineData("???", "minimal")]
        public void Style_Id_Is_Normalized(string value, string id)
        {
            var saved = AppSettings.Data.UiStyle;
            try
            {
                AppSettings.Data.UiStyle = value;
                Assert.Equal(id, AppSettings.StyleId);
            }
            finally { AppSettings.Data.UiStyle = saved; }
        }

        [Fact]
        public void Old_Default_Font_Is_Migrated_Once()
        {
            var old = AppSettings.Migrate(new SettingsData { UiFont = "Consolas" });
            Assert.Equal("Bahnschrift", old.UiFont);
            Assert.True(old.FontMigrated);
            var chosen = AppSettings.Migrate(new SettingsData { UiFont = "Courier New", FontMigrated = true });
            Assert.Equal("Courier New", chosen.UiFont);
            var again = AppSettings.Migrate(new SettingsData { UiFont = "Consolas", FontMigrated = true });
            Assert.Equal("Consolas", again.UiFont);
            Assert.Equal("Bahnschrift", new SettingsData().UiFont);
        }

        [Theory]
        [InlineData("Навыки Восприятия", "👁")]
        [InlineData("Perception Skills", "👁")]
        [InlineData("Ближний бой", "🗡")]
        [InlineData("Fighting Skills", "🗡")]
        [InlineData("Дальний бой", "🔫")]
        [InlineData("Ranged Weapon Skills", "🔫")]
        [InlineData("Образовательные (прод.)", "📚")]
        [InlineData("Технические Навыки", "🔧")]
        [InlineData("Социальные Навыки", "💬")]
        [InlineData("Сценические Навыки", "🎭")]
        [InlineData("Ничего похожего", "")]
        [InlineData(null, "")]
        public void Category_Icons_Are_Found_In_Both_Languages(string name, string icon)
        {
            Assert.Equal(icon, CategoryIconConverter.For(name));
        }

        [Fact]
        public void Empty_Armor_Gauge_Gets_A_Positive_Maximum()
        {
            var c = new AtLeastOneConverter();
            Assert.Equal(1d, c.Convert(0, typeof(double), null, null));
            Assert.Equal(1d, c.Convert(null, typeof(double), null, null));
            Assert.Equal(11d, c.Convert(11, typeof(double), null, null));
        }
    }
}
