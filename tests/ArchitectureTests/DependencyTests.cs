using BazarKav.BuildingBlocks.Application.Abstractions;
using BazarKav.BuildingBlocks.Observability.Correlation;
using BazarKav.BuildingBlocks.Web.Errors;
using Xunit;

namespace BazarKav.ArchitectureTests;

public sealed class DependencyTests
{
    [Fact]
    public void Application_has_no_web_logging_implementation_or_observability_dependency()
    {
        var references = typeof(IQuery<>).Assembly.GetReferencedAssemblies().Select(a => a.Name!);
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Serilog", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.Contains("Observability") || name.Contains("BuildingBlocks.Web"));
    }

    [Fact]
    public void Observability_has_no_application_or_web_dependency()
    {
        var references = typeof(CorrelationId).Assembly.GetReferencedAssemblies().Select(a => a.Name!);
        Assert.DoesNotContain(references, name => name.Contains("BuildingBlocks.Application") || name.Contains("BuildingBlocks.Web"));
        Assert.DoesNotContain(references, name => name.StartsWith("MediatR", StringComparison.Ordinal)
            || name.StartsWith("FluentValidation", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildingBlocks_have_no_module_or_persistence_dependency()
    {
        foreach (var assembly in new[] { typeof(IQuery<>).Assembly, typeof(CorrelationId).Assembly, typeof(ResultExtensions).Assembly })
        {
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference =>
                reference.Name!.StartsWith("BazarKav.", StringComparison.Ordinal)
                && !reference.Name.StartsWith("BazarKav.BuildingBlocks.", StringComparison.Ordinal));
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), reference =>
                reference.Name!.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        }
    }
}
