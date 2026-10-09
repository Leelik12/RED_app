using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CyberpunkGen
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            CyberpunkRED_Generator.ErrorLog.Install(this);
            CyberpunkRED_Generator.Packs.Apply();
            CyberpunkRED_Generator.Loc.Install();
        }
    }
}
