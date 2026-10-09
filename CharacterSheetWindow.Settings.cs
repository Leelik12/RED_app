using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberpunkRED_Generator
{
    // настройки и «О программе» — общие окна SettingsView (те же, что в главном меню и ГМ-экране)
    public partial class CharacterSheetWindow
    {
        private void InitSettingsMenu()
        {
            RegisterMenuItem("⚙ Настройки", ShowSettingsDialog, "ПРОГРАММА");
            RegisterMenuItem("ℹ О программе", ShowAboutDialog, "ПРОГРАММА");
        }

        private void ShowAboutDialog()
        {
            CloseOverlays();
            SettingsView.ShowAbout(this);
        }

        private void ShowSettingsDialog()
        {
            CloseOverlays();
            SettingsView.ShowSettings(this);
        }
    }
}
