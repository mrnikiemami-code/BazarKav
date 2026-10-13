using BazarKav.Tenant.Application.Commands.CreateTenant;
using BazarKav.Tenant.Contracts;
using BazarKav.Tenant.Presentation.Endpoints;
using BazarKav.Tenant.Presentation;
using BazarKav.BuildingBlocks.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using FluentValidation;
using MediatR;
using Xunit;
using TenantEntity = BazarKav.Tenant.Domain.Entities.Tenant;

namespace BazarKav.ArchitectureTests;

public sealed class TenantArchitectureTests
{
    [Fact]
    public void Contracts_do_not_contain_http_request_wrappers()
    {
        Assert.DoesNotContain(typeof(TenantDto).Assembly.GetExportedTypes(),
            type => type.Name.EndsWith("Request", StringComparison.Ordinal));
    }

    [Fact]
    public void Actual_endpoint_body_types_belong_to_presentation_requests()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddBazarKavApplication(TenantPresentation.ApplicationAssembly);
        builder.Services.AddTenantPresentation();
        using var app = builder.Build();
        app.MapTenantEndpoints();
        var bodies = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .Select(endpoint => endpoint.Metadata.GetMetadata<IAcceptsMetadata>()?.RequestType)
            .Where(type => type is not null).Cast<Type>().ToArray();
        Assert.Equal(3, bodies.Length);
        Assert.All(bodies, type =>
        {
            Assert.Equal(typeof(TenantEndpoints).Assembly, type.Assembly);
            Assert.Equal("BazarKav.Tenant.Presentation.Requests", type.Namespace);
            Assert.EndsWith("Request", type.Name);
        });
    }

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
