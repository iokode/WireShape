using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi;

namespace IOKode.WireShape.OpenApi;

/// <summary>
/// What a whole document says about its own schemas: which of them anything points at, and what points at what.
/// </summary>
public static class Documents
{
    /// <summary>The schemas the document describes, under the name each of them goes by.</summary>
    public static IDictionary<string, IOpenApiSchema> GetSchemas(OpenApiDocument document) =>
        document.Components?.Schemas ?? new Dictionary<string, IOpenApiSchema>();

    /// <summary>
    /// The members a schema offers, wherever it keeps them: a schema that says what it inherits keeps its own
    /// under the part that declares them.
    /// </summary>
    public static IDictionary<string, IOpenApiSchema> GetMembers(IOpenApiSchema schema)
    {
        var members = new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);

        foreach (var member in schema.Properties ?? new Dictionary<string, IOpenApiSchema>())
        {
            members[member.Key] = member.Value;
        }

        foreach (var part in schema.AllOf ?? new List<IOpenApiSchema>())
        {
            foreach (var member in GetMembers(part))
            {
                members[member.Key] = member.Value;
            }
        }

        return members;
    }

    /// <summary>The name of every schema the document points at, wherever it points at it from.</summary>
    public static ISet<string> GetPointedAt(OpenApiDocument document)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var path in document.Paths ?? new OpenApiPaths())
        {
            foreach (var operation in path.Value.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>())
            {
                Collect(operation, names);
            }
        }

        foreach (var schema in document.Components?.Schemas?.Values.ToArray() ?? Array.Empty<IOpenApiSchema>())
        {
            Collect(schema, names);
        }

        return names;
    }

    /// <summary>
    /// Puts a schema of its own wherever this one points at a pointer these name, as deep as the pointing goes.
    /// </summary>
    /// <param name="schema">The schema to go through.</param>
    /// <param name="instead">What to write in place of each pointer, by the pointer it replaces.</param>
    public static void Replace(IOpenApiSchema schema, IReadOnlyDictionary<string, IOpenApiSchema> instead)
    {
        // A reference holds nothing of its own: what it points at is a schema of the document and is replaced there.
        if (schema is not OpenApiSchema written)
        {
            return;
        }

        if (written.Properties != null)
        {
            foreach (var (name, property) in written.Properties.ToArray())
            {
                if (Instead(property, instead) is { } replaced)
                {
                    written.Properties[name] = replaced;
                }
                else
                {
                    Replace(property, instead);
                }
            }
        }

        if (written.Items != null)
        {
            written.Items = Instead(written.Items, instead) ?? written.Items;
            Replace(written.Items, instead);
        }

        if (written.AdditionalProperties != null)
        {
            written.AdditionalProperties = Instead(written.AdditionalProperties, instead) ?? written.AdditionalProperties;
            Replace(written.AdditionalProperties, instead);
        }

        foreach (var composition in new[] {written.AllOf, written.AnyOf, written.OneOf})
        {
            if (composition == null)
            {
                continue;
            }

            for (var index = 0; index < composition.Count; index++)
            {
                if (Instead(composition[index], instead) is { } replaced)
                {
                    composition[index] = replaced;
                }
                else
                {
                    Replace(composition[index], instead);
                }
            }
        }
    }

    private static IOpenApiSchema? Instead(
        IOpenApiSchema schema,
        IReadOnlyDictionary<string, IOpenApiSchema> instead)
    {
        if (schema is not OpenApiSchemaReference reference || reference.Reference.ReferenceV3 is not { } pointer)
        {
            return null;
        }

        return instead.GetValueOrDefault(pointer);
    }

    private static void Collect(OpenApiOperation operation, ISet<string> names)
    {
        foreach (var parameter in operation.Parameters ?? new List<IOpenApiParameter>())
        {
            Collect(parameter.Schema, names);
        }

        foreach (var content in operation.RequestBody?.Content ?? new Dictionary<string, OpenApiMediaType>())
        {
            Collect(content.Value.Schema, names);
        }

        foreach (var response in operation.Responses ?? new OpenApiResponses())
        {
            foreach (var content in response.Value.Content ?? new Dictionary<string, OpenApiMediaType>())
            {
                Collect(content.Value.Schema, names);
            }
        }
    }

    private static void Collect(IOpenApiSchema? schema, ISet<string> names)
    {
        if (schema is OpenApiSchemaReference reference)
        {
            if (reference.Reference.Id != null)
            {
                names.Add(reference.Reference.Id);
            }

            return;
        }

        if (schema is not OpenApiSchema written)
        {
            return;
        }

        foreach (var member in written.Properties ?? new Dictionary<string, IOpenApiSchema>())
        {
            Collect(member.Value, names);
        }

        Collect(written.Items, names);
        Collect(written.AdditionalProperties, names);

        foreach (var composition in new[] {written.AllOf, written.AnyOf, written.OneOf})
        {
            foreach (var part in composition ?? new List<IOpenApiSchema>())
            {
                Collect(part, names);
            }
        }
    }
}
