using MagicVilla_API.Models.Dto;
using MagicVilla_API.Models;

namespace MagicVilla_API.Repository.IRepostiory
{
    public interface IUserRepository
    {
        bool IsUniqueUser(string username);
        Task<TokenDTO> Login(LoginRequestDTO loginRequestDTO);
        Task<UserDTO> Register(RegisterationRequestDTO registerationRequestDTO);
        Task<TokenDTO> RefreshAccesToken(TokenDTO tokenDTO);
    }
}
