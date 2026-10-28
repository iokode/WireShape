using System;
using System.Collections.Generic;

namespace IOKode.WireShape;

/// <summary>
/// A source that can also name every type it knows, because there is a list behind it and not only a convention.
/// </summary>
/// <remarks>
/// The difference is not of convenience. What a catalog names is among the declarations of a wire, which is what
/// everything that walks them reads — the type descriptors, the schemas of a document — so those see it without
/// anybody mentioning it first. What only a convention recognizes is not there, and is answered when something
/// asks and not before.
/// <para>
/// Being one is therefore a statement about the source and not a spare method: a place that keeps forms
/// somebody wrote down — a table of converters, a registry, an attribute over each type — can be read through,
/// and a convention over an open family cannot.
/// </para>
/// </remarks>
public interface IShapeCatalog : IShapeSource
{
    /// <summary>Every type this source knows, with its form.</summary>
    IReadOnlyDictionary<Type, Shape> GetDeclarations(ISerializer serializer);
}
