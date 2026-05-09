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
        public string LawyerName { get; set; } = string.Empty;
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

            bool isBrandNewClaim = CaseToClaim.Status == CaseStatus.Open;
            bool isReturningLawyer = CaseToClaim.Status == CaseStatus.Closed && CaseToClaim.AcceptedByUserId == request.LawyerId;
            bool isPreviousOwner = CaseToClaim.PreviousLawyerId == request.LawyerId;

            // If it's not open AND it's not a returning lawyer reopening their own case... block them!
            if (!isBrandNewClaim && !isReturningLawyer)
            {
                throw new Exception("This case is no longer available or is locked by another lawyer.");
            }

            if (isReturningLawyer || isPreviousOwner)
            {
                CaseToClaim.Status = CaseStatus.Reopened;
            }
            else
            {
                CaseToClaim.Status = CaseStatus.InReview;
            }

            CaseToClaim.AcceptedByUserId = request.LawyerId;
            CaseToClaim.AssignedFirmId = request.FirmId;
            CaseToClaim.LawyerName = request.LawyerName; // Save lawyer name
            CaseToClaim.LastModifiedByUserId = request.LawyerId;

            await _Context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
