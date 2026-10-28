namespace IOKode.WireShape;

/// <summary>What was declared of a type: the form it travels in, and who said so.</summary>
/// <remarks>
/// The form is what every reader of a wire is after. Who said it matters where a wire is composed of parts and
/// the form alone no longer tells them apart: two parts may each declare a type of their own that happens to be
/// called the same, and whoever has to give one of them a name of its own — a schema of a document, a type of a
/// client — has nothing else to tell them by.
/// </remarks>
/// <param name="Shape">The written form of the type.</param>
/// <param name="Declarer">
/// The name of whoever declared it, which this library never reads and never compares to anything but another
/// name.
/// </param>
public sealed record Declaration(Shape Shape, string Declarer);
