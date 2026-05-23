using CaseBridge_Cases.Data;
using CaseBridge_Cases.Models;
using CaseBridge_Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CaseBridge_Cases.Consumers
{
    public class JuniorRemovedFromFirmConsumer : IConsumer<JuniorRemovedFromFirmEvent>
    {
        private readonly CaseDbContext _context;
        private readonly ILogger<JuniorRemovedFromFirmConsumer> _logger;

        public JuniorRemovedFromFirmConsumer(CaseDbContext context, ILogger<JuniorRemovedFromFirmConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<JuniorRemovedFromFirmEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("Received JuniorRemovedFromFirmEvent. Reassigning active cases from JuniorId {JuniorId} to SeniorId {SeniorId}", msg.JuniorId, msg.SeniorId);

            // Find all active cases assigned to this junior
            // We consider 'Open', 'InProgress', and 'InReview' as active states that need reassignment.
            var activeCases = await _context.Cases
                .Where(c => c.AcceptedByUserId == msg.JuniorId && c.Status != CaseStatus.Closed)
                .ToListAsync();

            if (activeCases.Any())
            {
                foreach (var activeCase in activeCases)
                {
                    // Reassign to the Senior Lawyer
                    activeCase.AcceptedByUserId = msg.SeniorId;
                    activeCase.LastModifiedByUserId = msg.SeniorId; // Audit log update
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully reassigned {Count} active cases from JuniorId {JuniorId} to SeniorId {SeniorId}", activeCases.Count, msg.JuniorId, msg.SeniorId);
            }
            else
            {
                _logger.LogInformation("JuniorId {JuniorId} had no active cases to reassign.", msg.JuniorId);
            }
        }
    }
}
