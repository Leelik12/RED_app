using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CyberpunkRED_Generator
{
    // логика АНТИХАЙПА (окна гма)

    public partial class GMScreenWindow : Window
    {
        // --- МЕТОДЫ-МОСТЫ ДЛЯ ВАЛИДАЦИИ UI ---
        private void NumberValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
            => UIHelpers.NumberValidationTextBox(sender, e);

        private void NumberLimitTextBox_TextChanged(object sender, TextChangedEventArgs e)
            => UIHelpers.NumberLimitTextBox_TextChanged(sender, e);
        public ObservableCollection<CombatantViewModel> Combatants { get; set; } = new ObservableCollection<CombatantViewModel>();
        public ObservableCollection<EnemySaveData> AvailableEnemies { get; set; } = new ObservableCollection<EnemySaveData>();

        // для создания врагов
        public List<string> AllSkillNames => CoreDataBase.AllSkills.Select(s => s.Name).OrderBy(n => n).ToList();
        public ObservableCollection<SkillSaveData> NewEnemySkills { get; set; } = new ObservableCollection<SkillSaveData>();

        public List<string> AllCyberwareNames => CoreDataBase.AllCyberware.Select(c => c.Name).Distinct().OrderBy(n => n).ToList();
        public ObservableCollection<string> NewEnemyCyberware { get; set; } = new ObservableCollection<string>();

        private int _currentTurnIndex = -1;
        private int _roundCount = 1;

        public GMScreenWindow()
        {
            Theme.Install(this);
            InitializeComponent();
            Title = "ТЕРМИНАЛ ГЕЙМ-МАСТЕРА · v" + AppInfo.VersionText;
            this.DataContext = this;
        }

        private void BtnNextTurn_Click(object sender, RoutedEventArgs e)
        {
            if (Combatants.Count == 0) return;

            if (_currentTurnIndex >= 0 && _currentTurnIndex < Combatants.Count)
                Combatants[_currentTurnIndex].IsActiveTurn = false;

            _currentTurnIndex++;

            if (_currentTurnIndex >= Combatants.Count)
            {
                _currentTurnIndex = 0;
                _roundCount++;
                TxtRoundCounter.Text = $"РАУНД: {_roundCount}";
                TxtTimeCounter.Text = $"Прошло времени: {(_roundCount - 1) * 3} сек.";
            }

            Combatants[_currentTurnIndex].IsActiveTurn = true;
        }

        private void BtnWeather_Click(object sender, RoutedEventArgs e) => WeatherView.ShowWindow(this);
        private void BtnTarot_Click(object sender, RoutedEventArgs e) => TarotView.ShowWindow(this);

        // справочные инструменты ГМа открываются отдельными окнами (вкладок у ГМ-экрана нет)
        private void BtnGmTools_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu { PlacementTarget = BtnGmTools, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
            void Add(string title, Action act)
            {
                var item = new MenuItem { Header = title };
                item.Click += (s, a) => act();
                menu.Items.Add(item);
            }
            Add("🗺 Карта Найт-Сити", () => MapView.ShowWindow(this, null));
            Add("🌙 Ночные рынки", () => NightMarketView.ShowWindow(this, null));
            Add("🔥 Перчинки для боя (Прожжённые)", () => TwistsView.ShowWindow(this, null));
            Add("🏁 Погоня", () => ChaseView.ShowWindow(this, null));
            Add("♻ Сбор обломков", () => SalvageView.ShowWindow(this));
            Add("🍸 Бар VA-11 HALL-A", () => DrinksView.ShowWindow(this));
            Add("🛠 Панкноут", () => PunknautView.ShowWindow(this));
            Add("⏱ Эффекты и таймеры", () => EffectsView.ShowWindow(this, null));
            Add("📜 Правила (выдержки)", () => RulesView.ShowWindow(this));
            menu.IsOpen = true;
        }

        private void BtnSortInitiative_Click(object sender, RoutedEventArgs e)
        {
            if (Combatants.Count == 0) return;

            var sorted = Combatants.OrderByDescending(c => c.Initiative).ToList();
            Combatants.Clear();
            foreach (var c in sorted) Combatants.Add(c);

            _currentTurnIndex = -1;
            foreach (var c in Combatants) c.IsActiveTurn = false;

            _roundCount = 1;
            TxtRoundCounter.Text = "РАУНД: 1";
            TxtTimeCounter.Text = "Прошло времени: 0 сек.";

            MessageBox.Show("Инициатива отсортирована. Бой начался!", "ИНФО", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // карточка воина
        private void BtnAblateHead_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is CombatantViewModel c && c.HeadArmor > 0) c.HeadArmor--; }
        private void BtnAblateBody_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is CombatantViewModel c && c.BodyArmor > 0) c.BodyArmor--; }
        private void BtnDamage_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is CombatantViewModel c) c.CurrentHP--; }
        private void BtnHeal_Click(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is CombatantViewModel c) c.CurrentHP++; }
        private void BtnRemoveCombatant_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Tag is CombatantViewModel c)) return;
            int idx = Combatants.IndexOf(c);
            if (idx < 0) return;
            Combatants.RemoveAt(idx);
            // чтобы очередь хода не сбивалась: удалённый до текущего (или текущий) сдвигает индекс назад
            if (idx <= _currentTurnIndex) _currentTurnIndex--;
        }

        private void BtnAddGenCyber_Click(object sender, RoutedEventArgs e)
        {
            string cyberName = CbGenCyber.Text?.Trim();
            if (!string.IsNullOrEmpty(cyberName) && !NewEnemyCyberware.Contains(cyberName))
            {
                // Проверяем, есть ли такой имплант в базе (без учета регистра)
                bool exists = CoreDataBase.AllCyberware.Any(c => c.Name.Equals(cyberName, StringComparison.OrdinalIgnoreCase));
                if (exists)
                {
                    NewEnemyCyberware.Add(cyberName);
                    CbGenCyber.Text = "";
                }
                else
                {
                    MessageBox.Show("Такого импланта нет в базе! Проверьте правильность написания или добавьте его через генератор.", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnRemoveGenCyber_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is string cyber)
            {
                NewEnemyCyberware.Remove(cyber);
            }
        }

        private void BtnAddCritToCombatant_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is CombatantViewModel c && !string.IsNullOrEmpty(c.SelectedInjuryToAdd))
            {
                var def = CoreDataBase.CriticalInjuries.FirstOrDefault(x => x.Name == c.SelectedInjuryToAdd);
                if (def != null)
                {
                    c.ActiveInjuries.Add(new GMCriticalInjuryItem
                    {
                        Name = def.Name,
                        Description = def.Description,
                        QuickFix = def.QuickFix,
                        Treatment = def.Treatment,
                        AllActionsPenalty = def.AllActionsPenalty,
                        MovePenalty = def.MovePenalty,
                        DeathSavePenalty = def.DeathSavePenalty,
                        EffectText = def.EffectText
                    });
                    c.Recalculate();
                }
            }
        }

        private void BtnRemoveCritFromCombatant_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is object[] args && args.Length == 2)
            {
                if (args[0] is CombatantViewModel combatant && args[1] is GMCriticalInjuryItem injury)
                {
                    combatant.ActiveInjuries.Remove(injury);
                    combatant.Recalculate();
                }
            }
        }

        private void BtnAddPlayer_Click(object sender, RoutedEventArgs e)
        {
            Combatants.Add(new CombatantViewModel
            {
                Name = "ИГРОК",
                IsPlayer = true,
                Initiative = 0,
                MaxHP = 40,
                CurrentHP = 40,
                HeadArmor = 11,
                BodyArmor = 11,
                BaseMove = 5,
                BaseDeathSave = 5,
                CombatStatsDisplay = "Смотреть в листе игрока",
                Weapons = "",
                Gear = "",
                Cyberware = ""
            });
        }

        // генератор и оверлей
        private void BtnOpenEnemyManager_Click(object sender, RoutedEventArgs e)
        {
            LoadEnemiesFromFolder();
            if (TxtEnemySearch != null) TxtEnemySearch.Text = "";
            RbGenCN.IsChecked = true;
            _editingEnemyName = null;
            NewEnemySkills.Clear();
            NewEnemyCyberware.Clear();
            EnemyManagerOverlay.Visibility = Visibility.Visible;
        }

        private void BtnCloseEnemyManager_Click(object sender, RoutedEventArgs e)
        {
            if (TxtEnemySearch != null) TxtEnemySearch.Text = "";
            EnemyManagerOverlay.Visibility = Visibility.Collapsed;
        }

        private void GenMode_Changed(object sender, RoutedEventArgs e)
        {
            if (PanelGenCN == null || PanelGenFull == null) return;
            bool isCN = RbGenCN.IsChecked == true;
            PanelGenCN.Visibility = isCN ? Visibility.Visible : Visibility.Collapsed;
            PanelGenFull.Visibility = isCN ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BtnAddGenSkill_Click(object sender, RoutedEventArgs e)
        {
            if (CbGenSkills.SelectedItem != null && int.TryParse(TxtGenSkillLevel.Text, out int inputValue))
            {
                string skillName = CbGenSkills.SelectedItem.ToString();
                int skillLevel = inputValue;

                // если галочка стоит значит ввели финалку (Навык + СТАТ) из корника. 
                // нам нужно вычесть СТАТ, чтобы сохранить в базу чистый уровень навыка.
                if (ChkSkillIsTotal.IsChecked == true)
                {
                    var def = CoreDataBase.AllSkills.FirstOrDefault(s => s.Name == skillName);
                    if (def != null)
                    {
                        string statKey = def.Stat;
                        switch (statKey)
                        {
                            case "ИНТ": statKey = "INT"; break;
                            case "РЕА": statKey = "REF"; break;
                            case "ЛВК": statKey = "DEX"; break;
                            case "ТЕХ": statKey = "TECH"; break;
                            case "ХАР": statKey = "COOL"; break;
                            case "ВОЛЯ": statKey = "WILL"; break;
                            case "УДЧ": statKey = "LUCK"; break;
                            case "СКО": statKey = "MOVE"; break;
                            case "ТЕЛ": statKey = "BODY"; break;
                            case "ЭМП": statKey = "EMP"; break;
                        }

                        int statValue = 5; // базовое на всякий
                        switch (statKey)
                        {
                            case "INT": int.TryParse(StINT.Text, out statValue); break;
                            case "REF": int.TryParse(StREF.Text, out statValue); break;
                            case "DEX": int.TryParse(StDEX.Text, out statValue); break;
                            case "TECH": int.TryParse(StTECH.Text, out statValue); break;
                            case "COOL": int.TryParse(StCOOL.Text, out statValue); break;
                            case "WILL": int.TryParse(StWILL.Text, out statValue); break;
                            case "LUCK": int.TryParse(StLUCK.Text, out statValue); break;
                            case "MOVE": int.TryParse(StMOVE.Text, out statValue); break;
                            case "BODY": int.TryParse(StBODY.Text, out statValue); break;
                            case "EMP": int.TryParse(StEMP.Text, out statValue); break;
                        }

                        skillLevel = inputValue - statValue;

                        if (skillLevel < 0) skillLevel = 0;
                    }
                }

                NewEnemySkills.Add(new SkillSaveData { Name = skillName, Total = skillLevel });
            }
        }

        // имя пресета, открытого на редактирование (чтобы при переименовании предложить удалить старый)
        private string _editingEnemyName;

        private void BtnEditEnemyPreset_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is EnemySaveData enemyData)
            {
                _editingEnemyName = enemyData.Name;
                TxtGenName.Text = enemyData.Name;
                TxtGenHP.Text = enemyData.HP.ToString();
                TxtGenHead.Text = enemyData.HeadArmor.ToString();
                TxtGenBody.Text = enemyData.BodyArmor.ToString();
                TxtGenWeapons.Text = enemyData.Weapons;
                TxtGenGear.Text = enemyData.Gear;

                if (enemyData.IsCNMode)
                {
                    RbGenCN.IsChecked = true;
                    TxtGenCN.Text = enemyData.CombatNumber.ToString();
                    TxtGenDeathSaveCN.Text = enemyData.BaseDeathSave.ToString();
                    TxtGenMove.Text = enemyData.BaseMove.ToString();
                }
                else
                {
                    RbGenFull.IsChecked = true;
                    StINT.Text = enemyData.Stats.ContainsKey("INT") ? enemyData.Stats["INT"].ToString() : "5";
                    StREF.Text = enemyData.Stats.ContainsKey("REF") ? enemyData.Stats["REF"].ToString() : "5";
                    StDEX.Text = enemyData.Stats.ContainsKey("DEX") ? enemyData.Stats["DEX"].ToString() : "5";
                    StTECH.Text = enemyData.Stats.ContainsKey("TECH") ? enemyData.Stats["TECH"].ToString() : "5";
                    StCOOL.Text = enemyData.Stats.ContainsKey("COOL") ? enemyData.Stats["COOL"].ToString() : "5";
                    StWILL.Text = enemyData.Stats.ContainsKey("WILL") ? enemyData.Stats["WILL"].ToString() : "5";
                    StLUCK.Text = enemyData.Stats.ContainsKey("LUCK") ? enemyData.Stats["LUCK"].ToString() : "5";
                    StMOVE.Text = enemyData.Stats.ContainsKey("MOVE") ? enemyData.Stats["MOVE"].ToString() : "5";
                    StBODY.Text = enemyData.Stats.ContainsKey("BODY") ? enemyData.Stats["BODY"].ToString() : "5";
                    StEMP.Text = enemyData.Stats.ContainsKey("EMP") ? enemyData.Stats["EMP"].ToString() : "5";

                    NewEnemySkills.Clear();
                    if (enemyData.Skills != null)
                    {
                        foreach (var kvp in enemyData.Skills)
                        {
                            NewEnemySkills.Add(new SkillSaveData { Name = kvp.Key, Total = kvp.Value });
                        }
                    }
                }

                NewEnemyCyberware.Clear();
                if (!string.IsNullOrWhiteSpace(enemyData.Cyberware))
                {
                    var parts = enemyData.Cyberware.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        string cleanName = part.Trim();
                        if (!string.IsNullOrWhiteSpace(cleanName))
                        {
                            NewEnemyCyberware.Add(cleanName);
                        }
                    }
                }
            }
        }
        private void BtnRemoveGenSkill_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is SkillSaveData s) NewEnemySkills.Remove(s);
        }

        private void LoadEnemiesFromFolder()
        {
            AvailableEnemies.Clear();
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Enemies");
            if (Directory.Exists(folderPath))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                foreach (string file in Directory.GetFiles(folderPath, "*.json"))
                {
                    try { AvailableEnemies.Add(JsonSerializer.Deserialize<EnemySaveData>(File.ReadAllText(file), options)); } catch { }
                }
            }
            Npcs.AddTo(AvailableEnemies);
        }

        private void BtnSpawnEnemy_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is EnemySaveData enemyData)
            {
                int count = int.TryParse(TxtSpawnCount?.Text, out int n) ? Math.Max(1, Math.Min(30, n)) : 1;
                var added = SpawnEnemy(enemyData, count);
                MessageBox.Show(count == 1 ? $"{added[0]} добавлен в трекер инициативы!" : $"Добавлено в трекер: {string.Join(", ", added)}", "УСПЕХ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // добавляет в бой count копий врага; несколько одинаковых нумеруются
        public List<string> SpawnEnemy(EnemySaveData enemyData, int count)
        {
            var names = CombatSaves.SpawnNames(enemyData.Name, count, Combatants.Select(c => c.Name));
            foreach (var name in names)
            {
                var combatant = new CombatantViewModel
                {
                    OriginalData = enemyData,
                    Name = name,
                    IsPlayer = false,
                    Initiative = 0,
                    MaxHP = enemyData.HP,
                    CurrentHP = enemyData.HP,
                    HeadArmor = enemyData.HeadArmor,
                    BodyArmor = enemyData.BodyArmor,
                    BaseMove = enemyData.BaseMove,
                    BaseDeathSave = enemyData.BaseDeathSave,
                    Weapons = enemyData.Weapons,
                    Gear = enemyData.Gear,
                    Cyberware = enemyData.Cyberware
                };

                combatant.RebuildStatsDisplay();
                Combatants.Add(combatant);
            }
            return names;
        }

        private void BtnSaveNewEnemy_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtGenName.Text))
            {
                MessageBox.Show("Имя врага не может быть пустым!", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var enemy = new EnemySaveData
            {
                Name = TxtGenName.Text.Trim(),
                HP = int.TryParse(TxtGenHP.Text, out int hp) ? hp : 20,
                HeadArmor = int.TryParse(TxtGenHead.Text, out int ha) ? ha : 0,
                BodyArmor = int.TryParse(TxtGenBody.Text, out int ba) ? ba : 0,
                IsCNMode = RbGenCN.IsChecked == true,
                Weapons = TxtGenWeapons.Text,
                Gear = TxtGenGear.Text,
                Cyberware = string.Join(", ", NewEnemyCyberware)
            };

            if (enemy.IsCNMode)
            {
                enemy.CombatNumber = int.TryParse(TxtGenCN.Text, out int cn) ? cn : 12;
                enemy.BaseDeathSave = int.TryParse(TxtGenDeathSaveCN.Text, out int ds) ? ds : 5;
                enemy.BaseMove = int.TryParse(TxtGenMove.Text, out int mv) ? mv : 5;
            }
            else
            {
                enemy.Stats["INT"] = UIHelpers.ParseIntOr(StINT.Text, 5);
                enemy.Stats["REF"] = UIHelpers.ParseIntOr(StREF.Text, 5);
                enemy.Stats["DEX"] = UIHelpers.ParseIntOr(StDEX.Text, 5);
                enemy.Stats["TECH"] = UIHelpers.ParseIntOr(StTECH.Text, 5);
                enemy.Stats["COOL"] = UIHelpers.ParseIntOr(StCOOL.Text, 5);
                enemy.Stats["WILL"] = UIHelpers.ParseIntOr(StWILL.Text, 5);
                enemy.Stats["LUCK"] = UIHelpers.ParseIntOr(StLUCK.Text, 5);
                enemy.Stats["MOVE"] = UIHelpers.ParseIntOr(StMOVE.Text, 5);
                enemy.Stats["BODY"] = UIHelpers.ParseIntOr(StBODY.Text, 5);
                enemy.Stats["EMP"] = UIHelpers.ParseIntOr(StEMP.Text, 5);

                enemy.BaseMove = enemy.Stats["MOVE"];
                enemy.BaseDeathSave = enemy.Stats["BODY"];

                foreach (var sk in NewEnemySkills) enemy.Skills[sk.Name] = sk.Total;
            }

            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Enemies");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string filePath = UIHelpers.EnemyFilePath(enemy.Name);
            var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            File.WriteAllText(filePath, JsonSerializer.Serialize(enemy, options), System.Text.Encoding.UTF8);

            var existingItem = AvailableEnemies.FirstOrDefault(x => x.Name.Equals(enemy.Name, StringComparison.OrdinalIgnoreCase));
            if (existingItem != null)
            {
                AvailableEnemies.Remove(existingItem);
            }
            AvailableEnemies.Add(enemy);

            // пресет переименовали: предлагаем убрать старый, а не оставлять дубль
            if (_editingEnemyName != null && !_editingEnemyName.Equals(enemy.Name, StringComparison.OrdinalIgnoreCase)
                && MessageBox.Show($"Имя изменено. Удалить старый пресет '{_editingEnemyName}'?", "ПЕРЕИМЕНОВАНИЕ", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try { File.Delete(UIHelpers.EnemyFilePath(_editingEnemyName)); } catch { }
                var old = AvailableEnemies.FirstOrDefault(x => x.Name.Equals(_editingEnemyName, StringComparison.OrdinalIgnoreCase));
                if (old != null) AvailableEnemies.Remove(old);
            }
            _editingEnemyName = null;

            TxtGenName.Text = ""; TxtGenWeapons.Text = ""; TxtGenGear.Text = ""; CbGenCyber.Text = ""; NewEnemySkills.Clear(); NewEnemyCyberware.Clear();

            MessageBox.Show($"Враг '{enemy.Name}' сохранен в базу!", "СОХРАНЕНО", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDeleteEnemyPreset_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is EnemySaveData enemyData)
            {
                if (enemyData.IsBuiltIn) { MessageBox.Show("Это встроенный пресет DLC «" + enemyData.Source + "». Его можно скрыть, отключив пакет в настройках.", "ПРЕСЕТ DLC"); return; }
                var res = MessageBox.Show($"Удалить пресет '{enemyData.Name}' из базы навсегда?", "ПОДТВЕРДИТЕ УДАЛЕНИЕ", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                {
                    string filePath = UIHelpers.EnemyFilePath(enemyData.Name);

                    if (File.Exists(filePath))
                    {
                        try
                        {
                            File.Delete(filePath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при удалении файла: {ex.Message}", "ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    AvailableEnemies.Remove(enemyData);
                }
            }
        }

        private void TxtEnemySearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = TxtEnemySearch.Text.Trim();
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(AvailableEnemies);
            view.Filter = q.Length == 0 ? (Predicate<object>)null : o => o is EnemySaveData en && (en.Name ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
            view.Refresh();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e) => SettingsView.ShowSettings(this);

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Закрыть терминал ГМа? Текущий бой будет потерян.", "ВЫХОД", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                MainWindow main = new MainWindow();
                main.Show();
                this.Close();
            }
        }
    }
}