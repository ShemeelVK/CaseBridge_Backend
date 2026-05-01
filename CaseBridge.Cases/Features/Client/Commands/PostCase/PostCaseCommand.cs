using MediatR;
using CaseBridge_Cases.Data;
using CaseBridge_Cases.Models;
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
    }

    public class PostCaseHandler : IRequestHandler<PostCaseCommand, int>
    {
        private readonly CaseDbContext _context;
        public PostCaseHandler(CaseDbContext context)
        {
            _context = context;
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

            return newCase.Id;
        }
    }
}
