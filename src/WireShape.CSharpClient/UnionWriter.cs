using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IOKode.WireShape.AspNetCore;

namespace IOKode.WireShape.CSharpClient;

/// <summary>
/// Writes a type that comes out as one of several forms. C# has no way of saying "a text or an object", so what
/// is written is one type that holds either and says which it holds, with a reading of its own that tells them
/// apart by what arrived.
/// </summary>
/// <remarks>
/// Which alternative it holds is a value of an enumeration, so that covering them is a <c>switch</c> whose
/// missing arm the compiler reports. <c>Is</c> and <c>As</c> are for where the caller knows which to expect,
/// and <c>TryAs</c> for asking without an exception.
/// </remarks>
internal sealed class UnionWriter(StringBuilder written, ApiModel model, TypeNames names)
{
    /// <summary>What a value looks like on arrival, which is all the reading has to go on.</summary>
    private enum Arrival
    {
        Text,
        Number,
        Truth,
        Object,
        List
    }

    /// <param name="IsValue">
    /// Whether the type is one of values, which decides how the one that is held is read back out: the field
    /// admits nothing either way, and what admits nothing is read out of a value by its own member and out of a
    /// reference by saying there is one.
    /// </param>
    private readonly record struct Written(string Name, string Type, Arrival Arrives, bool IsValue)
    {
        /// <summary>The field holding this one, read as the type it is.</summary>
        public string Read(string field) => IsValue ? $"{field}!.Value" : $"{field}!";
    }

    /// <exception cref="InvalidOperationException">
    /// Two alternatives arrive looking the same — two texts, two objects — so nothing could tell which of them
    /// came. Whether a reader could tell them apart by a pattern or by which members they carry is not
    /// something this reading can do, and a reading that guessed would hand over the wrong one in silence.
    /// </exception>
    public void Write(Type type, Structure.OneOf union)
    {
        var name = names.Of(type);
        var alternatives = union.Alternatives
            .Select(alternative => new Written(
                DtoWriter.Pascal(alternative.Name),
                names.Of(alternative.Type),
                ArrivalOf(alternative.Type),
                alternative.Type.IsValueType))
            .ToArray();

        if (alternatives.GroupBy(alternative => alternative.Arrives).FirstOrDefault(alike => alike.Count() > 1)
            is { } twice)
        {
            throw new InvalidOperationException(
                $"'{name}' comes out as {string.Join(" and as ", twice.Select(alike => alike.Name))}, which " +
                $"arrive looking the same — {twice.Key.ToString().ToLowerInvariant()} — so nothing could tell " +
                "which of them came. Tell them apart in what travels, or read them by hand.");
        }

        WriteKinds(name, alternatives);
        WriteHolder(name, alternatives);
        WriteReading(name, alternatives);
    }

    private void WriteKinds(string name, IReadOnlyList<Written> alternatives)
    {
        written.AppendLine($"public enum {name}Kind");
        written.AppendLine("{");
        foreach (var alternative in alternatives)
        {
            written.AppendLine($"    {alternative.Name},");
        }

        written.AppendLine("}");
        written.AppendLine();
    }

    private void WriteHolder(string name, IReadOnlyList<Written> alternatives)
    {
        var parameters = alternatives.Select(alternative => $"{Held(alternative.Type)} {Field(alternative.Name)}");

        written.AppendLine($"[System.Text.Json.Serialization.JsonConverter(typeof({name}JsonConverter))]");
        // A class and not a value, because the value a struct has before anything is assigned would claim to
        // hold the first alternative and hold nothing: a reading of it would answer null where it should throw,
        // and a switch over it would take a branch of something that was never set. Unassigned is null here,
        // which the language already polices.
        written.AppendLine($"public sealed partial record {name}");
        written.AppendLine("{");

        foreach (var alternative in alternatives)
        {
            written.AppendLine($"    private readonly {Held(alternative.Type)} {Field(alternative.Name)};");
        }

        written.AppendLine();
        written.AppendLine($"    private {name}({name}Kind kind, {string.Join(", ", parameters)})");
        written.AppendLine("    {");
        written.AppendLine("        Kind = kind;");
        foreach (var alternative in alternatives)
        {
            written.AppendLine($"        this.{Field(alternative.Name)} = {Field(alternative.Name)};");
        }

        written.AppendLine("    }");
        written.AppendLine();
        written.AppendLine("    /// <summary>Which of the alternatives this one holds.</summary>");
        written.AppendLine($"    public {name}Kind Kind {{ get; }}");

        foreach (var alternative in alternatives)
        {
            var arguments = new[] {$"{name}Kind.{alternative.Name}"}
                .Concat(alternatives.Select(other =>
                    other.Name == alternative.Name ? "value" : $"default({Held(other.Type)})"));

            written.AppendLine();
            written.AppendLine($"    public static {name} Of{alternative.Name}({alternative.Type} value) =>");
            written.AppendLine($"        new({string.Join(", ", arguments)});");
            written.AppendLine();
            written.AppendLine($"    public bool Is{alternative.Name} => Kind == {name}Kind.{alternative.Name};");
            written.AppendLine();
            written.AppendLine($"    public {alternative.Type} As{alternative.Name} => Is{alternative.Name}");
            written.AppendLine($"        ? {alternative.Read(Field(alternative.Name))}");
            written.AppendLine("        : throw new InvalidOperationException(" +
                               $"$\"This holds {{Kind}} and not {alternative.Name}.\");");
            written.AppendLine();
            written.AppendLine($"    public bool TryAs{alternative.Name}(out {alternative.Type} value)");
            written.AppendLine("    {");
            written.AppendLine($"        value = Is{alternative.Name} " +
                               $"? {alternative.Read(Field(alternative.Name))} : default!;");
            written.AppendLine();
            written.AppendLine($"        return Is{alternative.Name};");
            written.AppendLine("    }");
            // Not for one that arrives as a list, because a list and a map are called by an interface here and
            // the language allows no conversion from one. That branch is made by its own name and nothing else.
            if (alternative.Arrives != Arrival.List)
            {
                written.AppendLine();
                written.AppendLine($"    public static implicit operator {name}({alternative.Type} value) =>");
                written.AppendLine($"        Of{alternative.Name}(value);");
            }
        }

        written.AppendLine("}");
        written.AppendLine();
    }

    private void WriteReading(string name, IReadOnlyList<Written> alternatives)
    {
        written.AppendLine($"public sealed class {name}JsonConverter : " +
                           $"System.Text.Json.Serialization.JsonConverter<{name}>");

        written.AppendLine("{");
        written.AppendLine($"    public override {name} Read(ref System.Text.Json.Utf8JsonReader reader, " +
                           "Type typeToConvert, System.Text.Json.JsonSerializerOptions options) =>");

        written.AppendLine("        reader.TokenType switch");
        written.AppendLine("        {");
        foreach (var alternative in alternatives)
        {
            foreach (var token in Tokens(alternative.Arrives))
            {
                written.AppendLine($"            System.Text.Json.JsonTokenType.{token} => {name}." +
                                   $"Of{alternative.Name}(System.Text.Json.JsonSerializer" +
                                   $".Deserialize<{alternative.Type}>(ref reader, options)!),");
            }
        }

        written.AppendLine("            _ => throw new System.Text.Json.JsonException(" +
                           $"$\"A {name} does not arrive as {{reader.TokenType}}.\")");

        written.AppendLine("        };");
        written.AppendLine();
        written.AppendLine($"    public override void Write(System.Text.Json.Utf8JsonWriter writer, {name} value, " +
                           "System.Text.Json.JsonSerializerOptions options)");

        written.AppendLine("    {");
        written.AppendLine("        switch (value.Kind)");
        written.AppendLine("        {");
        foreach (var alternative in alternatives)
        {
            written.AppendLine($"            case {name}Kind.{alternative.Name}:");
            written.AppendLine("                System.Text.Json.JsonSerializer.Serialize(" +
                               $"writer, value.As{alternative.Name}, options);");

            written.AppendLine("                break;");
        }

        written.AppendLine("        }");
        written.AppendLine("    }");
        written.AppendLine("}");
        written.AppendLine();
    }

    /// <summary>
    /// The type of the field holding one alternative, which admits nothing because only one of them is held at
    /// a time.
    /// </summary>
    private static string Held(string type) => type.EndsWith("?") ? type : type + "?";

    private static string Field(string name) => char.ToLowerInvariant(name[0]) + name[1..];

    private static IEnumerable<string> Tokens(Arrival arrival) => arrival switch
    {
        Arrival.Text => new[] {"String"},
        Arrival.Number => new[] {"Number"},
        Arrival.Truth => new[] {"True", "False"},
        Arrival.Object => new[] {"StartObject"},
        Arrival.List => new[] {"StartArray"},
        _ => throw new ArgumentOutOfRangeException(nameof(arrival), arrival, "Nothing arrives like this.")
    };

    /// <summary>
    /// What one alternative looks like on arrival, asked of the model and not of the type: a type declared to
    /// travel as a text arrives as a text however it is written here.
    /// </summary>
    private Arrival ArrivalOf(Type type) => model.Types.GetValueOrDefault(type) switch
    {
        Structure.Carried carried => ArrivalOf(carried.By),
        Structure.Enumeration {ByName: true} => Arrival.Text,
        Structure.Enumeration => Arrival.Number,
        Structure.Object => Arrival.Object,
        _ => OfItsOwn(type)
    };

    /// <summary>
    /// What a type with no row of its own arrives as: one of the carriers every writer writes whole, or the
    /// shape of what it holds.
    /// </summary>
    private static Arrival OfItsOwn(Type type)
    {
        if (Carrier.Of(type) is { } known)
        {
            return known.Kind switch
            {
                ValueKind.Text => Arrival.Text,
                ValueKind.Integer or ValueKind.Decimal => Arrival.Number,
                ValueKind.Boolean => Arrival.Truth,
                _ => Arrival.Object
            };
        }

        return typeof(IEnumerable).IsAssignableFrom(type) ? Arrival.List : Arrival.Object;
    }
}
