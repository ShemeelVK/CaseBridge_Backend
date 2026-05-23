using CaseBridge_Cases.Data;
using CaseBridge_Contracts;
using MassTransit;
using System.Threading.Tasks;

namespace CaseBridge_Cases.Consumers
{
    public class CaseSummaryGeneratedConsumer : IConsumer<CaseSummaryGeneratedEvent>
    {
        private readonly CaseDbContext _context;

        public CaseSummaryGeneratedConsumer(CaseDbContext context)
        {
            _context = context;
        }

        public async Task Consume(ConsumeContext<CaseSummaryGeneratedEvent> context)
        {
            var message = context.Message;

            var caseEntity = await _context.Cases.FindAsync(message.CaseId);
            if (caseEntity != null)
            {
                caseEntity.AiSummary = message.SummaryText;
                await _context.SaveChangesAsync();
            }
        }
    }
}
