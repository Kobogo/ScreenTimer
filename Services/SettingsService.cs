using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ScreenTimer.Services
{
    public class SettingsService
    {
        // Gemmer i samme mappe som .exe filen
        private readonly string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public (double Left, double Top, bool IsMini) LoadSettings()
        {
            if (File.Exists(_path))
            {
                try
                {
                    var json = File.ReadAllText(_path);
                    var s = JsonConvert.DeserializeObject<JObject>(json);
                    if (s != null)
                    {
                        // Hent værdier med fallback til NaN/false hvis de mangler i JSON
                        double left = s["Left"]?.Value<double>() ?? double.NaN;
                        double top = s["Top"]?.Value<double>() ?? double.NaN;
                        bool isMini = s["IsMini"]?.Value<bool>() ?? false;

                        return (left, top, isMini);
                    }
                }
                catch
                {
                    // Ved fejl i læsning returneres standard
                }
            }
            return (double.NaN, double.NaN, false);
        }

        public void SaveSettings(double left, double top, bool isMini)
        {
            try
            {
                // Vi gemmer kun hvis koordinaterne er fornuftige (ikke minimeret til -32000)
                if (left < -10000 || top < -10000) return;

                var settings = new { Left = left, Top = top, IsMini = isMini };
                File.WriteAllText(_path, JsonConvert.SerializeObject(settings, Formatting.Indented));
            }
            catch { }
        }
    }
}