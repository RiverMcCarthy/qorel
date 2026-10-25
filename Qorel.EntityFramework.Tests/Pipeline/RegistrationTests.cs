using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Qorel.EntityFramework.Tests.Pipeline;

public class RegistrationTests
{
    [Fact]
    public void Pipeline_IsResolvedForEveryRegisteredModel()
    {
        using ServiceProvider provider = Services().BuildServiceProvider(validateScopes: true);
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IQueryPipeline<ProductEntity, ProductField>>().Should().NotBeNull();
    }

    [Fact]
    public void Pipeline_IsScopedLikeTheDbContext()
    {
        using ServiceProvider provider = Services().BuildServiceProvider(validateScopes: true);

        Func<IQueryPipeline<ProductEntity, ProductField>> act = () => provider.GetRequiredService<IQueryPipeline<ProductEntity, ProductField>>();

        act.Should().Throw<InvalidOperationException>().WithMessage("*scoped*");
    }

    [Fact]
    public void AddEntityFramework_Twice_Throws()
    {
        QorelBuilder builder = new ServiceCollection().AddQorel().AddEntityFramework<TestDbContext>();

        Func<QorelBuilder> act = () => builder.AddEntityFramework<TestDbContext>();

        act.Should().Throw<InvalidOperationException>();
    }

    private static IServiceCollection Services()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options => options.UseSqlite("DataSource=:memory:"));
        services.AddQorel().AddModel(new ProductQueryModel()).AddEntityFramework<TestDbContext>();
        return services;
    }
}
