using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IOKode.WireShape.AspNetCore;

namespace IOKode.WireShape.CSharpClient;

/// <summary>
/// Writes the types a client carries. They are what travels and nothing else: no rule of their own to keep, no
/// behavior, no relation beyond the one a discriminator tells apart. What comes back has already been checked
/// by whoever sent it.
/// </summary>
public sealed class DtoWriter(ApiModel model, TypeNames names)
{
    private readonly StringBuilder written = new();
    private readonly HashSet<Type> done = new();

    /// <summary>The C# written so far: the types, and nothing that uses them.</summary>
    public string Written => written.ToString();

    /// <summary>Writes every type asked for, and every type those ask for in turn.</summary>
    public void WritePending()
    {
        for (var pending = names.TakePending(); pending.Count > 0; pending = names.TakePending())
        {
            foreach (var type in pending)
            {
                Write(type);
            }
        }
    }

    private void Write(Type type)
    {
        if (!done.Add(type))
        {
            return;
        }

        switch (model.Types.GetValueOrDefault(type))
        {
            case Structure.Enumeration enumeration:
                WriteEnum(type, enumeration);
                return;

            case Structure.OneOf union:
                new UnionWriter(written, model, names).Write(type, union);
                return;
        }

        if (names.CarriedBy(type) is { } carrier)
        {
            WriteValue(type, carrier);

            return;
        }

        var described = Described(type);
        if (described.Kinds.Count > 0)
        {
            WriteHierarchy(type, described);

            return;
        }

        WriteObject(type, described);
    }

    /// <summary>
    /// What the model says a type is made of. A type nobody described carries nothing, which is what a type
    /// written as it stands does.
    /// </summary>
    private Structure.Object Described(Type type) =>
        model.Types.GetValueOrDefault(type) as Structure.Object
        ?? new Structure.Object(Array.Empty<Member>(), null, Array.Empty<Kind>());

    /// <summary>
    /// A type that travels as a bare value. It keeps its name here, so that what a value means does not come
    /// down to which number or which text it happens to be: two identifiers cannot be handed over for one
    /// another, and an amount cannot be read as a percentage.
    /// </summary>
    /// <remarks>
    /// Declared partial and without a reading of its own, so that whoever consumes the client can say how one
    /// is shown. What travels is answered apart from that, and a route or a query carries it unchanged.
    /// </remarks>
    private void WriteValue(Type type, Type carrier)
    {
        var name = names.Of(type);
        var carried = names.Of(carrier);

        written.AppendLine($"[System.Text.Json.Serialization.JsonConverter(typeof({name}JsonConverter))]");
        written.AppendLine($"public readonly partial record struct {name}({carried} Value) : IApiValue");
        written.AppendLine("{");
        written.AppendLine("    object? IApiValue.Written => Value;");
        written.AppendLine();
        written.AppendLine($"    public static implicit operator {carried}({name} value) => value.Value;");
        written.AppendLine();
        written.AppendLine($"    public static implicit operator {name}({carried} value) => new(value);");
        written.AppendLine("}");
        written.AppendLine();

        written.AppendLine(
            $"public sealed class {name}JsonConverter : System.Text.Json.Serialization.JsonConverter<{name}>");
        written.AppendLine("{");
        written.AppendLine($"    public override {name} Read(ref System.Text.Json.Utf8JsonReader reader, " +
                           "Type typeToConvert, System.Text.Json.JsonSerializerOptions options) =>");
        written.AppendLine(
            $"        new(System.Text.Json.JsonSerializer.Deserialize<{carried}>(ref reader, options)!);");
        written.AppendLine();
        written.AppendLine($"    public override void Write(System.Text.Json.Utf8JsonWriter writer, {name} value, " +
                           "System.Text.Json.JsonSerializerOptions options) =>");
        written.AppendLine("        System.Text.Json.JsonSerializer.Serialize(writer, value.Value, options);");
        written.AppendLine("}");
        written.AppendLine();
    }

    /// <summary>
    /// An enumeration as it travels: by the text the wire writes for each value, or by the number each one has.
    /// The number is written out, because what travels is the number and not the order the values are declared
    /// in.
    /// </summary>
    private void WriteEnum(Type type, Structure.Enumeration enumeration)
    {
        if (enumeration.ByName)
        {
            written.AppendLine(
                "[System.Text.Json.Serialization.JsonConverter(" +
                "typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]");
        }

        written.AppendLine($"public enum {names.Of(type)}");
        written.AppendLine("{");
        foreach (var value in enumeration.Values)
        {
            // Said one by one only where the word differs from the name, which is what a converter of its own
            // does: whoever reads the client sees the name, and what goes on the wire is this. Only where words
            // travel at all, there being no word to say of a value that travels as a number.
            if (enumeration.ByName && value.Written != value.Name)
            {
                written.AppendLine(
                    $"    [System.Text.Json.Serialization.JsonStringEnumMemberName(\"{value.Written}\")]");
            }

            written.AppendLine($"    {value.Name} = {value.Underlying},");
        }

        written.AppendLine("}");
        written.AppendLine();
    }

    /// <summary>
    /// A type that travels as one of several kinds. The word that tells them apart is written into the type
    /// itself, so that reading one is reading a property and not guessing.
    /// </summary>
    private void WriteHierarchy(Type type, Structure.Object described)
    {
        var name = names.Of(type);

        written.AppendLine("[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = " +
                           $"\"{model.Discriminator}\")]");

        foreach (var kind in described.Kinds)
        {
            written.AppendLine("[System.Text.Json.Serialization.JsonDerivedType(" +
                               $"typeof({names.Of(kind.Type)}), \"{kind.Discriminator}\")]");
        }

        WriteObject(type, described, declaredName: name, isAbstract: true);

        foreach (var kind in described.Kinds)
        {
            Write(kind.Type);
        }
    }

    private void WriteObject(
        Type type,
        Structure.Object described,
        string? declaredName = null,
        bool isAbstract = false)
    {
        var name = declaredName ?? names.Of(type);
        var @base = described.Base;
        var inherited = @base == null
            ? Array.Empty<string>()
            : Described(@base).Members.Select(member => member.Name).ToArray();

        // A kind added to a hierarchy that another client declares says so on itself: that hierarchy was written
        // without knowing this kind existed, so it cannot be the one to name it.
        if (@base != null && names.IsReused(@base))
        {
            var kind = Described(@base).Kinds
                .FirstOrDefault(declared => declared.Type == type)?.Discriminator;

            if (kind != null)
            {
                written.AppendLine($"[ApiPolymorphicKind(typeof({names.Of(@base)}), \"{kind}\")]");
            }
        }

        // Declared partial so that whoever consumes the client can give a type a presentation of its own,
        // such as the text it reads as, without that text being written here.
        written.AppendLine(
            $"public {(isAbstract ? "abstract " : "sealed ")}partial record {name}" +
            $"{(@base == null ? string.Empty : " : " + names.Of(@base))}");

        written.AppendLine("{");

        foreach (var member in described.Members.Where(member => !inherited.Contains(member.Name)))
        {
            var property = names.Of(member.Type, member.IsOptional);
            written.AppendLine($"    [System.Text.Json.Serialization.JsonPropertyName(\"{member.Name}\")]");

            // Settable, because what a call is given is filled in by whoever makes the call, field by field as
            // it is typed in, and not only at the moment the value is built. A member only ever written by the
            // answer is not asked of whoever calls, because there is nothing for them to say about it.
            var declaration = member switch
            {
                {IsAnswerOnly: true, IsOptional: false} =>
                    $"public {property} {Pascal(member.Name)} {{ get; set; }} = default!;",
                {IsOptional: true} => $"public {property} {Pascal(member.Name)} {{ get; set; }}",
                _ => $"public required {property} {Pascal(member.Name)} {{ get; set; }}"
            };

            written.AppendLine($"    {declaration}");
            written.AppendLine();
        }

        written.AppendLine("}");
        written.AppendLine();
    }

    internal static string Pascal(string name) =>
        name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name[1..];
}
