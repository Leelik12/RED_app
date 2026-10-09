using System.Linq;
using System.Text.Json;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class JournalTests
    {
        [Fact]
        public void Compose_Adds_Bonus_To_Die()
        {
            var r = SkillRoll.Compose("Атлетика", 9, 7, "7");
            Assert.Equal(16, r.Total);
            Assert.Contains("Атлетика", r.Text);
            Assert.Contains("= 16", r.Text);
        }

        [Fact]
        public void Journal_Caps_Entries_And_Notifies()
        {
            var j = new JournalData();
            int changed = 0; j.Changed = () => changed++;
            for (int i = 0; i < JournalData.MaxEntries + 20; i++) j.Add("note", "n" + i);
            Assert.Equal(JournalData.MaxEntries, j.Entries.Count);
            Assert.Equal("n20", j.Entries.First().Text);
            Assert.Equal(JournalData.MaxEntries + 20, changed);
            j.Clear();
            Assert.Empty(j.Entries);
        }

        [Fact]
        public void Journal_RoundTrips_And_Old_Saves_Open_Empty()
        {
            var d = new CharacterSaveData();
            d.Journal.Add("roll", "x");
            d.Journal.Portrait = "AAAA";
            var back = JsonSerializer.Deserialize<CharacterSaveData>(JsonSerializer.Serialize(d));
            Assert.Single(back.Journal.Entries);
            Assert.Equal("AAAA", back.Journal.Portrait);
            var old = JsonSerializer.Deserialize<CharacterSaveData>("{\"Name\":\"Old\"}");
            Assert.NotNull(old.Journal);
            Assert.Empty(old.Journal.Entries);
        }

        [Fact]
        public void Portrait_Bad_Input_Gives_Null()
        {
            Assert.Null(Portraits.ToImage("not base64!"));
            Assert.Null(Portraits.ToImage(null));
        }
    }
}
