# Windows Prayer Time

A quiet Windows tray app for prayer times, Adhan reminders, and Iqamah nudges while you are working at your PC.

![Windows Prayer Time widget](screenshots/widget-bottom-right.png)

## Highlights

- Compact countdown widget near the bottom-right taskbar area.
- Selectable visual themes using bundled transparent PNG backgrounds.
- Manual theme editor for moving text boxes, changing fonts, adjusting sizes, colors, alignment, and shadow softness.
- Tray icon with the next prayer and countdown.
- Auto-location mode: detects approximate network location, then asks AlAdhan for prayer times by latitude and longitude.
- Manual location modes: city/country or exact coordinates.
- Automatic calculation method option so AlAdhan can choose the closest local authority.
- Separate Adhan and Iqamah behavior:
  - Adhan is intentionally subtle.
  - Iqamah can be more assertive when keyboard or mouse activity shows you are still at the PC.
- Configurable Iqamah offsets:
  - Fajr: 25 minutes
  - Dhuhr: 20 minutes
  - Asr: 20 minutes
  - Maghrib: 5 minutes
  - Isha: 20 minutes
- Manual per-prayer minute corrections.
- Start with Windows option.
- Local JSON settings and cached timings.

## How It Works

Windows Prayer Time uses the free [AlAdhan Prayer Times API](https://aladhan.engconsults.com/rest-api.html). In auto-location mode, it first resolves an approximate location from the current network using [ipapi](https://ipapi.co/), then calls AlAdhan's coordinate-based timings endpoint.

If auto-location is unavailable, the app falls back to the configured city and country.

## Install

Download or build the app, then run:

```powershell
WindowsPrayerTime.exe
```

The app starts in the Windows notification area. Double-click the tray icon or countdown widget to open the schedule.

## Settings

Right-click the tray icon and choose `Settings`.

Available settings:

- Location source:
  - Auto-detect by network
  - Manual city/country
  - Manual coordinates
- Calculation method:
  - Automatic by location
  - Dubai, Gulf Region, Umm Al-Qura, Muslim World League, ISNA, and more
- Asr school:
  - Shafi / standard
  - Hanafi
- Iqamah offsets by prayer.
- Manual prayer-time corrections.
- Countdown position:
  - Above taskbar
  - Taskbar band (experimental)
- Widget theme:
  - Simple compact
  - Gold dark blue
  - Glassy cyan
  - Dark purple
- Manual theme editor:
  - Resize the whole widget
  - Position and resize each text element
  - Pick fonts and font sizes
  - Adjust colors and alignment
  - Soften or strengthen the text shadow
- Sound, Adhan, and Iqamah alert toggles.
- PC activity threshold for assertive Iqamah reminders.
- Start with Windows.

Settings and cached timings are stored in:

```powershell
%APPDATA%\WindowsPrayerTime
```

## Build From Source

Requirements:

- Windows 10 or Windows 11
- .NET SDK 9 or newer

Build:

```powershell
dotnet build -c Release
```

Publish a single Windows executable:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true
```

The published app will be under:

```powershell
bin\Release\net9.0-windows\win-x64\publish\WindowsPrayerTime.exe
```

## Privacy Notes

- Prayer times are fetched from AlAdhan.
- Auto-location mode uses ipapi to resolve an approximate network-based latitude and longitude.
- The app stores settings locally under `%APPDATA%\WindowsPrayerTime`.
- No account, database, telemetry, or cloud sync is built into this app.

## Project Structure

```text
Models/      App settings and prayer-time models
Services/    AlAdhan API, location detection, startup, sounds, widget state
UI/          Tray widget, alerts, dashboard, and settings forms
Assets/      Bundled widget theme backgrounds
```

## License

MIT
