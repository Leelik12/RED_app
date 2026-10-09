using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace CyberpunkRED_Generator
{
    // отмена/повтор, автосохранение и резервные копии листа персонажа
    public partial class CharacterSheetWindow
    {
        private const int MaxUndoSteps = 60;
        private const int MaxBackupsPerCharacter = 15;

        private readonly List<string> _undoStates = new List<string>();
        private readonly List<string> _redoStates = new List<string>();
        private string _baselineJson;   // зафиксированное текущее состояние
        private string _lastSavedJson;  // состояние, записанное на диск (null — есть несохранённые данные)
        private bool _dataReady;
        private bool _restoring;
        private bool _commitQueued;
        private bool _loadedFromRecovery;
        private DispatcherTimer _autosaveTimer;
        private DateTime _lastBackup = DateTime.MinValue;

        private void InitSafety()
        {
            if (!_dataReady) return;

            _baselineJson = SafeSerialize();
            _lastSavedJson = _loadedFromRecovery ? null : _baselineJson;

            PreviewMouseLeftButtonUp += (s, e) => QueueCommit();
            PreviewKeyUp += (s, e) => { if (e.Key == Key.Enter || e.Key == Key.Tab) QueueCommit(); };
            AddHandler(Keyboard.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler((s, e) => QueueCommit()), true);
            PreviewKeyDown += HandleUndoKeys;

            _autosaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(2) };
            _autosaveTimer.Tick += (s, e) => Autosave();
            _autosaveTimer.Start();

            UpdateUndoButtons();
        }

        private void StopAutosave()
        {
            if (_autosaveTimer != null) { _autosaveTimer.Stop(); _autosaveTimer = null; }
        }

        // --- отмена / повтор ---

        private string SafeSerialize()
        {
            try { return SerializeCurrentState(); }
            catch { return null; }
        }

        private bool IsDirty()
        {
            if (!_dataReady) return false;
            if (_lastSavedJson == null) return true;
            string now = SafeSerialize();
            return now != null && now != _lastSavedJson;
        }

        private void QueueCommit()
        {
            if (_commitQueued || !_dataReady) return;
            _commitQueued = true;
            Dispatcher.BeginInvoke(new Action(() => { _commitQueued = false; CommitChange(); }), DispatcherPriority.ContextIdle);
        }

        // запоминает предыдущее состояние, если лист изменился с последней фиксации
        private void CommitChange()
        {
            if (!_dataReady || _restoring) return;
            string now = SafeSerialize();
            if (now == null || now == _baselineJson) return;

            if (_baselineJson != null) _undoStates.Add(_baselineJson);
            if (_undoStates.Count > MaxUndoSteps) _undoStates.RemoveAt(0);
            _redoStates.Clear();
            _baselineJson = now;
            UpdateUndoButtons();
        }

        private void Undo()
        {
            CommitChange();
            if (_undoStates.Count == 0) return;
            _redoStates.Add(_baselineJson);
            string prev = _undoStates[_undoStates.Count - 1];
            _undoStates.RemoveAt(_undoStates.Count - 1);
            RestoreSnapshot(prev);
        }

        private void Redo()
        {
            CommitChange();
            if (_redoStates.Count == 0) return;
            _undoStates.Add(_baselineJson);
            string next = _redoStates[_redoStates.Count - 1];
            _redoStates.RemoveAt(_redoStates.Count - 1);
            RestoreSnapshot(next);
        }

        private void RestoreSnapshot(string json)
        {
            _restoring = true;
            try
            {
                CloseOverlays();
                ApplyCharacterJson(json);
                _baselineJson = SafeSerialize() ?? json;
            }
            finally { _restoring = false; }
            UpdateUndoButtons();
        }

        private void CloseOverlays()
        {
            if (CyberwareOverlay != null) CyberwareOverlay.Visibility = Visibility.Collapsed;
            if (EnemyManagerOverlay != null) EnemyManagerOverlay.Visibility = Visibility.Collapsed;
        }

        private void UpdateUndoButtons()
        {
            if (BtnUndo != null) BtnUndo.IsEnabled = _undoStates.Count > 0;
            if (BtnRedo != null) BtnRedo.IsEnabled = _redoStates.Count > 0;
        }

        private void HandleUndoKeys(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
            // внутри текстового поля работает его собственная отмена ввода
            if (Keyboard.FocusedElement is TextBoxBase) return;

            if (e.Key == Key.Z) { Undo(); e.Handled = true; }
            else if (e.Key == Key.Y) { Redo(); e.Handled = true; }
        }

        private void BtnUndo_Click(object sender, RoutedEventArgs e) => Undo();
        private void BtnRedo_Click(object sender, RoutedEventArgs e) => Redo();

        // --- автосохранение и резервные копии ---

        private static string _backupsRoot = AppDomain.CurrentDomain.BaseDirectory;
        private static string BackupsDir => Path.Combine(_backupsRoot, "Backups");

        // стабильный ключ файла: имя + хэш полного пути (чтобы одноимённые файлы из разных папок не пересекались)
        private static string FileKey(string path)
        {
            uint hash = 2166136261;
            foreach (char c in Path.GetFullPath(path).ToLowerInvariant()) hash = (hash ^ c) * 16777619;
            return UIHelpers.SafeFileName(Path.GetFileNameWithoutExtension(path)) + "_" + hash.ToString("x8");
        }

        private static string RecoveryPath(string path) => Path.Combine(BackupsDir, FileKey(path) + ".autosave.json");

        private void DeleteRecoveryFile()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentFilePath)) return;
                string p = RecoveryPath(_currentFilePath);
                if (File.Exists(p)) File.Delete(p);
            }
            catch { }
        }

        private void Autosave()
        {
            if (!_dataReady || _restoring || string.IsNullOrEmpty(_currentFilePath)) return;
            try
            {
                string now = SafeSerialize();
                if (now == null || now == _lastSavedJson) return;
                Directory.CreateDirectory(BackupsDir);
                File.WriteAllText(RecoveryPath(_currentFilePath), now, Encoding.UTF8);
            }
            catch { }
        }

        // перед перезаписью файла кладём его копию в Backups (не чаще раза в 10 минут), храним последние 15
        private void BackupExistingFile()
        {
            try
            {
                if (!File.Exists(_currentFilePath)) return;
                if (DateTime.Now - _lastBackup < TimeSpan.FromMinutes(10)) return;

                Directory.CreateDirectory(BackupsDir);
                string key = FileKey(_currentFilePath);
                File.Copy(_currentFilePath, Path.Combine(BackupsDir, $"{key}.{DateTime.Now:yyyyMMdd_HHmmss}.bak.json"), true);
                _lastBackup = DateTime.Now;

                var old = new DirectoryInfo(BackupsDir).GetFiles(key + ".*.bak.json")
                    .OrderByDescending(f => f.Name).Skip(MaxBackupsPerCharacter);
                foreach (var f in old) { try { f.Delete(); } catch { } }
            }
            catch { }
        }
    }
}
