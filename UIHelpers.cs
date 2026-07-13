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
