namespace AutoDealerSphere.Shared.Models
{
    public class EmailSettingsResponse
    {
        public EmailSettings Settings { get; set; } = new EmailSettings();
        public string PlainPassword { get; set; } = "";
    }
}
