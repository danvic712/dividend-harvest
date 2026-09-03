using System.Linq.Expressions;
using DividendHarvest.Domain.Contracts;
using Moq;

namespace DividendHarvest.Application.Tests;

internal static class RepositoryMock
{
    public static Mock<IRepository<TEntity>> Create<TEntity>(
        IEnumerable<TEntity> entities)
        where TEntity : class
    {
        var data = entities.ToList();
        var repository = new Mock<IRepository<TEntity>>();

        repository
            .Setup(item => item.AnyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CancellationToken _) => data.Count > 0);
        repository
            .Setup(item => item.AnyAsync(
                It.IsAny<Expression<Func<TEntity, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> predicate, CancellationToken _) =>
                data.AsQueryable().Any(predicate));
        repository
            .Setup(item => item.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<TEntity, bool>>>(),
                It.IsAny<Expression<Func<TEntity, object>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .ReturnsAsync((
                Expression<Func<TEntity, bool>> predicate,
                Expression<Func<TEntity, object>>? orderBy,
                bool descending,
                CancellationToken _,
                bool _) => ApplyOrder(data.AsQueryable().Where(predicate), orderBy, descending)
                    .FirstOrDefault());
        repository
            .Setup(item => item.SingleOrDefaultAsync(
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .ReturnsAsync((CancellationToken _, bool _) => data.SingleOrDefault());
        repository
            .Setup(item => item.SingleOrDefaultAsync(
                It.IsAny<Expression<Func<TEntity, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> predicate, CancellationToken _, bool _) =>
                data.AsQueryable().SingleOrDefault(predicate));
        repository
            .Setup(item => item.ListAsync(
                It.IsAny<Expression<Func<TEntity, bool>>?>(),
                It.IsAny<IReadOnlyList<Expression<Func<TEntity, object>>>?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .ReturnsAsync((
                Expression<Func<TEntity, bool>>? predicate,
                IReadOnlyList<Expression<Func<TEntity, object>>>? orderBy,
                bool descending,
                CancellationToken _,
                bool _) =>
            {
                var query = data.AsQueryable();
                if (predicate is not null)
                {
                    query = query.Where(predicate);
                }

                return ApplyOrder(query, orderBy, descending).ToArray();
            });
        repository
            .Setup(item => item.AddAsync(
                It.IsAny<TEntity>(),
                It.IsAny<CancellationToken>()))
            .Callback((TEntity entity, CancellationToken _) => data.Add(entity))
            .Returns(Task.CompletedTask);
        repository
            .Setup(item => item.RemoveWhereAsync(
                It.IsAny<Expression<Func<TEntity, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> predicate, CancellationToken _) =>
            {
                var removed = data.RemoveAll(entity => predicate.Compile()(entity));
                return removed;
            });

        return repository;
    }

    private static IQueryable<TEntity> ApplyOrder<TEntity>(
        IQueryable<TEntity> query,
        Expression<Func<TEntity, object>>? orderBy,
        bool descending)
        where TEntity : class
        => orderBy is null
            ? query
            : descending
                ? query.OrderByDescending(orderBy)
                : query.OrderBy(orderBy);

    private static IQueryable<TEntity> ApplyOrder<TEntity>(
        IQueryable<TEntity> query,
        IReadOnlyList<Expression<Func<TEntity, object>>>? orderBy,
        bool descending)
        where TEntity : class
    {
        if (orderBy is null || orderBy.Count == 0)
        {
            return query;
        }

        var orderedQuery = descending
            ? query.OrderByDescending(orderBy[0])
            : query.OrderBy(orderBy[0]);
        for (var index = 1; index < orderBy.Count; index++)
        {
            orderedQuery = descending
                ? orderedQuery.ThenByDescending(orderBy[index])
                : orderedQuery.ThenBy(orderBy[index]);
        }

        return orderedQuery;
    }
}
