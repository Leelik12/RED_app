using System;
using System.Windows.Data;

namespace CyberpunkRED_Generator
{
    // шкала с нулевым максимумом (нет брони) показывается пустой, а не полной
    public class AtLeastOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => value is int i ? (double)Math.Max(1, i) : 1d;

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => Binding.DoNothing;
    }

    // значок категории навыков для заголовков детального стиля; названия узнаются на русском и английском
    public class CategoryIconConverter : IValueConverter
    {
        private static readonly string[][] Icons =
        {
            new[] { "восприят", "perception", "👁" }, new[] { "физич", "physical", "💪" }, new[] { "управлен", "control", "🚗" },
            new[] { "образоват", "education", "📚" }, new[] { "социал", "social", "💬" }, new[] { "техническ", "technique", "🔧" },
            new[] { "ближн", "fighting", "🗡" }, new[] { "дальн", "ranged", "🔫" }, new[] { "сценич", "performance", "🎭" },
        };

        public static string For(string name)
        {
            string n = (name ?? "").ToLowerInvariant();
            foreach (var i in Icons) if (n.Contains(i[0]) || n.Contains(i[1])) return i[2];
            return "";
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => For(value as string);

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => Binding.DoNothing;
    }
}
