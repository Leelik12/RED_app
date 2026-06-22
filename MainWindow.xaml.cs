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
            this.Close(); // Закрываем главное меню, или можешь оставить открытым
        }
    }
}