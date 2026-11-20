namespace ChefBot.Core.ResourceAccess.Contracts.Messages;

/// <summary>A backup file on disk.</summary>
internal sealed record StoredBackup(string Name, DateTimeOffset CreatedAt, long SizeBytes);
