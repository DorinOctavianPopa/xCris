# xCris

**xCris** is a WPF-based web browser built on top of [Microsoft WebView2](https://learn.microsoft.com/en-us/microsoft-edge/webview2/), designed as an external companion framework for **Ecris V**. It combines a fully functional tabbed browser with a built-in developer side-panel that lets you inspect and manipulate the DOM, run JavaScript, and monitor live page events — all from a single window.

---

## Table of Contents

- [Features](#features)
- [Screenshots](#screenshots)
- [Requirements](#requirements)
- [Getting Started](#getting-started)
  - [Clone the repository](#clone-the-repository)
  - [Build](#build)
  - [Run](#run)
- [Project Structure](#project-structure)
- [Usage](#usage)
  - [Navigation](#navigation)
  - [Tabbed Browsing](#tabbed-browsing)
  - [DOM Explorer](#dom-explorer)
  - [JavaScript Console](#javascript-console)
  - [Event Monitor](#event-monitor)
  - [Keyboard Shortcuts](#keyboard-shortcuts)
- [Models](#models)
- [Dependencies](#dependencies)
- [Contributing](#contributing)
- [License](#license)

---

## Features

| Feature | Description |
|---|---|
| **Tabbed browsing** | Open, switch, and manage multiple tabs from a top tab-strip. |
| **Smart address bar** | Navigates to URLs directly, or falls back to a Google search when no `.` is detected. |
| **HTTPS indicator** | Shows a 🔒 Secure / ⚠ Not Secure badge depending on the connection scheme. |
| **DOM Explorer** | Query any CSS selector, view matched elements, and live-edit their `id`, `class`, `innerText`, or `innerHTML`. |
| **JavaScript Console** | Write and execute arbitrary JavaScript in the context of the active page; output is shown inline. |
| **Event Monitor** | Capture page-level DOM events (`click`, `change`, `input`, `submit`, `keydown`, `scroll`, `load`) and `console.log`/`warn`/`error` messages through a bidirectional WebView2 message bridge. |
| **Toggleable side panel** | The developer panel can be hidden/shown at any time without reloading the page. |
| **Dark theme UI** | A modern Catppuccin-inspired dark colour palette throughout. |

---

## Screenshots

> _Add screenshots here once the application is running._

---

## Requirements

| Requirement | Version |
|---|---|
| .NET | **9.0** (Windows) |
| Microsoft WebView2 Runtime | Bundled via NuGet `Microsoft.Web.WebView2` ≥ 1.0.2957 |
| Windows | Windows 10 / 11 (required by WPF + WebView2) |

---

## Getting Started

### Clone the repository

```bash
git clone https://github.com/DorinOctavianPopa/xCris.git
cd xCris
```

### Build

```bash
dotnet build xCris.csproj
```

For a release build:

```bash
dotnet build xCris.csproj -c Release
```

### Run

```bash
dotnet run --project xCris.csproj
```

Or open `xCris.csproj` in **Visual Studio 2022** (or later) and press **F5**.

> **Note:** WebView2 requires a compatible Edge WebView2 Runtime to be installed on the machine. In most cases this is already present on Windows 10/11. If not, it will be downloaded automatically the first time the application starts.

---

## Project Structure

```
xCris/
├── App.xaml              # WPF application entry point
├── App.xaml.cs           # Application startup logic
├── MainWindow.xaml       # Main window layout (browser + developer panel)
├── MainWindow.xaml.cs    # All UI logic: navigation, tabs, DOM, console, events
├── AssemblyInfo.cs       # Assembly metadata
├── xCris.csproj          # SDK-style project file (.NET 9 / WPF)
└── Models/
    ├── BrowserTab.cs     # Model for a browser tab (title, URL, loading state)
    ├── DomElement.cs     # Model for a queried DOM element
    └── PageEvent.cs      # Model for a captured page event
```

---

## Usage

### Navigation

- Type a **full URL** (e.g. `https://example.com`) into the address bar and press **Enter**.
- Type a **domain shorthand** (e.g. `example.com`) — the `https://` prefix is added automatically.
- Type a **search query** (no `.` in the text) and xCris will search Google for it.
- Use the **← Back**, **→ Forward**, **↺ Refresh**, and **⌂ Home** buttons in the toolbar.

### Tabbed Browsing

- Click **＋** in the tab strip to open a new tab (defaults to `https://www.google.com`).
- Click any tab button to switch to it. The active tab is highlighted in the accent colour.

### DOM Explorer

1. Open the side panel (press **F12** or click the panel toggle button).
2. Navigate to the **DOM** tab.
3. Enter a CSS selector (e.g. `h1`, `#main`, `.nav-link`) and press **Enter** or click **Query**.
4. Matched elements are listed with their tag, id, class, and a preview of their text/HTML.
5. Click a row to populate the **Element Details** fields.
6. Edit `id`, `class`, `innerText`, or `innerHTML` and click **Apply** to live-update the element on the page.
7. Click **Remove** to delete the selected element from the DOM.

### JavaScript Console

1. Navigate to the **Console** tab in the side panel.
2. Enter any JavaScript expression or statement in the input box.
3. Click **Run** (or press **Enter**) to execute it in the page context.
4. The return value is printed below. `console.log`, `console.warn`, and `console.error` calls made by the page are also forwarded here automatically.

### Event Monitor

1. Navigate to the **Events** tab in the side panel.
2. Use the checkboxes to select which event types to listen for: `click`, `change`, `input`, `submit`, `keydown`, `scroll`, `load`.
3. Events are captured via a JavaScript bridge injected into the page after each navigation and streamed back to the event list in real time.
4. Each entry shows the event type, target selector, detail, and a precise timestamp.

### Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `F5` | Reload the current page |
| `F12` | Toggle the developer side panel |
| `Ctrl + T` | Open a new tab |
| `Ctrl + L` | Focus the address bar |

---

## Models

### `BrowserTab`

Represents a tab in the tab strip. Properties: `Title`, `Url`, `IsLoading`, `IsSelected`. Implements `INotifyPropertyChanged`.

### `DomElement`

Represents a DOM node returned by a CSS selector query. Properties: `TagName`, `Id`, `ClassName`, `InnerText`, `InnerHTML`, `Selector`. The `ToString()` override returns a compact CSS-style identifier (e.g. `DIV#header` or `SPAN.nav-link`).

### `PageEvent`

Represents a page event captured by the JavaScript bridge. Properties: `EventType`, `TargetSelector`, `Detail`, `Timestamp`. The `FormattedTimestamp` property returns a human-readable `HH:mm:ss.fff` string.

---

## Dependencies

| Package | Version | Purpose |
|---|---|---|
| [`Microsoft.Web.WebView2`](https://www.nuget.org/packages/Microsoft.Web.WebView2) | 1.0.2957.106 | Chromium-based web rendering engine for WPF |

All other dependencies are part of the .NET 9 SDK.

---

## Contributing

Contributions are welcome! To get started:

1. Fork the repository.
2. Create a feature branch: `git checkout -b feature/your-feature`.
3. Commit your changes: `git commit -m "feat: add your feature"`.
4. Push to your fork: `git push origin feature/your-feature`.
5. Open a Pull Request.

Please keep pull requests focused and ensure the project builds without warnings before submitting.

---

## License

This project does not currently include a license file. Please contact the repository owner for licensing information.
