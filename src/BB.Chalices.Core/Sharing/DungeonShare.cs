using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using BB.Chalices.Core.Binary;

namespace BB.Chalices.Core.Sharing;

// One dungeon to be shared: a display name, an optional category tag and the raw
// 125-byte record. Bytes must be exactly DungeonStructure.Size long.
public sealed record ShareItem(string Name, string? Category, byte[] Bytes);

// A versioned set of dungeons packed together for sharing (one dungeon, a whole
// altar, or a whole list are all just a set).
public sealed record ShareSet(int Version, IReadOnlyList<ShareItem> Items);

// Encodes and decodes shareable dungeon sets. A code is JSON -> deflate ->
// base64url with a short prefix; decoding also accepts a raw single-dungeon hex
// string and a legacy whole-altar hex blob for backward compatibility.
public static class DungeonShare
{
    public const string CodePrefix = "BBD1-";
    public const int CurrentVersion = 1;

    // Compact on-the-wire shape: v = version, i = items; per item n = name,
    // c = category (optional), h = 125-byte record as hex.
    private sealed class Dto
    {
        [JsonPropertyName("v")] public int V { get; set; }
        [JsonPropertyName("i")] public List<ItemDto> I { get; set; } = new();
    }

    private sealed class ItemDto
    {
        [JsonPropertyName("n")] public string N { get; set; } = "";
        [JsonPropertyName("c")] public string? C { get; set; }
        [JsonPropertyName("h")] public string H { get; set; } = "";
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Encode(ShareSet set)
    {
        var dto = new Dto { V = set.Version };
        foreach (var item in set.Items)
            dto.I.Add(new ItemDto
            {
                N = item.Name,
                C = item.Category,
                H = Convert.ToHexString(item.Bytes),
            });

        byte[] json = JsonSerializer.SerializeToUtf8Bytes(dto, JsonOptions);

        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
            deflate.Write(json, 0, json.Length);

        return CodePrefix + ToBase64Url(output.ToArray());
    }

    // Strips everything that is not a hex digit, so pasted codes survive stray
    // whitespace, newlines or separators. Shared by every hex paste/import path.
    public static string CompactHex(string? input) =>
        input is null ? string.Empty : new string(input.Where(Uri.IsHexDigit).ToArray());

    public static bool TryDecode(string? input, out ShareSet set)
    {
        set = new ShareSet(CurrentVersion, Array.Empty<ShareItem>());
        if (string.IsNullOrWhiteSpace(input))
            return false;

        string trimmed = input.Trim();
        if (trimmed.StartsWith(CodePrefix, StringComparison.Ordinal))
            return TryDecodeCode(trimmed[CodePrefix.Length..], out set);

        // Legacy import: a raw hex string of one or more whole 125-byte records.
        string compact = CompactHex(trimmed);
        if (compact.Length == 0 || compact.Length % (DungeonStructure.Size * 2) != 0)
            return false;

        byte[] all;
        try { all = Convert.FromHexString(compact); }
        catch (FormatException) { return false; }

        var legacy = new List<ShareItem>();
        for (int offset = 0; offset < all.Length; offset += DungeonStructure.Size)
            legacy.Add(new ShareItem("", null, all[offset..(offset + DungeonStructure.Size)]));

        set = new ShareSet(CurrentVersion, legacy);
        return true;
    }

    private static bool TryDecodeCode(string payload, out ShareSet set)
    {
        set = new ShareSet(CurrentVersion, Array.Empty<ShareItem>());
        try
        {
            byte[] deflated = FromBase64Url(payload);
            using var input = new MemoryStream(deflated);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            deflate.CopyTo(output);

            var dto = JsonSerializer.Deserialize<Dto>(output.ToArray(), JsonOptions);
            if (dto is null || dto.I.Count == 0)
                return false;

            var items = new List<ShareItem>(dto.I.Count);
            foreach (var raw in dto.I)
            {
                if (string.IsNullOrEmpty(raw.H))
                    return false;
                byte[] bytes = Convert.FromHexString(raw.H);
                if (bytes.Length != DungeonStructure.Size)
                    return false;
                items.Add(new ShareItem(raw.N ?? "", string.IsNullOrEmpty(raw.C) ? null : raw.C, bytes));
            }

            set = new ShareSet(dto.V == 0 ? CurrentVersion : dto.V, items);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidDataException or JsonException or ArgumentException)
        {
            return false;
        }
    }

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string value)
    {
        string s = value.Replace('-', '+').Replace('_', '/');
        s = (s.Length % 4) switch { 2 => s + "==", 3 => s + "=", _ => s };
        return Convert.FromBase64String(s);
    }
}
