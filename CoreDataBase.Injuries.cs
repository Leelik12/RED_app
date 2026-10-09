using System.Collections.Generic;

namespace CyberpunkRED_Generator
{
    public static partial class CoreDataBase
    {

        //СПАРШЕНО!!!!!!!!!!!!
        public static readonly List<CriticalInjuryDef> CriticalInjuries = new List<CriticalInjuryDef>
        {
            new CriticalInjuryDef { Name = "Оторванная Рука", Description = "Лишился руки. Всё, что было в этой руке, теперь на земле.", EffectText = "Штраф Спасброска увеличивается на 1.", QuickFix = "Нет", Treatment = "Хирургия (17)", AllActionsPenalty = 0, MovePenalty = 0, DeathSavePenalty = 1},
            new CriticalInjuryDef { Name = "Оторванное Запястье", Description = "Лишился запястья. Всё, было в этой руке, теперь на земле.", EffectText = "Штраф СпасБросока увеличивается на 1.", QuickFix = "Нет", Treatment = "Хирургия (17)", AllActionsPenalty = 0, MovePenalty = 0, DeathSavePenalty = 1},
            new CriticalInjuryDef { Name = "Разрыв легкого", Description = "-2 к СКО (Минимум 1)", EffectText = "Штраф Спасброска увеличивается на 1.", QuickFix = "Парамедик (15)", Treatment = "Хирургия (15)", AllActionsPenalty = 0, MovePenalty = 2, DeathSavePenalty = 1},
            new CriticalInjuryDef { Name = "Перелом рёбер", Description = "Сломано несколько ребер.", EffectText = "В конце каждого хода, в котором ты двигался пешком более 4 метров, ты получаешь Бонусный Урон в ПЗ от этого повреждения.", QuickFix = "Парамедик (13)", Treatment = "Парамедик (15) или Хирургия (13)", AllActionsPenalty = 0, MovePenalty = 0 , DeathSavePenalty = 0},
            new CriticalInjuryDef { Name = "Перелом Руки", Description = "Кость руки сломана.", EffectText = "Сломаной рукой нельзя пользоваться. Всё, было в этой руке, теперь на земле.", QuickFix = "Парамедик (13)", Treatment = "Парамедик (15) или Хирургия (13)", AllActionsPenalty = 0, MovePenalty = 0 , DeathSavePenalty = 0},
            new CriticalInjuryDef { Name = "Инородное тело (Тело)", Description = "Что-то застряло в вашем теле.", EffectText = "В конце каждого хода, в котором ты двигался пешком более 4 метров, ты получаешь Бонусный Урон в ПЗ от этого повреждения.", QuickFix = "Первая помощь (13) или Парамедик (13)", Treatment = "Первая Помощь устраняет Эффект от повреждения", AllActionsPenalty = 0, MovePenalty = 0, DeathSavePenalty = 0},
            new CriticalInjuryDef { Name = "Перелом Ноги", Description = "Кость ноги сломана.", EffectText = "-4 к СКО (Минимум 1)", QuickFix = "Парамедик (13)", Treatment = "Парамедик (15) или Хирургия (13)", AllActionsPenalty = 0, MovePenalty = 4, DeathSavePenalty = 0},
            new CriticalInjuryDef { Name = "Разрыв Мышцы", Description = "Мышца серьезно порвана.", EffectText = "-2 к Ближним Атакам", QuickFix = "Первая помощь (13) или Парамедик (13)", Treatment = "Первая Помощь устраняет Эффект от повреждения", AllActionsPenalty = 0, MovePenalty = 0, DeathSavePenalty = 0,
                SkillModifiers = new List<SkillModifierDef> { new SkillModifierDef { SkillName = "Рукопашный бой", Value = -2, ModType = "Normal" },
                                                              new SkillModifierDef { SkillName = "Боевые искусства (x2)", Value = -2, ModType = "Normal" },
                                                              new SkillModifierDef { SkillName = "Оружие ближнего боя", Value = -2, ModType = "Normal" } } },
            new CriticalInjuryDef { Name = "Травма позвоночника", Description = "Поврежден спинной мозг.", EffectText = "В свой следующий ход ты можешь только Перемещаться. Штраф Спасброска увеличивается на 1.", QuickFix = "Парамедик (15)", Treatment = "Хирургия (15)", AllActionsPenalty = 0, MovePenalty = 0 , DeathSavePenalty = 1},
            new CriticalInjuryDef { Name = "Раздробленные Пальцы", Description = "Пальцы раздроблены.", EffectText = "-4 ко всем Действиям связанным с этой рукой.", QuickFix = "Парамедик (13)", Treatment = "Хирургия (15)", AllActionsPenalty = 0, MovePenalty = 0, DeathSavePenalty = 0},
            new CriticalInjuryDef { Name = "Оторванная Нога", Description = "Лишился ноги.", EffectText = "-6 к СКО (Минимум 1). Ты не можешь Уклоняться от атак. Штраф Спасброска увеличивается на 1.", QuickFix = "Нет", Treatment = "Хирургия (17)", AllActionsPenalty = 0, MovePenalty = 6, DeathSavePenalty = 1},

            new CriticalInjuryDef { Name = "Потеря Глаза", Description = "Лишился глаза.", EffectText = "-4 к дальним атакам и Проверкам Внимательности, использующим зрение. Штраф Спасброска увеличивается на 1.", QuickFix = "Нет", Treatment = "Хирургия (17)", AllActionsPenalty = 0, MovePenalty = 0 , DeathSavePenalty = 1,
                SkillModifiers = new List<SkillModifierDef> { new SkillModifierDef { SkillName = "Внимательность", Value = -4, ModType = "Visual" },
                                                              new SkillModifierDef { SkillName = "Автоматический огонь (x2)", Value = -4, ModType = "Normal" },
                                                              new SkillModifierDef { SkillName = "Оружие кр. калибра (x2)", Value = -4, ModType = "Normal" },
                                                              new SkillModifierDef { SkillName = "Пистолеты", Value = -4, ModType = "Normal" },
                                                              new SkillModifierDef { SkillName = "Стрельба из лука", Value = -4, ModType = "Normal" },
                                                              new SkillModifierDef { SkillName = "Тактическое оружие", Value = -4, ModType = "Normal" } } },
            new CriticalInjuryDef { Name = "Черепно-Мозговая Травма", Description = "Тяжелая черепно-мозговая травма.", EffectText = "-2 ко всем Действиям. Штраф Спасброска увеличивается на 1.", QuickFix = "Нет", Treatment = "Хирургия (17)", AllActionsPenalty = 2, MovePenalty = 0, DeathSavePenalty = 1},
            new CriticalInjuryDef { Name = "Повреждение Глаза", Description = "Глаз поврежден.", EffectText = "-2 к дальним атакам и проверкам Внимательности, использующим зрение.", QuickFix = "Парамедик (15)", Treatment = "Хирургия (13)", AllActionsPenalty = 0, MovePenalty = 0, DeathSavePenalty = 0,
                SkillModifiers = new List<SkillModifierDef> { new SkillModifierDef { SkillName = "Внимательность", Value = -2, ModType = "Visual" },
                                                              new SkillModifierDef { SkillName = "Автоматический огонь (x2)", Value = -2, ModType = "Normal" },
                                                              new SkillModifierDef { SkillName = "Оружие кр. калибра (x2)", Value = -2, ModType = "Normal" },
                                                              new SkillModifierDef { SkillName = "Пистолеты", Value = -2, ModType = "Normal" },
                                                              new SkillModifierDef { SkillName = "Стрельба из лука", Value = -2, ModType = "Normal" },
                                                              new SkillModifierDef { SkillName = "Тактическое оружие", Value = -2, ModType = "Normal" } } },
            new CriticalInjuryDef { Name = "Сотрясение Мозга", Description = "Сильный удар по голове.", EffectText = "-2 ко всем Действиям.", QuickFix = "Первая помощь (13) или Парамедик (13)", Treatment = "Первая Помощь устраняет Эффект от повреждения", AllActionsPenalty = 2, MovePenalty = 0, DeathSavePenalty = 0},
            new CriticalInjuryDef { Name = "Перелом челюсти", Description = "Челюсть сломана.", EffectText = "-4 ко всем Действиям связанным с речью", QuickFix = "Парамедик (13)", Treatment = "Парамедик (13) или Хирургия (13)", AllActionsPenalty = 0, MovePenalty = 0, DeathSavePenalty = 0},
            new CriticalInjuryDef { Name = "Инородное тело (Голова)", Description = "Что-то застряло в голове.", EffectText = "В конце каждого хода, в котором ты двигался пешком более 4 метров, ты получаешь Бонусный Урон в ПЗ от этого повреждения.", QuickFix = "Первая Помощь (13) или Парамедик (13)", Treatment = "Первая Помощь устраняет Эффект от повреждения", AllActionsPenalty = 0, MovePenalty = 0, DeathSavePenalty = 0},
            new CriticalInjuryDef { Name = "Травма Шеи", Description = "Травма мышц шеи.", EffectText = "Штраф Спасброска увеличивается на 1.", QuickFix = "Парамедик (13)", Treatment = "Парамедик (13) или Хирургия (13)", AllActionsPenalty = 0, MovePenalty = 0, DeathSavePenalty = 1 },
            new CriticalInjuryDef { Name = "Перелом Черепа", Description = "Перелом Черепа", EffectText = "Прицельные выстрелы в голову умножают урон, который проходит через твои ОС, на 3 вместо 2. Штраф Спасброска увеличивается на 1.", QuickFix = "Парамедик (15)", Treatment = "Парамедик (15) или Хирургия (15)", AllActionsPenalty = 0, MovePenalty = 0 , DeathSavePenalty = 1},
            new CriticalInjuryDef { Name = "Повреждение слуха", Description = "Что-то со слухом", EffectText = "Каждый раз, когда ты в свой ход пешком передвигался дальше 4 метров, в свой следующий ход ты не можешь перемещаться. Также ты получаешь -2 к проверкам Восприятия, использующим слух.", QuickFix = "Парамедик (13)", Treatment = "Хирургия (13)", AllActionsPenalty = 0, MovePenalty = 0, DeathSavePenalty = 0,
                SkillModifiers = new List<SkillModifierDef> { new SkillModifierDef { SkillName = "Внимательность", Value = -2, ModType = "Audio" } } },            new CriticalInjuryDef { Name = "Травма трахеи", Description = "Ты не можешь говорить.", EffectText = "Штраф Спасброска увеличивается на 1.", QuickFix = "Нет", Treatment = "Хирургия (15)", AllActionsPenalty = 0, MovePenalty = 0 , DeathSavePenalty = 1},
            new CriticalInjuryDef { Name = "Оторванное Ухо", Description = "Лишился Уха.", EffectText = "Каждый раз, когда ты в свой ход пешком передвигался дальше 4 метров, в свой следующий ход ты не можешь передвигался. Также ты получаешь -4 к проверкам Внимательности, использующим слух. Штраф Спасброска увеличивается на 1.", QuickFix = "Нет", Treatment = "Хирургия (17)", AllActionsPenalty = 0, MovePenalty = 0 , DeathSavePenalty = 1,
                SkillModifiers = new List<SkillModifierDef> { new SkillModifierDef { SkillName = "Внимательность", Value = -4, ModType = "Audio" } } },
        };
    }
}
