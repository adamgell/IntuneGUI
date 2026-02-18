using IntuneManager.Core.Services;
using Microsoft.Graph.Beta;

namespace IntuneManager.Core.Tests.Services;

public class Wave45ServiceContractsTests
{
    public static IEnumerable<object[]> ServiceContracts()
    {
        yield return [typeof(AutopilotService), typeof(IAutopilotService)];
        yield return [typeof(DeviceHealthScriptService), typeof(IDeviceHealthScriptService)];
        yield return [typeof(MacCustomAttributeService), typeof(IMacCustomAttributeService)];
        yield return [typeof(FeatureUpdateProfileService), typeof(IFeatureUpdateProfileService)];
        yield return [typeof(NamedLocationService), typeof(INamedLocationService)];
        yield return [typeof(AuthenticationStrengthService), typeof(IAuthenticationStrengthService)];
        yield return [typeof(AuthenticationContextService), typeof(IAuthenticationContextService)];
        yield return [typeof(TermsOfUseService), typeof(ITermsOfUseService)];
    }

    [Theory]
    [MemberData(nameof(ServiceContracts))]
    public void Service_ImplementsInterface(Type serviceType, Type interfaceType)
    {
        Assert.True(interfaceType.IsAssignableFrom(serviceType));
    }

    [Theory]
    [MemberData(nameof(ServiceContracts))]
    public void Service_HasGraphClientConstructor(Type serviceType, Type _)
    {
        var constructor = serviceType.GetConstructor([typeof(GraphServiceClient)]);
        Assert.NotNull(constructor);
    }
}