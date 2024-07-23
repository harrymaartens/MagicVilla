using MagicVilla_API.Models;
using System.Linq.Expressions;

namespace MagicVilla_API.Repository.IRepostiory
{
    public interface IRepository<T> where T : class
    {
        // Deze toevoeging: public Villa Villa { get; set; } id nodig voor deze navigatie property: public Villa Villa { get; set; }
        Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null,
            int pageSize = 3, int pageNumber = 1);
        Task<T> GetAsync(Expression<Func<T, bool>> filter = null, bool tracked = true, string? includeProperties = null);
        Task CreateAsync(T entity);
        Task RemoveAsync(T entity);
        Task SaveAsync();
    }
}
