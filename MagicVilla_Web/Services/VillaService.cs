using MagicVilla_Utility;
using MagicVilla_Web.Models;
using MagicVilla_Web.Models.Dto;
using MagicVilla_Web.Services.IServices;

namespace MagicVilla_Web.Services
{
    // BaseService vereist een clientFactory
    public class VillaService : IVillaService
    {
        // IHttpClientFactory is noodzakelijk voor de VillaService.
        private readonly IHttpClientFactory _clientFactory;
        private string villaUrl;
		private readonly IBaseService _baseService;

		// Dependency Injection, configuration is vereist voor villaUrl uit appsettings
		public VillaService(IHttpClientFactory clientFactory, IConfiguration configuration, IBaseService baseService)
        {
            _baseService = baseService;
            _clientFactory = clientFactory;

            // Dit komt uit de appsettings.json en geeft localhost:7001 aan villaUrl
            villaUrl = configuration.GetValue<string>("ServiceUrls:VillaAPI");

        }

        public async Task<T> CreateAsync<T>(VillaCreateDTO dto)
        {
			return await _baseService.SendAsync<T>(new APIRequest()
            {
                ApiType = SD.ApiType.POST,
                Data = dto,
                Url = villaUrl + $"/api/{SD.CurrentAPIVersion}/villaAPI",
                ContentType = SD.ContentType.MultipartFormData
            });
        }

        public async Task<T> DeleteAsync<T>(int id)
        {
			return await _baseService.SendAsync<T>(new APIRequest()
            {
                ApiType = SD.ApiType.DELETE,
                Url = villaUrl + $"/api/{SD.CurrentAPIVersion}/villaAPI/" + id
            });
        }

        public async Task<T> GetAllAsync<T>()
        {
			return await _baseService.SendAsync<T>(new APIRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = villaUrl + $"/api/{SD.CurrentAPIVersion}/villaAPI"
            });
        }

        public async Task<T> GetAsync<T>(int id)
        {
			return await _baseService.SendAsync<T>(new APIRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = villaUrl + $"/api/{SD.CurrentAPIVersion}/villaAPI/" + id
            });
        }

        public async Task<T> UpdateAsync<T>(VillaUpdateDTO dto)
        {
			return await _baseService.SendAsync<T>(new APIRequest()
            {
                ApiType = SD.ApiType.PUT,
                Data = dto,
                Url = villaUrl + $"/api/{SD.CurrentAPIVersion}/villaAPI/" + dto.Id,
                ContentType = SD.ContentType.MultipartFormData
            });
        }        
    }
}
