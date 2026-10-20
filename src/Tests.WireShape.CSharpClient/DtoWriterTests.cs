using System;
using System.Collections.Generic;
using IOKode.WireShape.AspNetCore;
using Xunit;

namespace IOKode.WireShape.CSharpClient.Tests;

/// <summary>
/// What a client declares of the types that travel in it, written out of the model and nothing else.
/// </summary>
public class DtoWriterTests
{
    [Fact]
    public void AValueThatKeepsItsName_ShouldBeDeclaredAsOneWithAReadingOfItsOwn()
    {
        // Act
        var written = Write(typeof(TrackingCode));

        // Assert
        Assert.Contains(
            "public readonly partial record struct TrackingCode(string Value) : IApiValue",
            written);

        Assert.Contains("public static implicit operator string(TrackingCode value) => value.Value;", written);
        Assert.Contains("public sealed class TrackingCodeJsonConverter", written);
    }

    /// <summary>
    /// Settable so that a call is filled in field by field, required where the caller has to say it, and asked
    /// of nobody where only an answer ever carries it.
    /// </summary>
    [Fact]
    public void SomethingWithMembers_ShouldBeDeclaredWithThemAsTheyTravel()
    {
        // Act
        var written = Write(typeof(Consignment));

        // Assert
        Assert.Contains("public sealed partial record Consignment", written);
        Assert.Contains("[System.Text.Json.Serialization.JsonPropertyName(\"code\")]", written);
        Assert.Contains("public required TrackingCode Code { get; set; }", written);
        Assert.Contains("public string? Instructions { get; set; }", written);
        Assert.Contains("public DateOnly Dispatched { get; set; } = default!;", written);
    }

    [Fact]
    public void AnEnumerationThatTravelsByWord_ShouldSayTheWordWhereItDiffersFromTheName()
    {
        // Act
        var written = Write(typeof(CarrierService));

        // Assert
        Assert.Contains("JsonStringEnumConverter", written);
        Assert.Contains("[System.Text.Json.Serialization.JsonStringEnumMemberName(\"next-day\")]", written);
        Assert.Contains("NextDay = 1,", written);

        // Nothing said of the one whose word is its name, there being nothing to correct.
        Assert.DoesNotContain("JsonStringEnumMemberName(\"Economy\")", written);
    }

    /// <summary>
    /// A number is not a word, so nothing is said of each value; and the number is written out, because what
    /// travels is the number and not the order the values are declared in.
    /// </summary>
    [Fact]
    public void AnEnumerationThatTravelsByNumber_ShouldSayNoWordsAtAll()
    {
        // Act
        var written = Write(typeof(ParcelSize));

        // Assert
        Assert.DoesNotContain("JsonStringEnumConverter", written);
        Assert.DoesNotContain("JsonStringEnumMemberName", written);
        Assert.Contains("Small = 1,", written);
        Assert.Contains("Large = 2,", written);
    }

    [Fact]
    public void WhatATypeTurnsOutToBe_ShouldBeDeclaredOnTheTypeItself()
    {
        // Act
        var written = Write(typeof(Parcel));

        // Assert
        Assert.Contains(
            "[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = \"kind\")]",
            written);

        Assert.Contains(
            "[System.Text.Json.Serialization.JsonDerivedType(typeof(Envelope), \"envelope\")]",
            written);

        Assert.Contains("public abstract partial record Parcel", written);
        Assert.Contains("public sealed partial record Envelope : Parcel", written);

        // What the base already declares is declared once and not again by each kind.
        Assert.Equal(1, written.Split("public required decimal Weight").Length - 1);
    }

    [Fact]
    public void WhatComesOutAsOneOfSeveralForms_ShouldBeDeclaredAsOneTypeThatHoldsEither()
    {
        // Act
        var written = Write(typeof(Phone));

        // Assert
        Assert.Contains("public enum PhoneKind", written);
        Assert.Contains("    National,", written);
        Assert.Contains("    International,", written);
        Assert.Contains("public sealed partial record Phone", written);
        Assert.Contains("public PhoneKind Kind { get; }", written);
        Assert.Contains("public static Phone OfNational(string value)", written);
        Assert.Contains("public bool IsNational => Kind == PhoneKind.National;", written);
        Assert.Contains("public string AsNational => IsNational", written);
        Assert.Contains("public bool TryAsNational(out string value)", written);
        Assert.Contains("public static implicit operator Phone(string value)", written);

        // A branch of values is read out of the field by its own member, there being nothing for the one that
        // says a reference is there to do.
        Assert.Contains("? extension!.Value", written);
        Assert.Contains("? national!", written);

        // And a branch that arrives as a list is made by its own name only: a list is called by an interface
        // here, and the language allows no conversion from one.
        Assert.DoesNotContain("implicit operator Phone(IReadOnlyList<string> value)", written);
        Assert.Contains("public static Phone OfSeveral(IReadOnlyList<string> value)", written);
    }

    /// <summary>
    /// Told apart by what arrived, which is all a reading has to go on: a text is read as the one alternative
    /// that arrives as a text, and an object as the one that arrives as an object.
    /// </summary>
    [Fact]
    public void TheReadingOfAUnion_ShouldTellTheAlternativesApartByWhatArrived()
    {
        // Act
        var written = Write(typeof(Phone));

        // Assert
        Assert.Contains("System.Text.Json.JsonTokenType.String => Phone.OfNational(", written);
        Assert.Contains("System.Text.Json.JsonTokenType.Number => Phone.OfExtension(", written);
        Assert.Contains("System.Text.Json.JsonTokenType.StartObject => Phone.OfInternational(", written);
        Assert.Contains("System.Text.Json.JsonTokenType.StartArray => Phone.OfSeveral(", written);

        // A truth arrives as one of two tokens, so it is read from either.
        Assert.Contains("System.Text.Json.JsonTokenType.True => Phone.OfWithheld(", written);
        Assert.Contains("System.Text.Json.JsonTokenType.False => Phone.OfWithheld(", written);

        Assert.Contains("case PhoneKind.National:", written);
        Assert.Contains("does not arrive as {reader.TokenType}", written);
    }

    /// <summary>
    /// Nothing could tell which of them came, and a reading that guessed would hand over the wrong one in
    /// silence.
    /// </summary>
    [Fact]
    public void AUnionWhoseAlternativesArriveAlike_ShouldBeRefused()
    {
        // Arrange
        var types = new Dictionary<Type, Structure>(Shipments.Types)
        {
            [typeof(Phone)] = new Structure.OneOf(new[]
            {
                new AlternativeType("national", typeof(string)),
                new AlternativeType("international", typeof(TrackingCode))
            })
        };

        var model = new ApiModel(
            Array.Empty<ApiAction>(), types, Array.Empty<HierarchyKind>(), "kind");

        var names = new TypeNames(model);
        names.Of(typeof(Phone));

        // Act
        var writing = () => new DtoWriter(model, names).WritePending();

        // Assert
        var refused = Assert.Throws<InvalidOperationException>(writing);
        Assert.Contains("National", refused.Message);
        Assert.Contains("International", refused.Message);
    }

    private static string Write(Type type)
    {
        var model = Shipments.Of();
        var names = new TypeNames(model);
        names.Of(type);

        var writer = new DtoWriter(model, names);
        writer.WritePending();

        return writer.Written;
    }
}
