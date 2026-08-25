namespace xCris.Models
{
    public class BrowserSettings
    {
        // ── General ───────────────────────────────────────────────────────────
        public string HomePageUrl { get; set; } = "https://www.google.com";

        // ── Privacy & Security ────────────────────────────────────────────────
        public bool IsScriptEnabled { get; set; } = true;
        public bool AreDefaultScriptDialogsEnabled { get; set; } = true;
        public bool IsWebMessageEnabled { get; set; } = true;
        public bool IsPasswordAutosaveEnabled { get; set; } = false;
        public bool IsGeneralAutofillEnabled { get; set; } = true;

        // ── Appearance ────────────────────────────────────────────────────────
        public bool IsStatusBarEnabled { get; set; } = true;
        public bool IsZoomControlEnabled { get; set; } = true;
        public bool IsBuiltInErrorPageEnabled { get; set; } = true;
        public double DefaultZoomFactor { get; set; } = 1.0;

        // ── Developer ─────────────────────────────────────────────────────────
        public bool AreDevToolsEnabled { get; set; } = true;
        public bool AreDefaultContextMenusEnabled { get; set; } = true;
        public bool AreBrowserAcceleratorKeysEnabled { get; set; } = true;

        // ── Advanced ──────────────────────────────────────────────────────────
        public string UserAgent { get; set; } = string.Empty;

        public BrowserSettings Clone() => (BrowserSettings)MemberwiseClone();
    }
}
