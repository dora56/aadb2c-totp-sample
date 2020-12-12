using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Functions.Models
{
    public class B2CResponseModel
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }
        [JsonPropertyName("status")]
        public int Status { get; set; }
        [JsonPropertyName("userMessage")]
        public string UserMessage { get; set; }

        // Optional claims
        [JsonPropertyName("qrCodeBitmap")]
        public string QrCodeBitmap { get; set; }
        [JsonPropertyName("secretKey")]
        public string SecretKey { get; set; }
        [JsonPropertyName("timeStepMatched")]
        public string TimeStepMatched { get; set; }

        public B2CResponseModel(string message, HttpStatusCode status)
        {
            this.UserMessage = message;
            this.Status = (int)status;
            this.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
    }
}
