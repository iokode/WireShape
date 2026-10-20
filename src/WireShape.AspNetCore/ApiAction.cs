using System;
using System.Collections.Generic;

namespace IOKode.WireShape.AspNetCore;

/// <summary>Where a value of a call travels.</summary>
public enum ApiParameterOrigin
{
    Route,
    Query,
    Body,
    Form
}

/// <summary>A value a call carries.</summary>
public sealed record ApiParameter(string Name, Type Type, ApiParameterOrigin Origin, bool IsOptional);

/// <summary>
/// One call of the API, as the application serves it: the method and the path it answers, what it takes and what
/// it gives back, and the operation it stands for.
/// </summary>
public sealed record ApiAction
{
    public required string Method { get; init; }

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
    /// The command or the query the action invokes, which is what the call is named after. Null for an action
    /// that stands for no operation of its own, which is named after its method instead.
    /// </summary>
    public Type? Operation { get; init; }

    public required string MethodName { get; init; }

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
