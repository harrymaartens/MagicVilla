using MagicVilla_Utility;
using MagicVilla_Web.Models;
using MagicVilla_Web.Services.IServices;
using Newtonsoft.Json;
using System.Text;

namespace MagicVilla_Web.Services
{
    public class BaseService : IBaseService
    {
        public APIResponse responseModel { get; set; }

        // De HttpClientFactory is onderdeel van DI
        public IHttpClientFactory httpClient { get; set; }
        
        public BaseService(IHttpClientFactory httpClient)
        {
            this.responseModel = new();
            this.httpClient = httpClient;
        }

        public async Task<T> SendAsync<T>(APIRequest apiRequest)
        {
            try
            {
                var client = httpClient.CreateClient("MagicAPI");
                HttpRequestMessage message = new HttpRequestMessage();
                message.Headers.Add("Accept", "application/json");
                message.RequestUri = new Uri(apiRequest.Url);
                if (apiRequest.Data != null)
                {
                    // Data will not be null in POST/PUT HTTP calls.
                    message.Content = new StringContent(JsonConvert.SerializeObject(apiRequest.Data),
                        Encoding.UTF8, "application/json");
                }
                // Een API request is een enum, die we vinden in SD, waardoor we een switch condition kunnen maken.
                switch (apiRequest.ApiType)
                {
                    case SD.ApiType.POST:
                        message.Method = HttpMethod.Post;
                        break;
                    case SD.ApiType.PUT:
                        message.Method = HttpMethod.Put;
                        break;
                    case SD.ApiType.DELETE:
                        message.Method = HttpMethod.Delete;
                        break;
                    default:
                        message.Method = HttpMethod.Get;
                        break;
                }
                HttpResponseMessage apiResponse = null;

                // Bij een error zet hier een breakpoint, zodat je kan zien waar het fout gaat.
                apiResponse = await client.SendAsync(message);

                // This API content will have to deserialize that and once we deserialize it should
                // be the model which is APIResponse. 
                var apiContent = await apiResponse.Content.ReadAsStringAsync();
                try
                {
                    // So we will deserialize that object and we will call the variable as APIResponse. 
                    APIResponse ApiResponse = JsonConvert.DeserializeObject<APIResponse>(apiContent);
                    if(apiResponse.StatusCode == System.Net.HttpStatusCode.BadRequest
                        || apiResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        ApiResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
                        ApiResponse.IsSuccess = false;
						var res = JsonConvert.SerializeObject(ApiResponse);
						var returnObj = JsonConvert.DeserializeObject<T>(res);
						return returnObj;
					}
				}
				catch (Exception e)
                {
					var exceptionResponse= JsonConvert.DeserializeObject<T>(apiContent);
                    return exceptionResponse;
				}
				var APIResponse = JsonConvert.DeserializeObject<T>(apiContent);
				return APIResponse;
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
    }
}
