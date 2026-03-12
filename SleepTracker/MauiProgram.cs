using SleepTracker.Data;
using SleepTracker.ViewModels;

namespace SleepTracker;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMaui()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register the SQLite database as a singleton so the same connection
        // is reused across the application lifetime.
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "sleep.db3");
        builder.Services.AddSingleton(new SleepDatabase(dbPath));

        // Register the view model and main page.
        builder.Services.AddTransient<SleepViewModel>();
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
