using System.IO;
using System.Linq;
using System.Text.Json;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    // персонажи из старых версий должны открываться и приводиться к текущему формату
    public class MigrationTests
    {
        static string Dir => Path.Combine(AppContext_BaseDir(), "Fixtures");
        static string AppContext_BaseDir() => System.AppDomain.CurrentDomain.BaseDirectory;

        public static System.Collections.Generic.IEnumerable<object[]> Files()
            => Directory.GetFiles(Dir, "*.json").Select(f => new object[] { Path.GetFileName(f) });

        static CharacterSaveData Load(string name)
        {
            string json = File.ReadAllText(Path.Combine(Dir, name));
            return JsonSerializer.Deserialize<CharacterSaveData>(json);
        }

        [Theory, MemberData(nameof(Files))]
        public void Upgrade_BringsAnyOldFileToCurrentSchema(string file)
        {
            var d = Load(file);
            Assert.Equal(0, d.SchemaVersion);
            int from = SaveSchema.Upgrade(d);
            Assert.Equal(0, from);
            Assert.Equal(SaveSchema.Current, d.SchemaVersion);
            Assert.False(string.IsNullOrWhiteSpace(d.Name));
            Assert.NotNull(d.Stats); Assert.NotNull(d.Skills); Assert.NotNull(d.Weapons);
            Assert.NotNull(d.GearItems); Assert.NotNull(d.CyberwareBlocks); Assert.NotNull(d.Net); Assert.NotNull(d.Finance);
            Assert.NotEmpty(d.ActiveRoles);
            Assert.All(d.ActiveRoles, r => Assert.True(r.Rank >= 1));
        }

        [Theory, MemberData(nameof(Files))]
        public void Upgrade_IsIdempotent(string file)
        {
            var d = Load(file);
            SaveSchema.Upgrade(d);
            string a = JsonSerializer.Serialize(d);
            Assert.Equal(SaveSchema.Current, SaveSchema.Upgrade(d));
            Assert.Equal(a, JsonSerializer.Serialize(d));
        }

        [Fact]
        public void MultiRoleString_IsSplitIntoRoles()
        {
            var d = Load("legacy_multirole_string.json");
            SaveSchema.Upgrade(d);
            Assert.Equal(new[] { "Соло", "Фиксер" }, d.ActiveRoles.Select(r => r.Name).ToArray());
            Assert.All(d.ActiveRoles, r => Assert.Equal(5, r.Rank));
        }

        [Fact]
        public void NewerFile_IsDetected()
        {
            var d = new CharacterSaveData { SchemaVersion = SaveSchema.Current + 1 };
            Assert.True(SaveSchema.IsFromNewerVersion(d));
            Assert.False(SaveSchema.IsFromNewerVersion(new CharacterSaveData { SchemaVersion = SaveSchema.Current }));
        }

        [Fact]
        public void Upgrade_PreservesStatsAndSkills()
        {
            var d = Load("legacy_1_9.json");
            int stats = d.Stats.Count, skills = d.Skills.Count; string name = d.Name;
            SaveSchema.Upgrade(d);
            Assert.Equal(stats, d.Stats.Count); Assert.Equal(skills, d.Skills.Count); Assert.Equal(name, d.Name);
        }
    }
}
