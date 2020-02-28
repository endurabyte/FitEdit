using AutoMapper.Configuration;
using Dauer.BlazorApp.Server.Middleware.Wrappers;
using Dauer.BlazorApp.Server.Models;
using Dauer.BlazorApp.Shared.Dto;

namespace Dauer.BlazorApp.Server.Data.Mapping
{
    public class MappingProfile : MapperConfigurationExpression
    {
        /// <summary>
        /// Create automap mapping profiles
        /// </summary>
        public MappingProfile()
        {
            CreateMap<Todo, TodoDto>().ReverseMap();           
            CreateMap<UserProfile, UserProfileDto>().ReverseMap();
            CreateMap<ApiLogItem, ApiLogItemDto>().ReverseMap();
            CreateMap<Message, MessageDto>().ReverseMap();
        }
    }
}
