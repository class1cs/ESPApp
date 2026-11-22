using System;
using ESPApp.UI.ViewModels;

namespace ESPApp.UI.Services;


public sealed class PageNavigationService : IPageNavigationService
{
    public PageViewModelBase? CurrentPage { get; private set; }

    public event Action<PageViewModelBase?>? CurrentPageChanged;

    public void NavigateTo(PageViewModelBase page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (ReferenceEquals(CurrentPage, page))
        {
            return;
        }

        CurrentPage = page;
        CurrentPageChanged?.Invoke(page);
    }
}
