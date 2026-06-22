using System;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    public partial class LifepathWindow : Window
    {
        private Random _rnd = new Random();

        public LifepathWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            CbOrigin.ItemsSource = CoreDataBase.Origins;
            CbPersonality.ItemsSource = CoreDataBase.Personalities;
            CbClothing.ItemsSource = CoreDataBase.Clothing;
            CbHair.ItemsSource = CoreDataBase.Hair;
            CbValue.ItemsSource = CoreDataBase.Values;
            CbPeople.ItemsSource = CoreDataBase.People;
            CbPerson.ItemsSource = CoreDataBase.Persons;
            CbPossession.ItemsSource = CoreDataBase.Possessions;
            CbFamilyBackground.ItemsSource = CoreDataBase.FamilyBackground;
            CbChildhood.ItemsSource = CoreDataBase.ChildhoodEnvironment;
            CbParents.ItemsSource = CoreDataBase.Parents;
            CbFamilyCrisis.ItemsSource = CoreDataBase.FamilyCrisis;
            CbLifeGoal.ItemsSource = CoreDataBase.LifeGoals;

            // Выбор первого пункта по умолчанию
            CbOrigin.SelectedIndex = 0; CbPersonality.SelectedIndex = 0;
            CbClothing.SelectedIndex = 0; CbHair.SelectedIndex = 0;
            CbValue.SelectedIndex = 0; CbPeople.SelectedIndex = 0;
            CbPerson.SelectedIndex = 0; CbPossession.SelectedIndex = 0;
            CbFamilyBackground.SelectedIndex = 0; CbChildhood.SelectedIndex = 0;
            CbParents.SelectedIndex = 0; CbFamilyCrisis.SelectedIndex = 0;
            CbLifeGoal.SelectedIndex = 0;
        }

        private void BtnRandomize_Click(object sender, RoutedEventArgs e)
        {
            CbOrigin.SelectedIndex = _rnd.Next(0, 10);
            CbPersonality.SelectedIndex = _rnd.Next(0, 10);
            CbClothing.SelectedIndex = _rnd.Next(0, 10);
            CbHair.SelectedIndex = _rnd.Next(0, 10);
            CbValue.SelectedIndex = _rnd.Next(0, 10);
            CbPeople.SelectedIndex = _rnd.Next(0, 10);
            CbPerson.SelectedIndex = _rnd.Next(0, 10);
            CbPossession.SelectedIndex = _rnd.Next(0, 10);
            CbFamilyBackground.SelectedIndex = _rnd.Next(0, 10);
            CbChildhood.SelectedIndex = _rnd.Next(0, 10);
            CbParents.SelectedIndex = _rnd.Next(0, 10);
            CbFamilyCrisis.SelectedIndex = _rnd.Next(0, 10);
            CbLifeGoal.SelectedIndex = _rnd.Next(0, 10);
        }

        private void UpdateLog(object sender, SelectionChangedEventArgs e)
        {
            // Проверка, чтобы лог не падал при инициализации окна
            if (TxtTerminalLog == null || CbOrigin.SelectedItem == null || CbLifeGoal.SelectedItem == null || CbFamilyBackground.SelectedIndex < 0) return;

            // Берем описание из второго массива по тому же индексу
            string familyDesc = CoreDataBase.FamilyBackgroundDesc[CbFamilyBackground.SelectedIndex];

            TxtTerminalLog.Text = $"> АНАЛИЗ БИОМЕТРИИ И ИСТОРИИ...\n\n" +
                                  $"> ПРОИСХОЖДЕНИЕ И СЕМЬЯ:\n" +
                                  $"  - Корни: {CbOrigin.SelectedItem}\n" +
                                  $"  - Семья: {CbFamilyBackground.SelectedItem}\n" +
                                  $"  - Описание: {familyDesc}\n" +
                                  $"  - Родители: {CbParents.SelectedItem}\n" +
                                  $"  - Детство: {CbChildhood.SelectedItem}\n" +
                                  $"  - Трагедия: {CbFamilyCrisis.SelectedItem}\n\n" +
                                  $"> ХАРАКТЕР: {CbPersonality.SelectedItem}\n\n" +
                                  $"> ВНЕШНИЙ ВИД:\n" +
                                  $"  - Стиль одежды: {CbClothing.SelectedItem}\n" +
                                  $"  - Прическа: {CbHair.SelectedItem}\n\n" +
                                  $"> ПСИХОЛОГИЧЕСКИЙ ПРОФИЛЬ:\n" +
                                  $"  - Отношение к людям: {CbPeople.SelectedItem}\n" +
                                  $"  - Ценность: {CbValue.SelectedItem}\n" +
                                  $"  - Важный человек: {CbPerson.SelectedItem}\n" +
                                  $"  - Важная вещь: {CbPossession.SelectedItem}\n\n" +
                                  $"> ЦЕЛЬ В ЖИЗНИ: {CbLifeGoal.SelectedItem}\n\n" +
                                  $"> СТАТУС: ФОРМИРОВАНИЕ ЗАВЕРШЕНО.";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LifepathRelationsWindow pathWindow = new LifepathRelationsWindow();
            pathWindow.Show();
            this.Close();
        }
    }
}