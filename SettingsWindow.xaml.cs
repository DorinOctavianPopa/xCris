using System.Windows;
using xCris.Models;

namespace xCris
{
    public partial class SettingsWindow : Window
    {
        public BrowserSettings SavedSettings { get; private set; }

        public SettingsWindow(BrowserSettings current)
        {
            InitializeComponent();
            SavedSettings = current.Clone();
            PopulateControls(current);
        }

        private void PopulateControls(BrowserSettings s)
        {
            TxtHomePageUrl.Text = s.HomePageUrl;

            ChkIsScriptEnabled.IsChecked                 = s.IsScriptEnabled;
            ChkAreDefaultScriptDialogsEnabled.IsChecked  = s.AreDefaultScriptDialogsEnabled;
            ChkIsWebMessageEnabled.IsChecked             = s.IsWebMessageEnabled;
            ChkIsPasswordAutosaveEnabled.IsChecked       = s.IsPasswordAutosaveEnabled;
            ChkIsGeneralAutofillEnabled.IsChecked        = s.IsGeneralAutofillEnabled;

            ChkIsStatusBarEnabled.IsChecked              = s.IsStatusBarEnabled;
            ChkIsZoomControlEnabled.IsChecked            = s.IsZoomControlEnabled;
            ChkIsBuiltInErrorPageEnabled.IsChecked       = s.IsBuiltInErrorPageEnabled;
            TxtDefaultZoom.Text = ((int)Math.Round(s.DefaultZoomFactor * 100)).ToString();

            ChkAreDevToolsEnabled.IsChecked              = s.AreDevToolsEnabled;
            ChkAreDefaultContextMenusEnabled.IsChecked   = s.AreDefaultContextMenusEnabled;
            ChkAreBrowserAcceleratorKeysEnabled.IsChecked = s.AreBrowserAcceleratorKeysEnabled;

            TxtUserAgent.Text = s.UserAgent;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtDefaultZoom.Text.Trim(), out var zoomPct) || zoomPct < 25 || zoomPct > 500)
            {
                MessageBox.Show(this,
                    "Default zoom must be a whole number between 25 and 500.",
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtDefaultZoom.Focus();
                return;
            }

            SavedSettings = new BrowserSettings
            {
                HomePageUrl = TxtHomePageUrl.Text.Trim(),

                IsScriptEnabled                 = ChkIsScriptEnabled.IsChecked == true,
                AreDefaultScriptDialogsEnabled  = ChkAreDefaultScriptDialogsEnabled.IsChecked == true,
                IsWebMessageEnabled             = ChkIsWebMessageEnabled.IsChecked == true,
                IsPasswordAutosaveEnabled       = ChkIsPasswordAutosaveEnabled.IsChecked == true,
                IsGeneralAutofillEnabled        = ChkIsGeneralAutofillEnabled.IsChecked == true,

                IsStatusBarEnabled              = ChkIsStatusBarEnabled.IsChecked == true,
                IsZoomControlEnabled            = ChkIsZoomControlEnabled.IsChecked == true,
                IsBuiltInErrorPageEnabled       = ChkIsBuiltInErrorPageEnabled.IsChecked == true,
                DefaultZoomFactor               = zoomPct / 100.0,

                AreDevToolsEnabled              = ChkAreDevToolsEnabled.IsChecked == true,
                AreDefaultContextMenusEnabled   = ChkAreDefaultContextMenusEnabled.IsChecked == true,
                AreBrowserAcceleratorKeysEnabled = ChkAreBrowserAcceleratorKeysEnabled.IsChecked == true,

                UserAgent = TxtUserAgent.Text.Trim()
            };

            DialogResult = true;
            Close();
        }
    }
}
