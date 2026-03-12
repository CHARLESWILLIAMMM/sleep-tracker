# 🌙 Sleep Tracker

A local-first sleep and energy tracking app that runs **right in your browser** — no app store, no account needed.

## 🔗 Live link

**→ [https://charleswilliammm.github.io/sleep-tracker/](https://charleswilliammm.github.io/sleep-tracker/)**

---

## ⚙️ GitHub Pages setup (one-time, 3 clicks)

1. Go to **https://github.com/CHARLESWILLIAMMM/sleep-tracker/settings/pages**
2. **Source** → `Deploy from a branch`
3. **Branch** → `copilot/add-sleep-debt-calculation` · **Folder** → `/ (root)` → click **Save**

> After the PR is merged into `main`, change the branch to `main` and keep folder `/ (root)`.

On Android: tap the three-dot menu → **"Add to Home Screen"** to install it as a native app.

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
├── index.html                       ← PWA web app (served by GitHub Pages)
├── manifest.json                    ← PWA manifest (installable on Android)
├── sw.js                            ← Service worker (offline support)
├── icon.svg                         ← App icon
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
└── .github/workflows/pages.yml     ← CI: runs tests on every push
```