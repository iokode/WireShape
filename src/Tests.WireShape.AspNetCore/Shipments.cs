using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IOKode.WireShape.AspNetCore.Tests;

/// <summary>
/// What a parcel service serves, which is the surface these tests read.
/// </summary>
public readonly record struct TrackingCode(string Value);

public sealed record Consignment(TrackingCode Code, string? Instructions);

/// <summary>Said of an action that is of this surface, which is what tells it from the ones that are not.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ServedAttribute : Attribute;

[ApiController]
[Route("shipments")]
public sealed class ShipmentsController : ControllerBase
{
    [HttpGet("{code}")]
    [Served]
    [ProducesResponseType<Consignment>(StatusCodes.Status200OK)]
    public Task<Consignment> GetAsync(TrackingCode code, CancellationToken cancellation) =>
        throw new NotSupportedException();

    [HttpGet]
    [Served]
    [ProducesResponseType<IReadOnlyList<Consignment>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<Consignment>> ListAsync([FromQuery] TrackingCode? code) =>
        throw new NotSupportedException();

    [HttpPost]
    [Served]
    [ProducesResponseType<Consignment>(StatusCodes.Status201Created)]
    public Task<Consignment> Dispatch([FromBody] Consignment consignment) =>
        throw new NotSupportedException();

    [HttpGet("{code}/label")]
    [Served]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    public Task<IActionResult> LabelAsync(TrackingCode code) => throw new NotSupportedException();

    [HttpPost("{code}/proof")]
    [Served]
    [ProducesResponseType<Consignment>(StatusCodes.Status200OK)]
    public Task<Consignment> ProofAsync(TrackingCode code, IFormFile photograph) =>
        throw new NotSupportedException();

    [HttpDelete("{code}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task CancelAsync(TrackingCode code) => throw new NotSupportedException();
}
