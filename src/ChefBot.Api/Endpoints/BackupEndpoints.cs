using ChefBot.Core.Managers.Contracts.Messages;
using ChefBot.Core.Managers.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>Automatic backups, how long they are kept, and restoring from one. Admins only.</summary>
[Authorize(Policy = Sessions.AdminPolicy)]
public static class BackupEndpoints
{
    [WolverineGet("/api/system/backups")]
    public static Task<BackupsResponse> Get([FromServices] IMaintenanceManager maintenance, CancellationToken cancellationToken) =>
        maintenance.GetBackupsAsync(cancellationToken);

    /// <summary>Saves when backups run and how many are kept.</summary>
    [WolverinePut("/api/system/backups")]
    public static Task<BackupsResponse> Update(
        BackupSettingsRequest request, [FromServices] IMaintenanceManager maintenance, CancellationToken cancellationToken) =>
        maintenance.UpdateBackupSettingsAsync(request, cancellationToken);

    /// <summary>Makes a backup now and applies the retention rules.</summary>
    [WolverinePost("/api/system/backups")]
    public static Task<BackupsResponse> RunNow([FromServices] IMaintenanceManager maintenance, CancellationToken cancellationToken) =>
        maintenance.RunBackupAsync(cancellationToken);

    /// <summary>Downloads a backup file, to keep a copy somewhere else.</summary>
    [WolverineGet("/api/system/backups/{name}")]
    public static async Task<IResult> Download(string name, [FromServices] IMaintenanceManager maintenance, CancellationToken cancellationToken) =>
        Results.File(await maintenance.OpenBackupAsync(name, cancellationToken), "application/gzip", name);

    /// <summary>Counts what a backup holds, so it can be checked before restoring it.</summary>
    [WolverineGet("/api/system/backups/{name}/summary")]
    public static Task<BackupSummary> Summary(string name, [FromServices] IMaintenanceManager maintenance, CancellationToken cancellationToken) =>
        maintenance.GetBackupSummaryAsync(name, cancellationToken);

    [WolverineDelete("/api/system/backups/{name}")]
    public static Task<BackupsResponse> Delete(string name, [FromServices] IMaintenanceManager maintenance, CancellationToken cancellationToken) =>
        maintenance.DeleteBackupAsync(name, cancellationToken);

    /// <summary>Replaces all of ChefBot's data with a backup. Everything saved since that backup is lost.</summary>
    [WolverinePost("/api/system/backups/{name}/restore")]
    public static Task Restore(string name, [FromServices] IMaintenanceManager maintenance, CancellationToken cancellationToken) =>
        maintenance.RestoreBackupAsync(name, cancellationToken);
}
