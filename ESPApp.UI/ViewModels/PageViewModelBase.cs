namespace ESPApp.UI.ViewModels;


public abstract class PageViewModelBase : ViewModelBase
{
    protected PageViewModelBase(string title, string description, string icon)
    {
        Title = title;
        Description = description;
        Icon = icon;
    }

    public string Title { get; }
    public string Description { get; }
    public string Icon { get; }
}
