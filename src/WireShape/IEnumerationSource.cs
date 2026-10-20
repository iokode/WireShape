using System;

namespace IOKode.WireShape;

/// <summary>
/// Somewhere the words of an enumeration can be read from without anyone declaring them one by one.
/// </summary>
/// <remarks>
/// The words of a wire are usually a convention and not a list — every enumeration of this API travels in the
/// shape of its own name, cut this way or that — and a source says the convention once instead of a line per
/// value. What it answers is the same fact a declaration is: the word one value travels under.
/// <para>
/// One question and not two, unlike a source of forms or of kinds: which values an enumeration has is answered
/// by the language, so nobody ever has to ask a source to name them all.
/// </para>
/// </remarks>
public interface IEnumerationSource
{
    /// <summary>
    /// The word this value travels under, or null where this source has nothing to say about it.
    /// </summary>
    string? GetWord(Type enumType, object value, ISerializer serializer);
}
