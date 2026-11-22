using ESPApp.UI.ViewModels;
using FluentAssertions;

namespace ESPApp.Tests;

[Trait("Category", "Unit")]
public sealed class MainViewModelTests
{
    [Fact(DisplayName = "Главное окно: при запуске активна страница прошивки")]
    public void MainViewModel_StartsOnFirmwarePage()
    {
        using var host = TestApplicationHost.Create();

        host.Main.CurrentPage.Should().BeSameAs(host.Firmware);
        host.Firmware.IsSelected.Should().BeTrue();
    }

    [Fact(DisplayName = "Навигация: команда страницы меняет текущую страницу и выделение меню")]
    public void NavigateCommand_ChangesCurrentPageAndSelection()
    {
        using var host = TestApplicationHost.Create();
        var main = host.Main;

        host.Memory.NavigateCommand.Execute(null);

        main.CurrentPage.Should().BeSameAs(host.Memory);
        host.Memory.IsSelected.Should().BeTrue();
        main.NavigationPages
            .Where(page => !ReferenceEquals(page, host.Memory))
            .Should()
            .OnlyContain(page => !page.IsSelected);
    }

    [Fact(DisplayName = "DI: все ViewModel разрешаются как одни и те же Singleton-экземпляры")]
    public void ViewModels_AreResolvedFromDependencyInjectionAsSingletons()
    {
        using var host = TestApplicationHost.Create();
        var main = host.Main;

        host.GetRequired<ConnectionViewModel>().Should().BeSameAs(main.Connection);
        host.GetRequired<EspToolExecutionViewModel>().Should().BeSameAs(main.Execution);
        host.GetRequired<FirmwarePageViewModel>().Should().BeSameAs(main.Firmware);
        host.GetRequired<MemoryPageViewModel>().Should().BeSameAs(main.Memory);
        host.GetRequired<ChipInfoPageViewModel>().Should().BeSameAs(main.ChipInfo);
        host.GetRequired<ToolsPageViewModel>().Should().BeSameAs(main.Tools);
        host.GetRequired<WelcomeViewModel>().Should().BeSameAs(main.Welcome);
        host.GetRequired<MainViewModel>().Should().BeSameAs(main);
    }

    [Fact(DisplayName = "Навигация: все четыре страницы присутствуют в меню в заданном порядке")]
    public void NavigationPages_ContainsAllPagesInExpectedOrder()
    {
        using var host = TestApplicationHost.Create();

        host.Main.NavigationPages.Should().Equal(
            host.Firmware,
            host.Memory,
            host.ChipInfo,
            host.Tools);
    }
}
