using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.OpenApi;

namespace IOKode.WireShape.OpenApi.Testing;

/// <summary>
/// What has to be true of a document that describes a wire, whatever that document happens to describe. Each
/// one answers with what breaks it, so that a run says everything that is wrong instead of the first thing.
/// </summary>
/// <remarks>
/// None of these asserts the name a schema goes under. A rule written against a name has to be rewritten the
/// day the naming changes, and rewriting it is what hides that the change broke something. They look at
/// relations instead: who points at what, and what the wire writes.
/// </remarks>
public static class Invariants
{
    /// <summary>
    /// A reference reads the same in every version of the format, so which one is written here does not decide
    /// what is found.
    /// </summary>
    private const OpenApiSpecVersion WrittenAs = OpenApiSpecVersion.OpenApi3_1;

    /// <summary>
    /// Nothing is described that nothing points at. A schema nobody reaches is either residue of something that
    /// stopped being referenced or a type that walked into the document on its own.
    /// </summary>
    public static async Task<IReadOnlyCollection<string>> FindUnreferencedSchemasAsync(
        string name,
        OpenApiDocument document)
    {
        var written = await document.SerializeAsJsonAsync(WrittenAs);

        return Documents.GetSchemas(document).Keys
            .Where(id => !written.Contains($"\"#/components/schemas/{id}\"", StringComparison.Ordinal))
            .Select(id => $"{name}: nothing points at {id}")
            .ToArray();
    }

    /// <summary>
    /// Nothing points at what is not described. A reference to a schema the document does not have leaves
    /// whoever reads it with a member they cannot know the form of.
    /// </summary>
    public static async Task<IReadOnlyCollection<string>> FindDanglingReferencesAsync(
        string name,
        OpenApiDocument document)
    {
        var written = await document.SerializeAsJsonAsync(WrittenAs);
        var described = Documents.GetSchemas(document).Keys;

        return Regex.Matches(written, "\"#/components/schemas/(?<id>[^\"]+)\"")
            .Select(match => match.Groups["id"].Value)
            .Distinct(StringComparer.Ordinal)
            .Where(id => !described.Contains(id))
            .Select(id => $"{name}: something points at {id}, which the document does not describe")
            .ToArray();
    }

    /// <summary>
    /// No schema offers a member that never travels. Which member that is depends on what the wire is made of,
    /// so whoever runs this says which one, there being no way to ask a rule what it recognises.
    /// </summary>
    public static IReadOnlyCollection<string> FindSchemasCarrying(
        string name,
        OpenApiDocument document,
        string member)
    {
        return Documents.GetSchemas(document)
            .Where(schema => Documents.GetMembers(schema.Value).ContainsKey(member))
            .Select(schema => $"{name}: {schema.Key} carries '{member}', which never travels")
            .ToArray();
    }

    /// <summary>
    /// An enumeration is described as the wire writes it. Whether it travels as a name or as a number is asked
    /// of the serializer, by writing a value and looking at what came out, because what decides it is a
    /// converter and not the type.
    /// </summary>
    /// <param name="written">What the wire writes, asked of the thing that writes it.</param>
    /// <param name="nameOf">What a type is called in this document.</param>
    /// <param name="assemblies">Where the types this document describes are declared.</param>
    public static IReadOnlyCollection<string> FindEnumerationsDescribedOtherwise(
        string name,
        OpenApiDocument document,
        Wire written,
        Func<Type, string> nameOf,
        IEnumerable<Assembly> assemblies)
    {
        var schemas = Documents.GetSchemas(document);
        var wrong = new List<string>();

        foreach (var type in assemblies.SelectMany(TypesOf).Where(type => type.IsEnum).Distinct())
        {
            if (!schemas.TryGetValue(nameOf(type), out var schema))
            {
                continue;
            }

            var byName = written.TravelsByName(type);
            var asString = (schema.Type & JsonSchemaType.String) == JsonSchemaType.String;

            if (byName != asString)
            {
                wrong.Add($"{name}: {type.Name} travels as {(byName ? "a name" : "a number")} and is described " +
                          $"as {(asString ? "a string" : "a number")}");
            }
        }

        return wrong;
    }

    /// <summary>
    /// A type declared to travel as a value is described as that value and under its own name, which is all that
    /// is left of the declaration once the value is written: every amount is a number and every identifier a
    /// text, and the name is what told them apart.
    /// </summary>
    public static IReadOnlyCollection<string> FindNamedValuesDescribedOtherwise(
        string name,
        OpenApiDocument document,
        Wire written,
        Func<Type, string> nameOf)
    {
        var schemas = Documents.GetSchemas(document);
        var wrong = new List<string>();

        foreach (var (type, shape) in written.GetDeclaredShapes())
        {
            if (shape.Carrier is not {KeepsItsName: true} carrier)
            {
                continue;
            }

            if (!schemas.TryGetValue(nameOf(type), out var schema))
            {
                continue;
            }

            if (Documents.GetMembers(schema).Count > 0)
            {
                wrong.Add($"{name}: {type.Name} travels as a value and is described as something with members");
                continue;
            }

            if (carrier.Format != null && schema.Format != carrier.Format)
            {
                wrong.Add($"{name}: {type.Name} is declared as {carrier.Format} and is described " +
                          $"as {schema.Format ?? "nothing of the kind"}");
            }

            if (shape is Shape.AsText {Pattern: { } pattern} && schema.Pattern != pattern)
            {
                wrong.Add($"{name}: {type.Name} is declared to look like {pattern} and is described " +
                          $"as looking like {schema.Pattern ?? "anything"}");
            }
        }

        return wrong;
    }

    /// <summary>
    /// A value object travels as the data it carries and not with the conveniences it offers on top. A member
    /// worked out from the others is one the reader can work out too, and publishing it is one more thing to keep
    /// consistent for nothing.
    /// </summary>
    /// <param name="isValueObject">
    /// Which of the types is a value object, there being nothing in a type that says so.
    /// </param>
    /// <param name="answers">
    /// Whether a member of a type is answered although it is worked out, which is a decision of whoever serves
    /// the document and not of this.
    /// </param>
    public static IReadOnlyCollection<string> FindValueObjectsOfferingWhatIsWorkedOut(
        string name,
        OpenApiDocument document,
        Wire written,
        IEnumerable<Assembly> assemblies,
        Func<Type, string> nameOf,
        Func<Type, bool> isValueObject,
        Func<Type, string, bool> answers)
    {
        var schemas = Documents.GetSchemas(document);
        var offered = new List<string>();

        foreach (var type in assemblies.SelectMany(TypesOf).Where(isValueObject).Distinct())
        {
            if (!schemas.TryGetValue(nameOf(type), out var schema))
            {
                continue;
            }

            var published = Documents.GetMembers(schema).Keys;
            var workedOut = GetWorkedOutMembers(type, written)
                .Where(member => published.Contains(member.Name))
                .Where(member => !answers(type, member.PropertyName))
                .Select(member => member.Name)
                .ToArray();

            if (workedOut.Length > 0)
            {
                offered.Add($"{name}: {type.Name} offers {string.Join(", ", workedOut)}, which is worked out " +
                            "from what it carries");
            }
        }

        return offered;
    }

    /// <summary>
    /// The members a type works out instead of carrying, told apart by their getter: the getter of a member a
    /// type carries is written by the compiler, and the getter of one it works out is written by whoever wrote
    /// the type.
    /// </summary>
    private static IReadOnlyCollection<(string Name, string PropertyName)> GetWorkedOutMembers(
        Type type,
        Wire written)
    {
        return (written.GetMembers(type) ?? Array.Empty<Member>())
            .Select(member => (member.Name, Declared: member.Declared as PropertyInfo))
            .Where(member => member.Declared != null && IsWorkedOut(member.Declared))
            .Select(member => (member.Name, member.Declared!.Name))
            .ToArray();
    }

    private static bool IsWorkedOut(PropertyInfo property) =>
        property.GetMethod is { } getter &&
        getter.GetCustomAttribute<CompilerGeneratedAttribute>() == null;

    private static IEnumerable<Type> TypesOf(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException loaded)
        {
            return loaded.Types.Where(type => type != null).Select(type => type!);
        }
    }
}
