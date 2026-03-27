using FocusTimer.Core.Interfaces;
using FocusTimer.Core.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace FocusTimer.App.ViewModels;

/// <summary>
/// ViewModel for the MainWindow placeholder UI.
/// </summary>
public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly ISettingsProvider _settingsProvider;
    private Settings? _settings;
    private string _worklogDirectory = "Loading...";

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindowViewModel(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
        _ = LoadSettingsAsync();
    }

    public string WorklogDirectory
    {
        get => _worklogDirectory;
        private set
        {
            _worklogDirectory = value;
            OnPropertyChanged();
        }
    }

    private async Task LoadSettingsAsync()
    {
        _settings = await _settingsProvider.LoadAsync();
        WorklogDirectory = _settings.WorklogDirectory;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
