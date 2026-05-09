using CaseBridge_Cases.Data;
using CaseBridge_Cases.Models;
using CaseBridge_Contracts;
using MassTransit;
using MassTransit.Transports;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace CaseBridge_Cases.Features.Client.Command.PostCase
{
    public class PostCaseCommand : IRequest<int>
    {
        [JsonIgnore] 
        public int ClientId { get; set; }
        
        [JsonIgnore]
        public string ClientName { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Budget { get; set; }
        public List<int>? DocumentIds { get; set; }
    }

    public class PostCaseHandler : IRequestHandler<PostCaseCommand, int>
    {
        private readonly CaseDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        public PostCaseHandler(CaseDbContext context, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;

        }

        public async Task<int> Handle(PostCaseCommand request, CancellationToken cancellationToken)
        {
            var newCase = new Case
            {
                ClientId = request.ClientId,
                ClientName = request.ClientName, // Save client name
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                Status = CaseStatus.Open,
                Budget = request.Budget,
                CreatedAt = DateTime.UtcNow,
                LastModifiedByUserId = request.ClientId
            };

            _context.Cases.Add(newCase);
            await _context.SaveChangesAsync(cancellationToken);


            //Document Service
            if (request.DocumentIds != null && request.DocumentIds.Any())
            {
                // Find the orphaned documents the user just uploaded.
                var docs = await _context.CaseDocuments
                    .Where(d => request.DocumentIds.Contains(d.Id)
                             && d.CaseId == null
                             && d.UploaderId == request.ClientId)
                    .ToListAsync(cancellationToken);

                foreach (var doc in docs)
                {
                    doc.CaseId = newCase.Id;

                    //  FIRE THE EVENT TO THE AI SERVICE!
                    await _publishEndpoint.Publish(new DocumentUploadedEvent
                    {
                        DocumentId = doc.Id,
                        CaseId = newCase.Id,
                        FileUrl = doc.FileUrl
                    }, cancellationToken);
                }

                // Save the updated CaseDocuments back to the database
                await _context.SaveChangesAsync(cancellationToken);
            }

            return newCase.Id;
        }
    }
}
