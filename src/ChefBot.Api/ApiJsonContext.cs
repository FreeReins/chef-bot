using System.Text.Json;
using System.Text.Json.Serialization;
using ChefBot.Api.Https;
using ChefBot.Api.Messaging;

namespace ChefBot.Api;

/// <summary>
/// The JSON shapes of the request and response bodies that belong to the host rather than the core, made by the
/// source generator at build time. The core's own are in <see cref="Core.Managers.Contracts.ContractJsonContext"/>. Add a
/// line here for each new body type defined in this project.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(ImportStatus))]
[JsonSerializable(typeof(NewCertificateRequest))]
[JsonSerializable(typeof(HttpsStatusResponse))]
// Endpoints that answer with a file or a 202 return IResult, which the OpenAPI document describes as an empty object.
[JsonSerializable(typeof(IResult))]
public sealed partial class ApiJsonContext : JsonSerializerContext;
