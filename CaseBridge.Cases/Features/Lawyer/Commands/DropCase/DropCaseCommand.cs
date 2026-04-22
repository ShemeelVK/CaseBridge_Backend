using MediatR;
using Microsoft.EntityFrameworkCore;
using CaseBridge_Cases.Data;
using CaseBridge_Cases.Models;

namespace CaseBridge_Cases.Features.Lawyer.Commands.DropCase
{
    public class DropCaseCommand : IRequest<bool>
    {
        public int CaseId { get; set; }
        public int FirmId { get; set; }
        public int UserId  { get; set; }
    }

    public class DropCaseHandler : IRequestHandler<DropCaseCommand, bool>
    {
        private readonly CaseDbContext _context;
        public DropCaseHandler(CaseDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DropCaseCommand command, CancellationToken cancellationToken)
        {
            var caseToDrop= await _context.Cases.FirstOrDefaultAsync(r=>r.Id==command.CaseId, cancellationToken);

            if (caseToDrop == null)
                throw new Exception("Case not found.");

            if (caseToDrop.AssignedFirmId != command.FirmId)
                throw new UnauthorizedAccessException("You are not authorized to drop a case that belongs to another firm.");

            if (caseToDrop.Status == CaseStatus.Closed)
                throw new Exception("Cannot drop a closed case.");

            if (caseToDrop.Status == CaseStatus.Open)
                throw new Exception("This case is already open in the marketplace.");

            caseToDrop.Status = CaseStatus.Open;
            caseToDrop.AssignedFirmId = null;
            caseToDrop.AcceptedByUserId = null;
            caseToDrop.LastModifiedByUserId = command.UserId;

            // 4. Save to database
            await _context.SaveChangesAsync(cancellationToken);

            return true;

        }
    }
}
