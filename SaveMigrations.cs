using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberpunkRED_Generator
{
    // версионирование файлов персонажей: каждое изменение формата — новый шаг миграции,
    // поэтому персонажи из любой старой версии (0.x–1.9) открываются в новой
    public static class SaveSchema
    {
        // 0 — файл без поля SchemaVersion (все версии до 2.0), 1 — формат 2.0
        public const int Current = 1;

        private static readonly List<KeyValuePair<int, Action<CharacterSaveData>>> Steps =
            new List<KeyValuePair<int, Action<CharacterSaveData>>>
        {
            new KeyValuePair<int, Action<CharacterSaveData>>(1, From0To1),
        };

        // приводит данные к текущему формату; возвращает исходную версию
        public static int Upgrade(CharacterSaveData data)
        {
            int from = data.SchemaVersion;
            foreach (var step in Steps.Where(s => s.Key > from).OrderBy(s => s.Key))
            {
                step.Value(data);
                data.SchemaVersion = step.Key;
            }
            return from;
        }

        public static bool IsFromNewerVersion(CharacterSaveData data) => data.SchemaVersion > Current;

        // 0 -> 1: в старых файлах часть полей могла отсутствовать (поля добавлялись от версии к версии),
        // а роль хранилась одной строкой (до мультикласса 1.7)
        private static void From0To1(CharacterSaveData d)
        {
            d.Stats = d.Stats ?? new Dictionary<string, int>();
            d.SystemStats = d.SystemStats ?? new Dictionary<string, int>();
            d.Lifepath = d.Lifepath ?? new Dictionary<string, string>();
            d.RoleLifepath = d.RoleLifepath ?? new Dictionary<string, string>();
            d.Friends = d.Friends ?? new List<string>();
            d.Enemies = d.Enemies ?? new List<EnemyData>();
            d.TragicLoves = d.TragicLoves ?? new List<string>();
            d.Skills = d.Skills ?? new List<SkillSaveData>();
            d.Weapons = d.Weapons ?? new List<WeaponData>();
            d.GearItems = d.GearItems ?? new List<GearRowItem>();
            d.CyberwareBlocks = d.CyberwareBlocks ?? new List<CyberwareBlockItem>();
            d.CustomCyberwareList = d.CustomCyberwareList ?? new List<CyberwareDef>();
            d.CriticalInjuriesList = d.CriticalInjuriesList ?? new List<string>();
            d.ImprovementLog = d.ImprovementLog ?? new List<string>();
            d.NPCs = d.NPCs ?? new List<EnemySaveData>();
            d.Armor = d.Armor ?? new ArmorData();
            d.Net = d.Net ?? new NetrunData();
            d.Finance = d.Finance ?? new FinanceData();

            if (d.ActiveRoles == null || d.ActiveRoles.Count == 0)
            {
                int rank = d.SystemStats.ContainsKey("RoleRank") ? d.SystemStats["RoleRank"] : 4;
                d.ActiveRoles = new List<RoleData>();
                if (!string.IsNullOrWhiteSpace(d.Role))
                {
                    // «Соло/Фиксер» из старых файлов — это несколько ролей одного ранга
                    foreach (var name in d.Role.Split('/').Select(x => x.Trim()).Where(x => x.Length > 0))
                        d.ActiveRoles.Add(new RoleData { Name = name, Rank = rank });
                }
                if (d.ActiveRoles.Count == 0) d.ActiveRoles.Add(new RoleData { Name = d.Role, Rank = rank });
            }

            // старые версии не писали отдельный перечень критических травм списком
            if (d.CriticalInjuriesList.Count == 0 && !string.IsNullOrWhiteSpace(d.CriticalInjuries))
            {
                foreach (var part in d.CriticalInjuries.Split(new[] { '\n', ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string n = part.Trim();
                    if (CoreDataBase.CriticalInjuries.Any(x => x.Name == n)) d.CriticalInjuriesList.Add(n);
                }
            }
        }
    }
}
