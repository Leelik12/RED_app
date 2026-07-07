using System.IO;
using System;
using System.Windows;

namespace CyberpunkRED_Generator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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

        private void BtnOpenGMScreen_Click(object sender, RoutedEventArgs e)
        {
            GMScreenWindow gmWindow = new GMScreenWindow();
            gmWindow.Show();
            this.Close();
        }
    }
}