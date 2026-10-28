using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace IOKode.WireShape.Swashbuckle;

/// <summary>
/// The wrappers the generator met and what each of them wrapped, each under the name the wrapper went by.
/// </summary>
/// <remarks>
/// Filled in while the schemas are written and read once the document is whole, because a wrapper can only be
/// taken out of the way after everything that points at it has been written. What a type wraps does not depend on
/// which document is being written, so one of these answers for all of them.
/// </remarks>
public sealed class Unwrapping
{
    private readonly ConcurrentDictionary<string, Type> wrapped = new(StringComparer.Ordinal);

    /// <summary>
    /// Notes that the schema under this id is a wrapper, and the type it wraps. Noting the same id again keeps the
    /// last type noted.
    /// </summary>
    /// <param name="schemaId">The id the wrapper's schema went by in the document.</param>
    /// <param name="wrappedType">The type it wraps, which takes its place.</param>
    public void Note(string schemaId, Type wrappedType) => wrapped[schemaId] = wrappedType;

    /// <summary>Every wrapper noted so far, by the id its schema went by, with the type it wraps.</summary>
    public IReadOnlyCollection<KeyValuePair<string, Type>> Noted => wrapped.ToArray();
}
