using MediatR;
using CaseBridge_Cases.Data;
using CaseBridge_Cases.Models;
using Microsoft.EntityFrameworkCore;

namespace CaseBridge_Cases.Features.Lawyer.Commands.CloseCase
{
    public class CloseCaseCommand : IRequest<bool>
    {
        public int FirmId { get; set;}
        public int CaseId { get; set; }
    }

    public class CloseCaseHandler : IRequestHandler<CloseCaseCommand,bool>
    {
        private readonly CaseDbContext _context;
        public CloseCaseHandler(CaseDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(CloseCaseCommand command, CancellationToken cancellationToken)
        {
            var caseToClose = await _context.Cases.FirstOrDefaultAsync(r => r.Id == command.CaseId,cancellationToken);

            if (caseToClose == null)
            {
                throw new Exception("Case not found.");
            }

            if (caseToClose.AssignedFirmId != command.FirmId)
                throw new UnauthorizedAccessException("You are not authorized to close a case that belongs to another firm.");

            if (caseToClose.Status == CaseStatus.Closed)
                throw new Exception("This case is already closed.");

            caseToClose.Status = CaseStatus.Closed;

            await _context.SaveChangesAsync(cancellationToken);

            return true;

        }
    }
}
