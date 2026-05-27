using System.Text.Json;
using CashFlow.EntryService.Domain.Entities;
using CashFlow.EntryService.Domain.Repositories;
using CashFlow.SharedKernel.Events;
using CashFlow.SharedKernel.Handlers;
using CashFlow.SharedKernel.Results;

namespace CashFlow.EntryService.Application.Commands;

/// <summary>Handler que processa <see cref="CreateEntryCommand"/> persistindo o lançamento e o evento no outbox.</summary>
public class CreateEntryCommandHandler(
    IEntryRepository entryRepo,
    IOutboxRepository outboxRepo
) : ICommandHandler<CreateEntryCommand, Result<CreateEntryResult>>
{
    /// <inheritdoc/>
    public async Task<Result<CreateEntryResult>> HandleAsync(CreateEntryCommand command, CancellationToken ct)
    {
        try
        {
            var entry = Entry.Create(command.Type, command.Amount, command.Description, command.Date);

            // Persiste lançamento + outbox na mesma transação (Outbox Pattern)
            var entryEvent = new EntryCreatedEvent(
                EventId: Guid.NewGuid(),
                EntryId: entry.Id,
                Type: entry.Type.ToString(),
                Amount: entry.Amount,
                Description: entry.Description,
                Date: entry.Date,
                OccurredAt: DateTime.UtcNow
            );

            var outbox = OutboxMessage.Create(nameof(EntryCreatedEvent), JsonSerializer.Serialize(entryEvent));
            await entryRepo.AddAsync(entry, ct);
            await outboxRepo.AddAsync(outbox, ct);
            await entryRepo.SaveChangesAsync(ct);

            return Result<CreateEntryResult>.Ok(new CreateEntryResult(entry.Id, entry.Type, entry.Amount, entry.Date));
        }
        catch (DomainException ex)
        {
            return Result<CreateEntryResult>.Fail(ex.Message);
        }
    }
}
