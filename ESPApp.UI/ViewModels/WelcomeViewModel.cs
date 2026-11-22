using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ESPApp.Infrastructure.Interfaces;
using ESPApp.Infrastructure.Models;

namespace ESPApp.UI.ViewModels
{
    public partial class WelcomeViewModel : ViewModelBase
    {
        private readonly IEspToolConfigurator _configurator;
        private readonly ISettingsService _settingsService;
        private readonly AppSettings _settings;
        private readonly Dictionary<EsptoolSourceKind, EsptoolProbe> _results = new();
        private CancellationTokenSource? _detectCts;
        private bool _selectionTouched;

        public event Action<AppSettings, bool>? SettingsApplied;

        public event Action? CloseRequested;

        public IReadOnlyList<string> Chips { get; }

        public IReadOnlyList<string> Bauds { get; }

        [ObservableProperty]
        private EsptoolSourceKind _selectedKind = EsptoolSourceKind.Python;

        [ObservableProperty]
        private string _pythonPath = "python";

        [ObservableProperty]
        private string _exePath = string.Empty;

        [ObservableProperty]
        private string _selectedChip = "auto";

        [ObservableProperty]
        private string _selectedBaud = "460800";

        [ObservableProperty]
        private bool _showOnStart = true;

        [ObservableProperty]
        private string _pythonStatus = "не проверено";

        [ObservableProperty]
        private string _exeStatus = "не проверено";

        [ObservableProperty]
        private string _systemPathStatus = "не проверено";

        [ObservableProperty]
        private bool _isChecking;

        [ObservableProperty]
        private string _message = string.Empty;

        public bool IsPython
        {
            get => SelectedKind == EsptoolSourceKind.Python;
            set => Choose(value, EsptoolSourceKind.Python);
        }

        public bool IsExe
        {
            get => SelectedKind == EsptoolSourceKind.Exe;
            set => Choose(value, EsptoolSourceKind.Exe);
        }

        public bool IsSystemPath
        {
            get => SelectedKind == EsptoolSourceKind.SystemPath;
            set => Choose(value, EsptoolSourceKind.SystemPath);
        }

        public WelcomeViewModel(
            IEspToolConfigurator configurator,
            ISettingsService settingsService,
            AppSettings settings,
            ConnectionViewModel connection)
        {
            _configurator = configurator;
            _settingsService = settingsService;
            _settings = settings;
            Chips = connection.Chips;
            Bauds = connection.Bauds;
            Reset();
        }

        public void Reset()
        {
            _selectionTouched = false;
            SelectedKind = _settings.SourceKind;
            PythonPath = _settings.PythonPath;
            ExePath = _settings.ExePath;
            SelectedChip = _settings.DefaultChip;
            SelectedBaud = _settings.DefaultBaud;
            ShowOnStart = _settings.ShowWelcomeOnStart;
        }

        public async Task DetectAsync()
        {
            _detectCts?.Cancel();
            var cts = new CancellationTokenSource();
            _detectCts = cts;

            IsChecking = true;
            Message = string.Empty;
            _results.Clear();
            PythonStatus = "проверяю...";
            ExeStatus = "проверяю...";
            SystemPathStatus = "проверяю...";

            try
            {
                var python = _configurator.ProbeAsync(
                    new EsptoolSource(EsptoolSourceKind.Python, PythonPath, null), cts.Token);
                var exe = _configurator.ProbeAsync(
                    new EsptoolSource(
                        EsptoolSourceKind.Exe,
                        null,
                        string.IsNullOrWhiteSpace(ExePath) ? null : ExePath),
                    cts.Token);
                var systemPath = _configurator.ProbeAsync(
                    new EsptoolSource(EsptoolSourceKind.SystemPath), cts.Token);

                await Task.WhenAll(python, exe, systemPath);

                if (cts.IsCancellationRequested)
                {
                    return;
                }

                _results[EsptoolSourceKind.Python] = python.Result;
                _results[EsptoolSourceKind.Exe] = exe.Result;
                _results[EsptoolSourceKind.SystemPath] = systemPath.Result;

                PythonStatus = Describe(python.Result);
                ExeStatus = Describe(exe.Result);
                SystemPathStatus = Describe(systemPath.Result);

                if (!_settings.IsConfigured && !_selectionTouched)
                {
                    if (python.Result.Found) SelectedKind = EsptoolSourceKind.Python;
                    else if (exe.Result.Found) SelectedKind = EsptoolSourceKind.Exe;
                    else if (systemPath.Result.Found) SelectedKind = EsptoolSourceKind.SystemPath;
                }

                UpdateMessage();
            }
            finally
            {
                if (_detectCts == cts)
                {
                    IsChecking = false;
                }
            }
        }

        [RelayCommand]
        private Task CheckAsync() => DetectAsync();

        [RelayCommand]
        private async Task BrowseExeAsync()
        {
            var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            if (window is null)
            {
                return;
            }

            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите esptool",
                AllowMultiple = false
            });

            var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
            if (path is null)
            {
                return;
            }

            ExePath = path;
            _selectionTouched = true;
            SelectedKind = EsptoolSourceKind.Exe;
        }

        [RelayCommand]
        private void Continue()
        {
            _settings.IsConfigured = true;
            _settings.SourceKind = SelectedKind;
            _settings.PythonPath = string.IsNullOrWhiteSpace(PythonPath) ? "python" : PythonPath.Trim();
            _settings.ExePath = ExePath.Trim();
            _settings.DefaultChip = SelectedChip;
            _settings.DefaultBaud = SelectedBaud;
            _settings.ShowWelcomeOnStart = ShowOnStart;

            _configurator.Use(_settings.ToSource());
            var saved = _settingsService.Save(_settings);

            SettingsApplied?.Invoke(_settings, saved);
        }

        [RelayCommand]
        private void Cancel()
        {
            _detectCts?.Cancel();
            IsChecking = false;
            Reset();
            CloseRequested?.Invoke();
        }

        partial void OnSelectedKindChanged(EsptoolSourceKind value)
        {
            OnPropertyChanged(nameof(IsPython));
            OnPropertyChanged(nameof(IsExe));
            OnPropertyChanged(nameof(IsSystemPath));
            UpdateMessage();
        }

        partial void OnPythonPathChanged(string value)
        {
            if (_results.Remove(EsptoolSourceKind.Python))
            {
                PythonStatus = "изменено — нажмите «Проверить»";
                UpdateMessage();
            }
        }

        partial void OnExePathChanged(string value)
        {
            if (_results.Remove(EsptoolSourceKind.Exe))
            {
                ExeStatus = "изменено — нажмите «Проверить»";
                UpdateMessage();
            }
        }

        private void Choose(bool isChecked, EsptoolSourceKind kind)
        {
            if (!isChecked)
            {
                return;
            }

            _selectionTouched = true;
            SelectedKind = kind;
        }

        private static string Describe(EsptoolProbe probe)
        {
            return probe.Found ? $"✓ {probe.Details}" : $"✗ {probe.Details}";
        }

        private void UpdateMessage()
        {
            if (IsChecking || _results.Count == 0)
            {
                Message = string.Empty;
                return;
            }

            if (_results.TryGetValue(SelectedKind, out var probe) && !probe.Found)
            {
                var hint = SelectedKind switch
                {
                    EsptoolSourceKind.Python => "Python или модуль esptool не найдены. Установите Python и выполните: pip install esptool.",
                    EsptoolSourceKind.Exe => "Файл esptool не найден. Выберите его кнопкой «Выбрать» или положите в папку «esptool» рядом с программой.",
                    _ => "Команда esptool не найдена в PATH."
                };

                Message = $"{hint} Пока он недоступен, кнопки программы работать не будут.";
                return;
            }

            Message = "Всё готово: выбранный esptool отвечает.";
        }
    }
}
