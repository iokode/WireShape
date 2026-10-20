using System;
using Xunit;

namespace IOKode.WireShape.Tests;

/// <summary>
/// What a value written whole looks like, and which of them is worth keeping the name of what travels as it.
/// </summary>
public class CarrierTests
{
    public static TheoryData<Carrier> Known => new(Carrier.Known);

    [Theory]
    [MemberData(nameof(Known))]
    public void EveryCarrierTheLibraryKnows_ShouldBeAnsweredForItsType(Carrier carrier)
    {
        // Assert
        Assert.Equal(carrier, Carrier.Of(carrier.Type));
    }

    [Fact]
    public void ATypeWithMembersOfItsOwn_ShouldBeNobodysCarrier()
    {
        // Assert
        Assert.Null(Carrier.Of(typeof(CarrierTests)));
    }

    public static TheoryData<Carrier> MomentsInTime => new(
        Carrier.DateOnly, Carrier.TimeOnly, Carrier.DateTime, Carrier.DateTimeOffset, Carrier.TimeSpan);

    [Theory]
    [MemberData(nameof(MomentsInTime))]
    public void AMomentInTime_ShouldExplainItselfAndKeepNoNameOfOurs(Carrier carrier)
    {
        // Assert
        Assert.True(carrier.IsSelfExplanatory);
        Assert.False(carrier.KeepsItsName);
        Assert.Equal(ValueKind.Text, carrier.Kind);
    }

    public static TheoryData<Carrier> Values => new(
        Carrier.String, Carrier.Guid, Carrier.Decimal, Carrier.Int64, Carrier.Boolean);

    [Theory]
    [MemberData(nameof(Values))]
    public void AValueThatIsAnyValueOfItsKind_ShouldKeepTheNameOfWhatTravelsAsIt(Carrier carrier)
    {
        // Assert
        Assert.False(carrier.IsSelfExplanatory);
        Assert.True(carrier.KeepsItsName);
    }

    /// <summary>
    /// The three ways of holding a number with a fraction are one kind, because written they are the same
    /// number. Which of them a value is read back into is what the carrier's own type says.
    /// </summary>
    [Fact]
    public void EveryWayOfHoldingADecimalNumber_ShouldBeTheOneKind()
    {
        // Assert
        Assert.Equal(ValueKind.Decimal, Carrier.Single.Kind);
        Assert.Equal(ValueKind.Decimal, Carrier.Double.Kind);
        Assert.Equal(ValueKind.Decimal, Carrier.Decimal.Kind);
        Assert.Equal(new[] {typeof(float), typeof(double), typeof(decimal)}, new[]
        {
            Carrier.Single.Type, Carrier.Double.Type, Carrier.Decimal.Type
        });
    }

    /// <summary>
    /// A wire whose moments in time are not the framework's declares a carrier of its own, and it is described
    /// as a moment in time and not as a text with a name of ours over it.
    /// </summary>
    [Fact]
    public void ACarrierOfYourOwnTheReaderHasAKindFor_ShouldKeepNoNameOfOurs()
    {
        // Act
        var instant = Carrier.String with {Format = "date-time", IsSelfExplanatory = true};

        // Assert
        Assert.Equal(ValueKind.Text, instant.Kind);
        Assert.Equal(typeof(string), instant.Type);
        Assert.False(instant.KeepsItsName);
    }
}
