using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IOKode.WireShape.AspNetCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace IOKode.WireShape.CSharpClient.Tests;

/// <summary>
/// That what is written is C# a compiler accepts, which is the only thing the written text being right finally
/// means.
/// </summary>
public class ClientSourceTests
{
    /// <summary>
    /// Every kind of thing that travels at once — a value with a name and one without, an object, a hierarchy,
    /// both kinds of enumeration, a union, a file, a download, a form and a query — because what breaks a
    /// written file is usually one of them beside another.
    /// </summary>
    [Fact]
    public void WhatIsWritten_ShouldBeCSharpThatCompiles()
    {
        // Act
        var source = ClientSource.Write(Shipments.Of(BuildCalls()), "Shipping.Client", Shipments.NameOf);

        // Assert
        Assert.Empty(SaidAbout(source, DiagnosticSeverity.Error));
    }

    /// <summary>
    /// Whoever consumes a client builds it with every warning an error, so a written file that only compiles is
    /// not enough: it has to compile with nothing said about it.
    /// </summary>
    [Fact]
    public void WhatIsWritten_ShouldCompileWithNothingSaidAboutIt()
    {
        // Act
        var source = ClientSource.Write(Shipments.Of(BuildCalls()), "Shipping.Client", Shipments.NameOf);

        // Assert
        Assert.Empty(SaidAbout(source, DiagnosticSeverity.Warning));
    }

    /// <summary>
    /// The canary: an answer of nothing from the compiler passing for a clean one is how a test like this comes
    /// to prove nothing at all.
    /// </summary>
    [Fact]
    public void TheCompilerUsedHere_ShouldSayWhereCSharpIsWrong()
    {
        // Assert
        Assert.NotEmpty(SaidAbout(
            "namespace Shipping; public sealed class Broken { public int Wrong => \"a text\"; }",
            DiagnosticSeverity.Error));
    }

    /// <summary>
    /// The types another client declares are read from where that client put them, and are not written again.
    /// </summary>
    [Fact]
    public void WhatAnotherClientDeclares_ShouldBeReadFromThereAndCompileAllTheSame()
    {
        // Arrange
        var reused = new Dictionary<Type, string> {[typeof(Consignment)] = "Consignment"};

        // Act
        var source = ClientSource.Write(
            Shipments.Of(BuildCalls()),
            "Shipping.Client",
            Shipments.NameOf,
            reused,
            reference: "Shipping.Core");

        // Assert
        Assert.Contains("using Shipping.Core;", source);
        Assert.DoesNotContain("public sealed partial record Consignment", source);
    }

    private static ApiAction[] BuildCalls() => new[]
    {
        new ApiAction
        {
            Method = "GET",
            Route = "/consignments/{code}",
            Controller = "ConsignmentsResourceController",
            MethodName = "GetAsync",
            Parameters = new[]
            {
                new ApiParameter("code", typeof(TrackingCode), ApiParameterOrigin.Route, IsOptional: false),
                new ApiParameter("service", typeof(CarrierService), ApiParameterOrigin.Query, IsOptional: true)
            },
            Result = typeof(Consignment)
        },
        new ApiAction
        {
            Method = "POST",
            Route = "/consignments",
            Controller = "ConsignmentsResourceController",
            MethodName = "CreateAsync",
            Parameters = new[]
            {
                new ApiParameter("consignment", typeof(Consignment), ApiParameterOrigin.Body, IsOptional: false)
            },
            Result = typeof(TrackingCode)
        },
        new ApiAction
        {
            Method = "POST",
            Route = "/consignments/{code}/attachments",
            Controller = "ConsignmentsResourceController",
            MethodName = "AttachAsync",
            Parameters = new[]
            {
                new ApiParameter("code", typeof(TrackingCode), ApiParameterOrigin.Route, IsOptional: false),
                new ApiParameter("file", typeof(Attachment), ApiParameterOrigin.Form, IsOptional: false),
                new ApiParameter("replace", typeof(bool), ApiParameterOrigin.Form, IsOptional: false)
            },
            Result = null
        },
        new ApiAction
        {
            Method = "GET",
            Route = "/consignments/{code}/label",
            Controller = "ConsignmentsResourceController",
            MethodName = "DownloadLabelAsync",
            Parameters = new[]
            {
                new ApiParameter("code", typeof(TrackingCode), ApiParameterOrigin.Route, IsOptional: false)
            },
            IsDownload = true
        },
        new ApiAction
        {
            Method = "GET",
            Route = "/parcels",
            Controller = "ParcelsController",
            MethodName = "ListAsync",
            Parameters = new[]
            {
                new ApiParameter("size", typeof(ParcelSize), ApiParameterOrigin.Query, IsOptional: true),
                new ApiParameter("filter", typeof(PhoneParts), ApiParameterOrigin.Query, IsOptional: true)
            },
            Result = typeof(IReadOnlyList<Parcel>)
        },
        new ApiAction
        {
            Method = "GET",
            Route = "/customers/{code}/phone",
            Controller = "CustomersController",
            MethodName = "GetPhoneAsync",
            Parameters = new[]
            {
                new ApiParameter("code", typeof(TrackingCode), ApiParameterOrigin.Route, IsOptional: false)
            },
            Result = typeof(Phone)
        }
    };

    /// <summary>
    /// Compiled against the framework the tests themselves run on, which is the one the written client is for.
    /// </summary>
    private static IReadOnlyList<string> SaidAbout(string source, DiagnosticSeverity severity)
    {
        var references = ((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create(
            "WrittenClient",
            new[] {CSharpSyntaxTree.ParseText(source)},
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        return compilation.GetDiagnostics()
            .Where(said => said.Severity == severity)
            .Select(said => said.ToString())
            .ToArray();
    }
}
