using System.ComponentModel.DataAnnotations;

namespace BlazorApp.Server.Models
{
    public class Tenant
    {
        [Required]
        [MaxLength(128)]
        public string Title { get; set; }
    }
}
