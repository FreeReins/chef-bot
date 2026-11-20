using System.Security.Claims;
using ChefBot.Api.Contracts;
using ChefBot.Core.Accounts;
using ChefBot.Core.Backups;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace ChefBot.Api.Endpoints;

/// <summary>Automatic backups, how long they are kept, and restoring from one. Admins only.</summary>
public static class BackupEndpoints
{
    [WolverineGet("/api/system/backups")]
    public static async Task<BackupsResponse> Get(
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        [FromServices] BackupService backups,
        CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        return await DescribeAsync(backups, cancellationToken);
    }

    /// <summary>Saves when backups run and how many are kept.</summary>
    [WolverinePut("/api/system/backups")]
    public static async Task<BackupsResponse> Update(
        BackupSettingsRequest request,
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        [FromServices] BackupService backups,
        CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        await backups.UpdateSettingsAsync(request.Enabled, request.Time, request.KeepCount, request.KeepDays, cancellationToken);
        return await DescribeAsync(backups, cancellationToken);
    }

    /// <summary>Makes a backup now and applies the retention rules.</summary>
    [WolverinePost("/api/system/backups")]
    public static async Task<BackupsResponse> RunNow(
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        [FromServices] BackupService backups,
        CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        await backups.RunAsync(cancellationToken);
        return await DescribeAsync(backups, cancellationToken);
    }

    /// <summary>Downloads a backup file, to keep a copy somewhere else.</summary>
    [WolverineGet("/api/system/backups/{name}")]
    public static async Task<IResult> Download(
        string name,
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        [FromServices] BackupService backups,
        CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        return Results.File(backups.OpenRead(name), "application/gzip", name);
    }

    [WolverineDelete("/api/system/backups/{name}")]
    public static async Task<BackupsResponse> Delete(
        string name,
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        [FromServices] BackupService backups,
        CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        backups.Delete(name);
        return await DescribeAsync(backups, cancellationToken);
    }

    /// <summary>Replaces all of ChefBot's data with a backup. Everything saved since that backup is lost.</summary>
    [WolverinePost("/api/system/backups/{name}/restore")]
    public static async Task Restore(
        string name,
        ClaimsPrincipal principal,
        [FromServices] AccountService accounts,
        [FromServices] BackupService backups,
        CancellationToken cancellationToken)
    {
        await accounts.RequireAdminAsync(Sessions.UserId(principal), cancellationToken);
        await backups.RestoreAsync(name, cancellationToken);
    }

    private static async Task<BackupsResponse> DescribeAsync(BackupService backups, CancellationToken cancellationToken)
    {
        var settings = await backups.GetSettingsAsync(cancellationToken);
        return new BackupsResponse(
            settings.Enabled, settings.Time, settings.KeepCount, settings.KeepDays, settings.LastResult, backups.List());
    }
}
