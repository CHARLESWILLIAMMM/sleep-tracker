# 🌙 Sleep Tracker

A local-first sleep and energy tracking app that runs **right in your browser** — no app store, no account needed.

## 🔗 Open the app

**→ [https://charleswilliammm.github.io/sleep-tracker/](https://charleswilliammm.github.io/sleep-tracker/)**

On Android: tap the three-dot menu → **"Add to Home Screen"** to install it like a native app.

---

## What it does

| Feature | Description |
|---------|-------------|
| 😴 **Sleep Logging** | Log your bedtime + wake time — the app calculates hours automatically |
| 💤 **Sleep Debt** | Rolling 14-day debt vs an 8-hour baseline (same algorithm as the Android app) |
| ⚡ **Energy Curve** | 24-hour circadian energy prediction, adjusted for your sleep debt |
| 📊 **History** | 7-day bar chart of your sleep duration |
| 📴 **Offline** | Works without internet after the first visit (PWA + service worker) |

---

## Run the tests

```bash
dotnet test SleepTracker.Tests/SleepTracker.Tests.csproj
```

All 27 unit + integration tests should pass.

---

## Project structure

```
sleep-tracker/
├── docs/                            ← PWA web app (deployed to GitHub Pages)
│   ├── index.html                   ← Complete Android-like UI + algorithms
│   ├── manifest.json                ← PWA manifest (installable on Android)
│   ├── sw.js                        ← Service worker (offline support)
│   └── icon.svg                     ← App icon
│
├── SleepTracker/                    ← .NET MAUI Android app (native)
│   ├── Platforms/Android/
│   │   ├── AndroidManifest.xml      ← BODY_SENSORS, WAKE_LOCK, FOREGROUND_SERVICE
│   │   ├── SleepTrackingService.cs  ← Foreground service + accelerometer/light
│   │   ├── MainActivity.cs
│   │   └── BootReceiver.cs
│   ├── Data/SleepDatabase.cs        ← SQLite helper (sqlite-net-pcl)
│   ├── Models/SleepReading.cs
│   └── ViewModels/SleepViewModel.cs ← Sleep Debt + Energy Curve algorithms
│
├── SleepTracker.Tests/              ← xUnit tests for core algorithms
└── .github/workflows/pages.yml     ← CI: runs tests → deploys to GitHub Pages
```

---

## Enable GitHub Pages (one-time setup)

1. Go to **Settings → Pages** in this repository
2. Under **Source**, select **"GitHub Actions"**
3. The next push will automatically deploy to the live URL above