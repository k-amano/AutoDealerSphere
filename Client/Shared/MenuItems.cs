namespace AutoDealerSphere.Client.Shared
{
    public class MenuItems
    {
        public string Text { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string IconCss { get; set; } = string.Empty;
        public List<MenuItems>? Items { get; set; }
    }
}
