using ServiceLifeOS.Application;
using ServiceLifeOS.Domain.Entities;
using Xunit;

namespace ServiceLifeOS.Tests.Architecture;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Application_DoesNotReferenceEntityFrameworkCore()
    {
        var references = typeof(DependencyInjection).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, x => x.Name?.StartsWith("Microsoft.EntityFrameworkCore") == true);
    }

    [Fact]
    public void Application_DoesNotReferenceAspNetCore()
    {
        var references = typeof(DependencyInjection).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, x => x.Name?.StartsWith("Microsoft.AspNetCore") == true);
    }

    [Fact]
    public void Domain_DoesNotReferenceOuterLayers()
    {
        var references = typeof(AppUser).Assembly.GetReferencedAssemblies();
        var forbidden = new[]
        {
            "ServiceLifeOS.Application",
            "ServiceLifeOS.Infrastructure",
            "ServiceLifeOS.Api"
        };

        Assert.DoesNotContain(references, x => forbidden.Contains(x.Name));
    }
}
