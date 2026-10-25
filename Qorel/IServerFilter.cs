using System.Linq.Expressions;

namespace Qorel;

/// <summary>
/// A filter that gets applied to every query for <typeparamref name="TEntity"/>, like only showing users their own
/// items. Clients can't see it or turn it off.
/// </summary>
/// <remarks>
/// Register it in DI, usually as scoped so it can use the current user. The pipeline adds every registered filter to
/// every call, including pages, counts, <c>FindAsync</c> and <c>ListAsync</c>, so you can't forget it on one endpoint.
/// The predicate can use any property on the entity, navigation properties included, as long as your LINQ provider can
/// translate it.
/// </remarks>
/// <example>
/// <code>
/// public sealed class VisibleProducts(ICurrentUser user) : IServerFilter&lt;Product&gt;
/// {
///     public Expression&lt;Func&lt;Product, bool&gt;&gt; Predicate =&gt; user.IsAdmin
///         ? product =&gt; true
///         : product =&gt; product.OwnerId == user.Id || product.Shares.Any(share =&gt; share.UserId == user.Id);
/// }
///
/// builder.Services.AddScoped&lt;IServerFilter&lt;Product&gt;, VisibleProducts&gt;();
/// </code>
/// </example>
/// <typeparam name="TEntity">The entity to filter.</typeparam>
public interface IServerFilter<TEntity>
    where TEntity : class
{
    /// <summary>
    /// The condition an item has to match to show up in results.
    /// </summary>
    Expression<Func<TEntity, bool>> Predicate { get; }
}
