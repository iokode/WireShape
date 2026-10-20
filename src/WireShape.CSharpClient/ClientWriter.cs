using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IOKode.WireShape.AspNetCore;

namespace IOKode.WireShape.CSharpClient;

/// <summary>
/// Writes the clients: one class per controller, each with the calls that controller answers. They take an
/// <c>HttpClient</c> and nothing else, so what a call does is written here and not hidden behind an attribute.
/// </summary>
/// <param name="model">What the API is, said as data.</param>
/// <param name="names">What each type is called in the client being written.</param>
/// <param name="clientNameOf">
/// What a client made of one controller's calls is called, from the name the application gives the controller.
/// Nothing here takes a word off either end: what yours end in, and what a client of their calls is called, are
/// both yours.
/// </param>
public sealed class ClientWriter(ApiModel model, TypeNames names, Func<string, string> clientNameOf)
{
    private readonly StringBuilder written = new();

    public string Written => written.ToString();

    public void Write(IReadOnlyList<ApiAction> actions)
    {
        foreach (var controller in actions.GroupBy(action => action.Controller).OrderBy(group => group.Key))
        {
            WriteClient(controller.Key, controller.ToArray());
        }
    }

    private void WriteClient(string controller, IReadOnlyList<ApiAction> actions)
    {
        written.AppendLine($"public sealed class {clientNameOf(controller)}(HttpClient http)");
        written.AppendLine("{");

        foreach (var action in actions)
        {
            WriteCall(action);
        }

        written.AppendLine("}");
        written.AppendLine();
    }

    private void WriteCall(ApiAction action)
    {
        // Only from the point where everything after it may be left out too, because that is the only way a
        // parameter is allowed to have a value of its own.
        var optionalFrom = OptionalFrom(action.Parameters);

        var parameters = action.Parameters
            .Select((parameter, position) =>
                $"{names.Of(parameter.Type, parameter.IsOptional)} {Camel(parameter.Name)}" +
                (position >= optionalFrom ? " = null" : string.Empty))
            .Append("CancellationToken cancellationToken = default");

        var result = action switch
        {
            {IsDownload: true} => "Task<ApiDownload>",
            {Result: null} => "Task",
            _ => $"Task<{names.Of(action.Result)}>"
        };

        written.AppendLine($"    public async {result} {action.MethodName}({string.Join(", ", parameters)})");
        written.AppendLine("    {");
        WriteUrl(action);
        WriteRequest(action);

        // A download outlives the call: what is handed over is the content of the answer, so the answer is
        // disposed of by whoever reads it and not here.
        written.AppendLine(action.IsDownload
            ? "        var response = await http.SendAsync(request, cancellationToken);"
            : "        using var response = await http.SendAsync(request, cancellationToken);");

        written.AppendLine("        await ApiCall.EnsureAnsweredAsync(response, cancellationToken);");

        if (action.IsDownload)
        {
            written.AppendLine("        return await ApiCall.DownloadAsync(response, cancellationToken);");
        }
        else if (action.Result != null)
        {
            written.AppendLine($"        return await ApiCall.ReadAsync<{names.Of(action.Result)}>(" +
                               "response, cancellationToken);");
        }

        written.AppendLine("    }");
        written.AppendLine();
    }

    /// <summary>
    /// The path of the call, with what belongs to the path put into it and what belongs to the query hung off
    /// it.
    /// </summary>
    private void WriteUrl(ApiAction action)
    {
        var route = action.Route;
        foreach (var parameter in action.Parameters.Where(parameter =>
                     parameter.Origin == ApiParameterOrigin.Route))
        {
            route = route.Replace($"{{{parameter.Name}}}", $"{{ApiCall.Segment({Camel(parameter.Name)})}}");
        }

        written.AppendLine($"        var url = $\"{route}\";");

        var query = action.Parameters.Where(parameter => parameter.Origin == ApiParameterOrigin.Query).ToArray();
        if (query.Length == 0)
        {
            return;
        }

        written.AppendLine("        var query = new List<KeyValuePair<string, object?>>();");
        foreach (var parameter in query)
        {
            var members = MembersInQuery(parameter);
            if (members.Count == 0)
            {
                written.AppendLine("        query.Add(new KeyValuePair<string, object?>(" +
                                   $"\"{parameter.Name}\", {Camel(parameter.Name)}));");

                continue;
            }

            foreach (var member in members)
            {
                written.AppendLine("        query.Add(new KeyValuePair<string, object?>(" +
                                   $"\"{member.Name}\", {Camel(parameter.Name)}?.{Pascal(member.Name)}));");
            }
        }

        written.AppendLine("        url += ApiCall.Query(query);");
    }

    private void WriteRequest(ApiAction action)
    {
        written.AppendLine("        using var request = new HttpRequestMessage(" +
                           $"new HttpMethod(\"{action.Method}\"), url);");

        var body = action.Parameters.FirstOrDefault(parameter => parameter.Origin == ApiParameterOrigin.Body);
        if (body != null)
        {
            written.AppendLine($"        request.Content = ApiCall.Json({Camel(body.Name)});");
        }

        var form = action.Parameters.Where(parameter => parameter.Origin == ApiParameterOrigin.Form).ToArray();
        if (form.Length == 0)
        {
            return;
        }

        written.AppendLine("        var form = new MultipartFormDataContent();");
        foreach (var parameter in form)
        {
            // Which of the two is asked of the model and not of the name of the type: what travels as the
            // content of a file is sent as content, and everything else in a form is sent as a value.
            written.AppendLine(model.Types.GetValueOrDefault(parameter.Type) is Structure.File
                ? $"        ApiCall.AddFile(form, \"{parameter.Name}\", {Camel(parameter.Name)});"
                : $"        ApiCall.AddValue(form, \"{parameter.Name}\", {Camel(parameter.Name)});");
        }

        written.AppendLine("        request.Content = form;");
    }

    /// <summary>
    /// The first position from which every value may be left out, or one past the last when some value after it
    /// has to be given.
    /// </summary>
    private static int OptionalFrom(IReadOnlyList<ApiParameter> parameters)
    {
        var optionalFrom = parameters.Count;
        for (var position = parameters.Count - 1; position >= 0 && parameters[position].IsOptional; position--)
        {
            optionalFrom = position;
        }

        return optionalFrom;
    }

    /// <summary>
    /// The members a value in the query travels as. A value that stands on its own goes under its own name,
    /// while one that groups several of them is a grouping on this side only: what the query carries is its
    /// members, each under its own name, which is how the application reads them back.
    /// </summary>
    private IReadOnlyList<Member> MembersInQuery(ApiParameter parameter)
    {
        var type = Nullable.GetUnderlyingType(parameter.Type) ?? parameter.Type;

        return model.Types.GetValueOrDefault(type) is Structure.Object written
            ? written.Members
            : Array.Empty<Member>();
    }

    private static string Camel(string name) =>
        name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];

    private static string Pascal(string name) =>
        name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name[1..];
}
