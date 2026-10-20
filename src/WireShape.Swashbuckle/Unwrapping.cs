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

    public void Note(string schemaId, Type wrappedType) => wrapped[schemaId] = wrappedType;

    public IReadOnlyCollection<KeyValuePair<string, Type>> Noted => wrapped.ToArray();
}
