using System;
using System.Collections.Generic;
using System.Linq;
using CyberpunkRED_Generator;
using Xunit;

namespace CyberpunkGen.Tests
{
    public class CombatTests
    {
        [Fact]
        public void SpawnNames_Single_Stays_Plain_Until_Duplicate()
        {
            Assert.Equal(new[] { "Бандит" }, CombatSaves.SpawnNames("Бандит", 1, new string[0]));
            Assert.Equal(new[] { "Бандит 2" }, CombatSaves.SpawnNames("Бандит", 1, new[] { "Бандит" }));
        }

        [Fact]
        public void SpawnNames_Several_Are_Numbered_And_Continue()
        {
            Assert.Equal(new[] { "Бандит 1", "Бандит 2", "Бандит 3" }, CombatSaves.SpawnNames("Бандит", 3, new string[0]));
            Assert.Equal(new[] { "Бандит 4", "Бандит 5" }, CombatSaves.SpawnNames("Бандит", 2, new[] { "Бандит 1", "Бандит 3", "Игрок" }));
            Assert.Equal(new[] { "Бандит 2", "Бандит 3" }, CombatSaves.SpawnNames("Бандит", 2, new[] { "Бандит" }));
        }

        [Fact]
        public void Conditions_Add_Remove_Dedup()
        {
            string c = CombatSaves.AddCondition(null, "Оглушён");
            c = CombatSaves.AddCondition(c, "Горит");
            c = CombatSaves.AddCondition(c, "оглушён");
            Assert.Equal("Оглушён, Горит", c);
            Assert.Equal("Горит", CombatSaves.RemoveCondition(c, "ОГЛУШЁН"));
            Assert.Equal("", CombatSaves.RemoveCondition("Горит", "Горит"));
        }

        [Fact]
        public void Combat_Roundtrips_Through_Json()
        {
            var enemy = new EnemySaveData { Name = "Бандит", HP = 20, HeadArmor = 7, BodyArmor = 7, BaseMove = 5, BaseDeathSave = 5, Weapons = "Пистолет" };
            var a = new CombatantViewModel { OriginalData = enemy, Name = "Бандит 1", Initiative = 12, BaseMove = 5, BaseDeathSave = 5, Weapons = "Пистолет", Conditions = "Горит" };
            a.MaxHP = 20; a.CurrentHP = 9; a.HeadArmor = 6; a.BodyArmor = 4;
            var inj = CoreDataBase.CriticalInjuries.FirstOrDefault();
            if (inj != null)
                a.ActiveInjuries.Add(new GMCriticalInjuryItem { Name = inj.Name, Description = inj.Description, EffectText = inj.EffectText });
            var p = new CombatantViewModel { Name = "ИГРОК", IsPlayer = true, Initiative = 15, MaxHP = 40, CurrentHP = 40 };

            string json = CombatSaves.ToJson(CombatSaves.Capture(new[] { a, p }, 3, 1));
            var data = CombatSaves.FromJson(json);

            Assert.Equal(3, data.Round);
            Assert.Equal(1, data.Turn);
            Assert.Equal(2, data.Combatants.Count);
            var back = CombatSaves.Restore(data.Combatants[0]);
            Assert.Equal("Бандит 1", back.Name);
            Assert.Equal(9, back.CurrentHP);
            Assert.Equal(20, back.MaxHP);
            Assert.Equal(6, back.HeadArmor);
            Assert.Equal("Горит", back.Conditions);
            Assert.True(back.HasConditions);
            Assert.Equal("Бандит", back.OriginalData.Name);
            if (inj != null) Assert.Equal(inj.Name, back.ActiveInjuries.Single().Name);
            Assert.True(CombatSaves.Restore(data.Combatants[1]).IsPlayer);
        }

        private static EnemySaveData E(string name, string info = null) => new EnemySaveData { Name = name, HP = 20, Info = info, HeadArmor = 7, BodyArmor = 7 };

        [Fact]
        public void Tier_Detected_From_Dlc_Info()
        {
            Assert.Equal(Encounters.Lieutenant, Encounters.TierOf(E("a", "Прожжённый лейтенант. Роль: x.")));
            Assert.Equal(Encounters.MiniBoss, Encounters.TierOf(E("a", "Прожжённый мини-босс.")));
            Assert.Equal(Encounters.Boss, Encounters.TierOf(E("a", "Босс.")));
            Assert.Equal(Encounters.Six, Encounters.TierOf(E("a", "Прожжённая шестёрка.")));
            Assert.Equal(Encounters.Six, Encounters.TierOf(E("a")));
        }

        [Fact]
        public void Encounter_Budget_Follows_Party_Size_And_Ratios()
        {
            var pool = new List<EnemySaveData> { E("Шестёрка", "Прожжённая шестёрка"), E("Лейтенант", "Прожжённый лейтенант"), E("Мини-босс", "Прожжённый мини-босс") };
            var rng = new Random(3);

            // 4 игрока, обычная: бюджет 4 = мини-босс (3) + шестёрка (1)
            var s4 = Encounters.Generate(pool, 4, 1, rng);
            Assert.Equal(4, s4.Sum(s => Encounters.Cost(s.Tier) * s.Count));
            Assert.Contains(s4, s => s.Tier == Encounters.MiniBoss);

            // 2 игрока: мини-босса нет (он на троих), лейтенант есть
            var s2 = Encounters.Generate(pool, 2, 1, rng);
            Assert.DoesNotContain(s2, s => s.Tier == Encounters.MiniBoss);
            Assert.Contains(s2, s => s.Tier == Encounters.Lieutenant);
            Assert.Equal(2, s2.Sum(s => Encounters.Cost(s.Tier) * s.Count));

            // 1 игрок: только шестёрки
            var s1 = Encounters.Generate(pool, 1, 1, rng);
            Assert.All(s1, s => Assert.Equal(Encounters.Six, s.Tier));
        }

        [Fact]
        public void Encounter_Difficulty_Scales_Budget_And_Empty_Pool_Is_Safe()
        {
            var pool = new List<EnemySaveData> { E("Шестёрка") };
            var rng = new Random(1);
            int easy = Encounters.TotalCount(Encounters.Generate(pool, 4, 0, rng));
            int normal = Encounters.TotalCount(Encounters.Generate(pool, 4, 1, rng));
            int hard = Encounters.TotalCount(Encounters.Generate(pool, 4, 2, rng));
            Assert.Equal(2, easy); Assert.Equal(4, normal); Assert.Equal(6, hard);
            Assert.Empty(Encounters.Generate(new List<EnemySaveData>(), 4, 1, rng));
            Assert.Empty(Encounters.Generate(null, 4, 1, rng));
        }

        [Fact]
        public void Loot_Lists_Carried_Gear_For_Every_Slot()
        {
            var slots = new List<EncounterSlot> { new EncounterSlot { Enemy = new EnemySaveData { Name = "Бандит", Weapons = "Пистолет", Gear = "Куртка" }, Tier = Encounters.Six, Count = 2 } };
            var loot = Encounters.Loot(slots, new Random(5));
            Assert.Contains(loot, l => l.Contains("Бандит ×2") && l.Contains("Пистолет") && l.Contains("Куртка"));
        }
    }
}
