# 🌙 Sleep Tracker

A local-first sleep and energy tracking app that runs **right in your browser** — no app store, no account needed.

## ⚠️ Getting the link to work — do this first (30 seconds)

> If you see a **404 page**, GitHub Pages is not enabled yet.  
> Fix it in **3 clicks**:

1. Open **https://github.com/CHARLESWILLIAMMM/sleep-tracker/settings/pages**
2. Under **"Build and deployment → Source"**, choose **Deploy from a branch**
3. Set **Branch** to `gh-pages` and folder to `/ (root)`, then click **Save**

After that, every push automatically runs the tests and deploys. The live link will be:

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