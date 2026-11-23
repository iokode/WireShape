using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IOKode.WireShape.Swashbuckle.Filters;

/// <summary>
/// Says of a member that may be absent that it may be absent, by taking it out of the members a schema requires.
/// </summary>
/// <remarks>
/// The generator works out what is required from the type: an attribute, the <c>required</c> of the language, or
/// a reference the language says is never null where it is told to read that as required. None of them is the
/// fact that a member of that type may be left out, which is declared of the type on the wire, and a document
/// that lists such a member as required tells whoever reads it to give what they were meant to be free to leave
/// out.
/// </remarks>
public sealed class OptionalSchemaFilter(Wire wire) : ISchemaFilter
{
    /// <inheritdoc/>
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var optional = (wire.GetMembers(context.Type) ?? Array.Empty<Member>())
            .Where(member => member.IsOptional)
            .Select(member => member.Name)
            .ToArray();

        if (optional.Length == 0)
        {
            return;
        }

        foreach (var required in GetRequired(schema))
        {
            foreach (var name in optional)
            {
                required.Remove(name);
            }
        }
    }

    /// <summary>
    /// Where the members a schema requires are written, which is the schema itself and each of the ones it is
    /// made of: what a type adds to the one it derives from is written apart from what it derives from.
    /// </summary>
    private static IEnumerable<ISet<string>> GetRequired(IOpenApiSchema schema)
    {
        if (schema is not OpenApiSchema written)
        {
            yield break;
        }

        if (written.Required != null)
        {
            yield return written.Required;
        }

        foreach (var part in written.AllOf ?? new List<IOpenApiSchema>())
        {
            if (part is OpenApiSchema member && member.Required != null)
            {
                yield return member.Required;
            }
        }
    }
}
