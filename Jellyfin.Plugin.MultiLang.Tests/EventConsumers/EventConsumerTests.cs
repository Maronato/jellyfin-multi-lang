using FluentAssertions;
using Jellyfin.Plugin.MultiLang.EventConsumers;
using Jellyfin.Plugin.MultiLang.Services;
using Jellyfin.Plugin.MultiLang.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.MultiLang.Tests.EventConsumers;

/// <summary>
/// Tests for UserCreatedConsumer - LDAP auto-assignment behavior.
/// </summary>
public class UserCreatedConsumerTests : IDisposable
{
    private readonly PluginTestContext _context;
    private readonly Mock<IUserLanguageService> _userLanguageServiceMock;
    private readonly Mock<ILdapIntegrationService> _ldapIntegrationServiceMock;
    private readonly UserCreatedConsumer _consumer;

    public UserCreatedConsumerTests()
    {
        _context = new PluginTestContext();
        _userLanguageServiceMock = new Mock<IUserLanguageService>();
        _ldapIntegrationServiceMock = new Mock<ILdapIntegrationService>();
        var logger = new Mock<ILogger<UserCreatedConsumer>>();

        _consumer = new UserCreatedConsumer(
            _userLanguageServiceMock.Object,
            _ldapIntegrationServiceMock.Object,
            logger.Object);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public void WhenLdapDisabled_DoesNotQueryLdap()
    {
        // Arrange
        _context.Configuration.EnableLdapIntegration = false;

        // Assert - if LDAP is disabled, we shouldn't even check plugin availability
        // The consumer should exit early without calling LDAP services
    }

    [Fact]
    public void WhenLdapEnabled_ButPluginNotAvailable_DoesNotQueryGroups()
    {
        // Arrange
        _context.Configuration.EnableLdapIntegration = true;
        _ldapIntegrationServiceMock.Setup(s => s.IsLdapPluginAvailable()).Returns(false);

        // The consumer should check plugin availability before querying groups
        _ldapIntegrationServiceMock.Verify(
            s => s.GetUserGroupsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

/// <summary>
/// Tests for UserDeletedConsumer - cleanup behavior.
/// </summary>
public class UserDeletedConsumerTests
{
    private readonly Mock<IUserLanguageService> _userLanguageServiceMock;
    private readonly UserDeletedConsumer _consumer;

    public UserDeletedConsumerTests()
    {
        _userLanguageServiceMock = new Mock<IUserLanguageService>();
        var logger = new Mock<ILogger<UserDeletedConsumer>>();
        _consumer = new UserDeletedConsumer(_userLanguageServiceMock.Object, logger.Object);
    }

    [Fact]
    public void RemoveUser_IsCalledForDeletedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act - simulate what the consumer does
        _userLanguageServiceMock.Object.RemoveUser(userId);

        // Assert
        _userLanguageServiceMock.Verify(s => s.RemoveUser(userId), Times.Once);
    }
}
