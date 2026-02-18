using IntuneManager.Core.Auth;
using IntuneManager.Core.Models;

namespace IntuneManager.Core.Tests.Auth;

public class AuthMethodTests
{
    [Fact]
    public void AuthMethod_OnlySupportedValuesExist()
    {
        var values = Enum.GetValues<AuthMethod>();
        Assert.Equal(2, values.Length);
        Assert.Contains(AuthMethod.Interactive, values);
        Assert.Contains(AuthMethod.ClientSecret, values);
    }

    [Fact]
    public void TenantProfile_DefaultAuthMethodIsInteractive()
    {
        var profile = new TenantProfile
        {
            Name = "Test",
            TenantId = Guid.NewGuid().ToString(),
            ClientId = Guid.NewGuid().ToString()
        };

        Assert.Equal(AuthMethod.Interactive, profile.AuthMethod);
    }

    [Fact]
    public void TenantProfile_DoesNotHaveCertificateThumbprintProperty()
    {
        var props = typeof(TenantProfile).GetProperties();
        Assert.DoesNotContain(props, p => p.Name == "CertificateThumbprint");
    }

    [Fact]
    public async Task InteractiveBrowserAuthProvider_Interactive_ReturnsCredential()
    {
        var provider = new InteractiveBrowserAuthProvider();
        var profile = new TenantProfile
        {
            Name = "Test",
            TenantId = Guid.NewGuid().ToString(),
            ClientId = Guid.NewGuid().ToString(),
            AuthMethod = AuthMethod.Interactive
        };

        // Should not throw — merely constructs the credential object
        var credential = await provider.GetCredentialAsync(profile);
        Assert.NotNull(credential);
    }

    [Fact]
    public async Task InteractiveBrowserAuthProvider_ClientSecret_ReturnsCredential()
    {
        var provider = new InteractiveBrowserAuthProvider();
        var profile = new TenantProfile
        {
            Name = "Test",
            TenantId = Guid.NewGuid().ToString(),
            ClientId = Guid.NewGuid().ToString(),
            AuthMethod = AuthMethod.ClientSecret,
            ClientSecret = "super-secret"
        };

        var credential = await provider.GetCredentialAsync(profile);
        Assert.NotNull(credential);
    }

    [Fact]
    public async Task InteractiveBrowserAuthProvider_ClientSecretWithEmptySecret_Throws()
    {
        var provider = new InteractiveBrowserAuthProvider();
        var profile = new TenantProfile
        {
            Name = "Test",
            TenantId = Guid.NewGuid().ToString(),
            ClientId = Guid.NewGuid().ToString(),
            AuthMethod = AuthMethod.ClientSecret,
            ClientSecret = ""
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetCredentialAsync(profile));
    }
}
