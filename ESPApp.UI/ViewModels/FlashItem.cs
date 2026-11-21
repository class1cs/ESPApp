using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ESPApp.UI.ViewModels;

public partial class FlashItem : ObservableObject
{
    private readonly Func<FlashItem, Task> _browse;
    private readonly Action<FlashItem> _remove;

    [ObservableProperty]
    private string _address;

    [ObservableProperty]
    private string _filePath = string.Empty;

    public FlashItem(string address, Func<FlashItem, Task> browse, Action<FlashItem> remove)
    {
        _address = address;
        _browse = browse;
        _remove = remove;
    }

    [RelayCommand]
    private Task BrowseAsync() => _browse(this);

    [RelayCommand]
    private void Remove() => _remove(this);
}
