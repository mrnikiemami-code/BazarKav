using BazarKav.Tenant.Application.Commands.CreateTenant;
using BazarKav.Tenant.Contracts;
using BazarKav.Tenant.Presentation.Endpoints;
using FluentValidation;
using MediatR;
using Xunit;
using TenantEntity = BazarKav.Tenant.Domain.Entities.Tenant;

namespace BazarKav.ArchitectureTests;

public sealed class TenantArchitectureTests
{
    [Fact]
    public void Domain_and_contracts_have_only_system_dependencies()
    {
        foreach (var assembly in new[] { typeof(TenantEntity).Assembly, typeof(TenantDto).Assembly })
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(),
                name => !name.Name!.StartsWith("System", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_and_presentation_have_no_persistence_dependency()
    {
        foreach (var assembly in new[] { typeof(CreateTenantCommand).Assembly, typeof(TenantEndpoints).Assembly })
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), name =>
                name.Name!.Contains("Infrastructure") || name.Name.Contains("EntityFrameworkCore")
                || name.Name.StartsWith("Npgsql", StringComparison.Ordinal));
    }

    [Fact]
    public void All_validators_and_handlers_follow_separate_folders_and_namespaces()
    {
        var types = typeof(CreateTenantCommand).Assembly.GetTypes();
        var validators = types.Where(type => !type.IsAbstract && type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))).ToArray();
        Assert.Equal(5, validators.Length);
        Assert.All(validators, validator => Assert.StartsWith("BazarKav.Tenant.Application.Validation.", validator.Namespace));
        var handlers = types.Where(type => !type.IsAbstract && type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))).ToArray();
        Assert.Equal(5, handlers.Length);
        Assert.All(handlers, handler => Assert.Contains(
            handler.Name.EndsWith("CommandHandler", StringComparison.Ordinal) ? ".Commands." : ".Queries.", handler.Namespace!));
    }
}
