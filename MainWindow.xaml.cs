using System.IO;
using System;
using System.Windows;

namespace CyberpunkRED_Generator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            Theme.Install(this);
            InitializeComponent();
            Title = AppInfo.Title;
            TxtVersion.Text = "v" + AppInfo.VersionText;
            if (AppSettings.Data.CheckUpdates) Loaded += (s, e) => SettingsView.CheckUpdates(this, true);
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            CharacterGenerationWindow genWindow = new CharacterGenerationWindow();
            genWindow.Show();
            this.Close();
        }

        private void BtnLoadCharacter_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Characters");
            if (Directory.Exists(folderPath))
            {
                openFileDialog.InitialDirectory = folderPath;
            }

            openFileDialog.Filter = "JSON файлы персонажей (*.json)|*.json|Все файлы (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                CharacterSheetWindow sheetWindow = new CharacterSheetWindow(openFileDialog.FileName);
                sheetWindow.Show();
                this.Close();
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e) => SettingsView.ShowSettings(this);
        private void BtnRules_Click(object sender, RoutedEventArgs e) => RulesView.ShowWindow(this);
        private void BtnUpdates_Click(object sender, RoutedEventArgs e) => SettingsView.CheckUpdates(this, false);
        private void BtnAbout_Click(object sender, RoutedEventArgs e) => SettingsView.ShowAbout(this);

        private void BtnOpenGMScreen_Click(object sender, RoutedEventArgs e)
        {
            GMScreenWindow gmWindow = new GMScreenWindow();
            gmWindow.Show();
            this.Close();
        }
    }
}