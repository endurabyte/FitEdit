using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Dauer.BlazorApp.Server.Services;
using Dauer.BlazorApp.Shared.Dto;
using Dauer.BlazorApp.Server.Middleware.Wrappers;
using Microsoft.AspNetCore.Http;
using IdentityModel;

namespace Dauer.BlazorApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserProfileController : ControllerBase
    {
        private readonly ILogger<UserProfileController> _logger;
        private readonly IUserProfileService _userProfileService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserProfileController(IUserProfileService userProfileService, ILogger<UserProfileController> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _userProfileService = userProfileService;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: api/UserProfile
        [HttpGet("Get")]
        public async Task<ApiResponse> Get()
        {
            Guid userId = new Guid(_httpContextAccessor.HttpContext.User.FindFirst(JwtClaimTypes.Subject).Value);
            return await _userProfileService.Get(userId);
        }

        // POST: api/UserProfile
        [HttpPost("Upsert")]
        public async Task<ApiResponse> Upsert(UserProfileDto userProfile)
        {
            if (!ModelState.IsValid)
            {
                return new ApiResponse(400, "User Model is Invalid");
            }

            await _userProfileService.Upsert(userProfile);
            return new ApiResponse(200, "Email Successfuly Sent");
        }

    }
}
