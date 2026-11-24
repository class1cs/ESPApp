using ESPApp.Infrastructure.Models;
using FluentAssertions;

namespace ESPApp.Tests;

[Trait("Category", "Unit")]
public sealed class WelcomeViewModelTests
{
    [Fact(DisplayName = "Первоначальная настройка: Reset загружает значения из AppSettings")]
    public void Reset_LoadsValuesFromSettings()
    {
        using var host = TestApplicationHost.Create(new AppSettings
        {
            IsConfigured = true,
            SourceKind = EsptoolSourceKind.Exe,
            PythonPath = "python3",
            ExePath = "C:/tools/esptool.exe",
            DefaultChip = "esp32s3",
            DefaultBaud = "921600",
            ShowWelcomeOnStart = false
        });

        var vm = host.Welcome;

        vm.SelectedKind.Should().Be(EsptoolSourceKind.Exe);
        vm.PythonPath.Should().Be("python3");
        vm.ExePath.Should().Be("C:/tools/esptool.exe");
        vm.SelectedChip.Should().Be("esp32s3");
        vm.SelectedBaud.Should().Be("921600");
        vm.ShowOnStart.Should().BeFalse();
    }

    [Fact(DisplayName = "Первоначальная настройка: проверка запускает поиск Python, EXE и PATH")]
    public async Task DetectAsync_ProbesAllSupportedSources()
    {
        using var host = TestApplicationHost.Create();
        host.Configurator.ProbeHandler = (source, _) => Task.FromResult(
            new EsptoolProbe(true, $"найдено: {source.Kind}"));

        await host.Welcome.DetectAsync();

        host.Configurator.ProbeRequests.Should().HaveCount(3);
        host.Configurator.ProbeRequests.Select(source => source.Kind)
            .Should().BeEquivalentTo(new[]
            {
                EsptoolSourceKind.Python,
                EsptoolSourceKind.Exe,
                EsptoolSourceKind.SystemPath
            });
        host.Welcome.PythonStatus.Should().Contain("найдено");
        host.Welcome.ExeStatus.Should().Contain("найдено");
        host.Welcome.SystemPathStatus.Should().Contain("найдено");
        host.Welcome.IsChecking.Should().BeFalse();
    }

    [Fact(DisplayName = "Первоначальная настройка: при первом запуске автоматически выбирается первый найденный источник")]
    public async Task DetectAsync_SelectsFirstAvailableSourceOnFirstRun()
    {
        using var host = TestApplicationHost.Create(new AppSettings { IsConfigured = false });
        host.Configurator.ProbeHandler = (source, _) => Task.FromResult(source.Kind switch
        {
            EsptoolSourceKind.Python => new EsptoolProbe(false, "Python не найден"),
            EsptoolSourceKind.Exe => new EsptoolProbe(true, "EXE найден"),
            _ => new EsptoolProbe(true, "PATH найден")
        });

        await host.Welcome.DetectAsync();

        host.Welcome.SelectedKind.Should().Be(EsptoolSourceKind.Exe);
    }

    [Fact(DisplayName = "Первоначальная настройка: ручной выбор пользователя не перезаписывается результатом проверки")]
    public async Task DetectAsync_PreservesUserSelection()
    {
        using var host = TestApplicationHost.Create(new AppSettings { IsConfigured = false });
        host.Configurator.ProbeHandler = (_, _) =>
            Task.FromResult(new EsptoolProbe(true, "найдено"));
        host.Welcome.IsSystemPath = true;

        await host.Welcome.DetectAsync();

        host.Welcome.SelectedKind.Should().Be(EsptoolSourceKind.SystemPath);
    }

    [Fact(DisplayName = "Первоначальная настройка: изменение Python-пути сбрасывает предыдущий результат проверки")]
    public async Task ChangingPythonPath_InvalidatesPreviousProbeResult()
    {
        using var host = TestApplicationHost.Create();
        host.Configurator.ProbeHandler = (_, _) =>
            Task.FromResult(new EsptoolProbe(true, "Python найден"));
        await host.Welcome.DetectAsync();
        host.Welcome.PythonStatus.Should().Contain("✓");

        host.Welcome.PythonPath = "python-custom";

        host.Welcome.PythonStatus.Should().Be("изменено — нажмите «Проверить»");
        host.Welcome.Message.Should().Be("Настройки выбранного источника изменены — нажмите «Проверить».");
    }

    [Fact(DisplayName = "Первоначальная настройка: Continue сохраняет настройки и применяет выбранный esptool")]
    public void Continue_SavesSettingsAndAppliesConfiguration()
    {
        using var host = TestApplicationHost.Create();
        var vm = host.Welcome;
        vm.SelectedKind = EsptoolSourceKind.Exe;
        vm.PythonPath = " python3 ";
        vm.ExePath = " C:/tools/esptool.exe ";
        vm.SelectedChip = "esp32s3";
        vm.SelectedBaud = "921600";
        vm.ShowOnStart = false;

        AppSettings? appliedSettings = null;
        bool? saveSucceeded = null;
        vm.SettingsApplied += (settings, saved) =>
        {
            appliedSettings = settings;
            saveSucceeded = saved;
        };

        vm.ContinueCommand.Execute(null);

        host.SettingsService.SaveCallCount.Should().Be(1);
        host.SettingsService.LastSavedSettings.Should().BeSameAs(appliedSettings);
        host.Configurator.Current.Should().Be(new EsptoolSource(
            EsptoolSourceKind.Exe,
            "python3",
            "C:/tools/esptool.exe"));
        appliedSettings.Should().NotBeNull();
        appliedSettings!.IsConfigured.Should().BeTrue();
        appliedSettings.DefaultChip.Should().Be("esp32s3");
        appliedSettings.DefaultBaud.Should().Be("921600");
        appliedSettings.ShowWelcomeOnStart.Should().BeFalse();
        saveSucceeded.Should().BeTrue();
    }

    [Fact(DisplayName = "Первоначальная настройка: событие продолжения сообщает, если сохранение настроек не удалось")]
    public void Continue_ReportsFailedSettingsSave()
    {
        using var host = TestApplicationHost.Create();
        host.SettingsService.SaveResult = false;
        bool? saveSucceeded = null;
        host.Welcome.SettingsApplied += (_, saved) => saveSucceeded = saved;

        host.Welcome.ContinueCommand.Execute(null);

        saveSucceeded.Should().BeFalse();
    }

    [Fact(DisplayName = "Первоначальная настройка: Cancel восстанавливает исходные настройки и запрашивает закрытие")]
    public void Cancel_ResetsValuesAndRaisesCloseRequested()
    {
        using var host = TestApplicationHost.Create(new AppSettings
        {
            IsConfigured = true,
            SourceKind = EsptoolSourceKind.Python,
            PythonPath = "python-original",
            DefaultChip = "esp32",
            DefaultBaud = "460800",
            ShowWelcomeOnStart = true
        });
        var vm = host.Welcome;
        vm.PythonPath = "python-changed";
        vm.SelectedChip = "esp32s3";
        var closeRequested = false;
        vm.CloseRequested += () => closeRequested = true;

        vm.CancelCommand.Execute(null);

        vm.PythonPath.Should().Be("python-original");
        vm.SelectedChip.Should().Be("esp32");
        closeRequested.Should().BeTrue();
    }
}
