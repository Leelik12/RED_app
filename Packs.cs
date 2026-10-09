using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace CyberpunkRED_Generator
{
    // Предмет из DLC (или корника), описанный данными: оружие, броня, снаряжение, киберимпланты, транспорт, жильё, NPC…
    // Файл Data/items.json вшит в exe; его собирают скрипты Rulebook/items/*.py.
    public class PackItem
    {
        public string Id { get; set; }
        public string Pack { get; set; }          // id пакета (dlc03, bc …)
        public string Kind { get; set; }          // weapon | armor | gear | drug | cyber | vehicle | property | …
        public string Name { get; set; }
        public string NameEn { get; set; }
        public string Group { get; set; }         // подкатегория («Взрывчатка», «Одежда»)
        public int Cost { get; set; }
        public string Info { get; set; }          // правила кратко
        public string InfoEn { get; set; }
        public string Lib { get; set; }           // id записи в Справочнике (полный текст)
        public int Page { get; set; }

        // оружие
        public string Skill { get; set; }
        public string Damage { get; set; }
        public int Mag { get; set; }
        public string AmmoKind { get; set; }
        public int Rof { get; set; }
        public string Hands { get; set; }
        public bool Concealable { get; set; }
        public string Alt { get; set; }
        public bool Melee { get; set; }
        public int Quality { get; set; }          // -1 низкое, 0 обычное, 1 отличное (у экзотики оно зашито в предмет)

        // броня
        public int SP { get; set; }
        public int Penalty { get; set; }
        public bool Shield { get; set; }
        public bool Suit { get; set; }

        // киберимпланты
        public string Category { get; set; }      // блок листа, куда ставится («ПРАВАЯ КИБЕРРУКА (CYBERARM R)»)
        public string Install { get; set; }       // Клиника / Госпиталь / Молл
        public string HL { get; set; }            // потеря человечности («2d6 (7)»)
        public int Slots { get; set; }
        public bool Foundation { get; set; }
        public bool Paired { get; set; }
        public string Requires { get; set; }

        // свободные поля (транспорт: скорость, СКО…; жильё: аренда и т. п.)
        public Dictionary<string, string> Extra { get; set; }

        public string Get(string key) => Extra != null && Extra.TryGetValue(key, out var v) ? v : null;
        public override string ToString() => Name;
    }

    public class PackInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string NameEn { get; set; }
        public string Kind { get; set; }          // core | dlc
    }

    public static class Packs
    {
        public const string ResourceName = "Data.items.json";
        public const string CoreName = "Корник";
        private static List<PackInfo> _packs;
        private static List<PackItem> _items;
        private static readonly object Lock = new object();
        private static readonly List<object> Injected = new List<object>();

        public static IReadOnlyList<PackInfo> All { get { Ensure(); return _packs; } }
        public static IReadOnlyList<PackItem> Items { get { Ensure(); return _items; } }

        private class Doc { public int Version { get; set; } public List<PackInfo> Packs { get; set; } public List<PackItem> Items { get; set; } }

        private static void Ensure()
        {
            if (_items != null) return;
            lock (Lock)
            {
                if (_items != null) return;
                var packs = new List<PackInfo>(); var items = new List<PackItem>();
                try
                {
                    using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
                        if (s != null) Parse(s, out packs, out items);
                }
                catch (Exception ex) { ErrorLog.Write("packs", ex); }
                _packs = packs; _items = items;
            }
        }

        public static void Parse(Stream s, out List<PackInfo> packs, out List<PackItem> items)
        {
            var d = JsonSerializer.Deserialize<Doc>(s, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            packs = d?.Packs ?? new List<PackInfo>();
            items = d?.Items ?? new List<PackItem>();
        }

        public static void SetForTests(List<PackInfo> packs, List<PackItem> items) { lock (Lock) { _packs = packs; _items = items; } }

        public static bool IsEnabled(string packId) => !AppSettings.Data.DisabledPacks.Contains(packId);

        public static string PackName(string packId) => All.FirstOrDefault(p => p.Id == packId)?.Name ?? packId;

        public static IEnumerable<PackItem> Enabled(string kind = null)
            => Items.Where(i => IsEnabled(i.Pack) && (kind == null || i.Kind == kind));

        // ---------- встраивание в каталоги ----------

        // добавляет включённые предметы в каталоги (оружие, броня, снаряжение, киберимпланты); можно вызывать повторно
        public static void Apply()
        {
            lock (Lock)
            {
                foreach (var o in Injected)
                {
                    if (o is WeaponDef w) GearCatalog.Weapons.Remove(w);
                    else if (o is ArmorDef a) GearCatalog.Armor.Remove(a);
                    else if (o is GearDef g) GearCatalog.Gear.Remove(g);
                    else if (o is CyberwareDef c) CoreDataBase.AllCyberware.Remove(c);
                    else if (o is NetItemDef n) { NetCatalog.Decks.Remove(n); NetCatalog.Hardware.Remove(n); }
                }
                Injected.Clear();
                foreach (var it in Enabled())
                {
                    string src = PackName(it.Pack);
                    switch (it.Kind)
                    {
                        case "weapon": { var d = ToWeapon(it, src); GearCatalog.Weapons.Add(d); Injected.Add(d); break; }
                        case "armor": { var d = ToArmor(it, src); GearCatalog.Armor.Add(d); Injected.Add(d); break; }
                        case "gear":
                        case "drug":
                        case "ammo":
                            { var d = ToGear(it, src); GearCatalog.Gear.Add(d); Injected.Add(d); break; }
                        case "deck": { var d = ToNetItem(it, src, "Кибердека"); NetCatalog.Decks.Add(d); Injected.Add(d); break; }
                        case "netware": { var d = ToNetItem(it, src, "Железо"); NetCatalog.Hardware.Add(d); Injected.Add(d); break; }
                        case "cyber":
                            foreach (var d in ToCyber(it, src)) { CoreDataBase.AllCyberware.Add(d); Injected.Add(d); }
                            break;
                    }
                }
            }
        }

        public static WeaponDef ToWeapon(PackItem i, string src) => new WeaponDef
        {
            Name = i.Name, Group = i.Melee ? "Холодное" : "Экзотическое", Skill = i.Skill ?? (i.Melee ? "Оружие ближнего боя" : ""), Damage = i.Damage ?? "",
            Mag = i.Mag, AmmoKind = i.AmmoKind, Rof = i.Rof, Hands = string.IsNullOrEmpty(i.Hands) ? "1" : i.Hands, Concealable = i.Concealable, Cost = i.Cost,
            Alt = i.Alt, Info = i.Info, FixedQuality = true, Source = src, Standard = i.Melee ? i.Name : null
        };

        public static ArmorDef ToArmor(PackItem i, string src) => new ArmorDef
        {
            Name = i.Name, SP = i.SP, Penalty = i.Penalty, Cost = i.Cost, IsShield = i.Shield, IsSuit = i.Suit, Info = i.Info, Source = src
        };

        public static NetItemDef ToNetItem(PackItem i, string src, string kind) => new NetItemDef
        {
            Kind = kind, Name = i.Name, Slots = i.Slots, Cost = i.Cost, Effect = i.Info, Source = src
        };

        public static GearDef ToGear(PackItem i, string src) => new GearDef
        {
            Name = i.Name, Group = string.IsNullOrEmpty(i.Group) ? (i.Kind == "drug" ? "Наркотики и препараты" : "Снаряжение") : i.Group,
            Cost = i.Cost, Info = i.Info, Source = src
        };

        // импланты, подходящие в несколько блоков (обе руки/глаза/ноги), добавляются по копии на блок
        public static IEnumerable<CyberwareDef> ToCyber(PackItem i, string src)
        {
            var cats = new List<string>();
            string cat = i.Category ?? "ВНУТРЕННИЕ ИМПЛАНТЫ (INTERNAL)";
            cats.Add(cat);
            if (cat.Contains("(CYBERARM R)")) cats.Add(cat.Replace("(CYBERARM R)", "(CYBERARM L)").Replace("ПРАВАЯ", "ЛЕВАЯ"));
            else if (cat.Contains("(CYBEROPTIC R)")) cats.Add(cat.Replace("(CYBEROPTIC R)", "(CYBEROPTIC L)").Replace("ПРАВЫЙ", "ЛЕВЫЙ"));
            else if (cat.Contains("(CYBERLEG R)")) cats.Add(cat.Replace("(CYBERLEG R)", "(CYBERLEG L)").Replace("ПРАВАЯ", "ЛЕВАЯ"));
            foreach (var c in cats)
                yield return new CyberwareDef
                {
                    Name = i.Name, Category = c, Slots = i.Slots, HumanityLoss = i.HL ?? "0", IsFoundation = i.Foundation, IsPaired = i.Paired,
                    Requires = i.Requires ?? "", Source = src,
                    Description = (string.IsNullOrEmpty(i.Install) ? "" : "Установка: " + i.Install + ". ") + (i.Cost > 0 ? "Цена: " + GearCatalog.CostText(i.Cost) + ". " : "") + (i.Info ?? "")
                };
        }
    }
}
