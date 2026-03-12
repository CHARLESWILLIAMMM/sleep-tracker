using SleepTracker.ViewModels;

namespace SleepTracker;

public partial class MainPage : ContentPage
{
    private readonly SleepViewModel _viewModel;

    public MainPage(SleepViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Automatically refresh metrics when the page becomes visible.
        await _viewModel.RefreshAsync().ConfigureAwait(false);
    }
}
