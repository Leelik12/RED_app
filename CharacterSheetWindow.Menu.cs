using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberpunkRED_Generator
{
    // пункты меню листа; показываются в выдвижной панели (CharacterSheetWindow.Tabs.cs)
    public partial class CharacterSheetWindow
    {
        public class MenuEntry { public string Title, Group; public Action Action; }
        private static readonly string[] MenuGroups = { "ПЕРСОНАЖ", "КАТАЛОГИ", "ИГРА", "ФАЙЛ", "ПРОГРАММА" };
        private readonly List<MenuEntry> _menuItems = new List<MenuEntry>();

        private void InitMenu()
        {
            RegisterMenuItem("💰 Финансы и жильё", ShowFinanceDialog, "ПЕРСОНАЖ");
            RegisterMenuItem("🚗 Транспорт", () => VehiclesView.ShowWindow(this, DataContext as SheetViewModel), "ПЕРСОНАЖ");
            RegisterMenuItem("🏚 Штаб-квартира", () => HqView.ShowWindow(this, DataContext as SheetViewModel), "ПЕРСОНАЖ");
            RegisterMenuItem("🏠 Жильё", () => HousingView.ShowWindow(this, DataContext as SheetViewModel), "ПЕРСОНАЖ");
            RegisterMenuItem("📱 Агенты", () => AgentsView.ShowWindow(this, DataContext as SheetViewModel), "ПЕРСОНАЖ");
            RegisterMenuItem("📜 Правила (выдержки)", () => RulesView.ShowWindow(this), "КАТАЛОГИ");
            RegisterMenuItem("📖 Каталог оружия", () => OpenCatalog(0), "КАТАЛОГИ");
            RegisterMenuItem("📖 Каталог брони", () => OpenCatalog(1), "КАТАЛОГИ");
            RegisterMenuItem("📖 Каталог снаряжения", () => OpenCatalog(2), "КАТАЛОГИ");
            RegisterMenuItem("🃏 Таро Найт-Сити", () => TarotView.ShowWindow(this), "ИГРА");
            RegisterMenuItem("♻ Сбор обломков", () => SalvageView.ShowWindow(this), "ИГРА");
            RegisterMenuItem("🛠 Панкноут", () => PunknautView.ShowWindow(this), "ИГРА");
            InitExportMenu();
            InitBorgMenu();
            InitJournal();
            InitSettingsMenu();
        }

        private void RegisterMenuItem(string title, Action action, string group = "ПРОЧЕЕ")
            => _menuItems.Add(new MenuEntry { Title = title, Action = action, Group = group });
    }
}
