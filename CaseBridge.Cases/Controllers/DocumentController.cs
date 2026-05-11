using CaseBridge_Cases.Data;
using CaseBridge_Cases.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
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
        private readonly IMinioClient _minioClient;
        private readonly IConfiguration _config;

        public DocumentController(CaseDbContext context, IMinioClient minioClient, IConfiguration config)
        {
            _minioClient = minioClient;
            _context = context;
            _config = config;
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

            var uploadedDocs = new List<object>();
            var bucketName = _config["MinIO:BucketName"];
            var publicUrl = _config["MinIO:PublicUrl"];

            try
            {
                //SAFETY CHECK: Ensure the MinIO bucket exists before we start looping
                bool bucketExists = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
                if (!bucketExists)
                {
                    await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
                }

                foreach (var file in files)
                {
                    if (file.Length == 0) continue;

                    var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";

                    // STREAM DIRECTLY TO MINIO
                    using (var stream = file.OpenReadStream())
                    {
                        await _minioClient.PutObjectAsync(new PutObjectArgs()
                            .WithBucket(bucketName)
                            .WithObject(uniqueFileName)
                            .WithStreamData(stream)
                            .WithObjectSize(stream.Length)
                            .WithContentType(file.ContentType));
                    }

                    // GENERATE THE MINIO URL
                    // Note: In production, this would be your Amazon S3 URL
                    var minioUrl = $"{publicUrl}/{bucketName}/{uniqueFileName}";

                    // 5. SAVE TO SQL DATABASE
                    var newDoc = new CaseDocument
                    {
                        CaseId = null, // Waiting for React to submit the case!
                        UploaderId = uploaderId,
                        FileName = file.FileName,
                        FileUrl = minioUrl, // We save the MinIO URL now
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
            catch (Exception ex)
            {
                // If MinIO is offline, we catch it here so the API doesn't crash
                return StatusCode(500, $"Storage Error: {ex.Message}");
            }
        }
    }
}
