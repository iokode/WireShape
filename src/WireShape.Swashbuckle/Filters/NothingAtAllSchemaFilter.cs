using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IOKode.WireShape.Swashbuckle.Filters;

/// <summary>
/// Says of a member that may be nothing that it may be nothing, where what it travels as is pointed at by name.
/// </summary>
/// <remarks>
/// A reference has no room to say it: beside a reference nothing else is read, and what is left for saying it is
/// to offer the one thing or nothing at all. Without this, a member the wire writes as null would be described
/// as a member that is always something.
/// </remarks>
public sealed class NothingAtAllSchemaFilter(Wire wire) : ISchemaFilter
{
    /// <inheritdoc/>
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var optional = (wire.GetMembers(context.Type) ?? Array.Empty<Member>())
            .Where(member => member.IsOptional)
            .Select(member => member.Name)
            .ToHashSet();

        if (optional.Count == 0)
        {
            return;
        }

        foreach (var members in GetMembers(schema))
        {
            foreach (var (name, written) in members.ToArray())
            {
                if (optional.Contains(name) && written is OpenApiSchemaReference reference)
                {
                    members[name] = OrNothing(reference);
                }
            }
        }
    }

    /// <summary>
    /// Where the members of a schema are written, which is the schema itself and each of the ones it is made
    /// of: what a type adds to the one it derives from is written apart from what it derives from.
    /// </summary>
    private static IEnumerable<IDictionary<string, IOpenApiSchema>> GetMembers(IOpenApiSchema schema)
    {
        if (schema is not OpenApiSchema written)
        {
            yield break;
        }

        if (written.Properties != null)
        {
            yield return written.Properties;
        }

        foreach (var part in written.AllOf ?? new List<IOpenApiSchema>())
        {
            if (part is OpenApiSchema member && member.Properties != null)
            {
                yield return member.Properties;
            }
        }
    }

    private static IOpenApiSchema OrNothing(IOpenApiSchema schema) => new OpenApiSchema
    {
        AnyOf = new List<IOpenApiSchema> {schema, new OpenApiSchema {Type = JsonSchemaType.Null}}
    };
}
