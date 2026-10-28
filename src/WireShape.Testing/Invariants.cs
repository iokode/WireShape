using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IOKode.WireShape.Testing;

/// <summary>
/// What has to be true of what a wire is declared from, each rule answering with what broke it and saying
/// nothing when nothing did, so a run names everything that is wrong instead of the first thing.
/// </summary>
/// <remarks>
/// These hold a source to what being one says about it, which nothing else can: a wire answers, and an answer
/// is right or wrong on its own terms. What is wrong here is a source that contradicts itself, and a wire built
/// on one answers the two questions about a type differently without anybody being able to tell.
/// </remarks>
public static class Invariants
{
    /// <summary>
    /// A catalog names every type it answers about. Being a catalog rather than a plain source is a statement
    /// about the source — that there is a list behind it and not only a rule — and this is that statement held
    /// to what the source does.
    /// </summary>
    /// <remarks>
    /// It matters because the two are read in different places. What a catalog names is among the declarations
    /// of a wire, which is what everything that walks them reads: the type descriptors, the schemas of a
    /// document, the types a client carries. What it only answers about when asked is seen by none of those. So
    /// a catalog that answers about a type it did not name leaves that type described by whoever writes instead
    /// of by the form it was declared to travel in — a code that travels as a text ends up an object with one
    /// member — and every rule that walks the declarations passes, the type not being among them to check.
    /// <para>
    /// Asked of the types of the assemblies and not of the catalog, because a catalog that names too few cannot
    /// be asked which those are: that is the fault being looked for.
    /// </para>
    /// </remarks>
    /// <param name="catalog">The source being held to it.</param>
    /// <param name="serializer">What writes the wire, which the source is asked with.</param>
    /// <param name="assemblies">Where the types it may be asked about are declared.</param>
    public static IReadOnlyCollection<string> FindWhatACatalogDidNotName(
        IShapeCatalog catalog,
        ISerializer serializer,
        IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(assemblies);

        var named = catalog.GetDeclarations(serializer);

        return TypesOf(assemblies)
            .Where(type => !named.ContainsKey(type))
            .Select(type => (Type: type, Shape: Answered(catalog, type, serializer)))
            .Where(answer => answer.Shape != null)
            .Select(answer =>
                $"{catalog.GetType().Name} answers that {answer.Type.Name} travels as {answer.Shape} " +
                "and does not name it among the types it knows")
            .ToArray();
    }

    /// <summary>
    /// What the source says of a type, where asking is something it survives: a source reads a convention of
    /// somebody else's and a type it was never meant to see can make it throw, which is not the fault being
    /// looked for here.
    /// </summary>
    private static Shape? Answered(IShapeCatalog catalog, Type type, ISerializer serializer)
    {
        try
        {
            return catalog.GetShape(type, serializer);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// The types of the assemblies, with whatever could not be loaded left out: what cannot be loaded cannot be
    /// asked about either.
    /// </summary>
    private static IEnumerable<Type> TypesOf(IEnumerable<Assembly> assemblies) => assemblies.SelectMany(assembly =>
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException loaded)
        {
            return loaded.Types.Where(type => type != null).Select(type => type!);
        }
    });
}
