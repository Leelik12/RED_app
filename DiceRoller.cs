using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CyberpunkRED_Generator
{
    public struct DiceResult
    {
        public int Total;
        public string Detail;
    }

    // бросок кубиков по строке вида «2d6 (7)», «1d6/2 (2)», «1d10+2» или просто числа
    public static class DiceRoller
    {
        private static readonly Random Rng = new Random();
        private static readonly Regex Formula = new Regex(@"^\s*(\d+)\s*[dD]\s*(\d+)\s*(/\s*2)?\s*([+-]\s*\d+)?\s*$", RegexOptions.Compiled);

        public static int Die(int sides)
        {
            lock (Rng) return Rng.Next(1, Math.Max(2, sides + 1));
        }

        // проверка d10 по правилам: 10 — крит. успех (+ ещё 1d10), 1 — крит. провал (− ещё 1d10), без цепочек
        public static int D10Check(out string detail)
        {
            int d = Die(10);
            if (d == 10) { int e = Die(10); detail = $"10 +{e} крит. успех"; return 10 + e; }
            if (d == 1) { int e = Die(10); detail = $"1 −{e} крит. провал"; return 1 - e; }
            detail = d.ToString();
            return d;
        }

        // times > 1 — бросок повторяется и складывается (например, парный имплант)
        public static bool TryRoll(string text, int times, out DiceResult result)
        {
            result = new DiceResult();
            if (string.IsNullOrWhiteSpace(text) || times < 1) return false;

            // отбрасываем пояснение в скобках: «2d6 (7)» -> «2d6»
            int paren = text.IndexOf('(');
            string formula = (paren >= 0 ? text.Substring(0, paren) : text).Trim();

            var m = Formula.Match(formula);
            if (!m.Success)
            {
                if (!int.TryParse(formula, out int fixedValue)) return false;
                result.Total = fixedValue * times;
                result.Detail = times > 1 ? $"{fixedValue} × {times} = {result.Total}" : fixedValue.ToString();
                return true;
            }

            int count = int.Parse(m.Groups[1].Value);
            int sides = int.Parse(m.Groups[2].Value);
            bool half = m.Groups[3].Success;
            int bonus = m.Groups[4].Success ? int.Parse(m.Groups[4].Value.Replace(" ", "")) : 0;
            if (count < 1 || count > 100 || sides < 2) return false;

            int grand = 0;
            var parts = new List<string>();
            for (int t = 0; t < times; t++)
            {
                var dice = new List<int>();
                int sum = 0;
                for (int i = 0; i < count; i++) { int d = Die(sides); dice.Add(d); sum += d; }
                int value = sum;
                string text1 = $"{count}d{sides}: {string.Join(" + ", dice)}";
                if (half) { value = (sum + 1) / 2; text1 += $" = {sum}, /2 вверх = {value}"; }
                else if (count > 1) text1 += $" = {sum}";
                if (bonus != 0) { value += bonus; text1 += $" {(bonus > 0 ? "+" : "−")} {Math.Abs(bonus)} = {value}"; }
                grand += value;
                parts.Add(text1);
            }
            result.Total = grand;
            result.Detail = string.Join("\n", parts) + (times > 1 ? $"\nИтого: {grand}" : "");
            return true;
        }
    }
}
