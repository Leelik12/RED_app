using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    // Класс-обертка для одного навыка с привязками интерфейса
    public class SkillRow : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string StatName { get; set; }
        public bool IsBasic { get; set; }
        public bool IsX2 { get; set; }

        private int _baseStatValue = 5; // Заглушка. Сюда будут передаваться статы из Этапа 1

        private int _level;
        public int Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(); OnPropertyChanged(nameof(Total)); }
        }

        // Сумма считается только из Стата и Уровня навыка
        public int Total => _baseStatValue + Level;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // Класс-обертка для группы навыков
    public class SkillCategory
    {
        public string CategoryName { get; set; }
        public List<SkillRow> Skills { get; set; }
    }

    public partial class RoleAndSkillsWindow : Window
    {
        private List<SkillCategory> _categories;
        private const int MAX_POINTS = 86;

        public RoleAndSkillsWindow()
        {
            InitializeComponent();
            LoadSkills();
        }

        private void LoadSkills()
        {
            _categories = new List<SkillCategory>();

            var grouped = CoreDataBase.AllSkills.GroupBy(s => s.Category);

            foreach (var group in grouped)
            {
                var cat = new SkillCategory
                {
                    CategoryName = group.Key.ToUpper(),
                    Skills = new List<SkillRow>()
                };

                foreach (var def in group)
                {
                    var skill = new SkillRow
                    {
                        Name = def.Name,
                        StatName = def.Stat,
                        IsBasic = def.IsBasic,
                        IsX2 = def.IsX2,
                        Level = def.IsBasic ? 2 : 0
                    };

                    cat.Skills.Add(skill);
                }
                _categories.Add(cat);
            }

            IcCategories.ItemsSource = _categories;
            UpdatePointsCounter();
        }

        private void BtnMinus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is SkillRow skill)
            {
                int minLevel = skill.IsBasic ? 2 : 0;
                if (skill.Level > minLevel)
                {
                    skill.Level--;
                    UpdatePointsCounter();
                }
            }
        }

        private void BtnPlus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is SkillRow skill)
            {
                if (skill.Level < 6)
                {
                    int cost = skill.IsX2 ? 2 : 1;
                    if (GetSpentPoints() + cost <= MAX_POINTS)
                    {
                        skill.Level++;
                        UpdatePointsCounter();
                    }
                }
            }
        }

        private int GetSpentPoints()
        {
            int spent = 0;
            foreach (var cat in _categories)
            {
                foreach (var skill in cat.Skills)
                {
                    spent += skill.Level * (skill.IsX2 ? 2 : 1);
                }
            }
            return spent;
        }

        private void UpdatePointsCounter()
        {
            int spent = GetSpentPoints();
            int left = MAX_POINTS - spent;

            if (TxtPoints != null)
                TxtPoints.Text = left.ToString();
        }
    }
}