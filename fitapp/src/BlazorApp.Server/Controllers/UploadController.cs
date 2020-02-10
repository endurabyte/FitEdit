using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorApp.Server.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment environment;
        public UploadController(IWebHostEnvironment environment)
        {
            Directory.CreateDirectory(Path.Combine(environment.ContentRootPath, "uploads"));
            this.environment = environment;
        }

        [HttpPost]
        public async Task Post()
        {
            if (!HttpContext.Request.Form.Files.Any())
                return;

            foreach (var file in HttpContext.Request.Form.Files)
            {
                var path = Path.Combine(environment.ContentRootPath, "uploads", file.FileName);

                using var stream = new FileStream(path, FileMode.Create);
                await file.CopyToAsync(stream).ConfigureAwait(false);
            }
        }
    }
}
