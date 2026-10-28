using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using IOKode.WireShape.SystemTextJson;
using Xunit;

namespace IOKode.WireShape.Tests;

/// <summary>
/// What a wire answers about the form of a type, and in what order: what was declared of that type one by one,
/// then what a source answers, the first one added before the rest. And who declared it, where anybody did.
/// </summary>
public class WireTests
{
    private static readonly ISerializer serializer = new JsonSerializerOptionsSerializer(
        new JsonSerializerOptions {TypeInfoResolver = new DefaultJsonTypeInfoResolver()});

    [Fact]
    public void WhatWasDeclaredOfAType_ShouldBeAnswered()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add(typeof(Reference), new Shape.AsText(Pattern: "^[A-Z]{3}$"));

        // Act
        var shape = declaring.Build(serializer).GetShape(typeof(Reference));

        // Assert
        var declared = Assert.IsType<Shape.AsText>(shape);
        Assert.Equal("^[A-Z]{3}$", declared.Pattern);
    }

    [Fact]
    public void WhatWasDeclaredOfATypeNamedAsAnArgument_ShouldBeTheSameDeclaration()
    {
        // Arrange
        var written = new Shape.AsText(Pattern: "^[A-Z]{3}$");
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add<Reference>(written);

        // Act
        var shape = declaring.Build(serializer).GetShape(typeof(Reference));

        // Assert
        Assert.Same(written, shape);
    }

    [Fact]
    public void WhatWasDeclaredOfAType_ShouldBeAnsweredBeforeWhatASourceReads()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddShapeSource(Reads(typeof(Reference), Carrier.Int64));
        declarer.Add(typeof(Reference), new Shape.As(Carrier.String));

        // Act
        var shape = declaring.Build(serializer).GetShape(typeof(Reference));

        // Assert
        Assert.Equal(typeof(string), Assert.IsType<Shape.As>(shape).By.Type);
    }

    [Fact]
    public void ASecondFormDeclaredOfOneType_ShouldBeRefused()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add<Reference>(new Shape.As(Carrier.Int64));

        // Act
        var declaringAnother = () => declarer.Add<Reference>(new Shape.AsText());

        // Assert
        var refused = Assert.Throws<InvalidOperationException>(declaringAnother);
        Assert.Contains(typeof(Reference).FullName!, refused.Message);
    }

    /// <summary>
    /// Two parts of a wire saying the same thing is a coincidence and not a conflict, so nothing is refused
    /// and the form stays what both of them said.
    /// </summary>
    [Fact]
    public void TheSameFormDeclaredTwice_ShouldBeLetThrough()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add<Reference>(new Shape.As(Carrier.Int64));
        declarer.Add<Reference>(new Shape.As(Carrier.Int64));

        // Act
        var shape = declaring.Build(serializer).GetShape(typeof(Reference));

        // Assert
        Assert.Equal(typeof(long), Assert.IsType<Shape.As>(shape).By.Type);
    }

    [Fact]
    public void WhatAConventionRecognises_ShouldBeAnswered()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddShapeSource(Recognises(typeof(Reference), new Shape.Unwraps(0)));

        // Act
        var shape = declaring.Build(serializer).GetShape(typeof(Reference));

        // Assert
        Assert.IsType<Shape.Unwraps>(shape);
    }

    [Fact]
    public void WhatASourceReads_ShouldBeAnsweredWhereNobodyDeclaredIt()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddShapeSource(Reads(typeof(Reference), Carrier.Int64));

        // Act
        var shape = declaring.Build(serializer).GetShape(typeof(Reference));

        // Assert
        Assert.Equal(typeof(long), Assert.IsType<Shape.As>(shape).By.Type);
    }

    [Fact]
    public void WhereTwoSourcesCouldAnswer_ShouldBeAnsweredByTheOneTakenInFirst()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddShapeSource(Reads(typeof(Reference), Carrier.Int64));
        declarer.AddShapeSource(Reads(typeof(Reference), Carrier.String));

        // Act
        var shape = declaring.Build(serializer).GetShape(typeof(Reference));

        // Assert
        Assert.Equal(typeof(long), Assert.IsType<Shape.As>(shape).By.Type);
    }

    [Fact]
    public void WhatNobodyDeclaredAndNoSourceReads_ShouldBeAnsweredWithNothing()
    {
        // Arrange
        var wire = new WireBuilder().Build(serializer);

        // Act
        var shape = wire.GetShape(typeof(Reference));

        // Assert
        Assert.Null(shape);
    }

    [Fact]
    public void WhatAConventionRecognises_ShouldNotBeNamedAmongTheDeclarations()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddShapeSource(Recognises(typeof(Reference), new Shape.Unwraps(0)));
        declarer.Add(typeof(Count), new Shape.As(Carrier.Int32));

        // Act
        var declared = declaring.Build(serializer).GetDeclaredShapes();

        // Assert
        Assert.Contains(typeof(Count), declared.Keys);
        Assert.DoesNotContain(typeof(Reference), declared.Keys);
    }

    [Fact]
    public void WhatWasDeclaredOfAType_ShouldBeNamedAmongTheDeclarationsBeforeWhatACatalogReads()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddShapeSource(Reads(typeof(Reference), Carrier.Int64));
        declarer.Add(typeof(Reference), new Shape.As(Carrier.String));

        // Act
        var declared = declaring.Build(serializer).GetDeclaredShapes();

        // Assert
        Assert.Equal(typeof(string), Assert.IsType<Shape.As>(declared[typeof(Reference)].Shape).By.Type);
    }

    [Fact]
    public void WhatADeclarerDeclaredOfAType_ShouldBeNamedAfterIt()
    {
        // Arrange
        var declaring = new WireBuilder();
        declaring.GetDeclarer("dispatch").Add(typeof(Reference), new Shape.As(Carrier.String));

        // Act
        var wire = declaring.Build(serializer);

        // Assert
        Assert.Equal("dispatch", wire.GetDeclarerOf(typeof(Reference)));
        Assert.Equal("dispatch", wire.GetDeclaredShapes()[typeof(Reference)].Declarer);
    }

    [Fact]
    public void WhatACatalogNames_ShouldBeNamedAfterWhoeverAddedIt()
    {
        // Arrange
        var declaring = new WireBuilder();
        declaring.GetDeclarer("tracking").AddShapeSource(Reads(typeof(Reference), Carrier.Int64));

        // Act
        var wire = declaring.Build(serializer);

        // Assert
        Assert.Equal("tracking", wire.GetDeclarerOf(typeof(Reference)));
    }

    /// <summary>
    /// A convention declares the form of a family it did not name, so naming its types after whoever wrote it
    /// would say that the author of a convention declared somebody else's type.
    /// </summary>
    [Fact]
    public void WhatOnlyAConventionRecognises_ShouldHaveNoDeclarer()
    {
        // Arrange
        var declaring = new WireBuilder();
        declaring.GetDeclarer("dispatch").AddShapeSource(Recognises(typeof(Reference), new Shape.Unwraps(0)));

        // Act
        var wire = declaring.Build(serializer);

        // Assert
        Assert.NotNull(wire.GetShape(typeof(Reference)));
        Assert.Null(wire.GetDeclarerOf(typeof(Reference)));
    }

    [Fact]
    public void WhatNobodyDeclared_ShouldHaveNoDeclarer()
    {
        // Arrange
        var declaring = new WireBuilder();

        // Act
        var wire = declaring.Build(serializer);

        // Assert
        Assert.Null(wire.GetDeclarerOf(typeof(Reference)));
    }

    [Fact]
    public void TwoDeclarersDisagreeingAboutAType_ShouldBeRefusedNamingBoth()
    {
        // Arrange
        var declaring = new WireBuilder();
        declaring.GetDeclarer("dispatch").Add(typeof(Reference), new Shape.As(Carrier.String));

        // Act
        var refused = Assert.Throws<InvalidOperationException>(() =>
            declaring.GetDeclarer("tracking").Add(typeof(Reference), new Shape.As(Carrier.Int64)));

        // Assert
        Assert.Contains("dispatch", refused.Message);
        Assert.Contains("tracking", refused.Message);
    }

    /// <summary>
    /// Saying the same form again is agreeing with it, and whoever agrees did not declare it: taking the name
    /// of the last to speak would make it depend on the order the parts of a wire were composed in.
    /// </summary>
    [Fact]
    public void TwoDeclarersAgreeingAboutAType_ShouldKeepTheNameOfTheFirst()
    {
        // Arrange
        var declaring = new WireBuilder();
        declaring.GetDeclarer("dispatch").Add(typeof(Reference), new Shape.As(Carrier.String));
        declaring.GetDeclarer("tracking").Add(typeof(Reference), new Shape.As(Carrier.String));

        // Act
        var wire = declaring.Build(serializer);

        // Assert
        Assert.Equal("dispatch", wire.GetDeclarerOf(typeof(Reference)));
    }

    [Fact]
    public void TwoDeclarersDisagreeingAboutAKind_ShouldBeRefusedNamingBoth()
    {
        // Arrange
        var declaring = new WireBuilder();
        declaring.GetDeclarer("dispatch").AddKind<Parcel, Envelope>("envelope");

        // Act
        var refused = Assert.Throws<InvalidOperationException>(() =>
            declaring.GetDeclarer("tracking").AddKind<Parcel, Envelope>("packet"));

        // Assert
        Assert.Contains("dispatch", refused.Message);
        Assert.Contains("tracking", refused.Message);
    }

    /// <summary>
    /// A declaration of the members wins over what whoever writes would answer, which is what it is for: a type
    /// written by a converter that builds the object itself.
    /// </summary>
    [Fact]
    public void TheMembersDeclaredOfAType_ShouldBeAnsweredBeforeWhatWhoeverWritesSays()
    {
        // Arrange
        var declared = new[] {new Member("code", typeof(string), IsOptional: false)};
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add<Count>(new Shape.Members(declared));

        // Act
        var members = declaring.Build(serializer).GetMembers(typeof(Count));

        // Assert
        Assert.Same(declared, members);
    }

    [Fact]
    public void TheMembersOfATypeNobodyCanAnswerAbout_ShouldBeAnsweredWithNothingAtAll()
    {
        // Arrange
        var wire = new WireBuilder().Build(serializer);

        // Act
        var members = wire.GetMembers(typeof(string));

        // Assert
        Assert.Null(members);
    }

    /// <summary>
    /// The same members said twice is the same form said twice, which is what the refusal lets through. Said of
    /// the two forms that carry a list of their own, because a list is not compared by what it holds unless the
    /// form says so.
    /// </summary>
    [Fact]
    public void TheSameMembersDeclaredTwice_ShouldBeLetThrough()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add<Count>(new Shape.Members(new[] {new Member("code", typeof(string), IsOptional: false)}));

        // Act
        declarer.Add<Count>(new Shape.Members(new[] {new Member("code", typeof(string), IsOptional: false)}));

        // Assert
        var members = declaring.Build(serializer).GetMembers(typeof(Count))!;
        Assert.Equal("code", Assert.Single(members).Name);
    }

    [Fact]
    public void OtherMembersDeclaredOfOneType_ShouldBeRefused()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add<Count>(new Shape.Members(new[] {new Member("code", typeof(string), IsOptional: false)}));

        // Act
        var declaringAnother = () => declarer.Add<Count>(
            new Shape.Members(new[] {new Member("reference", typeof(string), IsOptional: false)}));

        // Assert
        Assert.Throws<InvalidOperationException>(declaringAnother);
    }

    [Fact]
    public void AKindDeclaredOfABase_ShouldBeAnswered()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddKind<Parcel, Envelope>("envelope");

        // Act
        var kinds = declaring.Build(serializer).GetKinds(typeof(Parcel));

        // Assert
        var kind = Assert.Single(kinds);
        Assert.Equal(typeof(Envelope), kind.Type);
        Assert.Equal("envelope", kind.Discriminator);
    }

    [Fact]
    public void ASecondWordGivenToOneKind_ShouldBeRefused()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddKind<Parcel, Envelope>("envelope");

        // Act
        var declaringAnother = () => declarer.AddKind<Parcel, Envelope>("env");

        // Assert
        var refused = Assert.Throws<InvalidOperationException>(declaringAnother);
        Assert.Contains("envelope", refused.Message);
    }

    /// <summary>
    /// One word cannot tell two kinds of one base apart: whoever reads it would not know which of the two
    /// arrived.
    /// </summary>
    [Fact]
    public void OneWordGivenToTwoKindsOfOneBase_ShouldBeRefused()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddKind<Parcel, Envelope>("box");

        // Act
        var declaringAnother = () => declarer.AddKind<Parcel, Pallet>("box");

        // Assert
        var refused = Assert.Throws<InvalidOperationException>(declaringAnother);
        Assert.Contains(typeof(Pallet).FullName!, refused.Message);
    }

    [Fact]
    public void TheSameKindDeclaredTwice_ShouldBeLetThrough()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddKind<Parcel, Envelope>("envelope");
        declarer.AddKind<Parcel, Envelope>("envelope");

        // Act
        var kinds = declaring.Build(serializer).GetKinds(typeof(Parcel));

        // Assert
        Assert.Equal("envelope", Assert.Single(kinds).Discriminator);
    }

    [Fact]
    public void OneWordTellingKindsOfTwoBasesApart_ShouldBeLetThrough()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddKind(new HierarchyKind(typeof(Parcel), typeof(Envelope), "flat"));
        declarer.AddKind(new HierarchyKind(typeof(Shipment), typeof(Express), "flat"));

        // Act
        var kinds = declaring.Build(serializer).GetKinds(typeof(Shipment));

        // Assert
        Assert.Contains(kinds, kind => kind.Type == typeof(Express) && kind.Discriminator == "flat");
    }

    [Fact]
    public void ABaseDeclaredAmongItsOwnKinds_ShouldNotBeOneOfThem()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddKind(new HierarchyKind(typeof(Parcel), typeof(Parcel), "parcel"));
        declarer.AddKind<Parcel, Envelope>("envelope");

        // Act
        var kinds = declaring.Build(serializer).GetKinds(typeof(Parcel));

        // Assert
        Assert.Equal(typeof(Envelope), Assert.Single(kinds).Type);
    }

    [Fact]
    public void TheKindsWhoeverWritesKnows_ShouldBeAnswered()
    {
        // Arrange
        var wire = new WireBuilder().Build(serializer);

        // Act
        var kinds = wire.GetKinds(typeof(Shipment));

        // Assert
        Assert.Equal(
            new[] {"express", "overnight"},
            kinds.Select(kind => kind.Discriminator).Order());
    }

    /// <summary>
    /// Declaring a kind is how the word whoever writes would give it is corrected, so the declaration wins and
    /// the other kinds of that base are left as they were.
    /// </summary>
    [Fact]
    public void AKindDeclaredOverWhatWhoeverWritesKnows_ShouldWin()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddKind<Shipment, Express>("express-delivery");

        // Act
        var kinds = declaring.Build(serializer).GetKinds(typeof(Shipment));

        // Assert
        Assert.Equal(
            new[] {"express-delivery", "overnight"},
            kinds.Select(kind => kind.Discriminator).Order());
    }

    [Fact]
    public void TheKindsASourceReads_ShouldBeAnswered()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddKindSource(new Parcels());

        // Act
        var kinds = declaring.Build(serializer).GetKinds(typeof(Parcel));

        // Assert
        Assert.Equal(new[] {"envelope", "pallet"}, kinds.Select(kind => kind.Discriminator).Order());
    }

    [Fact]
    public void AKindDeclaredOrNamedByASource_ShouldBeAmongTheDeclaredKinds()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddKindSource(new Parcels());
        declarer.AddKind<Shipment, Express>("express");

        // Act
        var declared = declaring.Build(serializer).GetDeclaredKinds();

        // Assert
        Assert.Equal(
            new[] {typeof(Envelope), typeof(Express), typeof(Pallet)},
            declared.Select(kind => kind.Type).OrderBy(type => type.Name));
    }

    /// <summary>
    /// What only whoever writes knows is not among them: it answers about a type that is named to it and cannot
    /// be asked to name them all, which is why a hierarchy nothing mentions has to be declared.
    /// </summary>
    [Fact]
    public void AKindOnlyWhoeverWritesKnows_ShouldNotBeAmongTheDeclaredKinds()
    {
        // Arrange
        var wire = new WireBuilder().Build(serializer);

        // Act
        var declared = wire.GetDeclaredKinds();

        // Assert
        Assert.Empty(declared);
    }

    [Fact]
    public void TheWordDeclaredOfAValue_ShouldBeWhatItComesOutAs()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddWord(CarrierService.NextDay, "next-day");

        // Act
        var written = declaring.Build(serializer).GetValueForm(typeof(CarrierService), CarrierService.NextDay);

        // Assert
        Assert.Equal("next-day", Assert.IsType<EnumerationValueForm.Word>(written).Text);
    }

    /// <summary>
    /// A word is of one value, so declaring it leaves the other values of that enumeration as whoever writes
    /// answers them.
    /// </summary>
    [Fact]
    public void TheOtherValuesOfADeclaredEnumeration_ShouldComeOutAsWhoeverWritesSays()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddWord(CarrierService.NextDay, "next-day");

        // Act
        var written = declaring.Build(serializer).GetValueForm(typeof(CarrierService), CarrierService.Economy);

        // Assert
        Assert.Equal("2", Assert.IsType<EnumerationValueForm.Integer>(written).Text);
    }

    [Fact]
    public void ASecondWordGivenToOneValue_ShouldBeRefused()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddWord(CarrierService.NextDay, "next-day");

        // Act
        var declaringAnother = () => declarer.AddWord(CarrierService.NextDay, "nextDay");

        // Assert
        var refused = Assert.Throws<InvalidOperationException>(declaringAnother);
        Assert.Contains("next-day", refused.Message);
    }

    [Fact]
    public void OneWordGivenToTwoValuesOfOneEnumeration_ShouldBeRefused()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddWord(CarrierService.NextDay, "quick");

        // Act
        var declaringAnother = () => declarer.AddWord(CarrierService.Economy, "quick");

        // Assert
        var refused = Assert.Throws<InvalidOperationException>(declaringAnother);
        Assert.Contains("quick", refused.Message);
    }

    [Fact]
    public void OneWordTellingValuesOfTwoEnumerationsApart_ShouldBeLetThrough()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddWord(CarrierService.NextDay, "quick");
        declarer.AddWord(ParcelSize.Small, "quick");

        // Act
        var written = declaring.Build(serializer).GetValueForm(typeof(ParcelSize), ParcelSize.Small);

        // Assert
        Assert.Equal("quick", Assert.IsType<EnumerationValueForm.Word>(written).Text);
    }

    [Fact]
    public void TheWordASourceReads_ShouldBeWhatAValueComesOutAs()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddWordSource(new LowerCasedWords());

        // Act
        var written = declaring.Build(serializer).GetValueForm(typeof(CarrierService), CarrierService.NextDay);

        // Assert
        Assert.Equal("nextday", Assert.IsType<EnumerationValueForm.Word>(written).Text);
    }

    [Fact]
    public void AnEnumerationDeclaredToTravelAsAText_ShouldTravelByName()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add<ParcelSize>(new Shape.AsText());

        // Act
        var byName = declaring.Build(serializer).TravelsByName(typeof(ParcelSize));

        // Assert
        Assert.True(byName);
    }

    /// <summary>
    /// Whether an enumeration travels as words is asked of every one of its values, because one that answers
    /// with a word and a number is one no document can describe.
    /// </summary>
    [Fact]
    public void AnEnumerationNotAllOfWhoseValuesComeOutAsWords_ShouldNotTravelByName()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.AddWord(CarrierService.NextDay, "next-day");

        // Act
        var byName = declaring.Build(serializer).TravelsByName(typeof(CarrierService));

        // Assert
        Assert.False(byName);
    }

    /// <summary>
    /// Either the words travel or the numbers do, so an enumeration declared to travel as a number and given
    /// words is a wire nobody could write.
    /// </summary>
    [Fact]
    public void WordsDeclaredOfAnEnumerationThatTravelsAsANumber_ShouldBeRefused()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add<CarrierService>(new Shape.As(Carrier.Int32));
        declarer.AddWord(CarrierService.NextDay, "next-day");

        // Act
        var building = () => declaring.Build(serializer);

        // Assert
        var refused = Assert.Throws<InvalidOperationException>(building);
        Assert.Contains("next-day", refused.Message);
    }

    /// <summary>
    /// What a wire says cannot change once it is built, which is the whole of why it is built instead of
    /// declared on.
    /// </summary>
    [Fact]
    public void WhatIsDeclaredAfterAWireIsBuilt_ShouldLeaveThatWireAsItWas()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add<Reference>(new Shape.As(Carrier.String));

        var wire = declaring.Build(serializer);

        // Act
        declarer.Add<Count>(new Shape.As(Carrier.Int32));
        declarer.AddKind<Parcel, Envelope>("envelope");

        // Assert
        Assert.Null(wire.GetShape(typeof(Count)));
        Assert.DoesNotContain(typeof(Count), wire.GetDeclaredShapes().Keys);
        Assert.Empty(wire.GetKinds(typeof(Parcel)));
        Assert.Empty(wire.GetDeclaredKinds());
    }

    [Fact]
    public void OneDeclarationBuiltTwice_ShouldMakeTwoWiresThatKnowTheSameThings()
    {
        // Arrange
        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add<Reference>(new Shape.As(Carrier.String));

        // Act
        var one = declaring.Build(serializer);
        var other = declaring.Build(serializer);

        // Assert
        Assert.NotSame(one, other);
        Assert.Equal(one.GetShape(typeof(Reference)), other.GetShape(typeof(Reference)));
    }

    private static IShapeSource Reads(Type type, Carrier carrier) => new Source(type, carrier);

    /// <summary>A convention: it recognises one type and has no list it could be read through.</summary>
    private static IShapeSource Recognises(Type type, Shape shape) => new Convention(type, shape);

    /// <summary>What a wire whose words are the names of the members, cut down, answers of each value.</summary>
    private sealed class LowerCasedWords : IEnumerationSource
    {
        public string? GetWord(Type enumType, object value, ISerializer serializer) =>
            Enum.GetName(enumType, value)?.ToLowerInvariant();
    }

    private sealed class Source(Type type, Carrier carrier) : IShapeCatalog
    {
        public Shape? GetShape(Type asked, ISerializer serializer) =>
            asked == type ? new Shape.As(carrier) : null;

        public IReadOnlyDictionary<Type, Shape> GetDeclarations(ISerializer serializer) =>
            new Dictionary<Type, Shape> {[type] = new Shape.As(carrier)};
    }

    private sealed class Convention(Type type, Shape shape) : IShapeSource
    {
        public Shape? GetShape(Type asked, ISerializer serializer) => asked == type ? shape : null;
    }

    private readonly record struct Reference(string Value);

    private readonly record struct Count(int Value);
}
