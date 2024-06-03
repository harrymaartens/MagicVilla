using MagicVilla_API.Models;

namespace MagicVilla_API.Repository.IRepostiory
{
    public interface IVillaNumberRepository : IRepository<VillaNumber>
    {
        Task<VillaNumber> UpdateAsync(VillaNumber entity);
    }
}
