using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace CyberpunkRED_Generator
{
    public class CombatantSave
    {
        public string Name { get; set; }
        public bool IsPlayer { get; set; }
        public int Initiative { get; set; }
        public int MaxHP { get; set; }
        public int CurrentHP { get; set; }
        public int HeadArmor { get; set; }
        public int BodyArmor { get; set; }
        public int BaseMove { get; set; }
        public int BaseDeathSave { get; set; }
        public string Weapons { get; set; }
        public string Gear { get; set; }
        public string Cyberware { get; set; }
        public string Conditions { get; set; }
        public List<string> Injuries { get; set; } = new List<string>();
        public EnemySaveData Original { get; set; }
    }

    public class CombatSaveData
    {
        public int SchemaVersion { get; set; } = 1;
        public string Saved { get; set; }
        public int Round { get; set; } = 1;
        public int Turn { get; set; } = -1;
        public List<CombatantSave> Combatants { get; set; } = new List<CombatantSave>();
    }

    // сохранение и загрузка боя ГМ-экрана, нумерация одинаковых врагов, состояния
    public static class CombatSaves
    {
        public static readonly string[] ConditionPresets =
        {
            "Оглушён", "Горит", "Отравлен", "Прижат огнём", "Схвачен", "Ослеплён", "В укрытии", "Без сознания", "Истекает кровью", "Под наркотиком"
        };

        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        public static CombatantSave Capture(CombatantViewModel c) => new CombatantSave
        {
            Name = c.Name, IsPlayer = c.IsPlayer, Initiative = c.Initiative, MaxHP = c.MaxHP, CurrentHP = c.CurrentHP,
            HeadArmor = c.HeadArmor, BodyArmor = c.BodyArmor, BaseMove = c.BaseMove, BaseDeathSave = c.BaseDeathSave,
            Weapons = c.Weapons, Gear = c.Gear, Cyberware = c.Cyberware, Conditions = c.Conditions,
            Injuries = c.ActiveInjuries.Select(i => i.Name).ToList(), Original = c.OriginalData
        };

        public static CombatantViewModel Restore(CombatantSave s)
        {
            var c = new CombatantViewModel
            {
                OriginalData = s.Original, Name = s.Name, IsPlayer = s.IsPlayer, Initiative = s.Initiative,
                BaseMove = s.BaseMove, BaseDeathSave = s.BaseDeathSave, Weapons = s.Weapons, Gear = s.Gear, Cyberware = s.Cyberware,
                Conditions = s.Conditions
            };
            c.MaxHP = Math.Max(1, s.MaxHP);
            c.CurrentHP = s.CurrentHP;
            c.HeadArmor = s.HeadArmor;
            c.BodyArmor = s.BodyArmor;
            foreach (var name in s.Injuries ?? new List<string>())
            {
                var def = CoreDataBase.CriticalInjuries.FirstOrDefault(x => x.Name == name);
                if (def == null) continue;
                c.ActiveInjuries.Add(new GMCriticalInjuryItem
                {
                    Name = def.Name, Description = def.Description, QuickFix = def.QuickFix, Treatment = def.Treatment,
                    AllActionsPenalty = def.AllActionsPenalty, MovePenalty = def.MovePenalty, DeathSavePenalty = def.DeathSavePenalty, EffectText = def.EffectText
                });
            }
            c.Recalculate();
            return c;
        }

        public static CombatSaveData Capture(IEnumerable<CombatantViewModel> list, int round, int turn)
            => new CombatSaveData { Saved = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), Round = round, Turn = turn, Combatants = list.Select(Capture).ToList() };

        public static string ToJson(CombatSaveData d) => JsonSerializer.Serialize(d, Json);

        public static CombatSaveData FromJson(string json) => JsonSerializer.Deserialize<CombatSaveData>(json, Json) ?? new CombatSaveData();

        // имена для count копий врага: «Бандит 1…N»; продолжает нумерацию, если такие уже в бою
        public static List<string> SpawnNames(string baseName, int count, IEnumerable<string> existing)
        {
            count = Math.Max(1, count);
            int max = 0;
            bool plain = false;
            string prefix = baseName + " ";
            foreach (var e in existing ?? Enumerable.Empty<string>())
            {
                if (e == baseName) plain = true;
                else if (e != null && e.StartsWith(prefix) && int.TryParse(e.Substring(prefix.Length), out int n)) max = Math.Max(max, n);
            }
            if (count == 1 && !plain && max == 0) return new List<string> { baseName };
            int start = Math.Max(max, plain ? 1 : 0) + 1;
            return Enumerable.Range(start, count).Select(n => prefix + n).ToList();
        }

        public static string AddCondition(string current, string add)
        {
            var parts = (current ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
            if (!string.IsNullOrWhiteSpace(add) && !parts.Contains(add.Trim(), StringComparer.OrdinalIgnoreCase)) parts.Add(add.Trim());
            return string.Join(", ", parts);
        }

        public static string RemoveCondition(string current, string remove)
            => string.Join(", ", (current ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).Where(p => p.Length > 0 && !p.Equals(remove, StringComparison.OrdinalIgnoreCase)));
    }
}
