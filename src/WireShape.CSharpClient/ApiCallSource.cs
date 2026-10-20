namespace IOKode.WireShape.CSharpClient;

/// <summary>
/// What every generated client needs to make a call and read its answer. It is written alongside them so that a
/// client carries nothing but itself: no package to install, no version to keep in step.
/// </summary>
public static class ApiCallSource
{
    private const string Text = """
        /// <summary>A file a call carries.</summary>
        public sealed record ApiFile(string Name, Stream Content, string ContentType);

        /// <summary>
        /// A value that travels as a value of another type: an amount as a number, an identifier as a GUID, a
        /// code as a text.
        /// </summary>
        /// <remarks>
        /// What travels is said here and not by reading the value as text, so that whoever consumes the client
        /// is free to read one as it should be shown without that changing what goes in a route or a query.
        /// </remarks>
        public interface IApiValue
        {
            object? Written { get; }
        }

        /// <summary>
        /// Says that this type is one of the kinds another can turn out to be, and the word that tells it apart.
        /// A kind the client that declares the hierarchy knows is said there; a kind another client adds is said
        /// here, because a hierarchy written without knowing a kind existed cannot name it.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        public sealed class ApiPolymorphicKindAttribute(Type baseType, string discriminator) : Attribute
        {
            public Type BaseType { get; } = baseType;

            public string Discriminator { get; } = discriminator;
        }

        /// <summary>
        /// A file a call answered with, read as it came. It holds the answer open for as long as its content is
        /// being read, so whoever asked for it disposes of it once they are done.
        /// </summary>
        public sealed class ApiDownload(HttpResponseMessage answered, string fileName, string contentType, Stream content)
            : IDisposable
        {
            /// <summary>The name the answer gave the file, or an empty string when it gave none.</summary>
            public string FileName { get; } = fileName;

            public string ContentType { get; } = contentType;

            public Stream Content { get; } = content;

            public void Dispose()
            {
                Content.Dispose();
                answered.Dispose();
            }
        }

        /// <summary>
        /// A call the application refused, with the reason it gave. The reason is written for whoever asked, so
        /// it is the one to show.
        /// </summary>
        public sealed class ApiException(HttpStatusCode status, string? title, string? detail)
            : Exception(detail ?? title ?? $"The request was answered with {(int) status}.")
        {
            public HttpStatusCode Status { get; } = status;

            public string? Title { get; } = title;

            public string? Detail { get; } = detail;
        }

        /// <summary>
        /// How what travels is read and written. The types this client declares say it by themselves, and a kind
        /// added to a hierarchy another client declares is said here, because only whoever added it knows of it.
        /// </summary>
        public static class ApiJson
        {
            private static readonly Dictionary<Type, List<(Type Derived, string Discriminator)>> declared = new();

            public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver {Modifiers = {Declare}}
            };

            /// <summary>
            /// Says that a type travels as one of several under another, with the word that tells it apart. It is
            /// said before the first call, because what has already been read or written is settled by then.
            /// </summary>
            public static void Polymorphic(Type baseType, Type derivedType, string discriminator)
            {
                if (!declared.TryGetValue(baseType, out var derived))
                {
                    derived = new List<(Type, string)>();
                    declared[baseType] = derived;
                }

                if (!derived.Any(registration => registration.Derived == derivedType))
                {
                    derived.Add((derivedType, discriminator));
                }
            }

            private static void Declare(JsonTypeInfo information)
            {
                if (!declared.TryGetValue(information.Type, out var derived))
                {
                    return;
                }

                information.PolymorphismOptions ??= new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = "<discriminator>"
                };

                foreach (var registration in derived)
                {
                    var known = information.PolymorphismOptions.DerivedTypes
                        .Any(candidate => candidate.DerivedType == registration.Derived);

                    if (!known)
                    {
                        information.PolymorphismOptions.DerivedTypes.Add(
                            new JsonDerivedType(registration.Derived, registration.Discriminator));
                    }
                }
            }
        }

        /// <summary>The parts of making a call that every call shares.</summary>
        public static class ApiCall
        {
            public static JsonSerializerOptions Options => ApiJson.Options;

            public static string Segment(object? value) => Uri.EscapeDataString(Text(value) ?? string.Empty);

            public static string Query(IReadOnlyList<KeyValuePair<string, object?>> values)
            {
                var written = values
                    .Where(value => value.Value != null)
                    .Select(value => $"{Uri.EscapeDataString(value.Key)}={Uri.EscapeDataString(Text(value.Value)!)}")
                    .ToArray();

                return written.Length == 0 ? string.Empty : "?" + string.Join("&", written);
            }

            public static HttpContent Json<T>(T value) =>
                new StringContent(JsonSerializer.Serialize(value, Options), Encoding.UTF8, "application/json");

            public static void AddFile(MultipartFormDataContent form, string name, ApiFile file)
            {
                var content = new StreamContent(file.Content);
                content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                form.Add(content, name, file.Name);
            }

            public static void AddValue(MultipartFormDataContent form, string name, object? value)
            {
                if (value != null)
                {
                    form.Add(new StringContent(Text(value)!), name);
                }
            }

            public static async Task EnsureAnsweredAsync(HttpResponseMessage response, CancellationToken cancellationToken)
            {
                if (response.IsSuccessStatusCode)
                {
                    return;
                }

                ProblemDetails? problem = null;
                try
                {
                    problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Options, cancellationToken);
                }
                catch (Exception)
                {
                }

                throw new ApiException(response.StatusCode, problem?.Title, problem?.Detail);
            }

            public static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
            {
                var value = await response.Content.ReadFromJsonAsync<T>(Options, cancellationToken);
                return value!;
            }

            public static async Task<ApiDownload> DownloadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
            {
                var disposition = response.Content.Headers.ContentDisposition;
                var fileName = disposition?.FileNameStar ?? disposition?.FileName?.Trim('"') ?? string.Empty;
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                var content = await response.Content.ReadAsStreamAsync(cancellationToken);

                return new ApiDownload(response, fileName, contentType, content);
            }

            private static string? Text(object? value) => value switch
            {
                null => null,
                IApiValue carried => Text(carried.Written),
                string written => written,
                bool written => written ? "true" : "false",

                // Written as it is written in a body, because a value of an enumeration travels under the word
                // the wire writes it as and not under the name it carries here. Said with the serializer so that
                // the two cannot drift, and unquoted because a route and a query carry text.
                Enum written => JsonSerializer.Serialize(written, written.GetType(), Options).Trim('"'),
                DateOnly written => written.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                TimeOnly written => written.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
                DateTime written => written.ToString("O", CultureInfo.InvariantCulture),
                IFormattable written => written.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        /// <summary>The reason the application gives when it refuses.</summary>
        public sealed record ProblemDetails
        {
            [JsonPropertyName("title")]
            public string? Title { get; init; }

            [JsonPropertyName("detail")]
            public string? Detail { get; init; }

            [JsonPropertyName("status")]
            public int? Status { get; init; }
        }
        """;

    /// <summary>
    /// The text, with the name of the member that tells one kind from another filled in. A client that looked
    /// for it under another name would read every answer of a polymorphic type as unreadable.
    /// </summary>
    /// <param name="discriminator">
    /// The member the word that tells one kind from another travels under, which is the one the model carries.
    /// </param>
    public static string Write(string discriminator) => Text.Replace(DiscriminatorPlaceholder, discriminator);

    private const string DiscriminatorPlaceholder = "<discriminator>";
}
