using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class AgentTests
    {
        [Theory]
        [InlineData("bad", false, 17)]
        [InlineData("std", false, 21)]
        [InlineData("good", false, 24)]
        [InlineData("bad", true, 15)]
        [InlineData("std", true, 17)]
        [InlineData("good", true, 21)]
        public void HackDcMatchesBookTables(string q, bool physical, int dc) => Assert.Equal(dc, Agents.HackDc(q, physical));

        [Fact]
        public void BookExampleHackSucceeds()
        {
            // Редай: Интерфейс 4 + база 11 + 4 на кубике = 19 против СЛ 17
            Assert.True(Agents.HackSucceeds(4 + 11 + 4, Agents.HackDc(Agents.Bad, false)));
            Assert.False(Agents.HackSucceeds(16, 17));
        }

        [Fact]
        public void SearchBonusGrowsWithMoreSaiiApp()
        {
            Assert.Equal(0, Agents.SearchBonus(null));
            var a = new AgentState();
            Assert.Equal(2, Agents.SearchBonus(a));
            a.Apps.Add(Agents.SearchApp);
            Assert.Equal(3, Agents.SearchBonus(a));
        }

        [Fact]
        public void MerlynGivesWardrobeBonus()
        {
            Assert.Equal(1, Agents.WardrobeBonus(new AgentState { Model = "Wyzard Technologies Merlyn" }));
            Assert.Equal(0, Agents.WardrobeBonus(new AgentState { Model = "Zetatech Grade A" }));
        }

        [Fact]
        public void AppRollsFollowBook()
        {
            Assert.Contains("Провал", Agents.HusTool(1));
            Assert.Contains("неприятности", Agents.HusTool(10));
            Assert.Contains("+10eb", Agents.HusTool(5));
            Assert.Equal(-1, Agents.TherapyDays(4));
            Assert.Equal(1, Agents.TherapyDays(7));
        }

        [Fact]
        public void PackHasAgentsAppsAndInternalOptions()
        {
            AppSettings.Data.DisabledPacks.Clear();
            var models = Agents.Models();
            Assert.Equal(8, models.Count);
            Assert.Equal(Agents.Good, Agents.QualityOf("Rocklin Augmentics Neuron"));
            Assert.Equal(Agents.Bad, Agents.QualityOf("Segotari Double Agent"));
            Assert.True(Agents.IsInternal("EBM PiR2"));
            Assert.Equal(16, Agents.AppItems().Count);          // 11 приложений Агентов (DLC) + 5 приложений Чёрного хрома
        }
    }
}
