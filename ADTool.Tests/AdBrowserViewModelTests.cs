using ADTool.Models;
using ADTool.Services;
using ADTool.ViewModels;
using Moq;
using Xunit;

namespace ADTool.Tests;

public class AdBrowserViewModelTests
{
    private static Mock<IAdService> MakeMock(
        IReadOnlyList<OuNode>? tree = null,
        IReadOnlyList<AdUser>? users = null)
    {
        var mock = new Mock<IAdService>();
        mock.Setup(s => s.GetOuTreeAsync())
            .ReturnsAsync(tree ?? Array.Empty<OuNode>());
        mock.Setup(s => s.GetUsersInOuAsync(It.IsAny<string>()))
            .ReturnsAsync(users ?? Array.Empty<AdUser>());
        mock.Setup(s => s.CheckIsDomainAdminAsync()).ReturnsAsync(true);
        return mock;
    }

    [Fact]
    public async Task LoadTreeAsync_PopulatesOuNodes()
    {
        IReadOnlyList<OuNode> tree = [new OuNode("contoso.com", "DC=contoso,DC=com", [])];
        var vm = new AdBrowserViewModel(MakeMock(tree: tree).Object, _ => { });

        await vm.LoadTreeAsync();

        Assert.Single(vm.OuNodes);
        Assert.Equal("contoso.com", vm.OuNodes[0].Name);
    }

    [Fact]
    public async Task LoadTreeAsync_SetsIsLoadingTreeFalseWhenDone()
    {
        var vm = new AdBrowserViewModel(MakeMock().Object, _ => { });

        await vm.LoadTreeAsync();

        Assert.False(vm.IsLoadingTree);
    }

    [Fact]
    public async Task SettingSelectedOu_LoadsUsersIntoCollection()
    {
        IReadOnlyList<AdUser> users = [new AdUser("alice@contoso.com", "Alice Smith")];
        var vm = new AdBrowserViewModel(MakeMock(users: users).Object, _ => { });

        vm.SelectedOu = new OuNode("Sales", "OU=Sales,DC=contoso,DC=com", []);
        await vm.LatestLoadUsersTask;

        Assert.Single(vm.Users);
        Assert.Equal("alice@contoso.com", vm.Users[0].UPN);
        Assert.Equal("Alice Smith", vm.Users[0].DisplayName);
    }

    [Fact]
    public async Task AddSelectedToListCommand_DisabledWhenNoUsersSelected()
    {
        IReadOnlyList<AdUser> users = [new AdUser("alice@contoso.com", "Alice Smith")];
        var vm = new AdBrowserViewModel(MakeMock(users: users).Object, _ => { });

        vm.SelectedOu = new OuNode("Sales", "OU=Sales,DC=contoso,DC=com", []);
        await vm.LatestLoadUsersTask;

        Assert.False(vm.AddSelectedToListCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddSelectedToListCommand_EnabledWhenUserSelected()
    {
        IReadOnlyList<AdUser> users = [new AdUser("alice@contoso.com", "Alice Smith")];
        var vm = new AdBrowserViewModel(MakeMock(users: users).Object, _ => { });

        vm.SelectedOu = new OuNode("Sales", "OU=Sales,DC=contoso,DC=com", []);
        await vm.LatestLoadUsersTask;
        vm.Users[0].IsSelected = true;

        Assert.True(vm.AddSelectedToListCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddSelectedToListCommand_PassesSelectedUsersToCallback()
    {
        IReadOnlyList<AdUser> users =
        [
            new AdUser("alice@contoso.com", "Alice Smith"),
            new AdUser("bob@contoso.com", "Bob Jones"),
        ];
        IReadOnlyList<AdUser>? received = null;
        var vm = new AdBrowserViewModel(MakeMock(users: users).Object, u => received = u);

        vm.SelectedOu = new OuNode("Sales", "OU=Sales,DC=contoso,DC=com", []);
        await vm.LatestLoadUsersTask;
        vm.Users[0].IsSelected = true;
        vm.AddSelectedToListCommand.Execute(null);

        Assert.NotNull(received);
        Assert.Single(received);
        Assert.Equal("alice@contoso.com", received[0].UPN);
    }

    [Fact]
    public async Task AddSelectedToListCommand_RaisesRequestClose()
    {
        IReadOnlyList<AdUser> users = [new AdUser("alice@contoso.com", "Alice Smith")];
        var vm = new AdBrowserViewModel(MakeMock(users: users).Object, _ => { });
        bool closeFired = false;
        vm.RequestClose += (_, _) => closeFired = true;

        vm.SelectedOu = new OuNode("Sales", "OU=Sales,DC=contoso,DC=com", []);
        await vm.LatestLoadUsersTask;
        vm.Users[0].IsSelected = true;
        vm.AddSelectedToListCommand.Execute(null);

        Assert.True(closeFired);
    }
}
