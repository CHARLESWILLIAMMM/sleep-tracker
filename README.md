# 🌙 Sleep Tracker

A local-first sleep and energy tracking app that runs **right in your browser** — no app store, no account needed.

## 🔗 Live link

**→ [https://charleswilliammm.github.io/sleep-tracker/](https://charleswilliammm.github.io/sleep-tracker/)**

---

## ⚙️ ONE-TIME GitHub Pages setup (do this once, takes 30 seconds)

> **You must change the Pages source to "GitHub Actions"** — "Deploy from a branch" will NOT work (it shows the README instead of the app).

1. Go to **https://github.com/CHARLESWILLIAMMM/sleep-tracker/settings/pages**
2. Under **"Build and deployment"**, change **Source** from `Deploy from a branch` → **`GitHub Actions`**
3. Click **Save**

That's it. The workflow will deploy automatically on every push and the link above will show the full app.

On Android: tap the three-dot menu → **"Add to Home Screen"** to install as a native app.

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
├── index.html                       ← PWA web app (deployed to GitHub Pages)
├── manifest.json                    ← PWA manifest (installable on Android)
├── sw.js                            ← Service worker (offline support)
├── icon.svg                         ← App icon
├── .nojekyll                        ← Prevents Jekyll from processing the site
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
└── .github/workflows/pages.yml     ← CI: tests + deploy via GitHub Actions Pages API
```