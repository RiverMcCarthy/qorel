using System.Linq.Expressions;

using Qorel.Spatial;

namespace Qorel.EntityFramework.Mapping;

/// <summary>
/// Optionally implemented on a <see cref="FieldMap{TEntity,TKey}"/> to support bounding-box
/// spatial filtering. When the pipeline receives a <see cref="BoundingBox"/> in a query,
/// it calls <see cref="BuildSpatialPredicate"/> automatically.
/// </summary>
/// <typeparam name="TEntity">The EF Core entity type.</typeparam>
public interface ISpatialFilter<TEntity>
{
    /// <summary>
    /// Builds a predicate that filters results to only items whose geometry field
    /// intersects <paramref name="bounds"/>. EF Core will translate this to SQL via
    /// the configured spatial provider (e.g. NetTopologySuite and PostGIS).
    /// </summary>
    Expression<Func<TEntity, bool>> BuildSpatialPredicate(BoundingBox bounds);
}
