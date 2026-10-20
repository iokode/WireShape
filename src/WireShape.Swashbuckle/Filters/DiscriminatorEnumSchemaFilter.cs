using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IOKode.WireShape.Swashbuckle.Filters;

/// <summary>
/// Says which words the member that tells one kind from another may hold, so that whoever reads the document
/// learns them from the member itself.
/// </summary>
public sealed class DiscriminatorEnumSchemaFilter(Wire wire) : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Discriminator?.Mapping == null)
        {
            return;
        }

        var discriminatorName = schema.Discriminator.PropertyName!;

        if (schema.Properties![discriminatorName] is not OpenApiSchema discriminatorSchema)
        {
            return;
        }

        discriminatorSchema.Enum ??= wire.GetKinds(context.Type)
            .Select(JsonNode (kind) => JsonValue.Create(kind.Discriminator))
            .ToList();
    }
}
