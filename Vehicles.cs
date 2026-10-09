using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CyberpunkRED_Generator
{
    // каталог транспорта: типы из корника (с. 190–191) и модели из DLC (kind = vehicle в Data/items.json)
    public class VehicleDef
    {
        public string Name;
        public string Kind;          // Наземный / Морской / Воздушный
        public string Description;
        public int Sdp;              // ПСП (пункты структурных повреждений)
        public int Seats;
        public int Combat;           // боевая скорость (СКО)
        public string Narrative;     // повествовательная скорость
        public int Cost;
        public string Source = Packs.CoreName;
        public bool Nomad;           // модель из DLC с пометкой кочевников
        public override string ToString() => Name;
    }

    // транспорт персонажа: хранится в файле листа
    public class OwnedVehicle : INotifyPropertyChanged
    {
        private string _name, _kind, _notes, _mods;
        private int _sdp, _maxSdp, _seats, _combat;

        public string Name { get => _name; set { _name = value; Raise(); } }
        public string Kind { get => _kind; set { _kind = value; Raise(); } }
        public int MaxSdp { get => _maxSdp; set { _maxSdp = value; Raise(); Raise(nameof(State)); } }
        public int Sdp { get => _sdp; set { _sdp = Math.Max(0, value); Raise(); Raise(nameof(State)); } }
        public int Seats { get => _seats; set { _seats = value; Raise(); } }
        public int Combat { get => _combat; set { _combat = value; Raise(); } }
        public string Mods { get => _mods; set { _mods = value; Raise(); } }
        public string Notes { get => _notes; set { _notes = value; Raise(); } }

        [System.Text.Json.Serialization.JsonIgnore]
        public string State => Sdp <= 0 ? "уничтожен" : Chase.Damaged(Sdp, MaxSdp) ? "повреждён (меньше половины ПСП)" : "исправен";

        public event PropertyChangedEventHandler PropertyChanged;
        private void Raise([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public static class Vehicles
    {
        public const string KindGround = "Наземный", KindSea = "Морской", KindAir = "Воздушный";

        private static VehicleDef V(string name, string kind, string desc, int sdp, int seats, int combat, string narr, int cost)
            => new VehicleDef { Name = name, Kind = kind, Description = desc, Sdp = sdp, Seats = seats, Combat = combat, Narrative = narr, Cost = cost };

        // таблицы «Наземный/Морской/Воздушный транспорт» корника (RU и EN совпадают по числам)
        public static readonly List<VehicleDef> Core = new List<VehicleDef>
        {
            V("Дорожный мотоцикл", KindGround, "Обычный мотоцикл на CHOOH².", 35, 2, 20, "161 км/ч", 20000),
            V("Спортивный мотоцикл", KindGround, "Экзотический уличный мотоцикл, способный развивать экстремальные скорости.", 35, 2, 60, "483 км/ч", 100000),
            V("Автомобиль", KindGround, "Обычный автомобиль на CHOOH².", 50, 4, 20, "161 км/ч", 30000),
            V("Спорткар", KindGround, "Спортивный автомобиль на CHOOH².", 50, 4, 40, "322 км/ч", 50000),
            V("Суперкар", KindGround, "Экзотический спорткар, способный развивать экстремальные скорости.", 50, 2, 60, "483 км/ч", 100000),
            V("Гидроцикл", KindSea, "Обычное персональное водное судно.", 35, 2, 20, "97 км/ч", 20000),
            V("Скоростной катер", KindSea, "Скоростной катер.", 50, 4, 20, "97 км/ч", 30000),
            V("Круизный катер", KindSea, "Роскошное судно с жилыми помещениями; цена за комнату под палубой (минимум две).", 60, 2, 10, "24 км/ч", 30000),
            V("Яхта", KindSea, "Роскошное прогулочное судно; цена за комнату (минимум четыре), 4 места в комнате.", 100, 4, 10, "24 км/ч", 50000),
            V("Гирокоптер", KindAir, "Малый винтокрылый летательный аппарат, популярный среди любителей полётов.", 35, 2, 20, "161 км/ч", 20000),
            V("Вертолёт", KindAir, "Полнофункциональный вертолёт, способный к продолжительному полёту.", 60, 4, 40, "322 км/ч", 40000),
            V("Многоцелевой аэродайн AV-4", KindAir, "Высокотехнологичный летательный аппарат с вертикальной тягой.", 100, 6, 40, "322 км/ч", 50000),
            V("Супер-аэродайн AV-9", KindAir, "Экзотический аппарат с вертикальной тягой, способный развивать экстремальные скорости.", 60, 2, 60, "483 км/ч", 100000),
            V("Аэрозеп", KindAir, "Современный грузовой дирижабль; цена за комнату под палубой (минимум две), 2 места в комнате.", 100, 2, 20, "161 км/ч", 30000),
        };

        // весь каталог: корник + включённые DLC
        public static List<VehicleDef> All()
        {
            var res = new List<VehicleDef>(Core);
            foreach (var i in Packs.Enabled("vehicle"))
                res.Add(new VehicleDef
                {
                    Name = i.Name,
                    Kind = GuessKind(i),
                    Description = i.Info,
                    Sdp = ToInt(i.Get("hp")),
                    Seats = ToInt(i.Get("seats")),
                    Combat = ToInt(i.Get("combat")),
                    Narrative = i.Get("narrative"),
                    Cost = i.Cost,
                    Source = Packs.PackName(i.Pack),
                    Nomad = ToInt(i.Get("nomad")) > 0
                });
            return res;
        }

        private static string GuessKind(PackItem i)
        {
            string k = i.Get("kind");
            return string.IsNullOrEmpty(k) ? KindGround : k;
        }

        private static int ToInt(string s) => int.TryParse(s, out int v) ? v : 0;

        public static OwnedVehicle Own(VehicleDef d) => new OwnedVehicle
        {
            Name = d.Name, Kind = d.Kind, MaxSdp = d.Sdp, Sdp = d.Sdp, Seats = d.Seats, Combat = d.Combat
        };
    }
}
