using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class NetPackTests
    {
        [Fact]
        public void MidnightDecksAndHardwareEnterNetCatalog()
        {
            AppSettings.Data.DisabledPacks.Clear();
            Packs.Apply();
            try
            {
                Assert.Equal(15, NetCatalog.Decks.Count(d => d.Source != null));
                Assert.Equal(11, NetCatalog.Hardware.Count(h => h.Source != null));
                var ker = NetCatalog.Decks.Single(d => d.Name.Contains("Керберос"));
                Assert.Equal(11, ker.Slots);
                Assert.Equal(1000, ker.Cost);
                Assert.Equal(1, NetCatalog.Hardware.Single(h => h.Name == "Кузня").Slots);
                Assert.Equal(20, NetCatalog.Decks.Single(d => d.Name.StartsWith("Тренировочная")).Cost);

                AppSettings.Data.DisabledPacks.Add("dlc16");
                Packs.Apply();
                Assert.DoesNotContain(NetCatalog.Decks, d => d.Source != null);
                Assert.Equal(3, NetCatalog.Decks.Count);
            }
            finally { AppSettings.Data.DisabledPacks.Clear(); Packs.Apply(); }
        }
    }
}
