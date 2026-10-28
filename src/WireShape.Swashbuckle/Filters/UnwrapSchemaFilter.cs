using System;

using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IOKode.WireShape.Swashbuckle.Filters;

/// <summary>
/// Takes note of a type that disappears once written, so that whatever points at the schema it got can be made to
/// point at what it wrapped.
/// </summary>
/// <remarks>
/// Which types those are is asked of the wire, so that this holds for whatever is declared to disappear and not
/// only for what happened to be named here.
/// </remarks>
public sealed class UnwrapSchemaFilter(
    Wire wire,
    Unwrapping unwrapping,
    Func<Type, string> nameOf) : ISchemaFilter
{
    /// <inheritdoc/>
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (wire.GetShape(context.Type) is not Shape.Unwraps unwraps)
        {
            return;
        }

        var arguments = context.Type.GetGenericArguments();
        if (unwraps.ArgumentIndex >= arguments.Length)
        {
            return;
        }

        unwrapping.Note(nameOf(context.Type), arguments[unwraps.ArgumentIndex]);
    }
}
