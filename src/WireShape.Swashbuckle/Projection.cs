using System;

using IOKode.WireShape.OpenApi;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IOKode.WireShape.Swashbuckle;

/// <summary>
/// Writes what a wire declares into the options the documents are generated with, so that a declared form is
/// said wherever such a value appears and said the same everywhere.
/// </summary>
public static class Projection
{
    /// <param name="options">The options of the generator, as they have been configured so far.</param>
    /// <param name="wire">The wire whose values the documents describe.</param>
    /// <param name="nameOf">What a type is called in the documents being written.</param>
    public static void Project(
        SwaggerGenOptions options,
        Wire wire,
        Func<Type, string> nameOf)
    {
        var written = options.SchemaGeneratorOptions.CustomTypeMappings;

        // A number that comes from a decimal is not a number that comes from a float, and a document that says
        // it is tells whoever reads it that an amount may arrive rounded. Said here so that it is said the same
        // wherever such a number appears, whether the type was declared to travel as one or holds one inside.
        written[typeof(decimal)] = Decimal;
        written[typeof(decimal?)] = Decimal;

        foreach (var (type, shape) in Schemas.GetProjected(wire))
        {
            // Written over whatever was there: whoever said it by hand said the same thing twice, and this is
            // the one of the two that was read from where the form is declared.
            written[type] = () => Schemas.GetSchema(type, shape, nameOf);

            if (type.IsValueType)
            {
                written[typeof(Nullable<>).MakeGenericType(type)] = () => Schemas.GetSchema(type, shape, nameOf);
            }
        }
    }

    private static IOpenApiSchema Decimal() => Schemas.GetValueSchema(new Shape.As(Carrier.Decimal));
}
