using System;
using ESPApp.UI.ViewModels;

namespace ESPApp.UI.Services;


public interface IPageNavigationService
{
    PageViewModelBase? CurrentPage { get; }

    event Action<PageViewModelBase?>? CurrentPageChanged;

    void NavigateTo(PageViewModelBase page);
}
