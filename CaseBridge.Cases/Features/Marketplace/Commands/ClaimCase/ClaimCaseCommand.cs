using CaseBridge_Cases.Data;
using CaseBridge_Cases.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaseBridge_Cases.Features.Marketplace.Commands.ClaimCase
{
    public class ClaimCaseCommand : IRequest<bool>
    {
        public int CaseId { get; set; }
        public int LawyerId { get; set; }
        public int FirmId { get; set; }
    }

    public class ClaimCaseHandler : IRequestHandler<ClaimCaseCommand, bool>
    {
        private readonly CaseDbContext _Context;
        public ClaimCaseHandler(CaseDbContext context)
        {
            _Context = context;
        }

        public async Task<bool> Handle(ClaimCaseCommand request,CancellationToken cancellationToken)
        {
            var CaseToClaim=await _Context.Cases.FirstOrDefaultAsync(r=>r.Id==request.CaseId,cancellationToken);

            if(CaseToClaim==null)
            {
                throw new Exception("Case not found");
            }

            if (CaseToClaim.Status != CaseStatus.Open)
                throw new Exception("This case is no longer available.");

            CaseToClaim.Status =CaseStatus.InReview ;
            CaseToClaim.AcceptedByUserId = request.LawyerId;
            CaseToClaim.AssignedFirmId = request.FirmId;
            CaseToClaim.LastModifiedByUserId = request.LawyerId;

            await _Context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
