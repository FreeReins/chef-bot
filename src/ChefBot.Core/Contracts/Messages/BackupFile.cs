namespace ChefBot.Core.Contracts.Messages;

/// <summary>A backup file.</summary>
public sealed record BackupFile(string Name, DateTimeOffset CreatedAt, long SizeBytes);
