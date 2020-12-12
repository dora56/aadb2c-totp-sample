using System.Text.Json;
using System.Text.Json.Serialization;

namespace Functions.Models
{
    public class InputClaimsModel
    {
        [JsonPropertyName("userName")]
        public string UserName { get; set; }
        [JsonPropertyName("secretKey")]
        public string SecretKey { get; set; }
        [JsonPropertyName("totpCode")]
        public string TotpCode { get; set; }
        [JsonPropertyName("timeStepMatched")]
        public string TimeStepMatched { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

        public static InputClaimsModel Parse(string json)
        {
            return JsonSerializer.Deserialize<InputClaimsModel>(json);
        }
    }
}
