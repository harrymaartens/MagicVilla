using MagicVilla_Web.Models.Dto;
using MagicVilla_Utility;
using MagicVilla_Web.Models;
using MagicVilla_Web.Services.IServices;
using Microsoft.AspNetCore.Http.HttpResults;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using static MagicVilla_Utility.SD;
using AutoMapper.Internal;
using Microsoft.AspNetCore.Authentication;
using System.Runtime.Intrinsics.X86;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System;

namespace MagicVilla_Web.Services
{
    public class BaseService : IBaseService
    {
        public APIResponse responseModel { get; set; }
        private readonly ITokenProvider _tokenProvider;
        private readonly IApiMessageRequestBuilder _apiMessageRequestBuilder;

        // protected: Dit veld is toegankelijk binnen de klasse zelf en in afgeleide klassen.
        protected readonly string VillaApiUrl;
        private IHttpContextAccessor _httpContextAccessor;

        // De HttpClientFactory is onderdeel van DI
        public IHttpClientFactory httpClient { get; set; }

        public BaseService(IHttpClientFactory httpClient, ITokenProvider tokenProvider, IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor, IApiMessageRequestBuilder apiMessageRequestBuilder)
        {
            _httpContextAccessor = httpContextAccessor;
			_tokenProvider = tokenProvider;
			this.responseModel = new();
            VillaApiUrl = configuration.GetValue<string>("ServiceUrls:VillaAPI");
            this.httpClient = httpClient;
            _apiMessageRequestBuilder = apiMessageRequestBuilder;
        }

        public async Task<T> SendAsync<T>(APIRequest apiRequest, bool withBearer = true)
        {
            try
            {
                var client = httpClient.CreateClient("MagicAPI");

                // We should pass a factory instead of instance of an HttpRequestMessage because
                // it is forbidden to use the same message object more than once. 
                // So if we have to retry request, then we must create a new message object.
                var messageFactory = () =>
                {
                    return _apiMessageRequestBuilder.Build(apiRequest);
                };

                HttpResponseMessage httpResponseMessage = null;
                                
                httpResponseMessage = await SendWithRefreshTokenAsync(client, messageFactory, withBearer);

                APIResponse FinalApiResponse = new()
                {
                    // And then if you notice when we created API response here, we set the success to be false.
                    IsSuccess = false
                };

                // This API content will have to deserialize that and once we deserialize it should
                // be the model which is APIResponse. That will be true for all the condition here,
                // but for default we will have to set that to be true.           
                try
                {
                    switch (httpResponseMessage.StatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            FinalApiResponse.ErrorMessages = new List<string>() { "Not Found" };
                            break;
                        case HttpStatusCode.Forbidden:
                            FinalApiResponse.ErrorMessages = new List<string>() { "Access Denied" };
                            break;
                        case HttpStatusCode.Unauthorized:
                            FinalApiResponse.ErrorMessages = new List<string>() { "Unauthorized" };
                            break;
                        case HttpStatusCode.InternalServerError:
                            FinalApiResponse.ErrorMessages = new List<string>() { "Internal Server Error" };
                            break;
                        default:
                            var apiContent = await httpResponseMessage.Content.ReadAsStringAsync();
                            FinalApiResponse.IsSuccess = true;
                            // So we will deserialize that object and we will call the variable as FinalAPIResponse. 
                            FinalApiResponse = JsonConvert.DeserializeObject<APIResponse>(apiContent);
                            break;
                    }                    
                }                
                catch (Exception e)
                {
                    // let me create a generic error message. Error encountered. We do not want to return anything.
                    FinalApiResponse.ErrorMessages = new List<string>() { "Error Encountered", e.Message.ToString() };
                }
                var res = JsonConvert.SerializeObject(FinalApiResponse);
                var returnObj = JsonConvert.DeserializeObject<T>(res);
                return returnObj;               
            }
            catch (AuthException)
            {
                throw;
            }
            catch (Exception e)
            {
                var dto = new APIResponse
                {
                    ErrorMessages = new List<string> { Convert.ToString(e.Message) },
                    IsSuccess = false
                };
                var res = JsonConvert.SerializeObject(dto);
                var APIResponse = JsonConvert.DeserializeObject<T>(res);
                return APIResponse;
            }
        }

        private async Task<HttpResponseMessage> SendWithRefreshTokenAsync(HttpClient httpClient, 
            Func<HttpRequestMessage> httpRequestMessageFactory, bool withBearer = true)
        {
            if (!withBearer)
            {
                return await httpClient.SendAsync(httpRequestMessageFactory());
            }
            else
            {
                TokenDTO tokenDTO = _tokenProvider.GetToken();
                if (tokenDTO != null && !string.IsNullOrEmpty(tokenDTO.AccessToken))
                {
                    {
                        // API can validate
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenDTO.AccessToken);
                    }
                }

                try
                {
                    // We have a new method here, but when we go to the implementation,
                    // if everything is valid, that is good.
                    var response = await httpClient.SendAsync(httpRequestMessageFactory());
                    if (response.IsSuccessStatusCode)
                        return response;

                    // IF this fails then we can pass refresh token!
                    if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        // GENERATE NEW Token from Refresh token / Sign in with that new token and retry
                        await InvokeRefreshTokenEndPoint(httpClient, tokenDTO.AccessToken, tokenDTO.RefreshToken);
                        response = await httpClient.SendAsync(httpRequestMessageFactory());
                        return response;
                    }
                    return response;
                }
                // Now, here you cannot catch the other exception after exception, because exception
                // will catch all the exceptions. So that way, if you want to catch this, you have
                // to catch that before it catches all the other exception.
                catch (AuthException)
                {
                    throw;
                }
                catch (HttpRequestException httpRequestException)
                {
                    if (httpRequestException.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        // refresh token and retry the request
                        await InvokeRefreshTokenEndPoint(httpClient, tokenDTO.AccessToken, tokenDTO.RefreshToken);
                        return await httpClient.SendAsync(httpRequestMessageFactory());
                    }
                    throw;
                }
            }
        }

        private async Task InvokeRefreshTokenEndPoint(HttpClient httpClient, string existingAccessToken, string existingRefreshToken)
        {
            HttpRequestMessage message = new ();
            message.Headers.Add("Accept", "application/json");
            message.RequestUri = new Uri($"{VillaApiUrl}/api/{SD.CurrentAPIVersion}/UsersAuth/refresh");
            message.Method = HttpMethod.Post;
            message.Content = new StringContent(JsonConvert.SerializeObject(new TokenDTO() 
            { 
                AccessToken = existingAccessToken,
                RefreshToken = existingRefreshToken
            }), Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(message);
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<APIResponse>(content);

            // Now, if the response is not successful in there, we have the flag API response.
            // That is, success is not true. That means that token refreshment failed, so we cannot
            // use the refresh token anymore. We have to remove that.
            if (apiResponse?.IsSuccess != true)
            {
                // But on top of that we will have to sign out the user as well.
                await _httpContextAccessor.HttpContext.SignOutAsync();
                // We need to clear the token as well.
                _tokenProvider.ClearToken();
                throw new AuthException();
            }
            else
            {
                // Everything is valid.
                var tokenDataStr = JsonConvert.SerializeObject(apiResponse.Result);
                var tokenDto = JsonConvert.DeserializeObject<TokenDTO>(tokenDataStr);

                if (tokenDto != null && !string.IsNullOrEmpty(tokenDto.AccessToken))
                {
                    // New method to sign in with the new token that we receive
                    await SignInWithNewTokens(tokenDto);

                    // For the Http client that we have, we have to set the authorization to be the new token.
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenDto.AccessToken);

                }
            }
        }

        private async Task SignInWithNewTokens(TokenDTO tokenDTO)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(tokenDTO.AccessToken);

            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, jwt.Claims.FirstOrDefault(u => u.Type == "unique_name").Value));
            identity.AddClaim(new Claim(ClaimTypes.Role, jwt.Claims.FirstOrDefault(u => u.Type == "role").Value));
            var principal = new ClaimsPrincipal(identity);
            await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);            
            _tokenProvider.SetToken(tokenDTO);
        }
    }
}
