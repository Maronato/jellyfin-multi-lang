using FluentAssertions;
using Jellyfin.Plugin.MultiLang.Api;
using Jellyfin.Plugin.MultiLang.Models;
using Jellyfin.Plugin.MultiLang.Services;
using Jellyfin.Plugin.MultiLang.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.MultiLang.Tests.Api;

/// <summary>
/// Tests for MultiLangController.
/// These tests verify that the controller correctly handles inputs and routes to services.
/// </summary>
public class MultiLangControllerTests : IDisposable
{
    private readonly PluginTestContext _context;
    private readonly Mock<IMirrorService> _mirrorServiceMock;
    private readonly Mock<IUserLanguageService> _userLanguageServiceMock;
    private readonly Mock<ILibraryAccessService> _libraryAccessServiceMock;
    private readonly Mock<ILdapIntegrationService> _ldapIntegrationServiceMock;
    private readonly MultiLangController _controller;

    public MultiLangControllerTests()
    {
        _context = new PluginTestContext();
        _mirrorServiceMock = new Mock<IMirrorService>();
        _userLanguageServiceMock = new Mock<IUserLanguageService>();
        _libraryAccessServiceMock = new Mock<ILibraryAccessService>();
        _ldapIntegrationServiceMock = new Mock<ILdapIntegrationService>();
        var logger = new Mock<ILogger<MultiLangController>>();

        _controller = new MultiLangController(
            _mirrorServiceMock.Object,
            _userLanguageServiceMock.Object,
            _libraryAccessServiceMock.Object,
            _ldapIntegrationServiceMock.Object,
            logger.Object);
    }

    public void Dispose() => _context.Dispose();

    #region CreateAlternative - Input validation

    [Fact]
    public void CreateAlternative_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateAlternativeRequest
        {
            Name = "",
            LanguageCode = "pt-BR",
            DestinationBasePath = "/media/pt"
        };

        // Act
        var result = _controller.CreateAlternative(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void CreateAlternative_EmptyLanguageCode_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateAlternativeRequest
        {
            Name = "Portuguese",
            LanguageCode = "",
            DestinationBasePath = "/media/pt"
        };

        // Act
        var result = _controller.CreateAlternative(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void CreateAlternative_EmptyPath_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateAlternativeRequest
        {
            Name = "Portuguese",
            LanguageCode = "pt-BR",
            DestinationBasePath = ""
        };

        // Act
        var result = _controller.CreateAlternative(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void CreateAlternative_ValidRequest_CreatesAlternative()
    {
        // Arrange
        var request = new CreateAlternativeRequest
        {
            Name = "Portuguese",
            LanguageCode = "pt-BR",
            DestinationBasePath = "/media/portuguese"
        };

        // Act
        var result = _controller.CreateAlternative(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        _context.Configuration.LanguageAlternatives.Should().ContainSingle();
        var created = _context.Configuration.LanguageAlternatives[0];
        created.Name.Should().Be("Portuguese");
        created.LanguageCode.Should().Be("pt-BR");
    }

    [Fact]
    public void CreateAlternative_DefaultsMetadataFromLanguageCode()
    {
        // Arrange
        var request = new CreateAlternativeRequest
        {
            Name = "Portuguese",
            LanguageCode = "pt-BR",
            DestinationBasePath = "/media/portuguese"
            // MetadataLanguage and MetadataCountry not specified
        };

        // Act
        _controller.CreateAlternative(request);

        // Assert
        var created = _context.Configuration.LanguageAlternatives[0];
        created.MetadataLanguage.Should().Be("pt", "should extract language from code");
        created.MetadataCountry.Should().Be("BR", "should extract country from code");
    }

    #endregion

    #region GetAlternatives - Returns configured alternatives

    [Fact]
    public void GetAlternatives_ReturnsConfiguredAlternatives()
    {
        // Arrange
        _context.AddLanguageAlternative("Portuguese", "pt-BR");
        _context.AddLanguageAlternative("Spanish", "es-ES");

        // Act
        var result = _controller.GetAlternatives();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var alternatives = (List<LanguageAlternative>)okResult.Value!;
        alternatives.Should().HaveCount(2);
    }

    #endregion

    #region DeleteAlternative - Removes from configuration

    [Fact]
    public async Task DeleteAlternative_NotFound_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeleteAlternative(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteAlternative_Found_RemovesFromConfig()
    {
        // Arrange
        var alternative = _context.AddLanguageAlternative();

        // Act
        var result = await _controller.DeleteAlternative(alternative.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _context.Configuration.LanguageAlternatives.Should().BeEmpty();
    }

    #endregion

    #region AddLdapGroupMapping - Input validation

    [Fact]
    public void AddLdapGroupMapping_EmptyGroupDn_ReturnsBadRequest()
    {
        // Arrange
        var alternative = _context.AddLanguageAlternative();
        var request = new AddLdapGroupMappingRequest
        {
            LdapGroupDn = "",
            LanguageAlternativeId = alternative.Id
        };

        // Act
        var result = _controller.AddLdapGroupMapping(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void AddLdapGroupMapping_AlternativeNotFound_ReturnsBadRequest()
    {
        // Arrange
        var request = new AddLdapGroupMappingRequest
        {
            LdapGroupDn = "CN=Test,DC=test",
            LanguageAlternativeId = Guid.NewGuid() // Non-existent
        };

        // Act
        var result = _controller.AddLdapGroupMapping(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result.Result!;
        badRequest.Value.Should().Be("Language alternative not found");
    }

    [Fact]
    public void AddLdapGroupMapping_ValidRequest_AddsMappingWithPriority()
    {
        // Arrange
        var alternative = _context.AddLanguageAlternative();
        var request = new AddLdapGroupMappingRequest
        {
            LdapGroupDn = "CN=Portuguese Users,DC=example,DC=com",
            LanguageAlternativeId = alternative.Id,
            Priority = 150
        };

        // Act
        var result = _controller.AddLdapGroupMapping(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        _context.Configuration.LdapGroupMappings.Should().ContainSingle();
        var mapping = _context.Configuration.LdapGroupMappings[0];
        mapping.Priority.Should().Be(150);
    }

    #endregion

    #region Settings - Gets and updates configuration

    [Fact]
    public void GetSettings_ReturnsCurrentSettings()
    {
        // Arrange
        _context.Configuration.SyncUserDisplayLanguage = true;
        _context.Configuration.SyncUserSubtitleLanguage = false;
        _context.Configuration.EnableLdapIntegration = true;
        _context.Configuration.MirrorSyncIntervalHours = 12;

        // Act
        var result = _controller.GetSettings();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var settings = (PluginSettings)((OkObjectResult)result.Result!).Value!;
        settings.SyncUserDisplayLanguage.Should().BeTrue();
        settings.SyncUserSubtitleLanguage.Should().BeFalse();
        settings.EnableLdapIntegration.Should().BeTrue();
        settings.MirrorSyncIntervalHours.Should().Be(12);
    }

    [Fact]
    public void UpdateSettings_UpdatesConfiguration()
    {
        // Arrange
        var settings = new PluginSettings
        {
            SyncUserDisplayLanguage = false,
            SyncUserSubtitleLanguage = true,
            SyncUserAudioLanguage = true,
            EnableLdapIntegration = true,
            MirrorSyncIntervalHours = 24
        };

        // Act
        var result = _controller.UpdateSettings(settings);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _context.Configuration.SyncUserDisplayLanguage.Should().BeFalse();
        _context.Configuration.EnableLdapIntegration.Should().BeTrue();
        _context.Configuration.MirrorSyncIntervalHours.Should().Be(24);
    }

    #endregion
}

/// <summary>
/// Tests for language code parsing helpers.
/// </summary>
public class LanguageCodeParsingTests
{
    [Theory]
    [InlineData("pt-BR", "pt", "BR")]
    [InlineData("en-US", "en", "US")]
    [InlineData("zh-CN", "zh", "CN")]
    [InlineData("ja", "ja", "")]
    [InlineData("fr-CA", "fr", "CA")]
    public void ParseLanguageCode_ExtractsComponents(string code, string expectedLang, string expectedCountry)
    {
        // Simulate the helper methods in the controller
        var dashIndex = code.IndexOf('-');
        var language = dashIndex > 0 ? code.Substring(0, dashIndex) : code;
        var country = dashIndex > 0 ? code.Substring(dashIndex + 1) : string.Empty;

        language.Should().Be(expectedLang);
        country.Should().Be(expectedCountry);
    }
}
