using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

using Microsoft.OpenApi;

namespace IOKode.WireShape.OpenApi;

/// <summary>
/// Says in a document what a wire declares about the written form of a type. Nothing here decides what travels:
/// it is read from where that is declared and written as a schema.
/// </summary>
/// <remarks>
/// A type a wire declares gets a schema under its name and whoever uses it points at that, because the name is
/// what is left of the declaration once the value is written: a number that is an amount of money to two decimal
/// places and a number that is one to four are the same number written, and only the name tells them apart. A
/// type whose kind the reader's own language has already, a date or an instant, is written where it is used,
/// there being nothing for a name of ours to add.
/// </remarks>
public static class Schemas
{
    /// <summary>
    /// What each kind of written value is called in a document. Four and not one per carrier, because what a
    /// document says of a value is what the value looks like, and which type it is read back into is not written
    /// anywhere: that is what the carrier's own format is for.
    /// </summary>
    private static JsonSchemaType TypeOf(ValueKind kind) => kind switch
    {
        ValueKind.Text => JsonSchemaType.String,
        ValueKind.Integer => JsonSchemaType.Integer,
        ValueKind.Decimal => JsonSchemaType.Number,
        ValueKind.Boolean => JsonSchemaType.Boolean,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "No document says a value of this kind.")
    };

    /// <summary>
    /// The types a form is written for: the ones declared to travel as something a document can describe by
    /// itself, less the enumerations, whose values are read one by one from the serializer and said in a schema
    /// of their own.
    /// </summary>
    /// <remarks>
    /// By the form and not by whether it has a carrier. A wrapper that disappears leaves nothing to describe and
    /// is taken out of the document elsewhere, and a type declared with the members it travels under is
    /// described by the generator that walks them.
    /// </remarks>
    public static IEnumerable<(Type Type, Shape Shape)> GetProjected(Wire wire)
    {
        return wire.GetDeclaredShapes()
            .Where(declaration => !declaration.Key.IsEnum)
            .Where(declaration => declaration.Value.Shape is
                Shape.As or Shape.AsText or Shape.AsFile or Shape.AsAnother or Shape.OneOf)
            .Select(declaration => (declaration.Key, declaration.Value.Shape));
    }

    /// <summary>The types that get a schema of their own because another type of the wire travels as them.</summary>
    public static IEnumerable<Type> GetComposite(Wire wire) =>
        GetProjected(wire)
            .Select(declaration => declaration.Shape)
            .OfType<Shape.AsAnother>()
            .Select(another => another.Type)
            .Distinct();

    /// <summary>
    /// The types that get a schema of their own because the name is what is left of them once written, each
    /// with the value that schema describes.
    /// </summary>
    public static IEnumerable<(Type Type, IOpenApiSchema Schema)> GetNamed(Wire wire, Func<Type, string> nameOf) =>
        GetProjected(wire)
            .Where(declaration =>
                declaration.Shape.Carrier is {KeepsItsName: true} || declaration.Shape is Shape.OneOf)
            .Select(declaration => (
                declaration.Type,
                declaration.Shape is Shape.OneOf union
                    ? GetUnionSchema(union, nameOf)
                    : GetValueSchema(declaration.Shape)));

    /// <summary>
    /// What a declared type looks like where it is used: the value itself where there is nothing for a name to
    /// add, and otherwise a reference to the schema of whoever keeps the name.
    /// </summary>
    /// <param name="declared">The type as it was declared.</param>
    /// <param name="shape">The form it was declared to travel in.</param>
    /// <param name="nameOf">What a type is called in the document being written.</param>
    public static IOpenApiSchema GetSchema(Type declared, Shape shape, Func<Type, string> nameOf) => shape switch
    {
        {Carrier.KeepsItsName: true} => new OpenApiSchemaReference(nameOf(declared)),
        {Carrier: not null} => GetValueSchema(shape),
        Shape.AsFile => GetFileSchema(),
        Shape.AsAnother another => new OpenApiSchemaReference(nameOf(another.Type)),

        // By its own name, as a value that keeps one is: that these alternatives are one field is said in the
        // schema of this type and nowhere else, so whoever uses it points at that.
        Shape.OneOf => new OpenApiSchemaReference(nameOf(declared)),
        _ => throw new ArgumentException($"A document says nothing of a form such as {shape}.", nameof(shape))
    };

    /// <summary>
    /// The schema of a value written whole: what its carrier looks like, and the pattern of the declaration,
    /// which only a text has.
    /// </summary>
    public static IOpenApiSchema GetValueSchema(Shape shape)
    {
        var carrier = shape.Carrier!;

        return new OpenApiSchema
        {
            Type = TypeOf(carrier.Kind),
            Format = carrier.Format,
            Pattern = shape is Shape.AsText text ? text.Pattern : null
        };
    }

    /// <summary>
    /// The schema of the content of a file, which a document says is a text of bytes and nothing else: what it
    /// is called and what it carries travel beside it and not in the schema.
    /// </summary>
    public static IOpenApiSchema GetFileSchema() =>
        new OpenApiSchema {Type = JsonSchemaType.String, Format = "binary"};

    /// <summary>
    /// The schema of a value that comes out as one of several forms: each alternative written as it would be
    /// written anywhere and under the name it was declared with, and nothing saying which of them arrived,
    /// there being nothing in what travels that says it.
    /// </summary>
    /// <remarks>
    /// The name goes in the <c>title</c> of the branch, which is where a document says what one branch of
    /// several is called, and is what whoever generates from the document reads to name it on their side.
    /// </remarks>
    /// <param name="union">The form it was declared to travel in.</param>
    /// <param name="nameOf">What a type is called in the document being written.</param>
    public static IOpenApiSchema GetUnionSchema(Shape.OneOf union, Func<Type, string> nameOf) =>
        new OpenApiSchema
        {
            OneOf = union.Alternatives
                .Select(alternative => Named(alternative, nameOf))
                .ToList()
        };

    private static IOpenApiSchema Named(Alternative alternative, Func<Type, string> nameOf)
    {
        if (alternative.Form is Shape.AsAnother another)
        {
            return new OpenApiSchemaReference(nameOf(another.Type)) {Title = alternative.Name};
        }

        var written = (OpenApiSchema) GetValueSchema(alternative.Form);
        written.Title = alternative.Name;

        return written;
    }

    /// <summary>
    /// The schema of a type that turns out to be one of several kinds: one of them, told apart by the word each
    /// carries under the member that holds it.
    /// </summary>
    /// <remarks>
    /// For a wire that knows its kinds where whoever writes it does not. Where whoever writes knows them, the
    /// document is written with them already and what a declaration adds is the words that member may hold.
    /// </remarks>
    /// <param name="kinds">What the type turns out to be, each with the word that tells it apart.</param>
    /// <param name="discriminator">The member the word travels under, which is of the whole document.</param>
    /// <param name="nameOf">What a type is called in the document being written.</param>
    public static IOpenApiSchema GetKindsSchema(
        IReadOnlyCollection<Kind> kinds,
        string discriminator,
        Func<Type, string> nameOf) =>
        new OpenApiSchema
        {
            OneOf = kinds
                .Select(kind => (IOpenApiSchema) new OpenApiSchemaReference(nameOf(kind.Type)))
                .ToList(),
            Discriminator = new OpenApiDiscriminator
            {
                PropertyName = discriminator,
                Mapping = kinds.ToDictionary(
                    kind => kind.Discriminator,
                    kind => new OpenApiSchemaReference(nameOf(kind.Type)))
            }
        };

    /// <summary>
    /// The schema of the member one kind is told apart by: the word it carries and that word only, so that
    /// whoever reads the document knows which to send and whoever writes one is held to it.
    /// </summary>
    public static IOpenApiSchema GetDiscriminatorSchema(string word) =>
        new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Enum = new List<JsonNode> {JsonValue.Create(word)!}
        };
}
