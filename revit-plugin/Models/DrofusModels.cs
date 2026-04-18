using System.Text.Json.Serialization;

namespace SuperpowersRevit.Models
{
    // ── Project ───────────────────────────────────────────────────────────────

    public sealed class DrofusProject
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }

    // ── Room ──────────────────────────────────────────────────────────────────

    public sealed class DrofusRoom
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("number")]
        public string Number { get; set; } = string.Empty;

        [JsonPropertyName("area")]
        public double Area { get; set; }

        [JsonPropertyName("function")]
        public string Function { get; set; } = string.Empty;

        [JsonPropertyName("level")]
        public string Level { get; set; } = string.Empty;

        /// <summary>
        /// Arbitrary key-value attributes that may map to shared/project parameters in Revit.
        /// </summary>
        [JsonPropertyName("attributes")]
        public Dictionary<string, string> Attributes { get; set; } = [];
    }

    // ── Item ──────────────────────────────────────────────────────────────────

    public sealed class DrofusItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("number")]
        public string Number { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("roomId")]
        public string RoomId { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("attributes")]
        public Dictionary<string, string> Attributes { get; set; } = [];
    }

    // ── Auth response ─────────────────────────────────────────────────────────

    public sealed class DrofusLoginResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("expires")]
        public string Expires { get; set; } = string.Empty;
    }

    // ── Paged list wrapper ────────────────────────────────────────────────────

    public sealed class DrofusPagedResponse<T>
    {
        [JsonPropertyName("data")]
        public List<T> Data { get; set; } = [];

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }
    }
}
