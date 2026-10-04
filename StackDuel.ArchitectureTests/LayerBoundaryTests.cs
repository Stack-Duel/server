using NetArchTest.Rules;
using StackDuel.Application.Commands;
using StackDuel.Domain.SeedWork;
using StackDuel.Infrastructure;
using System.Reflection;

namespace StackDuel.ArchitectureTests;

public class LayerBoundaryTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ICommand).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(InfrastructureServiceRegistration).Assembly;

    [Fact]
    public void Domain_Should_Not_Depend_On_Application()
    {
        var result = Types.InAssembly(DomainAssembly).ShouldNot().HaveDependencyOn("StackDuel.Application").GetResult();

        AssertPasses(result);
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly).ShouldNot().HaveDependencyOn("StackDuel.Infrastructure").GetResult();

        AssertPasses(result);
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(DomainAssembly).ShouldNot().HaveDependencyOn("StackDuel.Api").GetResult();

        AssertPasses(result);
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Application_Level_Frameworks()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Mediator", "FluentValidation", "Ardalis.Result", "Microsoft.EntityFrameworkCore")
            .GetResult();

        AssertPasses(result);
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("StackDuel.Infrastructure")
            .GetResult();

        AssertPasses(result);
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly).ShouldNot().HaveDependencyOn("StackDuel.Api").GetResult();

        AssertPasses(result);
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly).ShouldNot().HaveDependencyOn("StackDuel.Api").GetResult();

        AssertPasses(result);
    }

    private static void AssertPasses(TestResult result) => Assert.True(result.IsSuccessful);
}