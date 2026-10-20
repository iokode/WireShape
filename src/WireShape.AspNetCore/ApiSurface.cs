using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace IOKode.WireShape.AspNetCore;

/// <summary>
/// Reads the calls an assembly of controllers serves, asked of the application the host would build instead of
/// a server that answers.
/// </summary>
/// <remarks>
/// The calls are asked of the explorer of the application, which is what says how an API looks from outside and
/// what whoever writes a document reads as well. For it to answer about a value declared to travel as a text,
/// the binder has to have been taught that already: a type it cannot read from text is taken for something with
/// members and leaves nothing behind but the hole in the route.
/// </remarks>
/// <param name="controllers">The assembly whose controllers are read.</param>
/// <param name="conventions">
/// What the application is configured with, which is where whoever reads says how a route of theirs is built.
/// </param>
/// <param name="belongs">Whether an action is one of the calls being read.</param>
/// <param name="operationOf">What stands behind an action, where anything does.</param>
public sealed class ApiSurface(
    Assembly controllers,
    Action<MvcOptions> conventions,
    Func<MethodInfo, bool> belongs,
    Func<MethodInfo, Type?> operationOf)
{
    public IReadOnlyList<ApiAction> GetActions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvcCore(conventions)
            .AddApiExplorer()
            .AddApplicationPart(controllers);

        var provider = services.BuildServiceProvider();
        var descriptions = provider.GetRequiredService<IApiDescriptionGroupCollectionProvider>()
            .ApiDescriptionGroups.Items
            .SelectMany(group => group.Items);

        return descriptions
            .Where(description => description.ActionDescriptor is ControllerActionDescriptor)
            .Select(description => (
                Description: description,
                Action: (ControllerActionDescriptor) description.ActionDescriptor))
            .Where(described => described.Action.ControllerTypeInfo.Assembly == controllers)
            .Where(described => belongs(described.Action.MethodInfo))
            .Select(described => Describe(described.Description, described.Action))
            .OrderBy(action => action.Controller)
            .ThenBy(action => action.MethodName)
            .ToArray();
    }

    private ApiAction Describe(ApiDescription description, ControllerActionDescriptor action)
    {
        var parameters = Parameters(description, action);

        return new ApiAction
        {
            Method = description.HttpMethod ?? "GET",

            Route = "/" + description.RelativePath,
            Controller = action.ControllerName,
            Operation = operationOf(action.MethodInfo),
            MethodName = MethodNameOf(action),
            Parameters = parameters,
            Result = ResultOf(action.MethodInfo),
            IsDownload = IsDownloaded(ResultOf(action.MethodInfo)),
            IsMultipart = parameters.Any(parameter => parameter.Origin == ApiParameterOrigin.Form)
        };
    }

    /// <summary>
    /// What a call takes, one value per parameter of the action. The explorer spells out a group of values member
    /// by member, because that is how they travel; a client takes the group, so they are put back together by the
    /// parameter each of them came from.
    /// </summary>
    private static IReadOnlyList<ApiParameter> Parameters(
        ApiDescription description,
        ControllerActionDescriptor action)
    {
        return description.ParameterDescriptions
            .Where(parameter => parameter.ParameterDescriptor != null)
            .GroupBy(parameter => parameter.ParameterDescriptor)
            .Select(group => (
                Parameter: group.Key!,
                Source: group.First().Source,
                IsRequired: group.Any(parameter => parameter.IsRequired)))
            .Where(described => described.Parameter.ParameterType != typeof(CancellationToken))
            .Select(described => Describe(described.Parameter, described.Source, described.IsRequired))
            .ToArray();
    }

    private static ApiParameter Describe(ParameterDescriptor parameter, BindingSource? source, bool isRequired)
    {
        if (parameter.ParameterType == typeof(IFormFile))
        {
            return new ApiParameter(
                parameter.Name,
                parameter.ParameterType,
                ApiParameterOrigin.Form,
                IsOptional: false);
        }

        var origin = source switch
        {
            {Id: "Path"} => ApiParameterOrigin.Route,
            {Id: "Body"} => ApiParameterOrigin.Body,
            {Id: "Query"} => ApiParameterOrigin.Query,
            {Id: "Form"} or {Id: "FormFile"} => ApiParameterOrigin.Form,
            _ => IsSimple(parameter.ParameterType) ? ApiParameterOrigin.Query : ApiParameterOrigin.Body
        };

        // What the path is made of is never absent: a route with a hole in it is not a route.
        var isOptional = origin != ApiParameterOrigin.Route && !isRequired;

        return new ApiParameter(parameter.Name, parameter.ParameterType, origin, isOptional);
    }

    /// <summary>
    /// The name the call takes in a client. Every call is awaited, so every name says so, even when the action
    /// it comes from does not.
    /// </summary>
    private static string MethodNameOf(ControllerActionDescriptor descriptor)
    {
        var name = descriptor.MethodInfo.Name;
        return name.EndsWith("Async", StringComparison.Ordinal) ? name : name + "Async";
    }

    private static Type? ResultOf(MethodInfo action)
    {
        var produces = action.GetCustomAttributes(inherit: true)
            .OfType<ProducesResponseTypeAttribute>()
            .Where(attribute => attribute.StatusCode is >= 200 and < 300)
            .Select(attribute => attribute.Type)
            .FirstOrDefault(type => type != typeof(void) && type != typeof(object));

        return produces == typeof(ProblemDetails) ? null : produces;
    }

    /// <summary>
    /// Whether what the action says it answers with is a result and not a value. A result decides the answer by
    /// itself, and for a file what it decides is the content, which is read as content and not as a value.
    /// </summary>
    private static bool IsDownloaded(Type? result) =>
        result != null && typeof(IActionResult).IsAssignableFrom(result);

    private static bool IsSimple(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive || underlying.IsEnum || underlying == typeof(string) ||
               underlying == typeof(Guid) || underlying == typeof(decimal) || underlying == typeof(DateTime);
    }

}
