using SuperpowersRevit.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SuperpowersRevit.Services
{
    /// <summary>
    /// Communicates with the Drofus REST API using only built-in .NET 8 types (HttpClient + System.Text.Json).
    /// Authentication: POST /api/login → bearer token stored for subsequent calls.
    /// </summary>
    public class DrofusService : IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _username;
        private readonly string _password;
        private string _token = string.Empty;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public DrofusService(string host, string username, string password)
        {
            _username = username;
            _password = password;

            // Normalise host: strip trailing slash, ensure https
            host = host.TrimEnd('/');
            if (!host.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                host = $"https://{host}";

            _http = new HttpClient { BaseAddress = new Uri(host + "/") };
            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // ── Authentication ──────────────────────────────────────────────────

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                await AuthenticateAsync();
                return !string.IsNullOrEmpty(_token);
            }
            catch
            {
                return false;
            }
        }

        private async Task AuthenticateAsync()
        {
            if (!string.IsNullOrEmpty(_token))
                return;

            var payload = JsonSerializer.Serialize(new
            {
                username = _username,
                password = _password
            });

            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync("api/login", content);
            response.EnsureSuccessStatusCode();

            string body = await response.Content.ReadAsStringAsync();
            var login = JsonSerializer.Deserialize<DrofusLoginResponse>(body, JsonOpts)
                        ?? throw new InvalidOperationException("Empty login response.");

            _token = login.Token;
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);
        }

        // ── Projects ────────────────────────────────────────────────────────

        public async Task<List<DrofusProject>> GetProjectsAsync()
        {
            await AuthenticateAsync();
            return await GetListAsync<DrofusProject>("api/projects");
        }

        // ── Rooms ───────────────────────────────────────────────────────────

        public async Task<List<DrofusRoom>> GetRoomsAsync(string projectId)
        {
            await AuthenticateAsync();
            return await GetListAsync<DrofusRoom>($"api/projects/{projectId}/rooms");
        }

        // ── Items ───────────────────────────────────────────────────────────

        public async Task<List<DrofusItem>> GetItemsAsync(string projectId)
        {
            await AuthenticateAsync();
            return await GetListAsync<DrofusItem>($"api/projects/{projectId}/items");
        }

        public async Task<List<DrofusItem>> GetItemsByRoomAsync(string projectId, string roomId)
        {
            await AuthenticateAsync();
            return await GetListAsync<DrofusItem>($"api/projects/{projectId}/rooms/{roomId}/items");
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private async Task<List<T>> GetListAsync<T>(string endpoint)
        {
            using var response = await _http.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            string body = await response.Content.ReadAsStringAsync();

            // Try paged response first
            try
            {
                var paged = JsonSerializer.Deserialize<DrofusPagedResponse<T>>(body, JsonOpts);
                if (paged?.Data is not null)
                    return paged.Data;
            }
            catch { /* fall through to plain list */ }

            return JsonSerializer.Deserialize<List<T>>(body, JsonOpts) ?? [];
        }

        public void Dispose() => _http.Dispose();
    }
}
