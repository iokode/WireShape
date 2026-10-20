using Xunit;

namespace IOKode.WireShape.CSharpClient.Tests;

/// <summary>
/// What is written alongside a client so that it carries nothing but itself.
/// </summary>
public class ApiCallSourceTests
{
    /// <summary>
    /// A client that looked for the member that tells one kind from another under another name would read every
    /// answer of a polymorphic type as unreadable, so the one the model carries is filled in.
    /// </summary>
    [Fact]
    public void TheMemberThatTellsOneKindFromAnother_ShouldBeTheOneTheModelCarries()
    {
        // Act
        var written = ApiCallSource.Write("sort");

        // Assert
        Assert.Contains("TypeDiscriminatorPropertyName = \"sort\"", written);
        Assert.DoesNotContain("<discriminator>", written);
    }

    [Fact]
    public void WhatEveryCallNeeds_ShouldBeWrittenAlongsideIt()
    {
        // Act
        var written = ApiCallSource.Write("kind");

        // Assert
        Assert.Contains("public interface IApiValue", written);
        Assert.Contains("public sealed record ApiFile(", written);
        Assert.Contains("public static class ApiCall", written);
        Assert.Contains("public sealed class ApiException(", written);
    }
}
