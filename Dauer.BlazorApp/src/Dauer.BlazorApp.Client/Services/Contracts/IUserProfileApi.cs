using System.Threading.Tasks;
using Dauer.BlazorApp.Shared.Dto;

namespace Dauer.BlazorApp.Client.Services.Contracts
{
    /// <summary>
    /// Access to User Profile information
    /// </summary>
    public interface IUserProfileApi
    {
        Task<ApiResponseDto> Upsert(UserProfileDto userProfile);
        Task<ApiResponseDto> Get();
    }
}
