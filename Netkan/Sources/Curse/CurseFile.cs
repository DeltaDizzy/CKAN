using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CKAN.Versioning;

namespace CKAN.NetKAN.Sources.Curse
{
    public class CurseFile
    {
        [JsonConverter(typeof(JsonConvertGameVersion))]
        [JsonProperty] public GameVersion? version;

        [JsonConverter(typeof(JsonConvertGameVersion))]
        [JsonProperty] public GameVersion[]? versions;

        [JsonProperty] public string name = "";
        [JsonProperty] public string? type;
        [JsonProperty] public int id;
        [JsonProperty] public DateTime uploaded_at;
        [JsonProperty] public string? url;

        private string? _downloadUrl;
        private string? _filename;
        private string? _fileVersion;
        public  string ModPageUrl = "";

        /// <summary>
        /// Returns the direct path to the file
        /// </summary>
        /// <returns>
        /// The download URL
        /// </returns>
        public string GetDownloadUrl(string? userAgent)
        {
            _downloadUrl ??= Net.ResolveRedirect(new Uri(url + "/file"), userAgent)
                                ?.ToString();
            if (_downloadUrl == null)
            {
                throw new Kraken($"Too many redirects resolving: {url}/file");
            }
            return _downloadUrl;
        }

        /// <summary>
        /// Returns the Curse Id version of the file
        /// </summary>
        /// <returns>The Curse Id version</returns>
        public string GetCurseIdVersion()
            => "0curse" + id;

        /// <summary>
        /// Returns the filename of the file
        /// </summary>
        /// <returns>The filename</returns>
        public string GetFilename(string? userAgent)
        {
            if (_filename == null)
            {
                Match match = Regex.Match(GetDownloadUrl(userAgent), "[^/]*\\.zip");
                _filename = //match.Groups is [var grp, ..]
                            match.Groups.Count > 0
                            && match.Groups[0] is var grp
                                ? grp.Value
                                : GetCurseIdVersion();
            }
            return _filename;
        }

        /// <summary>
        /// Returns the version of the file
        /// </summary>
        /// <returns>The version</returns>
        public string GetFileVersion(string? userAgent)
        {
            if (_fileVersion == null)
            {
                Match match = Regex.Match(GetDownloadUrl(userAgent), "(v?[0-9][0-9a-z.]*[0-9a-z])[^0-9]*\\.zip");
                if (//match.Groups is [_, var grp, ..]
                    match.Groups.Count > 1
                    && match.Groups[1] is var grp)
                {
                    _fileVersion = grp.Value;
                }
                else
                {
                    _fileVersion = GetCurseIdVersion();
                }
            }
            return _fileVersion;
        }

        /// <summary>
        /// Curse has versions that don't play nicely with CKAN, for example "1.1-prerelease".
        /// This transformer strips out the dash and anything after it.
        /// </summary>
        internal class JsonConvertGameVersion : JsonConverter
        {
            public override object? ReadJson(JsonReader     reader,
                                             Type           objectType,
                                             object?        existingValue,
                                             JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    return JArray.Load(reader)
                        .Values<string>()
                        .OfType<string>()
                        .Select(v => GameVersion.Parse(Regex.Replace(v, @"-.*$", "")))
                        .ToArray();
                }
                else
                {
                    if (reader.Value == null)
                    {
                        return null;
                    }

                    var raw_version = reader.Value?.ToString();
                    return GameVersion.Parse(Regex.Replace(raw_version ?? "", @"-.*$", ""));
                }
            }

            public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override bool CanConvert(Type objectType)
            {
                throw new NotImplementedException();
            }
        }

    }
}
