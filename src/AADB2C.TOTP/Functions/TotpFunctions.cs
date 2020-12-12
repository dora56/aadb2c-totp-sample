using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Azure.WebJobs.Extensions.HttpApi;
using Functions.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OtpNet;
using QRCoder;

namespace Functions
{
    public class TotpFunctions: HttpFunctionBase
    {
        private readonly AppSettings _options;
        public TotpFunctions(
            IHttpContextAccessor httpContextAccessor,
            IOptions<AppSettings> options) : base(httpContextAccessor)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        [FunctionName("Generate")]
        public async Task<IActionResult> GenerateAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "totp/generate")]
            HttpRequest request, ILogger log)
        {
            log.LogInformation("Start QrCode Generate Function.");

            if (request.Body is null)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("Request content is null", HttpStatusCode.Conflict));
            }

            var input = await new StreamReader(request.Body, Encoding.UTF8).ReadToEndAsync();
            if (string.IsNullOrEmpty(input))
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("Request content is empty", HttpStatusCode.Conflict));
            }

            var inputClaims = InputClaimsModel.Parse(input);
            if (inputClaims == null)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("Can not deserialize input claims", HttpStatusCode.Conflict));
            }

            try
            {
                var secretKey = KeyGeneration.GenerateRandomKey(20);

                var TOTPUrl = GetTotpUrl(secretKey
                    , inputClaims.UserName
                    , _options.TOTPIssuer
                    , _options.TOTPTimestep
                    , _options.TOTPAccountPrefix);

                // Generate QR code for the above URL
                var qrCodeGenerator = new QRCodeGenerator();
                var qrCodeData = qrCodeGenerator.CreateQrCode(TOTPUrl, QRCodeGenerator.ECCLevel.L);
                var qrCode = new BitmapByteQRCode(qrCodeData);
                var qrCodeBitmap = qrCode.GetGraphic(4);

                var output = new B2CResponseModel(string.Empty, HttpStatusCode.OK)
                {
                    QrCodeBitmap = Convert.ToBase64String(qrCodeBitmap),
                    SecretKey = EncryptAndBase64(Convert.ToBase64String(secretKey))
                };

                return Ok(output);
            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel($"General error (REST API): {e.Message}", HttpStatusCode.Conflict));
            }
        }

        [FunctionName("Verify")]
        public async Task<IActionResult> VerifyAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "totp/verify")]
            HttpRequest request, ILogger log)
        {
            log.LogInformation("Start code Verify Function.");
            if (request.Body is null)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("Request content is null", HttpStatusCode.Conflict));
            }

            var input = await new StreamReader(request.Body, Encoding.UTF8).ReadToEndAsync();
            if (string.IsNullOrEmpty(input))
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("Request content is empty", HttpStatusCode.Conflict));
            }

            var inputClaims = InputClaimsModel.Parse(input);
            if (inputClaims == null)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("Can not deserialize input claims", HttpStatusCode.Conflict));
            }

            try
            {
                var secretKey = Convert.FromBase64String(DecryptAndBase64(inputClaims.SecretKey));

                var totp = new Totp(secretKey);

                // Verify the TOTP code provided by the users
                var verificationResult = totp.VerifyTotp(
                    inputClaims.TotpCode,
                    out var timeStepMatched,
                    VerificationWindow.RfcSpecifiedNetworkDelay);

                if (!verificationResult)
                {
                    return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("The verification code is invalid.", HttpStatusCode.Conflict));
                }
                // Using the input claim 'timeStepMatched', we check whether the verification code has already been used.
                // For sign-up, the 'timeStepMatched' input claim is null and should not be evaluated
                // For sign-in, the 'timeStepMatched' input claim contains the last time a code matched (from the user profile), and if equal to
                // the last time matched from the verify totp step, we know this code has already been used and can reject
                if (!string.IsNullOrEmpty(inputClaims.TimeStepMatched) && (inputClaims.TimeStepMatched).Equals(timeStepMatched.ToString()))
                {
                    return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("The verification code has already been used.", HttpStatusCode.Conflict));

                }

                var output = new B2CResponseModel(string.Empty, HttpStatusCode.OK)
                {
                    TimeStepMatched = timeStepMatched.ToString()
                };

                return Ok(output);

            }
            catch (Exception e)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel($"General error (REST API): {e.Message}", HttpStatusCode.Conflict));
            }
        }

        private string EncryptAndBase64(string encryptString)
        {
            var encryptionKey = _options.EncryptionKey;
            var clearBytes = Encoding.Unicode.GetBytes(encryptString);
            var salt = Encoding.Unicode.GetBytes(_options.SaltKey);

            using (var encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(encryptionKey, salt);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    encryptString = Convert.ToBase64String(ms.ToArray());
                }
            }
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(encryptString));
        }

        private string DecryptAndBase64(string cipherText)
        {
            // Base64 decode
            cipherText = Encoding.UTF8.GetString(Convert.FromBase64String(cipherText));

            var encryptionKey = _options.EncryptionKey;
            var salt = Encoding.Unicode.GetBytes(_options.SaltKey);

            cipherText = cipherText.Replace(" ", "+");
            var cipherBytes = Convert.FromBase64String(cipherText);
            using (var encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(encryptionKey, salt);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }

        private static string GetTotpUrl(byte[] key, string userName, string issuer, int timestep = 30, string prefix = null)
        {
            // if no prefix, we use the issuer
            prefix ??= issuer;

            // Escape any space, custom characters
            prefix = Uri.EscapeDataString(prefix);
            issuer = Uri.EscapeDataString(issuer);

            // Encode the key
            var secret = Base32Encoding.ToString(key);

            return $"otpauth://totp/{prefix}:{userName}?secret={secret}&period={timestep}&issuer={issuer}";
        }
    }
}
