using System;
using System.Collections.Generic;

namespace IOKode.WireShape.AspNetCore;

/// <summary>
/// What an API is, said as data: the calls it answers and the types that travel in them.
/// </summary>
/// <remarks>
/// Whoever reads this is reading a description and not an application: nothing here has to be asked of a
/// serializer, of a controller or of a wire, because whoever built it asked already. That is what lets the
/// writing of a client be done by something that knows nothing of where the API came from.
/// </remarks>
/// <param name="Actions">Every call of the API, in the order a client declares them.</param>
/// <param name="Types">
/// What each type comes out as, by the type it is: every type an action mentions, and every type those mention
/// in turn.
/// </param>
/// <param name="Discriminator">
/// The member a value that is one of several says which one it is under, which is of the whole API and not of
/// one type.
/// </param>
/// <param name="Kinds">
/// Every type this wire tells apart from another, with the type it travels under and the word that tells it
/// apart. A kind added to a hierarchy of the application is here although no call mentions it, because an
/// answer of the application turns out to be one of them.
/// </param>
public sealed record ApiModel(
    IReadOnlyList<ApiAction> Actions,
    IReadOnlyDictionary<Type, Structure> Types,
    IReadOnlyList<HierarchyKind> Kinds,
    string Discriminator);
