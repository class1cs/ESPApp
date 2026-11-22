using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ESPApp.UI.Services;

namespace ESPApp.UI.ViewModels;

/// <summary>
/// Common presentation and navigation behavior for a page hosted by MainWindow.
/// The same ViewModel is used by the sidebar and the page ContentControl.
/// </summary>
public abstract partial class PageViewModelBase : ViewModelBase
{
    private readonly IPageNavigationService _navigationService;

    protected PageViewModelBase(
        string title,
        string description,
        string icon,
        IPageNavigationService navigationService)
    {
        Title = title;
        Description = description;
        Icon = icon;
        _navigationService = navigationService;
        _navigationService.CurrentPageChanged += OnCurrentPageChanged;

        IsSelected = ReferenceEquals(_navigationService.CurrentPage, this);
    }

    public string Title { get; }
    public string Description { get; }
    public string Icon { get; }

    [ObservableProperty]
    private bool _isSelected;

    [RelayCommand]
    private void Navigate() => _navigationService.NavigateTo(this);

    private void OnCurrentPageChanged(PageViewModelBase? currentPage)
    {
        IsSelected = ReferenceEquals(currentPage, this);
    }
}
