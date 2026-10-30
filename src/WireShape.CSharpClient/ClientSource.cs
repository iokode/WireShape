using System;
using System.Collections.Generic;
using System.Text;
using IOKode.WireShape.AspNetCore;

namespace IOKode.WireShape.CSharpClient;

/// <summary>
/// The whole of a client, as one file: what every call needs, the types that travel, and a class per controller.
/// </summary>
/// <remarks>
/// The pieces are each their own and can be written apart, and this is them in the order a file holds them. The
/// head is here and not left to whoever calls, because which usings the written code needs is a fact of what is
/// written and not of who asked for it.
/// </remarks>
public static class ClientSource
{
    /// <param name="model">What the API is, said as data.</param>
    /// <param name="namespace">What the written code goes under, which is yours.</param>
    /// <param name="clientNameOf">
    /// What a client made of one controller's calls is called, from the name the application gives the
    /// controller.
    /// </param>
    /// <param name="reused">
    /// The types another client already declares, under the names it declares them by, so that two clients of
    /// one application speak of one type and not of two alike. Their namespace goes in
    /// <paramref name="reference"/>.
    /// </param>
    /// <param name="reference">
    /// A namespace the written code reads types from, which is where the reused ones live. Null when there are
    /// none.
    /// </param>
    /// <exception cref="ArgumentException">
    /// The model holds something no client can be written for: a union two of whose alternatives arrive looking
    /// the same, so that nothing could tell which of them came.
    /// </exception>
    public static string Write(
        ApiModel model,
        string @namespace,
        Func<string, string> clientNameOf,
        IReadOnlyDictionary<Type, string>? reused = null,
        string? reference = null)
    {
        var names = new TypeNames(model);
        foreach (var (type, declaredName) in reused ?? new Dictionary<Type, string>())
        {
            names.Reuse(type, declaredName);
        }

        var clients = new ClientWriter(model, names, clientNameOf);
        clients.Write(model.Actions);

        // After the clients, because asking for the name of a type is what queues its declaration: what has to
        // be written is known once every call has been written and not before.
        var types = new DtoWriter(model, names);

        // What the writer refuses is its state, and here every bit of that state is what this was handed: the
        // refusal is of an argument, and it is said as one.
        try
        {
            types.WritePending();
        }
        catch (InvalidOperationException refused)
        {
            throw new ArgumentException(refused.Message, nameof(model), refused);
        }

        return new StringBuilder()
            .Append(Head(@namespace, reference))
            .Append(ApiCallSource.Write(model.Discriminator))
            .AppendLine()
            .AppendLine()
            .Append(types.Written)
            .Append(clients.Written)
            .ToString();
    }

    /// <summary>
    /// What a file of written code needs before any of it: that it is written, that nothing in it is to be
    /// edited, and the usings everything below relies on.
    /// </summary>
    public static string Head(string @namespace, string? reference = null) =>
        new StringBuilder()
            .AppendLine("// Written from the model of an API. Do not edit.")
            .AppendLine("#nullable enable")
            .AppendLine()
            .AppendLine("using System;")
            .AppendLine("using System.Collections.Generic;")
            .AppendLine("using System.IO;")
            .AppendLine("using System.Linq;")
            .AppendLine("using System.Net;")
            .AppendLine("using System.Net.Http;")
            .AppendLine("using System.Net.Http.Headers;")
            .AppendLine("using System.Net.Http.Json;")
            .AppendLine("using System.Text;")
            .AppendLine("using System.Text.Json;")
            .AppendLine("using System.Text.Json.Serialization;")
            .AppendLine("using System.Text.Json.Serialization.Metadata;")
            .AppendLine("using System.Threading;")
            .AppendLine("using System.Threading.Tasks;")
            .AppendLine(reference == null ? string.Empty : $"using {reference};")
            .AppendLine()
            .AppendLine($"namespace {@namespace};")
            .AppendLine()
            .ToString();
}
