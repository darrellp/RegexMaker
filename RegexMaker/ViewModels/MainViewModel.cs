using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace RegexMaker.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public event EventHandler<SaveRequestedEventArgs>? SaveRequested;
    public event EventHandler<LoadRequestedEventArgs>? LoadRequested;
    public event EventHandler? ClearRequested;

    [ObservableProperty]
    private string _regexPattern = string.Empty;

    [RelayCommand]
    private void Save()
    {
        SaveRequested?.Invoke(this, new SaveRequestedEventArgs());
    }

    [RelayCommand]
    private void Load()
    {
        LoadRequested?.Invoke(this, new LoadRequestedEventArgs());
    }

    [RelayCommand]
    private void Clear()
    {
        ClearRequested?.Invoke(this, EventArgs.Empty);
    }
}

public class SaveRequestedEventArgs : EventArgs
{
}

public class LoadRequestedEventArgs : EventArgs
{
}
