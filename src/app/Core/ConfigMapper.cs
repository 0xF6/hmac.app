namespace hmac.Core
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.JSInterop;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class HMACProcessor
    {
        private readonly IJSRuntime _js;
        private string _secret;
        private OutputType _outputType = OutputType.Base64;
        private AdapterType _adapterType = AdapterType.SHA512;

        private JsonSerializerSettings serializerSettings { get; set; }
            = new JsonSerializerSettings();

        public HMACProcessor(IJSRuntime js, ILogger<HMACProcessor> logger)
        {
            _js = js;
            _logger = logger;
        }

        private bool skipEmptyFields { get; set; }


        public HMACProcessor WithSecret(string secret)
        {
            _secret = secret;
            return this;
        }

        public HMACProcessor WithOutput(OutputType type)
        {
            this._outputType = type;
            return this;
        }
        public HMACProcessor WithAdapter(AdapterType type)
        {
            this._adapterType = type;
            return this;
        }

        public HMACProcessor IgnoreNullValueHandling(bool ignore)
        {
            serializerSettings.NullValueHandling =
                ignore ?
                    NullValueHandling.Ignore :
                    NullValueHandling.Include;
            return this;
        }

        public HMACProcessor SkipEmptyFields(bool ignore)
        {
            skipEmptyFields = ignore;
            return this;
        }


        public string CreateSignString<T>(T body) where T : class
        {
            var signString = default(string);
            if (body is string str)
                signString = ToSignString(str);
            else
                signString = ToSignString(JsonConvert.SerializeObject(body, serializerSettings));
            return signString;
        }

        public Task<string> CreateSignForRaw(string body)
            => this._computeHash(body, _secret);

        public Task<string> CreateHash<T>(T body) where T : class
        {
            if (string.IsNullOrEmpty(_secret))
                throw new ArgumentException($"secret is null, use {nameof(WithSecret)} to set secret.");
            _logger.LogInformation($"Create hash by {typeof(T).FullName}");
            var signString = CreateSignString(body);
            var hash = this._computeHash(signString, _secret);
           _logger.LogInformation($"Created hash with string '{signString}', result: '{hash}'");
            OnSignConstruct?.Invoke(signString);
            return hash;
        }

        public delegate void SignStringConstruct(string hashString);

        public event SignStringConstruct OnSignConstruct;

        private async Task<string> _computeHash(string text, string secretKey)
        {
            var bs64 = await _js.InvokeAsync<string>("castHMAC", $"{_adapterType}", text, secretKey);
            switch (this._outputType)
            {
                case OutputType.Base64:
                    return bs64;
                case OutputType.Hex:
                    return Convert.FromBase64String(bs64).Aggregate(string.Empty, (s, e) => $"{s}{e:x2}", s => s);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // standard format 'zzz' has place 00:00, but need 0000
        // iso8601
        private string FormatDate { get; set; } = "yyyy-MM-ddTHH:mm:ss+0000";
        private ILogger<HMACProcessor> _logger { get; }

        private string ToSignString(string body)
        {
            Dictionary<string, object> DeserializeAndFlatten(string json)
            {
                var dict = new Dictionary<string, object>();
                FillDictionaryFromJToken(dict, JToken.Parse(json));
                return dict;
            }
            // Cast KeyValue pair to string line
            string Selector(KeyValuePair<string, object> x)
            {
                if (x.Value is JObject obj)
                    return string.Join("", obj.ToObject<Dictionary<string, object>>().Select(Selector));

                if (x.Value is DateTime d2)
                {
                    var format = "yyyy-MM-ddTHH:mm:ss+0000"; 
                    var dd = d2.ToUniversalTime();
                    return $"{x.Key}:{dd.ToString(format)};";
                }
                if (x.Value is bool b)
                    return $"{x.Key}:{(b ? "1" : "0")};";
                return $"{x.Key}:{Convert.ToString(x.Value, CultureInfo.InvariantCulture)};";
            }

            // skip signature key
            var flatten = DeserializeAndFlatten(body)
                .Where(x => !x.Key.Contains("signature"));

            if (skipEmptyFields)
                flatten = flatten.Where(x => x.Value != null)
                    .Where(x => !string.IsNullOrEmpty($"{x.Value}"));
            
            // ordering and return
            return string.Join("", flatten.OrderBy(x => x.Key).ToArray().Select(Selector)).TrimEnd(';');
        }

        private void FillDictionaryFromJToken(Dictionary<string, object> dict, JToken token, string prefix = "")
        {
            string Join(string p, string name)
                => (string.IsNullOrEmpty(p) ? name : prefix + ":" + name);
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var prop in token.Children<JProperty>())
                        FillDictionaryFromJToken(dict, prop.Value, Join(prefix, prop.Name));
                    break;

                case JTokenType.Array:
                    var index = 0;
                    foreach (var value in token.Children())
                    {
                        FillDictionaryFromJToken(dict, value, Join(prefix, index.ToString()));
                        index++;
                    }
                    break;

                default:
                    dict.Add(prefix, ((JValue)token).Value);
                    break;
            }
        }
    }
    public class ConfigMapper
    {
        private static readonly Dictionary<string, string> Map = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> ReverseMap = new Dictionary<string, string>();
        static ConfigMapper()
        {
            Map.Add("null", "Z");
            Map.Add("True", "H");
            Map.Add("False", "He");

            Map.Add("Adapter", "Cr");
            Map.Add("OutputType", "Al");
            Map.Add("CastBoolToInt", "Se");
            Map.Add("SkipEmptyFields", "Cu");
            Map.Add("IgnoreNullValue", "Zr");

            Map.Add("SHA512", "La");
            Map.Add("SHA384", "Pr");
            Map.Add("SHA256", "Eu");
            Map.Add("SHA1", "Nh");

            Map.Add("Base64", "Rb");
            Map.Add("Hex", "Ca");

            ReverseMap = Map.ToDictionary(x => x.Value, x => x.Key);

        }
        public static string MapToString(Config config)
        {
            var fields = typeof(Config).GetProperties().Select(x => x.Name);
            var dicOfKeys = fields.ToDictionary(x => x, x => "");

            var builder = new StringBuilder();
            foreach (var (key, value) in dicOfKeys)
            {
                var vl = typeof(Config).GetProperty(key)?.GetValue(config).ToString() ?? "null";
                builder.Append($"({Map[key]},{Map[vl]}):");
            }

            return builder.ToString().TrimEnd(':');
        }

        public static Config FromString(string str)
        {
            var config = new Config();
            var type = typeof(Config);
            foreach (var s in str.Split(':'))
            {
                var result = s.Trim(')').Trim('(').Split(',');

                var key = result.First();
                var value = result.Last();
                var prop = type.GetProperty(ReverseMap[key]);
                if (prop.PropertyType.IsEnum)
                    prop.SetValue(config, Enum.Parse(prop.PropertyType, ReverseMap[value]));
                if (prop.PropertyType == typeof(bool))
                    prop.SetValue(config, bool.Parse(ReverseMap[value]));
            }
            return config;
        }
    }

    public enum AdapterType : byte
    {
        SHA512,
        SHA384,
        SHA256,
        SHA1
    }
    public enum OutputType : byte
    {
        Base64,
        Hex
    }

    public class Config
    {
        public AdapterType Adapter { get; set; }
        public OutputType OutputType { get; set; }
        public bool CastBoolToInt { get; set; }
        public bool SkipEmptyFields { get; set; }
        public bool IgnoreNullValue { get; set; }
    }
}