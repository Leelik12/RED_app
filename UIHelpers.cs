using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    public static class UIHelpers
    {
        // имя для файла: недопустимые символы (\ / : * ? " < > |) заменяются на «_»
        public static string SafeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Без_имени";
            var bad = System.IO.Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (char c in name.Trim()) sb.Append(Array.IndexOf(bad, c) >= 0 ? '_' : c);
            string res = sb.ToString().TrimEnd('.', ' ');
            return res.Length == 0 ? "Без_имени" : res;
        }

        public static string EnemyFilePath(string name)
            => System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Enemies", $"Enemy_{SafeFileName(name).Replace(" ", "_")}.json");

        public static int ParseIntOr(string text, int fallback) => int.TryParse(text, out int v) ? v : fallback;

        public static void NumberValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]+$");
        }
        public static void NumberValidationTextBoxWithMinus(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[-0-9]+$");
        }
        public static void NumberLimit777_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && int.TryParse(tb.Text, out int val))
            {
                if (val > 777)
                {
                    tb.Text = "777";
                    tb.SelectionStart = tb.Text.Length;
                }
            }
        }
        public static void NumberLimitFromMinus777To777_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && int.TryParse(tb.Text, out int val))
            {
                if (val > 777)
                {
                    tb.Text = "777";
                    tb.SelectionStart = tb.Text.Length;
                }
                else if (val < -777)
                {
                    tb.Text = "-777";
                    tb.SelectionStart = tb.Text.Length;
                }
            }
        }
        public static void NumberLimitTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && int.TryParse(tb.Text, out int val))
            {
                if (val > 777)
                {
                    tb.Text = "777";
                    tb.SelectionStart = tb.Text.Length;
                }
            }
        }
    }
}
