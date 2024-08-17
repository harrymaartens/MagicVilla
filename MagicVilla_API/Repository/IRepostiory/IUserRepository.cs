using MagicVilla_API.Models.Dto;
using MagicVilla_API.Models;

namespace MagicVilla_API.Repository.IRepostiory
{
    public interface IUserRepository
    {
        bool IsUniqueUser(string username);

        // Dit zijn allemaal end-points
        Task<TokenDTO> Login(LoginRequestDTO loginRequestDTO);
        Task<UserDTO> Register(RegisterationRequestDTO registerationRequestDTO);
        Task<TokenDTO> RefreshAccesToken(TokenDTO tokenDTO);
        Task RevokeRefreshToken(TokenDTO tokenDTO);
    }
}
