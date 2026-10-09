using System;
using System.Collections.Generic;
using System.Linq;

namespace CyberpunkRED_Generator
{
    // состояние агента персонажа: модель и установленные приложения (включается галочкой в настройках стола)
    public class AgentState
    {
        public string Model { get; set; }
        public List<string> Apps { get; set; } = new List<string>();
    }

    public class AgentCommand
    {
        public string Name, Text, Time;
        public int Seconds;
    }

    // DLC «Все об агентах»: взлом агентов (СЛ по качеству), внедрение команд, бонусы приложений и мелкие броски
    public static class Agents
    {
        public const string Bad = "bad", Standard = "std", Good = "good";
        public const string SearchApp = "Больше САИИ";
        public const int RangeMeters = 20;

        public static string QualityName(string q) => q == Bad ? "Плохое качество" : q == Good ? "Отличное качество" : "Стандартное качество";

        // проверка Электроники/Безопасности: удалённо через Взломщик и при физическом доступе
        public static int HackDc(string quality, bool physical)
        {
            if (quality == Bad) return physical ? 15 : 17;
            if (quality == Good) return physical ? 21 : 24;
            return physical ? 17 : 21;
        }

        public static readonly AgentCommand[] Commands =
        {
            new AgentCommand { Name = "Базовая функция", Seconds = 3, Time = "3 секунды (1 раунд)",
                Text = "Подать сигнал уведомления, включить фонарик, отключить звук, выключить агент." },
            new AgentCommand { Name = "Основная функция", Seconds = 60, Time = "1 минута (20 раундов)",
                Text = "Запустить приложение, совершить звонок, загрузить или выгрузить файл." },
            new AgentCommand { Name = "Защитная функция", Seconds = 300, Time = "5 минут (100 раундов)",
                Text = "Сменить учётные данные и заблокировать пользователя; на 24 часа передавать хакеру местоположение или данные о звонках и CitiNet; поставить чёрный ход — автоуспех любых взломов этого агента на 24 часа." },
        };

        public const string HackRules =
            "Взлом требует Взломщика (500eb): агент цели в пределах 20 м в прямой видимости (в кармане или сумке можно). Только внешние агенты — внутренние и одноразовые телефоны не взламываются Взломщиком.\n" +
            "Проверка Электроника/Безопасность занимает 1 минуту. Если хакер добыл данные о защите другим путём (социнженерия), +2. Провал: САИИ цели замечает вторжение и предупреждает владельца через 30 секунд.\n" +
            "Свисток для обработки (100eb) на кибердеке добавляет ранг Интерфейса Нетраннера к проверке. Физический доступ: Взломщик не нужен, но СЛ ниже.\n" +
            "За один взлом внедряется одна команда без проверки; для следующей взломайте агента заново. Команды живут не дольше 24 часов.\n" +
            "Обнаружение: владелец (или кто он разрешит) делает Электроника/Безопасность против СЛ первоначальной проверки. Перезагрузка — 5 минут, отключает Взломщик и убирает внедрённые команды. Личность и место хакера не раскрываются.";

        public const string GmNotes =
            "Агент без облачной копии: все настройки, файлы и изученное поведение живут только в железе — потерять агент значит потерять всё. " +
            "Связи нет в экранированных зданиях, подземельях и Боевой зоне (см. «Погода в Найт-Сити»). САИИ может предостерегать игроков по их собственным привычкам.";

        public static string QualityOf(string model)
        {
            var it = Packs.Enabled().FirstOrDefault(i => i.Name == model && (i.Get("type") == "agent" || i.Get("type") == "internal"));
            return it?.Get("quality") ?? Standard;
        }

        public static List<PackItem> Models() => Packs.Enabled("gear").Where(i => i.Get("type") == "agent")
            .Concat(Packs.Enabled("cyber").Where(i => i.Get("type") == "internal")).ToList();

        public static List<PackItem> AppItems() => Packs.Enabled("gear").Where(i => i.Get("type") == "app").ToList();

        // бонус к Поиску информации: +2 от агента, +3 с «Больше САИИ»
        public static int SearchBonus(AgentState a)
        {
            if (a == null) return 0;
            return a.Apps.Contains(SearchApp) ? 3 : 2;
        }

        // «Гардероб и стиль» +1, пока надет Merlyn
        public static int WardrobeBonus(AgentState a) => a != null && a.Model != null && a.Model.Contains("Merlyn") ? 1 : 0;

        public static bool IsInternal(string model)
            => Packs.Enabled("cyber").Any(i => i.Name == model && i.Get("type") == "internal");

        // 1d10 HusTool: 1 — провал, 10 — неприятность, иначе 10eb
        public static string HusTool(int d10)
            => d10 <= 1 ? "Провал: клиента нет на месте, время потеряно, денег нет."
             : d10 >= 10 ? "Заказ выполнен (+10eb), но возникли неприятности: бросок по таблице «Встречи в Найт-Сити» (CP:R 417)."
             : "Заказ выполнен: +10eb.";

        // 1d10 Вдохновитель: чёт — терапия на день короче, нечёт — на день дольше
        public static int TherapyDays(int d10) => d10 % 2 == 0 ? -1 : 1;

        public static string Therapy(int d10)
            => d10 % 2 == 0 ? "Чётное: терапия завершается на день раньше (ясное понимание психики)." : "Нечётное: терапия требует ещё один день («понимание» мешает прогрессу).";

        public static bool HackSucceeds(int total, int dc) => total >= dc;
    }
}
