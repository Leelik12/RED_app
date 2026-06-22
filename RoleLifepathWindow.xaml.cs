using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberpunkRED_Generator
{
    public partial class RoleLifepathWindow : Window
    {
        private Random _rnd = new Random();
        private List<ComboBox> _dynamicComboBoxes = new List<ComboBox>();
        private bool _isLoaded = false;

        public RoleLifepathWindow()
        {
            InitializeComponent();

            // Подгружаем список ролей
            CbRole.ItemsSource = CoreDataBase.Roles;
            _isLoaded = true;
            CbRole.SelectedIndex = 0; // Спровоцирует CbRole_SelectionChanged
        }

        private void CbRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || CbRole.SelectedItem == null) return;

            string selectedRole = CbRole.SelectedItem.ToString();
            GenerateQuestions(selectedRole);
        }

        // Динамическая генерация вопросов под конкретную роль
        private void GenerateQuestions(string role)
        {
            PanelQuestions.Children.Clear();
            _dynamicComboBoxes.Clear();

            if (!CoreDataBase.RoleLifepaths.ContainsKey(role)) return;

            var tables = CoreDataBase.RoleLifepaths[role];

            foreach (var table in tables)
            {
                // Создаем заголовок вопроса
                TextBlock tb = new TextBlock
                {
                    Text = table.Title,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#999999")),
                    Margin = new Thickness(0, 0, 0, 5)
                };

                // Создаем выпадающий список вариантов
                ComboBox cb = new ComboBox
                {
                    ItemsSource = table.Options,
                    SelectedIndex = 0,
                    Tag = table.Title // Сохраняем вопрос в Tag, чтобы знать к чему относится ответ
                };

                // Подписываем на обновление лога
                cb.SelectionChanged += (s, e) => UpdateLog();

                _dynamicComboBoxes.Add(cb);

                PanelQuestions.Children.Add(tb);
                PanelQuestions.Children.Add(cb);
            }

            UpdateLog();
        }

        private void BtnRandomize_Click(object sender, RoutedEventArgs e)
        {
            // Кидаем случайный кубик для каждого сгенерированного комбобокса
            foreach (var cb in _dynamicComboBoxes)
            {
                if (cb.Items.Count > 0)
                {
                    cb.SelectedIndex = _rnd.Next(0, cb.Items.Count);
                }
            }
        }

        private void UpdateLog()
        {
            if (TxtTerminalLog == null || CbRole.SelectedItem == null) return;

            StringBuilder log = new StringBuilder();
            log.AppendLine($"> АНАЛИЗ КАРЬЕРЫ: {CbRole.SelectedItem.ToString().ToUpper()}");
            log.AppendLine();

            // Проходимся по всем динамическим ответам
            foreach (var cb in _dynamicComboBoxes)
            {
                if (cb.SelectedItem != null)
                {
                    log.AppendLine($"  - {cb.Tag}:");
                    log.AppendLine($"    [{cb.SelectedItem}]");
                    log.AppendLine();
                }
            }

            log.AppendLine("> СТАТУС: ПРОФИЛЬ УТВЕРЖДЕН.");
            TxtTerminalLog.Text = log.ToString();
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            RoleAndSkillsWindow skillsWindow = new RoleAndSkillsWindow();
            skillsWindow.Show();
            this.Close();
        }
    }
}