using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace CyberpunkRED_Generator
{
    public class JournalEntry
    {
        public DateTime Time { get; set; }
        public string Kind { get; set; }    // roll | note | event
        public string Text { get; set; }
    }

    // журнал сессии и портрет персонажа: хранятся в файле персонажа (старые файлы без этих полей открываются пустыми)
    public class JournalData
    {
        public const int MaxEntries = 500;
        public List<JournalEntry> Entries { get; set; } = new List<JournalEntry>();
        public string Portrait { get; set; }    // PNG в base64, до 256 px по длинной стороне

        public Action Changed;   // поле, в файл не попадает

        public JournalEntry Add(string kind, string text)
        {
            var e = new JournalEntry { Time = DateTime.Now, Kind = kind, Text = text };
            Entries.Add(e);
            if (Entries.Count > MaxEntries) Entries.RemoveRange(0, Entries.Count - MaxEntries);
            Changed?.Invoke();
            return e;
        }

        public void Clear() { Entries.Clear(); Changed?.Invoke(); }
    }

    // бросок навыка: СУМ (уровень + характеристика + модификаторы) + 1d10 с критическими правилами корника
    public static class SkillRoll
    {
        public class Result
        {
            public int Total, Die;
            public string Detail, Text;
        }

        public static Result Compose(string name, int bonus, int die, string detail)
        {
            var r = new Result { Die = die, Detail = detail };
            r.Total = bonus + die;
            r.Text = $"{name}: {bonus} + [{detail}] = {r.Total}";
            return r;
        }

        public static Result Roll(SheetSkill skill)
        {
            string name = string.IsNullOrWhiteSpace(skill.SubName) ? skill.Name : skill.BaseName + ": " + skill.SubName;
            int die = DiceRoller.D10Check(out string detail);
            return Compose(name ?? skill.Name, skill.Base, die, detail);
        }
    }

    public static class Portraits
    {
        // читает картинку, уменьшает до 256 px и отдаёт PNG в base64; null — файл не картинка
        public static string FromFile(string path)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path);
                bmp.DecodePixelWidth = 256;
                bmp.EndInit();
                bmp.Freeze();
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bmp));
                using (var ms = new MemoryStream()) { enc.Save(ms); return Convert.ToBase64String(ms.ToArray()); }
            }
            catch (Exception ex) { ErrorLog.Write("Портрет", ex); return null; }
        }

        public static BitmapImage ToImage(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return null;
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = new MemoryStream(Convert.FromBase64String(base64));
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }
    }
}
