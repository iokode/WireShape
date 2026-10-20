using System;
using System.Collections.Generic;

namespace IOKode.WireShape;

/// <summary>
/// Somewhere the kinds of a wire can be read from without anyone declaring them one by one.
/// </summary>
/// <remarks>
/// A hierarchy is usually written down somewhere already — an attribute over each type, a table, a convention
/// over the assemblies — and a source reads the kinds from there instead of them being said twice. What a
/// source answers is the same fact a declaration is: which type a base turns out to be, and under which word.
/// </remarks>
public interface IHierarchySource
{
    /// <summary>
    /// What this base turns out to be, or empty where this source has nothing to say about it.
    /// </summary>
    IReadOnlyCollection<Kind> GetKinds(Type declaredBase, ISerializer serializer);

    /// <summary>
    /// Every kind this source can name. Empty where a source answers about a base when it is asked and cannot
    /// be asked to name them all.
    /// </summary>
    IReadOnlyCollection<HierarchyKind> GetDeclarations(ISerializer serializer);
}
