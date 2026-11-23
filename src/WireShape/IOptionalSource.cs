using System;

namespace IOKode.WireShape;

/// <summary>
/// Somewhere it can be read that a member of a type may be absent from what travels, without anyone declaring it
/// type by type.
/// </summary>
/// <remarks>
/// A type whose absence means something is usually one of a family — every <c>Amended&lt;T&gt;</c> of an API —
/// and a family is recognized by a convention and not listed: which arguments it is closed over is open. A source
/// says the convention once. What it answers is the same fact a declaration is: a member of that type may be left
/// out.
/// <para>
/// One question and not two, as with a source of words: a type absent from what travels is recognized when it is
/// met, and nothing ever has to ask a source to name them all.
/// </para>
/// </remarks>
public interface IOptionalSource
{
    /// <summary>
    /// Whether a member of this type may be absent from what travels. False where this source has nothing to say
    /// about it, which is no different from saying that it may not: there is nothing a source could take back.
    /// </summary>
    bool IsOptional(Type type, ISerializer serializer);
}
