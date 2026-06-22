using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CyberpunkRED_Generator
{
    public partial class LifepathRelationsWindow : Window
    {
        private Random _rnd = new Random();
        private bool _isLoaded = false;

        public LifepathRelationsWindow()
        {
            InitializeComponent();
            LoadData();
            _isLoaded = true;

            // По умолчанию выбираем 0 для всех (индекс 0)
            CbCountFriends.SelectedIndex = 0;
            CbCountEnemies.SelectedIndex = 0;
            CbCountLove.SelectedIndex = 0;
        }

        private void LoadData()
        {
            // Друзья
            CbF1.ItemsSource = CoreDataBase.Friends; CbF2.ItemsSource = CoreDataBase.Friends; CbF3.ItemsSource = CoreDataBase.Friends;

            // Враги
            CbE1Who.ItemsSource = CoreDataBase.EnemiesWho; CbE1Cause.ItemsSource = CoreDataBase.EnemiesCause; CbE1Hate.ItemsSource = CoreDataBase.EnemiesWhoHates; CbE1Action.ItemsSource = CoreDataBase.EnemiesAction;
            CbE2Who.ItemsSource = CoreDataBase.EnemiesWho; CbE2Cause.ItemsSource = CoreDataBase.EnemiesCause; CbE2Hate.ItemsSource = CoreDataBase.EnemiesWhoHates; CbE2Action.ItemsSource = CoreDataBase.EnemiesAction;
            CbE3Who.ItemsSource = CoreDataBase.EnemiesWho; CbE3Cause.ItemsSource = CoreDataBase.EnemiesCause; CbE3Hate.ItemsSource = CoreDataBase.EnemiesWhoHates; CbE3Action.ItemsSource = CoreDataBase.EnemiesAction;

            // Любовь
            CbL1.ItemsSource = CoreDataBase.TragicLove; CbL2.ItemsSource = CoreDataBase.TragicLove; CbL3.ItemsSource = CoreDataBase.TragicLove;

            // Проставляем дефолты
            CbF1.SelectedIndex = 0; CbF2.SelectedIndex = 0; CbF3.SelectedIndex = 0;
            CbE1Who.SelectedIndex = 0; CbE1Cause.SelectedIndex = 0; CbE1Hate.SelectedIndex = 0; CbE1Action.SelectedIndex = 0;
            CbE2Who.SelectedIndex = 0; CbE2Cause.SelectedIndex = 0; CbE2Hate.SelectedIndex = 0; CbE2Action.SelectedIndex = 0;
            CbE3Who.SelectedIndex = 0; CbE3Cause.SelectedIndex = 0; CbE3Hate.SelectedIndex = 0; CbE3Action.SelectedIndex = 0;
            CbL1.SelectedIndex = 0; CbL2.SelectedIndex = 0; CbL3.SelectedIndex = 0;
        }

        private void CountChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;

            int friends = CbCountFriends.SelectedIndex;
            PanelFriends.Visibility = friends > 0 ? Visibility.Visible : Visibility.Collapsed;
            SlotF1.Visibility = friends >= 1 ? Visibility.Visible : Visibility.Collapsed;
            SlotF2.Visibility = friends >= 2 ? Visibility.Visible : Visibility.Collapsed;
            SlotF3.Visibility = friends >= 3 ? Visibility.Visible : Visibility.Collapsed;

            int enemies = CbCountEnemies.SelectedIndex;
            PanelEnemies.Visibility = enemies > 0 ? Visibility.Visible : Visibility.Collapsed;
            SlotE1.Visibility = enemies >= 1 ? Visibility.Visible : Visibility.Collapsed;
            SlotE2.Visibility = enemies >= 2 ? Visibility.Visible : Visibility.Collapsed;
            SlotE3.Visibility = enemies >= 3 ? Visibility.Visible : Visibility.Collapsed;

            int love = CbCountLove.SelectedIndex;
            PanelLove.Visibility = love > 0 ? Visibility.Visible : Visibility.Collapsed;
            SlotL1.Visibility = love >= 1 ? Visibility.Visible : Visibility.Collapsed;
            SlotL2.Visibility = love >= 2 ? Visibility.Visible : Visibility.Collapsed;
            SlotL3.Visibility = love >= 3 ? Visibility.Visible : Visibility.Collapsed;

            UpdateLog(null, null);
        }

        private void BtnRandomize_Click(object sender, RoutedEventArgs e)
        {
            // Формула 1d10 - 7
            CbCountFriends.SelectedIndex = Math.Max(0, _rnd.Next(1, 11) - 7);
            CbCountEnemies.SelectedIndex = Math.Max(0, _rnd.Next(1, 11) - 7);
            CbCountLove.SelectedIndex = Math.Max(0, _rnd.Next(1, 11) - 7);

            // Рандомим внутренние поля на случай, если они открылись
            CbF1.SelectedIndex = _rnd.Next(0, 10); CbF2.SelectedIndex = _rnd.Next(0, 10); CbF3.SelectedIndex = _rnd.Next(0, 10);

            CbE1Who.SelectedIndex = _rnd.Next(0, 10); CbE1Cause.SelectedIndex = _rnd.Next(0, 10); CbE1Hate.SelectedIndex = _rnd.Next(0, 10); CbE1Action.SelectedIndex = _rnd.Next(0, 10);
            CbE2Who.SelectedIndex = _rnd.Next(0, 10); CbE2Cause.SelectedIndex = _rnd.Next(0, 10); CbE2Hate.SelectedIndex = _rnd.Next(0, 10); CbE2Action.SelectedIndex = _rnd.Next(0, 10);
            CbE3Who.SelectedIndex = _rnd.Next(0, 10); CbE3Cause.SelectedIndex = _rnd.Next(0, 10); CbE3Hate.SelectedIndex = _rnd.Next(0, 10); CbE3Action.SelectedIndex = _rnd.Next(0, 10);

            CbL1.SelectedIndex = _rnd.Next(0, 10); CbL2.SelectedIndex = _rnd.Next(0, 10); CbL3.SelectedIndex = _rnd.Next(0, 10);
        }

        private void UpdateLog(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded || TxtTerminalLog == null) return;

            StringBuilder log = new StringBuilder();
            log.AppendLine("> АНАЛИЗ СОЦИАЛЬНЫХ СВЯЗЕЙ...");
            log.AppendLine();

            int friends = CbCountFriends.SelectedIndex;
            log.AppendLine($"> ДРУЗЬЯ (Найдено: {friends}):");
            if (friends == 0) log.AppendLine("  - Отсутствуют.");
            if (friends >= 1) log.AppendLine($"  1. {CbF1.SelectedItem}");
            if (friends >= 2) log.AppendLine($"  2. {CbF2.SelectedItem}");
            if (friends >= 3) log.AppendLine($"  3. {CbF3.SelectedItem}");
            log.AppendLine();

            int enemies = CbCountEnemies.SelectedIndex;
            log.AppendLine($"> ВРАГИ (Найдено: {enemies}):");
            if (enemies == 0) log.AppendLine("  - Отсутствуют.");
            if (enemies >= 1) log.AppendLine($"  1. {CbE1Who.SelectedItem}. Причина: {CbE1Cause.SelectedItem}. {CbE1Hate.SelectedItem}. {CbE1Action.SelectedItem}.");
            if (enemies >= 2) log.AppendLine($"  2. {CbE2Who.SelectedItem}. Причина: {CbE2Cause.SelectedItem}. {CbE2Hate.SelectedItem}. {CbE2Action.SelectedItem}.");
            if (enemies >= 3) log.AppendLine($"  3. {CbE3Who.SelectedItem}. Причина: {CbE3Cause.SelectedItem}. {CbE3Hate.SelectedItem}. {CbE3Action.SelectedItem}.");
            log.AppendLine();

            int love = CbCountLove.SelectedIndex;
            log.AppendLine($"> ТРАГИЧЕСКИЕ РОМАНЫ (Найдено: {love}):");
            if (love == 0) log.AppendLine("  - Отсутствуют.");
            if (love >= 1) log.AppendLine($"  1. {CbL1.SelectedItem}");
            if (love >= 2) log.AppendLine($"  2. {CbL2.SelectedItem}");
            if (love >= 3) log.AppendLine($"  3. {CbL3.SelectedItem}");

            log.AppendLine();
            log.AppendLine("> СТАТУС: АНАЛИЗ ЗАВЕРШЕН.");

            TxtTerminalLog.Text = log.ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RoleLifepathWindow roleWindow = new RoleLifepathWindow();
            roleWindow.Show();
            this.Close();
        }
    }
}