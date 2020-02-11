using BlazorApp.Shared.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
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
        private readonly IMultiSinkFileRepository _repo;

        public UploadController(IWebHostEnvironment environment, IMultiSinkFileRepository repo)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            this.environment = environment;

            if (repo == null) throw new ArgumentNullException(nameof(repo));
            _repo = repo;
        }

        [HttpPost]
        public IActionResult Post()
        {
            if (!HttpContext.Request.Form.Files.Any())
                return BadRequest("No files provided");

            Task.Run(() => SaveFiles(HttpContext.Request.Form.Files));

            return Ok();
        }

        private async void SaveFiles(IFormFileCollection files)
        {
            var location = $"activities/{HttpContext.User.Identity.Name}"; 

            foreach (var file in files)
            {
                await _repo
                    .SaveAsync(file.OpenReadStream(), location, file.FileName)
                    .ConfigureAwait(false);
            }
        }
    }
}
