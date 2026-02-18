using Azure.Identity;
using IntuneManager.Core.Models;

namespace IntuneManager.Core.Tests.Models;

public class CloudEndpointsTests
{
    [Theory]
    [InlineData(CloudEnvironment.Commercial, "https://graph.microsoft.com/beta")]
    [InlineData(CloudEnvironment.GCC, "https://graph.microsoft.com/beta")]
    [InlineData(CloudEnvironment.GCCHigh, "https://graph.microsoft.us/beta")]
    [InlineData(CloudEnvironment.DoD, "https://dod-graph.microsoft.us/beta")]
    public void GetEndpoints_ReturnsCorrectGraphEndpoint(CloudEnvironment cloud, string expectedEndpoint)
    {
        var (graphBaseUrl, _) = CloudEndpoints.GetEndpoints(cloud);
        Assert.Equal(expectedEndpoint, graphBaseUrl);
    }

    [Theory]
    [InlineData(CloudEnvironment.Commercial)]
    [InlineData(CloudEnvironment.GCC)]
    public void GetEndpoints_PublicCloud_ReturnsPublicAuthorityHost(CloudEnvironment cloud)
    {
        var (_, authorityHost) = CloudEndpoints.GetEndpoints(cloud);
        Assert.Equal(AzureAuthorityHosts.AzurePublicCloud, authorityHost);
    }

    [Theory]
    [InlineData(CloudEnvironment.GCCHigh)]
    [InlineData(CloudEnvironment.DoD)]
    public void GetEndpoints_GovernmentCloud_ReturnsGovernmentAuthorityHost(CloudEnvironment cloud)
    {
        var (_, authorityHost) = CloudEndpoints.GetEndpoints(cloud);
        Assert.Equal(AzureAuthorityHosts.AzureGovernment, authorityHost);
    }

    [Theory]
    [InlineData(CloudEnvironment.Commercial, "https://graph.microsoft.com/.default")]
    [InlineData(CloudEnvironment.GCCHigh, "https://graph.microsoft.us/.default")]
    [InlineData(CloudEnvironment.DoD, "https://dod-graph.microsoft.us/.default")]
    public void GetScopes_ReturnsCorrectScope(CloudEnvironment cloud, string expectedScope)
    {
        var scopes = CloudEndpoints.GetScopes(cloud);
        Assert.Single(scopes);
        Assert.Equal(expectedScope, scopes[0]);
    }
}
