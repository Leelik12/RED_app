
using System.Collections.Generic;

namespace CyberpunkGen
{
    public class SkillModifierDef
    {
        public string SkillName { get; set; } // Какому навыку даем бонус (или "Все")
        public int Value { get; set; }        // Значение (+1, -4 и т.д.)
        public string ModType { get; set; } = "Normal"; // "Normal", "Visual", "Audio"
    }
}
