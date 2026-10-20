using System;
using System.Collections.Generic;
using IOKode.WireShape.AspNetCore;
using Xunit;

namespace IOKode.WireShape.CSharpClient.Tests;

/// <summary>
/// What a client makes of the calls an application serves: one class per controller, and in each the call as it
/// is made.
/// </summary>
public class ClientWriterTests
{
    [Fact]
    public void TheCallsOfOneController_ShouldBeOneClientNamedAsThatProjectNamesThem()
    {
        // Act
        var written = Write(BuildCall("GetAsync", "/consignments/{code}"));

        // Assert
        Assert.Contains("public sealed class ConsignmentsClient(HttpClient http)", written);
        Assert.Contains("public async Task<Consignment> GetAsync(", written);
    }

    [Fact]
    public void AValueInTheRoute_ShouldBePutIntoThePath()
    {
        // Act
        var written = Write(BuildCall(
            "GetAsync",
            "/consignments/{code}",
            new ApiParameter("code", typeof(TrackingCode), ApiParameterOrigin.Route, IsOptional: false)));

        // Assert
        Assert.Contains("var url = $\"/consignments/{ApiCall.Segment(code)}\";", written);
    }

    [Fact]
    public void AValueInTheQuery_ShouldBeHungOffThePath()
    {
        // Act
        var written = Write(BuildCall(
            "ListAsync",
            "/consignments",
            new ApiParameter("service", typeof(CarrierService), ApiParameterOrigin.Query, IsOptional: true)));

        // Assert
        Assert.Contains("query.Add(new KeyValuePair<string, object?>(\"service\", service));", written);
        Assert.Contains("url += ApiCall.Query(query);", written);
    }

    /// <summary>
    /// A value of several members in the query is a grouping on this side only: what the query carries is its
    /// members, each under its own name, which is how the application reads them back.
    /// </summary>
    [Fact]
    public void AGroupingInTheQuery_ShouldBeHungOffItMemberByMember()
    {
        // Act
        var written = Write(BuildCall(
            "ListAsync",
            "/consignments",
            new ApiParameter("filter", typeof(PhoneParts), ApiParameterOrigin.Query, IsOptional: true)));

        // Assert
        Assert.Contains("\"country\", filter?.Country", written);
        Assert.Contains("\"number\", filter?.Number", written);
    }

    [Fact]
    public void AValueInTheBody_ShouldBeSentAsJson()
    {
        // Act
        var written = Write(BuildCall(
            "CreateAsync",
            "/consignments",
            new ApiParameter("consignment", typeof(Consignment), ApiParameterOrigin.Body, IsOptional: false)));

        // Assert
        Assert.Contains("request.Content = ApiCall.Json(consignment);", written);
    }

    /// <summary>
    /// What travels as the content of a file is sent as content and everything else in the form as a value,
    /// which is asked of the model and not of the name of the type.
    /// </summary>
    [Fact]
    public void AFileInAForm_ShouldBeSentAsContentAndTheRestOfTheFormAsValues()
    {
        // Act
        var written = Write(BuildCall(
            "AttachAsync",
            "/consignments/attachments",
            new ApiParameter("file", typeof(Attachment), ApiParameterOrigin.Form, IsOptional: false),
            new ApiParameter("replace", typeof(bool), ApiParameterOrigin.Form, IsOptional: false)));

        // Assert
        Assert.Contains("ApiCall.AddFile(form, \"file\", file);", written);
        Assert.Contains("ApiCall.AddValue(form, \"replace\", replace);", written);
        Assert.Contains("request.Content = form;", written);
    }

    /// <summary>
    /// A download outlives the call, so the answer is disposed of by whoever reads the content and not by the
    /// call.
    /// </summary>
    [Fact]
    public void ACallThatAnswersWithAFile_ShouldHandOverTheContentAndNotDisposeOfTheAnswer()
    {
        // Arrange
        var call = BuildCall("DownloadAsync", "/consignments/{code}/label") with {IsDownload = true};

        // Act
        var written = Write(call);

        // Assert
        Assert.Contains("public async Task<ApiDownload> DownloadAsync(", written);
        Assert.Contains("var response = await http.SendAsync(request, cancellationToken);", written);
        Assert.DoesNotContain("using var response", written);
        Assert.Contains("return await ApiCall.DownloadAsync(response, cancellationToken);", written);
    }

    [Fact]
    public void ACallThatAnswersWithNothing_ShouldReadNothingBack()
    {
        // Arrange
        var call = BuildCall("DeleteAsync", "/consignments/{code}") with {Result = null};

        // Act
        var written = Write(call);

        // Assert
        Assert.Contains("public async Task DeleteAsync(", written);
        Assert.Contains("await ApiCall.EnsureAnsweredAsync(response, cancellationToken);", written);
        Assert.DoesNotContain("ApiCall.ReadAsync", written);
    }

    /// <summary>
    /// Only from the point where everything after it may be left out too, because that is the only way a
    /// parameter is allowed to have a value of its own.
    /// </summary>
    [Fact]
    public void AValueThatMayBeLeftOutBeforeOneThatMayNot_ShouldBeAskedForAllTheSame()
    {
        // Act
        var written = Write(BuildCall(
            "ListAsync",
            "/consignments",
            new ApiParameter("service", typeof(CarrierService), ApiParameterOrigin.Query, IsOptional: true),
            new ApiParameter("code", typeof(TrackingCode), ApiParameterOrigin.Query, IsOptional: false),
            new ApiParameter("size", typeof(ParcelSize), ApiParameterOrigin.Query, IsOptional: true)));

        // Assert
        Assert.Contains(
            "ListAsync(CarrierService? service, TrackingCode code, ParcelSize? size = null, " +
            "CancellationToken cancellationToken = default)",
            written);
    }

    private static ApiAction BuildCall(string method, string route, params ApiParameter[] parameters) =>
        new()
        {
            Method = "GET",
            Route = route,
            Controller = "ConsignmentsResourceController",
            MethodName = method,
            Parameters = parameters,
            Result = typeof(Consignment)
        };

    private static string Write(params ApiAction[] actions)
    {
        var model = Shipments.Of(actions);
        var writer = new ClientWriter(model, new TypeNames(model), Shipments.NameOf);
        writer.Write(actions);

        return writer.Written;
    }
}
