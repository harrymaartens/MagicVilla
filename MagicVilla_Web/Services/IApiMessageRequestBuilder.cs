using MagicVilla_Web.Models;

namespace MagicVilla_Web.Services
{
    public interface IApiMessageRequestBuilder
    {
        HttpRequestMessage Build(APIRequest apiRequest);
    }
}
