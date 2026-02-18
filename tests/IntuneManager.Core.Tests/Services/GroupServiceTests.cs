using IntuneManager.Core.Services;
using Microsoft.Graph.Beta.Models;

namespace IntuneManager.Core.Tests.Services;

public class GroupServiceTests
{
    [Fact]
    public void InferGroupType_SecurityEnabledOnly_ReturnsSecurity()
    {
        var group = new Group
        {
            SecurityEnabled = true,
            MailEnabled = false,
            GroupTypes = []
        };

        var result = GroupService.InferGroupType(group);
        Assert.Equal("Security", result);
    }

    [Fact]
    public void InferGroupType_MailEnabledSecurity_ReturnsMailEnabledSecurity()
    {
        var group = new Group
        {
            SecurityEnabled = true,
            MailEnabled = true,
            GroupTypes = []
        };

        var result = GroupService.InferGroupType(group);
        Assert.Equal("Mail-enabled Security", result);
    }

    [Fact]
    public void InferGroupType_DistributionGroup_ReturnsDistribution()
    {
        var group = new Group
        {
            SecurityEnabled = false,
            MailEnabled = true,
            GroupTypes = []
        };

        var result = GroupService.InferGroupType(group);
        Assert.Equal("Distribution", result);
    }

    [Fact]
    public void InferGroupType_UnifiedGroup_ReturnsMicrosoft365()
    {
        var group = new Group
        {
            SecurityEnabled = true,
            MailEnabled = false,
            GroupTypes = ["Unified"]
        };

        var result = GroupService.InferGroupType(group);
        Assert.Equal("Microsoft 365", result);
    }

    [Fact]
    public void InferGroupType_NoFlags_ReturnsSecurity()
    {
        var group = new Group
        {
            SecurityEnabled = false,
            MailEnabled = false,
            GroupTypes = []
        };

        var result = GroupService.InferGroupType(group);

        Assert.Equal("Security", result);
    }

    [Fact]
    public void InferGroupType_DynamicSecurityGroup_ReturnsSecurity()
    {
        var group = new Group
        {
            SecurityEnabled = true,
            MailEnabled = false,
            GroupTypes = ["DynamicMembership"]
        };

        var result = GroupService.InferGroupType(group);

        // A dynamic group that is security-enabled but not Unified → Security
        Assert.Equal("Security", result);
    }

    [Fact]
    public void InferGroupType_DynamicM365Group_ReturnsMicrosoft365()
    {
        var group = new Group
        {
            SecurityEnabled = false,
            MailEnabled = true,
            GroupTypes = ["Unified", "DynamicMembership"]
        };

        var result = GroupService.InferGroupType(group);

        Assert.Equal("Microsoft 365", result);
    }

    [Fact]
    public void InferGroupType_NullGroupTypes_ReturnsSecurity()
    {
        var group = new Group
        {
            SecurityEnabled = true,
            MailEnabled = false,
            GroupTypes = null
        };

        var result = GroupService.InferGroupType(group);

        Assert.Equal("Security", result);
    }

    [Fact]
    public void GroupMemberCounts_RecordProperties()
    {
        var counts = new GroupMemberCounts(10, 5, 2, 17);

        Assert.Equal(10, counts.Users);
        Assert.Equal(5, counts.Devices);
        Assert.Equal(2, counts.NestedGroups);
        Assert.Equal(17, counts.Total);
    }
}
