namespace Functions.Models
{
    public class AppSettings
    {
        public string TOTPIssuer { get; set; }
        public string TOTPAccountPrefix { get; set; }
        public int TOTPTimestep { get; set; }
        public string EncryptionKey { get; set; }
        public string SaltKey { get; set; }
    }
}
