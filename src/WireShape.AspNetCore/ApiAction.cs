using System;
using System.Collections.Generic;

namespace IOKode.WireShape.AspNetCore;

/// <summary>Where a value of a call travels.</summary>
public enum ApiParameterOrigin
{
    /// <summary>A segment of the path.</summary>
    Route,

    /// <summary>A value of the query string.</summary>
    Query,

    /// <summary>The body of the request.</summary>
    Body,

    /// <summary>A field of a form, which is where a file a call takes travels.</summary>
    Form
}

/// <summary>A value a call carries.</summary>
public sealed record ApiParameter(string Name, Type Type, ApiParameterOrigin Origin, bool IsOptional);

/// <summary>
/// One call of the API, as the application serves it: the method and the path it answers, what it takes and what
/// it gives back.
/// </summary>
public sealed record ApiAction
{
    /// <summary>The HTTP method the call is made with, as the explorer reports it: <c>GET</c>, <c>POST</c>.</summary>
    public required string Method { get; init; }

    /// <summary>
    /// The path the call answers, starting with a slash, with each value of the route in braces under its name:
    /// <c>/bookings/{reference}</c>. It is the route the application serves, conventions included.
    /// </summary>
    public required string Route { get; init; }

    /// <summary>
    /// The controller the action is on, as the application names it, which is what groups the calls of a
    /// client.
    /// </summary>
    /// <remarks>
    /// The name as it is and not cut down to anything: whether a controller of yours is called
    /// <c>ShipmentsController</c>, <c>ShipmentsCtrl</c> or something a generator of your own made up is yours,
    /// and so is what a client made of its calls is called. Nothing here takes a word off either end.
    /// </remarks>
    public required string Controller { get; init; }

    /// <summary>
    /// The name the call takes in a client: the name of the method, with <c>Async</c> added where it does not end
    /// in it already, because every call is awaited.
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    /// What the call takes, one per parameter of the action. A group of values the explorer spells out member by
    /// member is put back together under the parameter it came from, and what tells the call to stop is not one
    /// of them.
    /// </summary>
    public required IReadOnlyList<ApiParameter> Parameters { get; init; }

    /// <summary>What the call answers with, or null when it answers with nothing.</summary>
    public Type? Result { get; init; }

    /// <summary>
    /// Whether what it answers with is a file. A file is not read as a value: it is handed over as it came, with
    /// the name and the type the answer gave it.
    /// </summary>
    public bool IsDownload { get; init; }

    /// <summary>Whether what it takes travels as a file in a form.</summary>
    public bool IsMultipart { get; init; }
}
