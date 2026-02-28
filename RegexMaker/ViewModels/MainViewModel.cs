using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using Avalonia;
using Avalonia.Input.Platform;
using System.Threading.Tasks;

namespace RegexMaker.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public event EventHandler<SaveRequestedEventArgs>? SaveRequested;
    public event EventHandler<LoadRequestedEventArgs>? LoadRequested;
    public event EventHandler? ClearRequested;
    public event EventHandler<CopyRegexRequestedEventArgs>? CopyRegexRequested;

    [ObservableProperty]
    private string _regexPattern = string.Empty;

    [ObservableProperty]
    private object? _currentNodeViewModel;

    [ObservableProperty]
    private int _selectedCarouselIndex = 0;

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

    [RelayCommand]
    private void CopyRegex()
    {
        if (string.IsNullOrEmpty(RegexPattern))
            return;

        CopyRegexRequested?.Invoke(this, new CopyRegexRequestedEventArgs(RegexPattern));
    }
}

public class SaveRequestedEventArgs : EventArgs
{
}

public class LoadRequestedEventArgs : EventArgs
{
}

public class CopyRegexRequestedEventArgs : EventArgs
{
    public string RegexPattern { get; }

    public CopyRegexRequestedEventArgs(string regexPattern)
    {
        RegexPattern = regexPattern;
    }
}
