using System;
using System.Linq;
using System.Text.Json.Nodes;

using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IOKode.WireShape.Swashbuckle.Filters;

/// <summary>
/// Says of an enumeration what the wire writes for each of its values, by writing them and looking at what came
/// out. Whether one travels as a name or as a number is decided by a converter and not by the type, so it is the
/// only way of saying it that cannot disagree with the answers.
/// </summary>
public sealed class EnumerationSchemaFilter(Wire wire) : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum || schema is not OpenApiSchema written)
        {
            return;
        }

        var values = Enum.GetValues(context.Type).Cast<object>().ToArray();
        if (values.Length == 0)
        {
            return;
        }

        var byName = wire.TravelsByName(context.Type);
        var traveling = values
            .Select(value => wire.GetValueForm(context.Type, value)?.Text ?? ((Enum) value).ToString("D"))
            .ToArray();

        written.Type = byName ? JsonSchemaType.String : JsonSchemaType.Integer;
        written.Format = byName ? null : "int32";

        // The numbers as they travel and not as they underlie the members, because a converter is free to write
        // a value under a number of its own and it is the one a caller has to send.
        written.Enum = byName
            ? traveling.Select(word => (JsonNode) JsonValue.Create(word)!).ToList()
            : traveling.Select(number => JsonNode.Parse(number)!).ToList();
    }
}
