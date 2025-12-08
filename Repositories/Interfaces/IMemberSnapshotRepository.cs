namespace BurhaniGuards.Api.Repositories.Interfaces;

public interface IMemberSnapshotRepository
{
    Task UpsertAsync(string email, string? displayName, string role, CancellationToken cancellationToken = default);
}

