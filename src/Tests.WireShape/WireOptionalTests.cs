using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using IOKode.WireShape.SystemTextJson;
using Xunit;

namespace IOKode.WireShape.Tests;

/// <summary>
/// Which members may be absent from what travels: those whose type is declared so, by a line or by a source,
/// whatever the form of the type and whatever the language says of its nullability. And absent is not null.
/// </summary>
public class WireOptionalTests
{
    private static readonly ISerializer serializer = new JsonSerializerOptionsSerializer(
        new JsonSerializerOptions {TypeInfoResolver = new DefaultJsonTypeInfoResolver()});

    [Theory]
    [InlineData(nameof(Amendment.Nickname))]
    [InlineData(nameof(Amendment.Window))]
    public void AMemberOfATypeDeclaredOptional_ShouldBeOptional(string name)
    {
        // Act
        var member = MemberOf(BuildWire(), name);

        // Assert
        Assert.True(member.IsOptional);
    }

    [Theory]
    [InlineData(nameof(Amendment.Name))]
    [InlineData(nameof(Amendment.Boxes))]
    public void AMemberOfAFamilyASourceRecognizes_ShouldBeOptional(string name)
    {
        // Act
        var member = MemberOf(BuildWire(), name);

        // Assert
        Assert.True(member.IsOptional);
    }

    [Theory]
    [InlineData(nameof(Amendment.Instructions))]
    [InlineData(nameof(Amendment.Code))]
    public void AMemberOfATypeNobodyDeclaredOptional_ShouldNotBeOptional(string name)
    {
        // Act
        var member = MemberOf(BuildWire(), name);

        // Assert
        Assert.False(member.IsOptional);
    }

    /// <summary>Left out is not null: being one says nothing of being the other.</summary>
    [Theory]
    [InlineData(nameof(Amendment.Name))]
    [InlineData(nameof(Amendment.Boxes))]
    [InlineData(nameof(Amendment.Nickname))]
    [InlineData(nameof(Amendment.Window))]
    public void AMemberThatMayBeAbsent_ShouldNotBeNullableForThat(string name)
    {
        // Act
        var member = MemberOf(BuildWire(), name);

        // Assert
        Assert.False(member.IsNullable);
    }

    /// <summary>
    /// What travels is what the wrapper leaves behind, and that is what the language says may be null.
    /// </summary>
    [Fact]
    public void AMemberWhoseWrapperLeavesWhatMayBeNull_ShouldBeNullable()
    {
        // Act
        var member = MemberOf(BuildWire(), nameof(Amendment.Notes));

        // Assert
        Assert.True(member.IsNullable);
        Assert.True(member.IsOptional);
    }

    [Fact]
    public void AMemberOfATypeDeclaredOptional_ShouldBeOptionalInTheMembersDeclaredByHand()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddOptional<Nickname>();
        declarer.Add<Window>(new Shape.Members(new[]
        {
            new Member("label", typeof(Nickname), IsNullable: false, IsOptional: false)
        }));

        // Act
        var members = declaring.Build(serializer).GetMembers(typeof(Window))!;

        // Assert
        Assert.True(Assert.Single(members).IsOptional);
    }

    [Fact]
    public void ANullableOfATypeDeclaredOptional_ShouldBeOptional()
    {
        // Act
        var optional = BuildWire().IsOptional(typeof(Nickname?));

        // Assert
        Assert.True(optional);
    }

    [Fact]
    public void ATypeNobodyDeclaredOptional_ShouldNotBeOptional()
    {
        // Act
        var optional = BuildWire().IsOptional(typeof(string));

        // Assert
        Assert.False(optional);
    }

    /// <summary>There is no other answer for a second declarer to disagree with.</summary>
    [Fact]
    public void ATypeDeclaredOptionalByTwoDeclarers_ShouldBeOptional()
    {
        // Arrange
        var declaring = new WireBuilder();
        declaring.GetDeclarer("application").AddOptional<Nickname>();
        declaring.GetDeclarer("plugin").AddOptional<Nickname>();

        // Act
        var optional = declaring.Build(serializer).IsOptional(typeof(Nickname));

        // Assert
        Assert.True(optional);
    }

    [Fact]
    public void AMemberOfATypeDeclaredOptional_ShouldBeOptionalInWhatTravels()
    {
        // Act
        var types = BuildWire().GetStructures(new Type?[] {typeof(Amendment)});

        // Assert
        var described = Assert.IsType<Structure.Object>(types[typeof(Amendment)]);
        Assert.Equal(
            new[] {"Name", "Notes", "Boxes", "Nickname", "Window"},
            described.Members.Where(member => member.IsOptional).Select(member => member.Name));
    }

    private static Member MemberOf(Wire wire, string name) =>
        wire.GetMembers(typeof(Amendment))!.Single(member => member.Name == name);

    /// <summary>
    /// A wrapper that disappears and is recognized by a convention, a value of the same kind, a text and an
    /// object, each declared optional: the first two by a source and the rest by a line each.
    /// </summary>
    private static Wire BuildWire()
    {
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");

        declarer.AddShapeSource(new Unwrapping());
        declarer.Add<Nickname>(new Shape.AsText());

        declarer.AddOptionalSource(new AmendedOrKept());
        declarer.AddOptional<Nickname>();
        declarer.AddOptional<Window>();

        return declaring.Build(serializer);
    }

    public sealed record Amended<T>(T Value);

    public readonly record struct Kept<T>(T Value);

    public readonly record struct Nickname(string Value);

    public sealed record Window(int From, int To);

    public sealed record Amendment(
        Amended<string> Name,
        Amended<string?> Notes,
        Kept<int> Boxes,
        Nickname Nickname,
        Window Window,
        string? Instructions,
        string Code);

    private sealed class Unwrapping : IShapeSource
    {
        public Shape? GetShape(Type type, ISerializer serializer) => IsAmendedOrKept(type) ? new Shape.Unwraps(0) : null;
    }

    private sealed class AmendedOrKept : IOptionalSource
    {
        public bool IsOptional(Type type, ISerializer serializer) => IsAmendedOrKept(type);
    }

    private static bool IsAmendedOrKept(Type type) =>
        type.IsGenericType &&
        (type.GetGenericTypeDefinition() == typeof(Amended<>) || type.GetGenericTypeDefinition() == typeof(Kept<>));
}
