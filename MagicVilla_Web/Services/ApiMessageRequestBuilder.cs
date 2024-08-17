using MagicVilla_Utility;
using MagicVilla_Web.Models;
using Newtonsoft.Json;
using System.Text;
using static MagicVilla_Utility.SD;

namespace MagicVilla_Web.Services
{
    public class ApiMessageRequestBuilder : IApiMessageRequestBuilder
    {
        public HttpRequestMessage Build(APIRequest apiRequest)
        {
            HttpRequestMessage message = new();
            if (apiRequest.ContentType == SD.ContentType.MultipartFormData)
            {
                message.Headers.Add("Accept", "*/*");
            }
            else
            {
                message.Headers.Add("Accept", "application/json");
            }
            //message.Headers.Add("Accept", "application/json");
            message.RequestUri = new Uri(apiRequest.Url);

            if (apiRequest.ContentType == ContentType.MultipartFormData)
            {
                var content = new MultipartFormDataContent();

                foreach (var prop in apiRequest.Data.GetType().GetProperties())
                {
                    var value = prop.GetValue(apiRequest.Data);
                    if (value is FormFile)
                    {
                        var file = (FormFile)value;
                        if (file != null)
                        {
                            content.Add(new StreamContent(file.OpenReadStream()), prop.Name, file.FileName);
                        }
                    }
                    else
                    {
                        content.Add(new StringContent(value == null ? "" : value.ToString()), prop.Name);
                    }
                }
                message.Content = content;
            }
            else
            {
                if (apiRequest.Data != null)
                {
                    // Data will not be null in POST/PUT HTTP calls.
                    message.Content = new StringContent(JsonConvert.SerializeObject(apiRequest.Data),
                        Encoding.UTF8, "application/json");
                }
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
            return message;
        }
    }
}
