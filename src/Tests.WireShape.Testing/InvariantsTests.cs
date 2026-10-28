using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using IOKode.WireShape.SystemTextJson;
using Xunit;

namespace IOKode.WireShape.Testing.Tests;

/// <summary>
/// What a rule says of a source that breaks it, and that it says nothing of one that keeps it.
/// </summary>
public class InvariantsTests
{
    private static readonly ISerializer serializer = new JsonSerializerOptionsSerializer(
        new JsonSerializerOptions {TypeInfoResolver = new DefaultJsonTypeInfoResolver()});

    private static readonly Assembly[] assemblies = {typeof(InvariantsTests).Assembly};

    [Fact]
    public void ACatalogOfCodes_ShouldNameEveryCodeItAnswersAbout()
    {
        // Act
        var broken = Invariants.FindWhatACatalogDidNotName(new CodeShapes(), serializer, assemblies);

        // Assert
        Assert.Empty(broken);
    }

    /// <summary>
    /// The rule earns its keep by finding one, so a source that is wrong on purpose is kept here and the rule
    /// is asked about it. It names the type and what the source answered of it.
    /// </summary>
    [Fact]
    public void ACatalogThatAnswersAboutWhatItDidNotName_ShouldBeFound()
    {
        // Act
        var broken = Invariants.FindWhatACatalogDidNotName(new HalfNamedCodeShapes(), serializer, assemblies);

        // Assert
        var complaint = Assert.Single(broken);
        Assert.Contains(nameof(DeliveryCode), complaint);
        Assert.Contains(nameof(HalfNamedCodeShapes), complaint);
    }

    [Fact]
    public void ACatalogThatAnswersAboutNothing_ShouldBeFoundToHaveNothingWrongWithIt()
    {
        // Act
        var broken = Invariants.FindWhatACatalogDidNotName(new NothingShapes(), serializer, assemblies);

        // Assert
        Assert.Empty(broken);
    }
}

/// <summary>What a code of a consignment is, there being more than one kind of code on a parcel.</summary>
public interface ICode;

/// <summary>What a parcel is followed by.</summary>
public readonly record struct TrackingCode(string Value) : ICode;

/// <summary>What the round that delivers it is called.</summary>
public readonly record struct DeliveryCode(string Value) : ICode;

/// <summary>
/// Every code of this wire travels as the text it holds, named one by one from the codes the assemblies declare.
/// </summary>
public sealed class CodeShapes : IShapeCatalog
{
    private static readonly Shape code = new Shape.AsText();

    public Shape? GetShape(Type type, ISerializer serializer) => IsCode(type) ? code : null;

    /// <summary>
    /// Named by the same question it answers by, so that a code that appears is named without anybody adding a
    /// line: a list written out by hand is a list that stops being true.
    /// </summary>
    public IReadOnlyDictionary<Type, Shape> GetDeclarations(ISerializer serializer) =>
        typeof(CodeShapes).Assembly.GetTypes()
            .Where(IsCode)
            .ToDictionary(type => type, _ => code);

    internal static bool IsCode(Type type) => type is {IsInterface: false} && typeof(ICode).IsAssignableFrom(type);
}

/// <summary>
/// The same, written the way a catalog comes to contradict itself: it answers by what a type is and names by a
/// list somebody wrote down, and the day a second code appeared only one of the two learned about it.
/// </summary>
public sealed class HalfNamedCodeShapes : IShapeCatalog
{
    private static readonly Shape code = new Shape.AsText();

    public Shape? GetShape(Type type, ISerializer serializer) => CodeShapes.IsCode(type) ? code : null;

    public IReadOnlyDictionary<Type, Shape> GetDeclarations(ISerializer serializer) =>
        new Dictionary<Type, Shape> {[typeof(TrackingCode)] = code};
}

/// <summary>A catalog of an application that declares none of its own, which is a list of nothing and no lie.</summary>
public sealed class NothingShapes : IShapeCatalog
{
    public Shape? GetShape(Type type, ISerializer serializer) => null;

    public IReadOnlyDictionary<Type, Shape> GetDeclarations(ISerializer serializer) =>
        new Dictionary<Type, Shape>();
}
