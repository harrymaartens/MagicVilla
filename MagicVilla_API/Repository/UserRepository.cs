using MagicVilla_API.Data;
using MagicVilla_API.Models.Dto;
using MagicVilla_API.Models;
using MagicVilla_API.Repository.IRepostiory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace MagicVilla_API.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private string secretKey;
        private readonly IMapper _mapper;

        public UserRepository(ApplicationDbContext db, IConfiguration configuration,
            UserManager<ApplicationUser> userManager, IMapper mapper, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _mapper = mapper;
            _userManager = userManager;
            secretKey = configuration.GetValue<string>("ApiSettings:Secret");
            _roleManager = roleManager;
        }

        public bool IsUniqueUser(string username)
        {
            var user = _db.ApplicationUsers.FirstOrDefault(x => x.UserName == username);
            if (user == null)
            {
                return true;
            }
            return false;
        }

        public async Task<TokenDTO> Login(LoginRequestDTO loginRequestDTO)
        {
            var user = _db.ApplicationUsers
                .FirstOrDefault(u => u.UserName.ToLower() == loginRequestDTO.UserName.ToLower());

            bool isValid = await _userManager.CheckPasswordAsync(user, loginRequestDTO.Password);

            if (user == null || isValid == false)
            {
                return new TokenDTO()
                {
					AccessToken = ""
                };
            }

            var jwtTokenId = $"JTI{Guid.NewGuid()}";
            var accessToken = await GetAccessToken(user, jwtTokenId);

            // We have the get access token and whenever we are passing access token to the user on login, we will
            // also have to populate the refresh token. Create new refresh token. We need to pass the user ID that
            // will be inside user.Id and then we need the jwtTokenId. When we call the login next time it will
            // create a new entry in the refresh token table
            var refreshToken = await CreateNewRefreshToken(user.Id, jwtTokenId);

			TokenDTO tokenDTO = new()
            {
				AccessToken = accessToken,
                RefreshToken = refreshToken
			};
            return tokenDTO;
        }

        public async Task<UserDTO> Register(RegisterationRequestDTO registerationRequestDTO)
        {
            ApplicationUser user = new()
            {
                UserName = registerationRequestDTO.UserName,
                Email = registerationRequestDTO.UserName,
                NormalizedEmail = registerationRequestDTO.UserName.ToUpper(),
                Name = registerationRequestDTO.Name
            };

            try
            {
                var result = await _userManager.CreateAsync(user, registerationRequestDTO.Password);
                if (result.Succeeded) 
                {
                    if (!_roleManager.RoleExistsAsync(registerationRequestDTO.Role).GetAwaiter().GetResult())
                    {
                        await _roleManager.CreateAsync(new IdentityRole(registerationRequestDTO.Role));
                    }
                    await _userManager.AddToRoleAsync(user, registerationRequestDTO.Role);
                    var userToReturn = _db.ApplicationUsers
                       .FirstOrDefault(u => u.UserName == registerationRequestDTO.UserName);
                    return _mapper.Map<UserDTO>(userToReturn);
                }
            }
            catch (Exception e)
            { 
            
            }
            return new UserDTO();
        }

        private async Task<string> GetAccessToken(ApplicationUser user, string jwtTokenId)
        {
			var roles = await _userManager.GetRolesAsync(user);
			//if user was found generate JWT Token
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(secretKey);

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new Claim[]
				{
					new Claim(ClaimTypes.Name, user.UserName.ToString()),
					new Claim(ClaimTypes.Role, roles.FirstOrDefault()),
                    new Claim(JwtRegisteredClaimNames.Jti, jwtTokenId),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id)
				}),
				Expires = DateTime.UtcNow.AddMinutes(1),
				SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};

			var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenStr = tokenHandler.WriteToken(token);
            return tokenStr;
		}

        public async Task<TokenDTO> RefreshAccesToken(TokenDTO tokenDTO)
        {
            // Find an existing refresh token
            var existingRefreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(u => u.Refresh_Token == tokenDTO.RefreshToken);

            // If we do not get anything from the database, if that is null. In that case we will return new token DTO.
            if (existingRefreshToken == null)
                return new TokenDTO();

            // Compare data from existing refresh and access token provided and if there is any missmatch then consider it as a fraud
            var isTokenValid = GetAccessTokenData(tokenDTO.AccessToken, existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
            if (!isTokenValid) 
            {
                await MarkTokenAsInvalid(existingRefreshToken);
                // We will not return a new DTO right here because at this point the old token is valid and
                // we are in process of creating a new token, marking the old token as invalid and
                // returning back the new token.
                return new TokenDTO();
            }

            // When someone tries to use not valid refresh token, fraud possible
            if (!existingRefreshToken.IsValid)
            {
                await MarkAllTokenInChainAsInvalid(existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
            }

            // If just expired then mark as invalid and return empty
            if (existingRefreshToken.ExpiresAt < DateTime.UtcNow) 
            {
                await MarkTokenAsInvalid(existingRefreshToken);
                return new TokenDTO();
            }

            // replace old refresh with a new one with updated expire date
            var newRefreshToken = await CreateNewRefreshToken(existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);

            // revoke existing refresh token
            await MarkTokenAsInvalid(existingRefreshToken);

            // generate new access token
            // We have a method for that that is generate access token and the parameter we need user.
            // Let me pass application user and the JWT token ID that is inside the existing refresh token.
            var applicationUser = _db.ApplicationUsers.FirstOrDefault(u => u.Id == existingRefreshToken.UserId);
            if (applicationUser == null) 
                return new TokenDTO();

            // You might be thinking that we should be creating a new JWT token ID. Why are we reusing
            // the old token ID? The reason we are using the existing token ID is in database.
            // We can easily track which token IDs are related. One got expired. It created a new token ID using
            // the same token ID, or sometimes if you are working with JWT, it is also known as chain ID.
            // That way it will kind of tell you that hey, all of this refresh token belongs to the same chain and
            // because of that I will be using the existing JwtTokenId. Now why we need that chain logic or
            // what advantage that have? I will walk you through that as well.
            var newAccessToken = await GetAccessToken(applicationUser, existingRefreshToken.JwtTokenId);

            // Once we retrieve the new access token, we can return new token DTO and we can populate access token
            // as well as refresh token.
            return new TokenDTO
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        public async Task RevokeRefreshToken(TokenDTO tokenDTO)
        {
            var existingRefreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(_ => _.Refresh_Token == tokenDTO.RefreshToken);

            if (existingRefreshToken == null)
                return;

            // Compare data from existing refresh and access token provided and
            // if there is any missmatch then we should do nothing with refresh token
            var isTokenValid = GetAccessTokenData(tokenDTO.AccessToken, existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
            if (!isTokenValid)
            {
                return;
            }

            await MarkAllTokenInChainAsInvalid(existingRefreshToken.UserId, existingRefreshToken.JwtTokenId);
        }

        // When we create a new refresh token, we are storing that in the database.
        private async Task<string> CreateNewRefreshToken(string userId, string tokenId)
        {
            RefreshToken refreshToken = new()
            {
                IsValid = true,
                UserId = userId,
                JwtTokenId = tokenId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(2),
                Refresh_Token = Guid.NewGuid() + "-" + Guid.NewGuid()
            };

            await _db.RefreshTokens.AddAsync(refreshToken);
            await _db.SaveChangesAsync();
            return refreshToken.Refresh_Token;
        }

        // Een end-point GetAccessTokenData
        // A nice helper method that will extract the userId, tokenId from our access token.
        private bool GetAccessTokenData(string accessToken, string expectedUserId, string expectedTokenId)
        {
            try
            {                
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwt = tokenHandler.ReadJwtToken(accessToken);
                var jwtTokenId = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Jti).Value;
                var userId = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub).Value;
                return userId == expectedUserId && jwtTokenId == expectedTokenId;
            }
            catch
            {
                return false;
            }
        }

        private async Task MarkAllTokenInChainAsInvalid(string userId, string tokenId)
        {
            await _db.RefreshTokens.Where(u => u.UserId == userId
                && u.JwtTokenId == tokenId)
                    .ExecuteUpdateAsync(u => u.SetProperty(refreshToken => refreshToken.IsValid, false));
        }
        
        private Task MarkTokenAsInvalid(RefreshToken refreshToken)
        {
            refreshToken.IsValid = false;
            return _db.SaveChangesAsync();
        }        
    }
}
