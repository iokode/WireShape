using System;
using Xunit;

namespace IOKode.WireShape.Tests;

/// <summary>
/// What each form carries, and what every form answers however it was written.
/// </summary>
public class ShapeTests
{
    [Fact]
    public void AValueDeclaredAsAText_ShouldSayWhichTextsAndTravelAsAString()
    {
        // Act
        var shape = new Shape.AsText(Pattern: "^[A-Z]{3}$");

        // Assert
        Assert.Equal("^[A-Z]{3}$", shape.Pattern);
        Assert.Null(shape.Format);
        Assert.Equal(Carrier.String, shape.Carrier);
    }

    [Fact]
    public void AKindOfTextWithASettledName_ShouldSayTheName()
    {
        // Act
        var shape = new Shape.AsText(Format: "email");

        // Assert
        Assert.Equal("email", shape.Format);
        Assert.Null(shape.Pattern);
        Assert.Equal(Carrier.String with {Format = "email"}, shape.Carrier);
    }

    [Fact]
    public void AValueDeclaredAsAValue_ShouldTravelAsThatValue()
    {
        // Act
        var shape = new Shape.As(Carrier.Decimal with {Format = "money-2"});

        // Assert
        Assert.Equal(typeof(decimal), shape.Carrier!.Type);
        Assert.Equal(ValueKind.Decimal, shape.Carrier.Kind);
        Assert.Equal("money-2", shape.Carrier.Format);
    }

    /// <summary>
    /// What travels is whatever that other type turns out to be, so there is no value here to answer about.
    /// </summary>
    [Fact]
    public void AValueDeclaredAsAnotherTypeOfYours_ShouldCarryNoValueOfItsOwn()
    {
        // Act
        var shape = new Shape.AsAnother(typeof(ShapeTests));

        // Assert
        Assert.Equal(typeof(ShapeTests), shape.Type);
        Assert.Null(shape.Carrier);
    }

    [Fact]
    public void AUnionOfFewerThanTwoForms_ShouldBeRefused()
    {
        // Act
        var declaring = () => new Shape.OneOf(new[] {new Alternative("national", new Shape.AsText())});

        // Assert
        Assert.Throws<ArgumentException>(declaring);
    }

    /// <summary>
    /// A wrapper that disappears cannot be one of several, there being nothing of it to write as one of them.
    /// </summary>
    [Fact]
    public void AUnionOfAFormThatCannotBeWrittenAsOneOfSeveral_ShouldBeRefused()
    {
        // Act
        var declaring = () => new Shape.OneOf(new[]
        {
            new Alternative("national", new Shape.AsText()),
            new Alternative("wrapped", new Shape.Unwraps(0))
        });

        // Assert
        Assert.Throws<ArgumentException>(declaring);
    }

    /// <summary>
    /// Whoever reads them is offered them by name, so two under one name are two nobody could tell apart.
    /// </summary>
    [Fact]
    public void TwoAlternativesUnderOneName_ShouldBeRefused()
    {
        // Act
        var declaring = () => new Shape.OneOf(new[]
        {
            new Alternative("national", new Shape.AsText()),
            new Alternative("national", new Shape.As(Carrier.Decimal))
        });

        // Assert
        Assert.Throws<ArgumentException>(declaring);
    }

    [Fact]
    public void AnAlternativeUnderNoName_ShouldBeRefused()
    {
        // Act
        var declaring = () => new Shape.OneOf(new[]
        {
            new Alternative(" ", new Shape.AsText()),
            new Alternative("international", new Shape.As(Carrier.Decimal))
        });

        // Assert
        Assert.Throws<ArgumentException>(declaring);
    }

    [Fact]
    public void AUnion_ShouldCarryNoValueOfItsOwn()
    {
        // Act
        var union = new Shape.OneOf(BuildAlternatives());

        // Assert
        Assert.Null(union.Carrier);
    }

    /// <summary>
    /// By the alternatives and not by the list that holds them, so that two parts of a wire declaring the same
    /// union are saying it once.
    /// </summary>
    [Fact]
    public void TwoUnionsOfTheSameForms_ShouldBeTheOneUnion()
    {
        // Act
        var union = new Shape.OneOf(BuildAlternatives());
        var again = new Shape.OneOf(BuildAlternatives());

        // Assert
        Assert.Equal(union, again);
        Assert.Equal(union.GetHashCode(), again.GetHashCode());
    }

    /// <summary>
    /// A text is a form of its own and not one of these written a certain way, which is what keeps a pattern
    /// out of everything that is not a text.
    /// </summary>
    [Fact]
    public void AText_ShouldNotBeTheFormThatTravelsAsAnotherType()
    {
        // Assert
        Assert.IsNotType<Shape.As>(new Shape.AsText());
    }

    [Theory]
    [InlineData(typeof(Shape.AsFile))]
    public void WhatDoesNotTravelWholeAsAnotherType_ShouldCarryNothing(Type form)
    {
        // Act
        var shape = (Shape) Activator.CreateInstance(form)!;

        // Assert
        Assert.Null(shape.Carrier);
    }

    [Fact]
    public void AWrapperThatDisappears_ShouldCarryNothingOfItsOwn()
    {
        // Assert
        Assert.Null(new Shape.Unwraps(0).Carrier);
    }

    private static Alternative[] BuildAlternatives() => new[]
    {
        new Alternative("national", new Shape.AsText()),
        new Alternative("international", new Shape.As(Carrier.Decimal))
    };
}
