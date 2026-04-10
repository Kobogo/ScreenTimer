using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ScreenTimer.Services
{
    public class ApiService
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly string _apiUrl = "https://todoapi-spz1.onrender.com/api/Timer";
        private readonly int _userId = 3;

        public async Task<JObject?> FetchTimerDataAsync()
        {
            try
            {
                await _client.PostAsync($"{_apiUrl}/reset-daily-time/{_userId}", null);
                var response = await _client.GetStringAsync($"{_apiUrl}/{_userId}");
                return JsonConvert.DeserializeObject<JObject>(response);
            }
            catch { return null; }
        }

        public async Task UpdateStatusAsync(bool isRunning)
        {
            try
            {
                var json = JsonConvert.SerializeObject(isRunning);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _client.PatchAsync($"{_apiUrl}/{_userId}/toggle", content);
            }
            catch { }
        }

        public async Task SyncMinutesAsync(int minutesUsed)
        {
            try
            {
                var json = JsonConvert.SerializeObject(minutesUsed);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _client.PatchAsync($"{_apiUrl}/{_userId}/sync", content);
            }
            catch { }
        }
    }
}