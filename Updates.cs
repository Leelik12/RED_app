using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CyberpunkRED_Generator
{
    public class UpdateInfo
    {
        public bool Ok;                  // запрос удался
        public bool Newer;               // на GitHub есть версия новее установленной
        public string Tag, Url, Notes, Error;
    }

    // проверка обновлений по последнему релизу на GitHub (только чтение публичного API, без отправки данных о пользователе)
    public static class Updates
    {
        // «v2.0.1», «2.0.1-beta» -> 2.0.1; не разобрали -> null
        public static Version ParseTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return null;
            string t = tag.Trim().TrimStart('v', 'V');
            int cut = t.IndexOfAny(new[] { '-', '+', ' ' });
            if (cut > 0) t = t.Substring(0, cut);
            var parts = t.Split('.');
            if (parts.Length < 2 || parts.Length > 4) return null;
            return Version.TryParse(parts.Length == 2 ? t + ".0" : t, out var v) ? v : null;
        }

        public static bool IsNewer(string tag, Version current)
        {
            var v = ParseTag(tag);
            if (v == null || current == null) return false;
            var cur = new Version(current.Major, current.Minor, Math.Max(current.Build, 0));
            var rem = new Version(v.Major, v.Minor, Math.Max(v.Build, 0));
            return rem > cur;
        }

        public static UpdateInfo Parse(string json, Version current)
        {
            var info = new UpdateInfo();
            try
            {
                using (var doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    info.Tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() : null;
                    info.Url = root.TryGetProperty("html_url", out var u) ? u.GetString() : null;
                    info.Notes = root.TryGetProperty("body", out var b) ? b.GetString() : null;
                }
                info.Ok = !string.IsNullOrEmpty(info.Tag);
                info.Newer = info.Ok && IsNewer(info.Tag, current);
                if (!info.Ok) info.Error = "в ответе нет версии релиза";
            }
            catch (Exception ex) { info.Error = ex.Message; }
            return info;
        }

        public static async Task<UpdateInfo> CheckAsync()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) })
                {
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("CyberpunkRED-Terminal/" + AppInfo.VersionText);
                    string json = await http.GetStringAsync("https://api.github.com/repos/" + AppInfo.Repository + "/releases/latest").ConfigureAwait(false);
                    return Parse(json, AppInfo.Version);
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Write("Проверка обновлений", ex);
                return new UpdateInfo { Error = ex.Message };
            }
        }
    }
}
