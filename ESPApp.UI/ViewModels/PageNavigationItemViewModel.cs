using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ESPApp.UI.ViewModels;


public partial class PageNavigationItemViewModel : ViewModelBase
{
    private readonly Action<PageViewModelBase> _navigate;

    public PageNavigationItemViewModel(PageViewModelBase page, Action<PageViewModelBase> navigate)
    {
        Page = page;
        _navigate = navigate;
    }

    public PageViewModelBase Page { get; }
    public string Title => Page.Title;
    public string Description => Page.Description;
    public string Icon => Page.Icon;

    [ObservableProperty]
    private bool _isSelected;

    [RelayCommand]
    private void Navigate() => _navigate(Page);
}
