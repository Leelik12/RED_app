using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace CyberpunkRED_Generator
{
    // НИП из DLC: готовый враг для базы GM и NPC листа. Файл Data/npcs.json вшит в exe (его собирает Rulebook/build_items.py).
    public class PackNpc
    {
        public string Id { get; set; }
        public string Pack { get; set; }
        public string Group { get; set; }
        public string Name { get; set; }
        public string Tier { get; set; }
        public int Hp { get; set; }
        public int Wound { get; set; }
        public int Ds { get; set; }
        public int Head { get; set; }
        public int Body { get; set; }
        public int Move { get; set; }
        public Dictionary<string, int> Stats { get; set; }
        public Dictionary<string, int> Skills { get; set; }
        public string Weapons { get; set; }
        public string Gear { get; set; }
        public string Cyber { get; set; }
        public string Role { get; set; }
        public string Info { get; set; }
        public int Page { get; set; }
        public bool Paren { get; set; }
    }

    public static class Npcs
    {
        public const string ResourceName = "Data.npcs.json";
        private static List<PackNpc> _all;
        private static readonly object Lock = new object();

        public static IReadOnlyList<PackNpc> All { get { Ensure(); return _all; } }

        private class Doc { public int Version { get; set; } public List<PackNpc> Npcs { get; set; } }

        private static void Ensure()
        {
            if (_all != null) return;
            lock (Lock)
            {
                if (_all != null) return;
                var list = new List<PackNpc>();
                try
                {
                    using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
                        if (s != null) list = Parse(s);
                }
                catch (Exception ex) { ErrorLog.Write("npcs", ex); }
                _all = list;
            }
        }

        public static List<PackNpc> Parse(Stream s)
            => JsonSerializer.Deserialize<Doc>(s, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Npcs ?? new List<PackNpc>();

        public static void SetForTests(List<PackNpc> list) { lock (Lock) { _all = list; } }

        public static int Count(string packId) => All.Count(n => n.Pack == packId);

        // враги включённых пакетов DLC в формате базы GM; Source непустой — пресет встроенный, его нельзя удалить
        public static List<EnemySaveData> Presets() => All.Where(n => Packs.IsEnabled(n.Pack)).Select(ToEnemy).ToList();

        public static EnemySaveData ToEnemy(PackNpc n)
        {
            string role = string.IsNullOrWhiteSpace(n.Role) ? null : "Роль: " + n.Role + ".";
            string src = Packs.PackName(n.Pack);
            return new EnemySaveData
            {
                Name = n.Name,
                HP = n.Hp,
                HeadArmor = n.Head,
                BodyArmor = n.Body,
                BaseDeathSave = n.Ds,
                BaseMove = n.Move,
                Stats = new Dictionary<string, int>(n.Stats ?? new Dictionary<string, int>()),
                Skills = new Dictionary<string, int>(n.Skills ?? new Dictionary<string, int>()),
                Weapons = n.Weapons,
                Gear = n.Gear,
                Cyberware = n.Cyber,
                Source = src,
                Info = string.Join(" ", new[] { n.Tier, role, n.Info }.Where(x => !string.IsNullOrWhiteSpace(x)))
                       + (n.Page > 0 ? " (DLC «" + src + "», с. " + n.Page + ")" : ""),
            };
        }

        // добавляет встроенные пресеты в список; одноимённый пользовательский пресет имеет приоритет
        public static void AddTo(ICollection<EnemySaveData> target)
        {
            foreach (var e in Presets())
                if (!target.Any(x => string.Equals(x.Name, e.Name, StringComparison.OrdinalIgnoreCase))) target.Add(e);
        }
    }
}
