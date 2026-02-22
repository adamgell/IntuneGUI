using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class UserServiceTests
{
    [Fact]
    public void Service_ImplementsInterface()
    {
        Assert.True(typeof(IUserService).IsAssignableFrom(typeof(UserService)));
    }

    [Fact]
    public void Service_HasGraphClientConstructor()
    {
        var constructor = typeof(UserService).GetConstructor([typeof(GraphServiceClient)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void Interface_DefinesListUsersMethod()
    {
        var method = typeof(IUserService).GetMethod("ListUsersAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<User>>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesSearchUsersMethod()
    {
        var method = typeof(IUserService).GetMethod("SearchUsersAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<User>>), method.ReturnType);
    }

    [Fact]
    public void Interface_HasTwoMethods()
    {
        var methods = typeof(IUserService).GetMethods();
        Assert.Equal(2, methods.Length);
    }

    [Fact]
    public void ListUsersAsync_HasCancellationTokenWithDefault()
    {
        var method = typeof(IUserService).GetMethod("ListUsersAsync");
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
        Assert.True(parameters[0].HasDefaultValue);
    }

    [Fact]
    public void SearchUsersAsync_AcceptsStringQueryAndCancellationToken()
    {
        var method = typeof(IUserService).GetMethod("SearchUsersAsync");
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void Interface_AllMethodsAcceptCancellationToken()
    {
        var methods = typeof(IUserService).GetMethods();
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var lastParam = parameters[^1];
            Assert.Equal(typeof(CancellationToken), lastParam.ParameterType);
            Assert.True(lastParam.HasDefaultValue);
        }
    }

    [Fact]
    public void Service_HasPrivateGraphClientField()
    {
        var field = typeof(UserService).GetField("_graphClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);
        Assert.Equal(typeof(GraphServiceClient), field.FieldType);
    }
}
