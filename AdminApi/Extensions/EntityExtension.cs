using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.Data;

namespace AdminApi.Extensions
{
    public static class EntityExtension
    {
        public static async Task<bool> IsExistsAsync<T>(this ApplicationDbContext context, string key, object value) where T : class
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var propertyAccess = Expression.Property(parameter, key);
            var constant = Expression.Constant(value);
            var equalExpression = Expression.Equal(propertyAccess, constant);
            var lambda = Expression.Lambda<Func<T, bool>>(equalExpression, parameter);

            return await context.Set<T>().AnyAsync(lambda);
        }

    }
}