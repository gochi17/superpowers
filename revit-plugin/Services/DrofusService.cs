using SuperpowersRevit.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SuperpowersRevit.Services
{
    /// <summary>
    /// Thin REST client for the Drofus API.
    /// Uses only built-in .NET 8 types: HttpClient and System.Text.Json.
    ///
    /// Authentication flow:
    ///   POST {host}/api/login  →  receives a Bearer token
    ///   All subsequent calls attach: Authorization: Bearer {token}
    ///
    /// Blocking callers (Revit commands) should call methods via
    /// .GetAwaiter().GetResult() — ConfigureAwait(false) on every await
    /// inside this service prevents deadlocks when the calling context
    /// has a synchronisation context (e.g. WPF dispatcher).
    /// </summary>
    public sealed class DrofusService : IDisposable
    {
        private readonly HttpClient _http;
        private readonly string    _username;
        private readonly string    _password;
        private string             _token = string.Empty;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public DrofusService(string host, string username, string password)
        {
            _username = username;
            _password = password;

            // Normalise host — ensure scheme and strip trailing slash
            host = host.Trim().TrimEnd('/');
            if (!host.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                host = "https://" + host;

            _http = new HttpClient
            {
                BaseAddress = new Uri(host + "/"),
                Timeout     = TimeSpan.FromSeconds(30)
            };
            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // ── Connection test ──────────────────────────────────────────────────

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                await AuthenticateAsync().ConfigureAwait(false);
                return !string.IsNullOrEmpty(_token);
            }
            catch
            {
                return false;
            }
        }

        // ── Projects ─────────────────────────────────────────────────────────

        public async Task<List<DrofusProject>> GetProjectsAsync()
        {
            await AuthenticateAsync().ConfigureAwait(false);
            return await GetListAsync<DrofusProject>("api/projects").ConfigureAwait(false);
        }

        // ── Rooms ────────────────────────────────────────────────────────────

        public async Task<List<DrofusRoom>> GetRoomsAsync(string projectId)
        {
            await AuthenticateAsync().ConfigureAwait(false);
            return await GetListAsync<DrofusRoom>($"api/projects/{projectId}/rooms")
                             .ConfigureAwait(false);
        }

        // ── Items ────────────────────────────────────────────────────────────

        public async Task<List<DrofusItem>> GetItemsAsync(string projectId)
        {
            await AuthenticateAsync().ConfigureAwait(false);
            return await GetListAsync<DrofusItem>($"api/projects/{projectId}/items")
                             .ConfigureAwait(false);
        }

        public async Task<List<DrofusItem>> GetItemsByRoomAsync(string projectId, string roomId)
        {
            await AuthenticateAsync().ConfigureAwait(false);
            return await GetListAsync<DrofusItem>(
                             $"api/projects/{projectId}/rooms/{roomId}/items")
                             .ConfigureAwait(false);
        }

        // ── Authentication (lazy, cached) ────────────────────────────────────

        private async Task AuthenticateAsync()
        {
            if (!string.IsNullOrEmpty(_token))
                return;

            string payload = JsonSerializer.Serialize(new
            {
                username = _username,
                password = _password
            });

            using var content  = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync("api/login", content)
                                           .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            DrofusLoginResponse login =
                JsonSerializer.Deserialize<DrofusLoginResponse>(body, JsonOptions)
                ?? throw new InvalidOperationException("Drofus login returned an empty response.");

            if (string.IsNullOrEmpty(login.Token))
                throw new InvalidOperationException("Drofus login did not return a token.");

            _token = login.Token;
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _token);
        }

        // ── Generic GET helper ───────────────────────────────────────────────

        private async Task<List<T>> GetListAsync<T>(string endpoint)
        {
            using var response = await _http.GetAsync(endpoint).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // Drofus may return either a plain array or a paged wrapper object
            try
            {
                DrofusPagedResponse<T>? paged =
                    JsonSerializer.Deserialize<DrofusPagedResponse<T>>(body, JsonOptions);
                if (paged?.Data is { Count: > 0 })
                    return paged.Data;
            }
            catch (JsonException) { /* not a paged response — fall through */ }

            return JsonSerializer.Deserialize<List<T>>(body, JsonOptions) ?? [];
        }

        public void Dispose() => _http.Dispose();
    }
}
