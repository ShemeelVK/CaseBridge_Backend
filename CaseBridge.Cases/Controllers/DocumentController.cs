using CaseBridge_Cases.Data;
using CaseBridge_Cases.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
namespace CaseBridge_Cases.Controllers
{
    [ApiController]
    [Route("api/documents")]
    public class DocumentController : ControllerBase
    {
        private readonly CaseDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DocumentController(CaseDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocuments(List<IFormFile> files)
        {
            if (files == null || !files.Any())
            {
                return BadRequest();
            }

            var userIdClaim=User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("UserId");

            if (!int.TryParse(userIdClaim, out int uploaderId))
            {
                return Unauthorized();
            }

            var uploadPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var uploadedDocs = new List<object>();

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadPath, uniqueFileName);

                // Save the physical file to the disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var newDoc = new CaseDocument
                {
                    CaseId = null, // Explicitly null! Waiting for React to submit the case.
                    UploaderId = uploaderId,
                    FileName = file.FileName,
                    FileUrl = $"/uploads/{uniqueFileName}",
                    UploadedAt = DateTime.UtcNow
                };

                _context.CaseDocuments.Add(newDoc);
                await _context.SaveChangesAsync();

                uploadedDocs.Add(new
                {
                    documentId = newDoc.Id,
                    url = newDoc.FileUrl,
                    name = newDoc.FileName
                });

            }
                return Ok(uploadedDocs);
        }
    }
}
