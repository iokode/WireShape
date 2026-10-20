namespace IOKode.WireShape;

/// <summary>
/// What one value of an enumeration comes out as: the word it travels under, or the number it travels as. One
/// of the two and never both, because a value comes out once.
/// </summary>
/// <remarks>
/// The text is kept as it travels and not as the type has it. A converter is free to write a value under a word
/// of its own, and just as free to write it as a number that is not the one underlying the member — and a
/// document that published the member's own instead would tell whoever reads it to send something nobody
/// accepts.
/// </remarks>
public abstract record EnumerationValueForm
{
    private EnumerationValueForm()
    {
    }

    /// <summary>The text it travels as, for whoever reads it without caring which of the two it is.</summary>
    public abstract string Text { get; init; }

    /// <summary>It travels under a word, which is this one and not the name the member carries.</summary>
    public sealed record Word(string Text) : EnumerationValueForm;

    /// <summary>
    /// It travels as a whole number, which is this one and not the one underlying the member.
    /// </summary>
    public sealed record Integer(string Text) : EnumerationValueForm;
}
