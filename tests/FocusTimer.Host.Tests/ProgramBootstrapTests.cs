namespace FocusTimer.Host.Tests;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

public class ProgramBootstrapTests
{
    [Fact]
    public void ServicesProperty_GivenProgramType_ResolvesServiceProvider()
    {
        var programType = Type.GetType("FocusTimer.Host.Program, FocusTimer.Host");
        Assert.NotNull(programType);

        var servicesProp = programType!.GetProperty("Services", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(servicesProp);

        var serviceProvider = servicesProp!.GetValue(null) as IServiceProvider;

        Assert.NotNull(serviceProvider);
    }

    [Fact]
    public void ServicesProperty_GivenServiceProvider_ResolvesSettingsProvider()
    {
        var programType = Type.GetType("FocusTimer.Host.Program, FocusTimer.Host");
        Assert.NotNull(programType);

        var servicesProp = programType!.GetProperty("Services", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(servicesProp);

        var serviceProvider = servicesProp!.GetValue(null) as IServiceProvider;
        Assert.NotNull(serviceProvider);
        Assert.NotNull(serviceProvider!.GetService(typeof(FocusTimer.Core.Interfaces.ISettingsProvider)));
    }

    [Fact]
    public void BuildAvaloniaApp_GivenProgramType_ReturnsBuilderInstance()
    {
        var programType = Type.GetType("FocusTimer.Host.Program, FocusTimer.Host");
        Assert.NotNull(programType);

        var method = programType!.GetMethod("BuildAvaloniaApp", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);

        var builder = method!.Invoke(null, null);

        Assert.NotNull(builder);
        Assert.Equal("Avalonia.AppBuilder", builder!.GetType().FullName);
    }
}
