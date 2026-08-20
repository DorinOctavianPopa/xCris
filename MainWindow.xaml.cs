using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using xCris.Models;

namespace xCris
{
    public partial class MainWindow : Window
    {
        // ── State ──────────────────────────────────────────────────────────────
        private const string HomeUrl = "https://www.google.com";
        private bool _sidePanelVisible = true;
        private double _sidePanelWidth = 420;
        private int _eventCount;
        private bool _webViewReady;
        private readonly ObservableCollection<DomElement> _elements = new();
        private readonly ObservableCollection<PageEvent> _pageEvents = new();
        private readonly ObservableCollection<AutomationBinding> _automationBindings = new();
        private DomElement? _selectedElement;

        // Tracks tabs: key = button, value = (url, title)
        private readonly List<TabEntry> _tabs = new();
        private int _activeTabIndex = -1;

        private record TabEntry(Button Button, string Title, string Url);
        private sealed record AutomationTrigger(
            string BindingName,
            string EventType,
            string TargetSelector,
            string PropertyName,
            string MatchedValue,
            string ActionType,
            string ActionTarget,
            string ActionArgument,
            string Text,
            string Href);

        // ── Constructor ────────────────────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();
            LvElements.ItemsSource = _elements;
            LvEvents.ItemsSource = _pageEvents;
            LoadAutomationBindings();

            // Keyboard shortcuts
            KeyDown += MainWindow_KeyDown;

            Loaded += async (_, _) => await InitWebViewAsync();
        }

        // ── WebView initialisation ─────────────────────────────────────────────
        private async Task InitWebViewAsync()
        {
            await WebView.EnsureCoreWebView2Async();
        }

        private void WebView_CoreWebView2InitializationCompleted(object sender,
            CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess) return;

            _webViewReady = true;

            // Wire up the message channel for page→host events
            WebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

            // Create first tab
            AddTab(HomeUrl, "New Tab");
        }

        // ── Navigation events ──────────────────────────────────────────────────
        private void WebView_NavigationStarting(object sender,
            CoreWebView2NavigationStartingEventArgs e)
        {
            TxtAddress.Text = e.Uri;
            LoadingIndicator.Visibility = Visibility.Visible;
            SetStatus($"Navigating to {e.Uri}…");

            TxtSecure.Text = e.Uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? "🔒 Secure" : "⚠ Not Secure";
            TxtSecure.Foreground = e.Uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? (System.Windows.Media.Brush)FindResource("Success")
                : (System.Windows.Media.Brush)FindResource("Warning");
        }

        private async void WebView_NavigationCompleted(object sender,
            CoreWebView2NavigationCompletedEventArgs e)
        {
            LoadingIndicator.Visibility = Visibility.Collapsed;

            var title = WebView.CoreWebView2.DocumentTitle;
            var url   = WebView.Source?.ToString() ?? string.Empty;
            TxtAddress.Text = url;
            SetStatus(e.IsSuccess ? $"Done — {title}" : $"Failed: {e.WebErrorStatus}");

            UpdateActiveTabLabel(string.IsNullOrWhiteSpace(title) ? url : title);

            BtnBack.IsEnabled    = WebView.CoreWebView2.CanGoBack;
            BtnForward.IsEnabled = WebView.CoreWebView2.CanGoForward;

            if (e.IsSuccess)
                await InjectEventListenersAsync();
        }

        // ── Tab management ─────────────────────────────────────────────────────
        private void AddTab(string url, string title)
        {
            var btn = new Button
            {
                Content     = title,
                Style       = (Style)FindResource("NavButton"),
                MinWidth    = 100,
                MaxWidth    = 180,
                Height      = 28,
                Tag         = _tabs.Count,
                ToolTip     = url,
            };
            btn.Click += TabButton_Click;

            var entry = new TabEntry(btn, title, url);
            _tabs.Add(entry);
            TabStrip.Children.Add(btn);

            ActivateTab(_tabs.Count - 1);
        }

        private void ActivateTab(int index)
        {
            if (index < 0 || index >= _tabs.Count) return;

            _activeTabIndex = index;

            // Highlight active tab
            for (int i = 0; i < _tabs.Count; i++)
            {
                var b = _tabs[i].Button;
                b.FontWeight = i == index ? FontWeights.SemiBold : FontWeights.Normal;
                b.Foreground = i == index
                    ? (System.Windows.Media.Brush)FindResource("Accent")
                    : (System.Windows.Media.Brush)FindResource("TextMuted");
            }

            if (_webViewReady)
            {
                var url = _tabs[index].Url;
                NavigateTo(url);
            }
        }

        private void UpdateActiveTabLabel(string title)
        {
            if (_activeTabIndex < 0 || _activeTabIndex >= _tabs.Count) return;
            _tabs[_activeTabIndex].Button.Content = title.Length > 22
                ? title[..22] + "…"
                : title;
        }

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int idx)
                ActivateTab(idx);
        }

        private void BtnNewTab_Click(object sender, RoutedEventArgs e)
            => AddTab(HomeUrl, "New Tab");

        // ── Navigation helpers ─────────────────────────────────────────────────
        private void NavigateTo(string url)
        {
            if (!_webViewReady) return;

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
            {
                url = url.Contains('.') ? $"https://{url}" : $"https://www.google.com/search?q={Uri.EscapeDataString(url)}";
            }

            WebView.CoreWebView2.Navigate(url);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
            => WebView.CoreWebView2?.GoBack();

        private void BtnForward_Click(object sender, RoutedEventArgs e)
            => WebView.CoreWebView2?.GoForward();

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
            => WebView.CoreWebView2?.Reload();

        private void BtnHome_Click(object sender, RoutedEventArgs e)
            => NavigateTo(HomeUrl);

        private void TxtAddress_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                NavigateTo(TxtAddress.Text.Trim());
        }

        // ── Side panel ─────────────────────────────────────────────────────────
        private void BtnTogglePanel_Click(object sender, RoutedEventArgs e)
        {
            _sidePanelVisible = !_sidePanelVisible;
            if (_sidePanelVisible)
            {
                SidePanelColumn.Width = new GridLength(_sidePanelWidth);
            }
            else
            {
                _sidePanelWidth = SidePanelColumn.ActualWidth;
                SidePanelColumn.Width = new GridLength(0);
            }
        }

        private async void BtnAutomationConfig_Click(object sender, RoutedEventArgs e)
        {
            var window = new AutomationConfigWindow(_automationBindings)
            {
                Owner = this
            };

            if (window.ShowDialog() != true)
                return;

            _automationBindings.Clear();
            foreach (var binding in window.SavedBindings)
                _automationBindings.Add(binding.Clone());

            SaveAutomationBindings();
            SetStatus($"Saved {_automationBindings.Count} automation binding(s)");

            if (_webViewReady)
                await InjectEventListenersAsync();
        }

        // ── Keyboard shortcuts ─────────────────────────────────────────────────
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F12)
                BtnTogglePanel_Click(sender, new RoutedEventArgs());
            else if (e.Key == Key.F5)
                BtnRefresh_Click(sender, new RoutedEventArgs());
            else if (e.Key == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
                BtnNewTab_Click(sender, new RoutedEventArgs());
            else if (e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.Control)
                TxtAddress.Focus();
        }

        // ── DOM Explorer ───────────────────────────────────────────────────────
        private void TxtSelector_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BtnQuery_Click(sender, new RoutedEventArgs());
        }

        private async void BtnQuery_Click(object sender, RoutedEventArgs e)
        {
            if (!_webViewReady) return;

            var selector = TxtSelector.Text.Trim();
            if (string.IsNullOrEmpty(selector)) return;

            // Escape selector for safe use in JSON string (only escaping backslash and single-quote)
            var safeSel = selector.Replace("\\", "\\\\").Replace("'", "\\'");
            var js = $@"
(function() {{
    var nodes = document.querySelectorAll('{safeSel}');
    var result = [];
    nodes.forEach(function(n) {{
        result.push({{
            tagName:    n.tagName || '',
            id:         n.id || '',
            className:  n.className || '',
            innerText:  (n.innerText || '').substring(0, 60),
            innerHTML:  (n.innerHTML || '').substring(0, 120),
            selector:   (n.id ? '#' + n.id : (n.className ? '.' + n.className.split(' ').join('.') : n.tagName.toLowerCase()))
        }});
    }});
    return JSON.stringify(result);
}})()";

            var json = await ExecuteScriptSafeAsync(js);
            if (json is null) return;

            // Unwrap outer quotes added by ExecuteScript
            var unescaped = UnwrapJsonString(json);

            _elements.Clear();
            try
            {
                var items = JsonSerializer.Deserialize<List<JsonElement>>(unescaped);
                if (items is null) return;
                foreach (var item in items)
                {
                    _elements.Add(new DomElement
                    {
                        TagName   = item.GetProperty("tagName").GetString()?.ToUpperInvariant() ?? "",
                        Id        = item.GetProperty("id").GetString() ?? "",
                        ClassName = item.GetProperty("className").GetString() ?? "",
                        InnerText = item.GetProperty("innerText").GetString() ?? "",
                        InnerHTML = item.GetProperty("innerHTML").GetString() ?? "",
                        Selector  = item.GetProperty("selector").GetString() ?? ""
                    });
                }
            }
            catch { /* ignore parse errors */ }

            SetStatus($"Found {_elements.Count} element(s) matching '{selector}'");
        }

        private void LvElements_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedElement = LvElements.SelectedItem as DomElement;
            if (_selectedElement is null) return;

            TxtElemSelector.Text = _selectedElement.Selector;
            TxtElemId.Text       = _selectedElement.Id;
            TxtElemClass.Text    = _selectedElement.ClassName;
            TxtElemText.Text     = _selectedElement.InnerText;
            TxtElemHtml.Text     = _selectedElement.InnerHTML;
        }

        private async void BtnApplyElement_Click(object sender, RoutedEventArgs e)
        {
            if (!_webViewReady || _selectedElement is null) return;

            var sel      = _selectedElement.Selector.Replace("\\", "\\\\").Replace("'", "\\'");
            var newId    = TxtElemId.Text.Replace("\\", "\\\\").Replace("'", "\\'");
            var newClass = TxtElemClass.Text.Replace("\\", "\\\\").Replace("'", "\\'");
            var newText  = TxtElemText.Text.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n");
            var newHtml  = TxtElemHtml.Text.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n");

            var js = $@"
(function() {{
    var el = document.querySelector('{sel}');
    if (!el) return 'Element not found';
    el.id = '{newId}';
    el.className = '{newClass}';
    el.innerText = '{newText}';
    el.innerHTML = '{newHtml}';
    return 'Applied';
}})()";

            var result = await ExecuteScriptSafeAsync(js);
            SetStatus($"Apply result: {UnwrapJsonString(result ?? "null")}");
        }

        private async void BtnRemoveElement_Click(object sender, RoutedEventArgs e)
        {
            if (!_webViewReady || _selectedElement is null) return;

            var sel = _selectedElement.Selector.Replace("\\", "\\\\").Replace("'", "\\'");
            var js  = $@"
(function() {{
    var el = document.querySelector('{sel}');
    if (!el) return 'Not found';
    el.parentNode && el.parentNode.removeChild(el);
    return 'Removed';
}})()";

            var result = await ExecuteScriptSafeAsync(js);
            SetStatus($"Remove result: {UnwrapJsonString(result ?? "null")}");
            _elements.Remove(_selectedElement);
            _selectedElement = null;
        }

        private async void BtnRefreshDom_Click(object sender, RoutedEventArgs e)
        {
            await QueryDomAsync(TxtSelector.Text.Trim());
        }

        private async Task QueryDomAsync(string selector)
        {
            TxtSelector.Text = selector;
            await Task.Yield();
            BtnQuery_Click(this, new RoutedEventArgs());
        }

        // ── JavaScript Console ─────────────────────────────────────────────────
        private async void BtnRunJs_Click(object sender, RoutedEventArgs e)
        {
            if (!_webViewReady) return;

            var code   = TxtJsInput.Text;
            var result = await ExecuteScriptSafeAsync(code);
            AppendConsole($"» {code.Split('\n')[0]}\n← {result ?? "null"}\n");
        }

        private void BtnConsoleClear_Click(object sender, RoutedEventArgs e)
        {
            TxtConsoleOutput.Text = string.Empty;
        }

        private void AppendConsole(string text)
        {
            TxtConsoleOutput.AppendText(text);
            TxtConsoleOutput.ScrollToEnd();
        }

        // ── Event bridge ───────────────────────────────────────────────────────
        private async Task InjectEventListenersAsync()
        {
            if (!_webViewReady) return;

            var monitoredEvents = BuildEnabledEventsList();
            var automationBindings = _automationBindings
                .Where(binding => binding.IsEnabled && !string.IsNullOrWhiteSpace(binding.EventType))
                .Select(binding => new
                {
                    name = binding.Name,
                    selector = binding.Selector,
                    eventType = binding.EventType,
                    propertyName = binding.PropertyName,
                    propertyValue = binding.PropertyValue,
                    actionType = binding.ActionType,
                    actionTarget = binding.ActionTarget,
                    actionArgument = binding.ActionArgument
                })
                .ToList();

            var events = monitoredEvents
                .Concat(automationBindings.Select(binding => binding.eventType))
                .Where(evt => !string.IsNullOrWhiteSpace(evt))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var monitoredEventsJson = JsonSerializer.Serialize(monitoredEvents);
            var automationBindingsJson = JsonSerializer.Serialize(automationBindings);
            var eventsJson = JsonSerializer.Serialize(events);
            var js = $@"
(function() {{
    // Console bridge
    if (!window.__xCrisConsoleBridged) {{
        window.__xCrisConsoleBridged = true;
        var orig = {{ log: console.log, warn: console.warn, error: console.error }};
        ['log','warn','error'].forEach(function(lvl) {{
            console[lvl] = function() {{
                var msg = Array.from(arguments).map(function(a){{ return String(a); }}).join(' ');
                window.chrome.webview.postMessage(JSON.stringify({{ type: '__console__', level: lvl, detail: msg, target: '' }}));
                orig[lvl].apply(console, arguments);
            }};
        }});
    }}
    window.__xCrisMonitoredEvents = {monitoredEventsJson};
    window.__xCrisAutomationBindings = {automationBindingsJson};
    // DOM event listeners
    window.__xCrisListeners = window.__xCrisListeners || {{}};
    function xCrisSelectorSummary(target) {{
        if (!(target instanceof Element)) return '';
        if (target.id) return '#' + target.id;
        if (target.className && typeof target.className === 'string') return '.' + target.className.split(' ').join('.');
        return (target.tagName || '').toLowerCase();
    }}
    function xCrisPropertyValue(target, propertyName) {{
        if (!(target instanceof Element) || !propertyName) return '';
        var name = String(propertyName).toLowerCase();
        switch (name) {{
            case 'id': return target.id || '';
            case 'classname':
            case 'class': return target.className || '';
            case 'innertext':
            case 'text':
            case 'textcontent': return target.innerText || target.textContent || '';
            case 'innerhtml':
            case 'html': return target.innerHTML || '';
            case 'href': return target.getAttribute('href') || target.href || '';
            case 'value': return target.value || '';
            case 'name': return target.getAttribute('name') || '';
            case 'tag':
            case 'tagname': return target.tagName || '';
            case 'src': return target.getAttribute('src') || target.src || '';
            case 'title': return target.getAttribute('title') || '';
            default:
                try {{
                    var direct = target[propertyName];
                    if (direct != null) return String(direct);
                }} catch (_) {{}}
                return target.getAttribute(propertyName) || '';
        }}
    }}
    var evts = {eventsJson};
    evts.forEach(function(evt) {{
        if (window.__xCrisListeners[evt]) return;
        window.__xCrisListeners[evt] = true;
        document.addEventListener(evt, function(e) {{
            var source = e.target instanceof Element ? e.target : document.documentElement;
            if ((window.__xCrisMonitoredEvents || []).indexOf(evt) >= 0) {{
                window.chrome.webview.postMessage(JSON.stringify({{
                    type: evt,
                    target: xCrisSelectorSummary(source),
                    detail: e.detail != null ? String(e.detail) : ''
                }}));
            }}
            (window.__xCrisAutomationBindings || []).forEach(function(binding) {{
                if (!binding || String(binding.eventType || '').toLowerCase() !== String(evt).toLowerCase()) return;
                var matched = source;
                if (binding.selector && source instanceof Element) {{
                    try {{
                        matched = source.matches(binding.selector) ? source : source.closest(binding.selector);
                    }} catch (_) {{
                        matched = null;
                    }}
                }}
                if (!matched && binding.selector) return;
                if (!matched) matched = document.documentElement;
                var propertyValue = xCrisPropertyValue(matched, binding.propertyName);
                if (binding.propertyValue && propertyValue.indexOf(binding.propertyValue) === -1) return;
                window.chrome.webview.postMessage(JSON.stringify({{
                    type: '__automation__',
                    bindingName: binding.name || '',
                    eventType: evt,
                    target: xCrisSelectorSummary(matched),
                    propertyName: binding.propertyName || '',
                    matchedValue: propertyValue,
                    actionType: binding.actionType || '',
                    actionTarget: binding.actionTarget || '',
                    actionArgument: binding.actionArgument || '',
                    text: matched.innerText || matched.textContent || '',
                    href: xCrisPropertyValue(matched, 'href')
                }}));
            }});
        }}, true);
    }});
}})()";

            await ExecuteScriptSafeAsync(js);
        }

        private List<string> BuildEnabledEventsList()
        {
            var list = new List<string>();
            if (ChkClick.IsChecked   == true) list.Add("click");
            if (ChkChange.IsChecked  == true) list.Add("change");
            if (ChkInput.IsChecked   == true) list.Add("input");
            if (ChkSubmit.IsChecked  == true) list.Add("submit");
            if (ChkKeydown.IsChecked == true) list.Add("keydown");
            if (ChkScroll.IsChecked  == true) list.Add("scroll");
            if (ChkLoad.IsChecked    == true) list.Add("load");
            return list;
        }

        private void CoreWebView2_WebMessageReceived(object? sender,
            CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var json = e.TryGetWebMessageAsString();
                var doc  = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var type = root.GetProperty("type").GetString() ?? "";

                // Route console messages to the JS Console panel
                if (type == "__console__")
                {
                    var level = root.GetProperty("level").GetString() ?? "log";
                    var msg   = root.GetProperty("detail").GetString() ?? "";
                    Dispatcher.Invoke(() => AppendConsole($"[{level.ToUpperInvariant()}] {msg}\n"));
                    return;
                }

                if (type == "__automation__")
                {
                    var trigger = new AutomationTrigger(
                        root.GetProperty("bindingName").GetString() ?? "",
                        root.GetProperty("eventType").GetString() ?? "",
                        root.GetProperty("target").GetString() ?? "",
                        root.GetProperty("propertyName").GetString() ?? "",
                        root.GetProperty("matchedValue").GetString() ?? "",
                        root.GetProperty("actionType").GetString() ?? "",
                        root.GetProperty("actionTarget").GetString() ?? "",
                        root.GetProperty("actionArgument").GetString() ?? "",
                        root.GetProperty("text").GetString() ?? "",
                        root.GetProperty("href").GetString() ?? "");
                    _ = ExecuteAutomationAsync(trigger);
                    return;
                }

                var ev = new PageEvent
                {
                    EventType      = type,
                    TargetSelector = root.GetProperty("target").GetString()  ?? "",
                    Detail         = root.GetProperty("detail").GetString()  ?? "",
                    Timestamp      = DateTime.Now
                };

                Dispatcher.Invoke(() =>
                {
                    _pageEvents.Insert(0, ev);
                    if (_pageEvents.Count > 500) _pageEvents.RemoveAt(_pageEvents.Count - 1);
                    _eventCount++;
                    TxtEventCount.Text = $"Events: {_eventCount}";
                });
            }
            catch { /* ignore malformed messages */ }
        }

        private async void EventCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_webViewReady)
                await InjectEventListenersAsync();
        }

        private void BtnClearEvents_Click(object sender, RoutedEventArgs e)
        {
            _pageEvents.Clear();
            _eventCount = 0;
            TxtEventCount.Text = "Events: 0";
        }

        // ── Style inspector ────────────────────────────────────────────────────
        private async void BtnInspectStyles_Click(object sender, RoutedEventArgs e)
        {
            if (!_webViewReady) return;

            var sel    = TxtStyleSelector.Text.Trim().Replace("\\", "\\\\").Replace("'", "\\'");
            var js     = $@"
(function() {{
    var el = document.querySelector('{sel}');
    if (!el) return 'Element not found';
    var cs = window.getComputedStyle(el);
    var props = ['color','background-color','font-size','font-family','margin','padding',
                 'border','display','position','width','height','z-index','opacity',
                 'visibility','overflow','cursor','text-align'];
    var result = '';
    props.forEach(function(p) {{
        result += p + ': ' + cs.getPropertyValue(p) + ';\n';
    }});
    return result;
}})()";

            var result = await ExecuteScriptSafeAsync(js);
            TxtStylesOutput.Text = UnwrapJsonString(result ?? "null");
        }

        private async void BtnInjectCss_Click(object sender, RoutedEventArgs e)
        {
            if (!_webViewReady) return;

            var css  = TxtCssInput.Text.Replace("\\", "\\\\").Replace("`", "\\`");
            var js   = $@"
(function() {{
    var style = document.createElement('style');
    style.id = '__xCrisInjected';
    style.textContent = `{css}`;
    var existing = document.getElementById('__xCrisInjected');
    if (existing) existing.remove();
    document.head.appendChild(style);
    return 'CSS injected';
}})()";

            var result = await ExecuteScriptSafeAsync(js);
            SetStatus($"CSS inject result: {UnwrapJsonString(result ?? "null")}");
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private async Task<string?> ExecuteScriptSafeAsync(string js)
        {
            try
            {
                return await WebView.CoreWebView2.ExecuteScriptAsync(js);
            }
            catch (Exception ex)
            {
                AppendConsole($"[ERROR] {ex.Message}\n");
                return null;
            }
        }

        /// <summary>
        /// ExecuteScriptAsync returns JSON-encoded strings with surrounding quotes.
        /// This helper strips those outer quotes and unescapes the content.
        /// </summary>
        private static string UnwrapJsonString(string raw)
        {
            if (raw.StartsWith('"') && raw.EndsWith('"'))
            {
                var inner = raw[1..^1];
                return inner
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");
            }
            return raw;
        }

        private async Task ExecuteAutomationAsync(AutomationTrigger trigger)
        {
            var actionType = trigger.ActionType.Trim();

            switch (actionType.ToLowerInvariant())
            {
                case "runprogram":
                    RunExternalProgram(trigger.ActionTarget.Trim(), ResolveTemplate(trigger.ActionArgument, trigger));
                    break;

                case "executejavascript":
                    var js = ResolveTemplate(trigger.ActionArgument, trigger);
                    if (string.IsNullOrWhiteSpace(js))
                        js = ResolveTemplate(trigger.ActionTarget, trigger);

                    var result = await ExecuteScriptSafeAsync(js);
                    SetStatus($"Automation '{trigger.BindingName}' executed JavaScript");
                    if (result is not null)
                        AppendConsole($"[AUTOMATION] {trigger.BindingName}: {UnwrapJsonString(result)}\n");
                    break;

                case "applicationcommand":
                    await ExecuteApplicationCommandAsync(trigger.ActionTarget.Trim(), ResolveTemplate(trigger.ActionArgument, trigger), trigger);
                    break;

                default:
                    AppendConsole($"[AUTOMATION] Unsupported action type '{trigger.ActionType}'\n");
                    break;
            }
        }

        private void RunExternalProgram(string fileName, string arguments)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                AppendConsole("[AUTOMATION] External program path is empty.\n");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false
                });
                SetStatus($"Automation launched: {Path.GetFileName(fileName)}");
            }
            catch (Exception ex)
            {
                AppendConsole($"[AUTOMATION] Launch failed: {ex.Message}\n");
            }
        }

        private async Task ExecuteApplicationCommandAsync(string command, string argument, AutomationTrigger trigger)
        {
            switch (command.ToLowerInvariant())
            {
                case "togglepanel":
                    BtnTogglePanel_Click(this, new RoutedEventArgs());
                    SetStatus($"Automation '{trigger.BindingName}' toggled the panel");
                    break;

                case "refreshpage":
                    BtnRefresh_Click(this, new RoutedEventArgs());
                    SetStatus($"Automation '{trigger.BindingName}' refreshed the page");
                    break;

                case "goback":
                    BtnBack_Click(this, new RoutedEventArgs());
                    SetStatus($"Automation '{trigger.BindingName}' navigated back");
                    break;

                case "goforward":
                    BtnForward_Click(this, new RoutedEventArgs());
                    SetStatus($"Automation '{trigger.BindingName}' navigated forward");
                    break;

                case "gohome":
                    BtnHome_Click(this, new RoutedEventArgs());
                    SetStatus($"Automation '{trigger.BindingName}' navigated home");
                    break;

                case "clearevents":
                    BtnClearEvents_Click(this, new RoutedEventArgs());
                    SetStatus($"Automation '{trigger.BindingName}' cleared events");
                    break;

                case "querydom":
                    var selector = string.IsNullOrWhiteSpace(argument) ? trigger.TargetSelector : argument;
                    if (!string.IsNullOrWhiteSpace(selector))
                    {
                        await QueryDomAsync(selector);
                        SetStatus($"Automation '{trigger.BindingName}' queried '{selector}'");
                    }
                    break;

                case "navigate":
                    var url = string.IsNullOrWhiteSpace(argument) ? trigger.Href : argument;
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        NavigateTo(url);
                        SetStatus($"Automation '{trigger.BindingName}' navigated to {url}");
                    }
                    break;

                case "showmessage":
                    var message = string.IsNullOrWhiteSpace(argument)
                        ? $"Triggered automation '{trigger.BindingName}' for {trigger.TargetSelector}"
                        : argument;
                    MessageBox.Show(this, message, "xCris Automation", MessageBoxButton.OK, MessageBoxImage.Information);
                    SetStatus($"Automation '{trigger.BindingName}' displayed a message");
                    break;

                default:
                    AppendConsole($"[AUTOMATION] Unsupported application command '{command}'\n");
                    break;
            }
        }

        private static string ResolveTemplate(string template, AutomationTrigger trigger)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["selector"] = trigger.TargetSelector,
                ["eventType"] = trigger.EventType,
                ["propertyName"] = trigger.PropertyName,
                ["matchedValue"] = trigger.MatchedValue,
                ["text"] = trigger.Text,
                ["href"] = trigger.Href
            };

            var resolved = template;
            foreach (var pair in values)
            {
                resolved = resolved.Replace("{" + pair.Key + "}", pair.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                resolved = resolved.Replace("{" + pair.Key + "Json}", JsonSerializer.Serialize(pair.Value ?? string.Empty), StringComparison.OrdinalIgnoreCase);
            }

            return resolved;
        }

        private void LoadAutomationBindings()
        {
            try
            {
                var path = GetAutomationBindingsPath();
                if (!File.Exists(path))
                    return;

                var json = File.ReadAllText(path);
                var items = JsonSerializer.Deserialize<List<AutomationBinding>>(json);
                if (items is null)
                    return;

                foreach (var binding in items)
                    _automationBindings.Add(binding);
            }
            catch
            {
                // Ignore malformed local configuration files
            }
        }

        private void SaveAutomationBindings()
        {
            try
            {
                var path = GetAutomationBindingsPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var json = JsonSerializer.Serialize(_automationBindings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                AppendConsole($"[AUTOMATION] Failed to save bindings: {ex.Message}\n");
            }
        }

        private static string GetAutomationBindingsPath() =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "xCris",
                "automation-bindings.json");

        private void SetStatus(string message) => TxtStatus.Text = message;
    }
}
