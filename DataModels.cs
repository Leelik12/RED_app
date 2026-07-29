using CyberpunkRED_Generator;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace CyberpunkRED_Generator
{
    public class RoleData : INotifyPropertyChanged
    {
        private string _name;
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

        private int _rank;
        public int Rank { get => _rank; set { _rank = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public class CharacterSaveData
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public List<RoleData> ActiveRoles { get; set; } = new List<RoleData>();
        public bool IsGearSimpleMode { get; set; }
        public string GearSimpleNames { get; set; }
        public string GearSimpleNotes { get; set; }
        public List<EnemySaveData> NPCs { get; set; } = new List<EnemySaveData>();
        public string RoleAbilityNotes { get; set; }
        public Dictionary<string, int> Stats { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> SystemStats { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, string> Lifepath { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> RoleLifepath { get; set; } = new Dictionary<string, string>();
        public List<string> Friends { get; set; } = new List<string>();
        public List<EnemyData> Enemies { get; set; } = new List<EnemyData>();
        public List<string> TragicLoves { get; set; } = new List<string>();
        public List<SkillSaveData> Skills { get; set; } = new List<SkillSaveData>();
        public string Notes { get; set; }
        public CyberpunkRED_Generator.ArmorData Armor { get; set; } = new CyberpunkRED_Generator.ArmorData();
        public List<CyberpunkRED_Generator.WeaponData> Weapons { get; set; } = new List<CyberpunkRED_Generator.WeaponData>();
        public string StyleNotes { get; set; }
        public string Housing { get; set; }
        public string Rent { get; set; }
        public string Lifestyle { get; set; }
        public string ReputationEvents { get; set; }
        public string AmmoValue { get; set; }
        public string CashValue { get; set; }
        public List<CyberpunkRED_Generator.GearRowItem> GearItems { get; set; } = new List<CyberpunkRED_Generator.GearRowItem>();
        public List<CyberwareDef> CustomCyberwareList { get; set; } = new List<CyberwareDef>();
        public List<CyberpunkRED_Generator.CyberwareBlockItem> CyberwareBlocks { get; set; } = new List<CyberpunkRED_Generator.CyberwareBlockItem>();
        public string CriticalInjuries { get; set; }
        public string Addictions { get; set; }

        public List<string> CriticalInjuriesList { get; set; } = new List<string>();
    }
    public class EnemyData
    {
        public string Who { get; set; }
        public string Cause { get; set; }
        public string Hate { get; set; }
        public string Action { get; set; }
    }

    public class SkillSaveData
    {
        public string Name { get; set; }
        public int Level { get; set; }
        public int Total { get; set; }
    }
    public class SheetSkillCategory
    {
        public string Name { get; set; }
        public ObservableCollection<SheetSkill> Skills { get; set; }
    }
    public class SkillCategory
    {
        public string CategoryName { get; set; }
        public ObservableCollection<SkillRow> Skills { get; set; }
    }
}
