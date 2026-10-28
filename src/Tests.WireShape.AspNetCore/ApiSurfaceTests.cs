using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using IOKode.WireShape.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using IOKode.WireShape.SystemTextJson;
using Xunit;

namespace IOKode.WireShape.AspNetCore.Tests;

/// <summary>
/// What is read of an application that serves calls: which of them are read, the route of each, and where every
/// value of a call comes from.
/// </summary>
public class ApiSurfaceTests
{
    private static readonly IReadOnlyList<ApiAction> actions = ReadActions();

    [Fact]
    public void AnActionThatIsNotOfTheSurface_ShouldNotBeRead()
    {
        // Assert
        Assert.DoesNotContain("CancelAsync", actions.Select(action => action.MethodName));
    }

    [Fact]
    public void EveryActionOfTheSurface_ShouldBeReadUnderItsRoute()
    {
        // Assert
        Assert.Equal(
            new[]
            {
                ("POST", "/shipments"),
                ("GET", "/shipments/{code}"),
                ("GET", "/shipments/{code}/label"),
                ("GET", "/shipments"),
                ("POST", "/shipments/{code}/proof")
            },
            actions.Select(action => (action.Method, action.Route)));
    }

    [Fact]
    public void WhatACallIsNamedAfter_ShouldSayThatItIsAwaited()
    {
        // Assert
        Assert.Contains("DispatchAsync", actions.Select(action => action.MethodName));
    }

    [Fact]
    public void WhatStandsBehindAnAction_ShouldBeWhoeverReadsWasAskedFor()
    {
        // Assert
        Assert.All(actions, action => Assert.Equal(typeof(Consignment), action.Operation));
    }

    [Fact]
    public void WhatThePathIsMadeOf_ShouldComeFromTheRouteAndNeverBeAbsent()
    {
        // Act
        var code = FindAction("GetAsync").Parameters.Single();

        // Assert
        Assert.Equal(ApiParameterOrigin.Route, code.Origin);
        Assert.False(code.IsOptional);
        Assert.Equal(typeof(TrackingCode), code.Type);
    }

    [Fact]
    public void WhatACallIsAskedFor_ShouldNotIncludeWhatTellsItToStop()
    {
        // Assert
        Assert.Single(FindAction("GetAsync").Parameters);
    }

    [Fact]
    public void AValueThatMayBeAbsent_ShouldComeFromTheQueryStringAndSaySo()
    {
        // Act
        var code = FindAction("ListAsync").Parameters.Single();

        // Assert
        Assert.Equal(ApiParameterOrigin.Query, code.Origin);
        Assert.True(code.IsOptional);
    }

    [Fact]
    public void WhatACallIsSentWhole_ShouldComeFromTheBody()
    {
        // Act
        var consignment = FindAction("DispatchAsync").Parameters.Single();

        // Assert
        Assert.Equal(ApiParameterOrigin.Body, consignment.Origin);
        Assert.Equal(typeof(Consignment), consignment.Type);
        Assert.False(consignment.IsOptional);
    }

    [Fact]
    public void ACallThatCarriesAFile_ShouldBeSentInParts()
    {
        // Act
        var action = FindAction("ProofAsync");

        // Assert
        Assert.True(action.IsMultipart);
        Assert.Contains(ApiParameterOrigin.Form, action.Parameters.Select(parameter => parameter.Origin));
    }

    [Fact]
    public void ACallThatAnswersWithAResultOfItsOwn_ShouldBeReadAsSomethingToDownload()
    {
        // Assert
        Assert.True(FindAction("LabelAsync").IsDownload);
        Assert.False(FindAction("GetAsync").IsDownload);
    }

    private static ApiAction FindAction(string methodName) =>
        actions.Single(action => action.MethodName == methodName);

    private static IReadOnlyList<ApiAction> ReadActions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new TrackingCodeJsonConverter());
        var serializer = new JsonSerializerOptionsSerializer(options);

        var declaring = new WireBuilder();
        var declarer = declaring.GetDeclarer("application");
        declarer.Add(typeof(TrackingCode), new Shape.As(Carrier.String));
        var wire = declaring.Build(serializer);

        // Taught first: a value the binder cannot read from text is taken for something with members, and what
        // the route is made of would be read as a hole with nothing in it.
        foreach (var (type, converter) in TypeDescriptors.GetConverters(wire))
        {
            TypeDescriptors.Teach(type, converter);
        }

        return new ApiSurface(
            typeof(ShipmentsController).Assembly,
            _ => { },
            method => method.GetCustomAttribute<ServedAttribute>() != null,
            _ => typeof(Consignment)).GetActions();
    }
}

public sealed class TrackingCodeJsonConverter : JsonConverter<TrackingCode>
{
    public override TrackingCode Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        new(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, TrackingCode value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
