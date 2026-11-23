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
        /// What travels is said here, and its text is what a route, a query and the value's own <c>ToString</c>
        /// all write: the text it has in a body, and one and the same wherever it goes.
        /// </remarks>
        public interface IApiValue
        {
            object? Written { get; }
        }

        /// <summary>
        /// A member that may be left out of what travels, and that holds whether it was given at all. Left out, it
        /// does not travel; given, it travels as what it was given, null included where null is one of its
        /// values. The two are different things to whoever reads them: in a change, null is "make it nothing" and
        /// left out is "leave it as it is".
        /// </summary>
        /// <remarks>
        /// A value converts into one, so giving it is assigning the value, null included. Not assigning it at all
        /// is leaving it out, which is what a new one is.
        /// </remarks>
        [JsonConverter(typeof(ApiOptionalJsonConverterFactory))]
        public readonly struct ApiOptional<T> : IApiOptional, IEquatable<ApiOptional<T>>
        {
            private readonly T value;

            public ApiOptional(T value)
            {
                this.value = value;
                IsGiven = true;
            }

            /// <summary>A member left out.</summary>
            public static ApiOptional<T> LeftOut => default;

            /// <summary>Whether it was given, null included, rather than left out.</summary>
            public bool IsGiven { get; }

            /// <summary>What it was given.</summary>
            /// <exception cref="InvalidOperationException">It was left out, so it holds nothing.</exception>
            public T Value => IsGiven
                ? value
                : throw new InvalidOperationException($"This {typeof(T).Name} was left out, so it holds no value.");

            object? IApiOptional.Given => Value;

            public static implicit operator ApiOptional<T>(T value) => new(value);

            public bool Equals(ApiOptional<T> other) =>
                IsGiven == other.IsGiven && (!IsGiven || EqualityComparer<T>.Default.Equals(value, other.value));

            public override bool Equals(object? other) => other is ApiOptional<T> optional && Equals(optional);

            public override int GetHashCode() => IsGiven ? HashCode.Combine(true, value) : 0;

            public override string ToString() => IsGiven ? value?.ToString() ?? string.Empty : string.Empty;

            public static bool operator ==(ApiOptional<T> left, ApiOptional<T> right) => left.Equals(right);

            public static bool operator !=(ApiOptional<T> left, ApiOptional<T> right) => !left.Equals(right);
        }

        /// <summary>What every member that may be left out says of itself, whatever it holds.</summary>
        public interface IApiOptional
        {
            bool IsGiven { get; }

            /// <summary>What it was given.</summary>
            /// <exception cref="InvalidOperationException">It was left out, so it holds nothing.</exception>
            object? Given { get; }
        }

        /// <summary>
        /// Reads and writes a member that may be left out as what it holds. Whether it travels at all is not
        /// decided here: a member left out is not written, which is said of the member and not of its value.
        /// </summary>
        public sealed class ApiOptionalJsonConverterFactory : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert) =>
                typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(ApiOptional<>);

            public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
                (JsonConverter) Activator.CreateInstance(
                    typeof(ApiOptionalJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;
        }

        /// <inheritdoc cref="ApiOptionalJsonConverterFactory"/>
        public sealed class ApiOptionalJsonConverter<T> : JsonConverter<ApiOptional<T>>
        {
            /// <summary>Null is a value it may be given, so it is read as given and not as left out.</summary>
            public override bool HandleNull => true;

            public override ApiOptional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                new(JsonSerializer.Deserialize<T>(ref reader, options)!);

            /// <exception cref="NotSupportedException">
            /// It was left out. Only a member can be left out, and a member left out is not written at all.
            /// </exception>
            public override void Write(Utf8JsonWriter writer, ApiOptional<T> value, JsonSerializerOptions options)
            {
                if (!value.IsGiven)
                {
                    throw new NotSupportedException(
                        $"A {typeof(T).Name} left out cannot be written: only a member can be left out, and a " +
                        "member left out is not written at all.");
                }

                JsonSerializer.Serialize(writer, value.Value, options);
            }
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
        /// A call the application refused, with the reason it gave and what it said about each field of the
        /// request. Both are written for whoever asked, so they are the ones to show.
        /// </summary>
        public sealed class ApiException(
            HttpStatusCode status,
            string? title,
            string? detail,
            IReadOnlyDictionary<string, IReadOnlyList<string>> errors)
            : Exception(detail ?? title ?? $"The request was answered with {(int) status}.")
        {
            public HttpStatusCode Status { get; } = status;

            public string? Title { get; } = title;

            public string? Detail { get; } = detail;

            /// <summary>
            /// The messages about each field of the request, keyed by the field as the application named it, so
            /// that each can be shown next to its field. Empty when the application said nothing about any.
            /// </summary>
            public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; } = errors;
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
                TypeInfoResolver = new DefaultJsonTypeInfoResolver {Modifiers = {Declare, LeaveOut}}
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

            /// <summary>
            /// A member that may be left out and was left out is not written, which is what tells it apart from one given
            /// null.
            /// </summary>
            private static void LeaveOut(JsonTypeInfo information)
            {
                foreach (var property in information.Properties)
                {
                    if (property.PropertyType.IsGenericType &&
                        property.PropertyType.GetGenericTypeDefinition() == typeof(ApiOptional<>))
                    {
                        property.ShouldSerialize = (_, value) => value is IApiOptional {IsGiven: true};
                    }
                }
            }
        }

        /// <summary>The parts of making a call that every call shares.</summary>
        public static class ApiCall
        {
            public static JsonSerializerOptions Options => ApiJson.Options;

            /// <summary>The text a value travels as in a route, escaped.</summary>
            /// <exception cref="ArgumentException">
            /// The value is written as a collection or an object: the application reads neither back from the JSON
            /// they are written as.
            /// </exception>
            public static string Segment(object? value)
            {
                if (value is IApiValue carried)
                {
                    return Segment(carried.Written);
                }

                if (value == null)
                {
                    return string.Empty;
                }

                var written = JsonSerializer.SerializeToElement(value, value.GetType(), Options);
                if (written.ValueKind is JsonValueKind.Array or JsonValueKind.Object)
                {
                    throw new ArgumentException(
                        $"A value cannot travel in a route: it is written as {WrittenAs(written)}, and a route " +
                        "carries only values written as one text each.");
                }

                return Uri.EscapeDataString(Written(written));
            }

            /// <summary>
            /// The query a call carries, with a value under its key and a collection as one pair per element, all
            /// under the same key, which is how the application reads a collection back. A value left out and an
            /// empty collection write no pair, and an element that is null, or a member that may be left out and
            /// was given null, is written with no text, which is how the application reads it back as null.
            /// </summary>
            /// <exception cref="ArgumentException">
            /// A value is written as an object, or an element of a collection as a collection or an object: the
            /// application reads neither back from the JSON they are written as.
            /// </exception>
            public static string Query(IReadOnlyList<KeyValuePair<string, object?>> values)
            {
                var written = values
                    .SelectMany(value => Texts(value.Key, value.Value)
                        .Select(text => $"{Uri.EscapeDataString(value.Key)}={Uri.EscapeDataString(text)}"))
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

            /// <summary>
            /// A value of a form, under its name, and a collection as one field per element, all under the same
            /// name, which is how the application reads a collection back. A value left out and an empty
            /// collection add no field, and an element that is null is added with no text, which is how the
            /// application reads it back as null.
            /// </summary>
            /// <exception cref="ArgumentException">
            /// The value is written as an object, or an element of a collection as a collection or an object: the
            /// application reads neither back from the JSON they are written as.
            /// </exception>
            public static void AddValue(MultipartFormDataContent form, string name, object? value)
            {
                if (value is IApiValue carried)
                {
                    AddValue(form, name, carried.Written);
                    return;
                }

                if (value == null)
                {
                    return;
                }

                var written = JsonSerializer.SerializeToElement(value, value.GetType(), Options);
                if (written.ValueKind == JsonValueKind.Object)
                {
                    throw new ArgumentException(
                        $"The value '{name}' cannot travel in a form: it is written as an object, and a form " +
                        "carries only values written as one text each.");
                }

                var texts = written.ValueKind == JsonValueKind.Array
                    ? written.EnumerateArray().Select(element => Element(name, element, "a form", "field")).ToArray()
                    : new[] {Written(written)};

                foreach (var text in texts)
                {
                    form.Add(new StringContent(text), name);
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

                var errors = problem?.Errors?.ToDictionary(
                    field => field.Key,
                    field => (IReadOnlyList<string>) field.Value);

                throw new ApiException(
                    response.StatusCode,
                    problem?.Title,
                    problem?.Detail,
                    errors ?? new Dictionary<string, IReadOnlyList<string>>());
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

            /// <summary>
            /// The text a value travels as in a route or a query, before either escapes it: the one it has in a
            /// body. Written by the serializer, with the options a body is written with, so that the two cannot
            /// drift: a moment, a time of day and a word of an enumeration are each written once, and by it.
            /// </summary>
            /// <remarks>
            /// What it wrote is read back and not unquoted, because the quotes are not all the serializer adds:
            /// it escapes what it writes inside them, and a <c>+</c> would travel as <c>+</c>.
            /// </remarks>
            public static string? Text(object? value) => value switch
            {
                null => null,
                IApiValue carried => Text(carried.Written),
                IApiOptional {IsGiven: false} => null,
                IApiOptional optional => Text(optional.Given),
                _ => Written(JsonSerializer.SerializeToElement(value, value.GetType(), Options))
            };

            /// <summary>
            /// The texts a value travels as in a query: none for no value, one for a value, and one per element for
            /// a collection, each written as a value on its own would be. What the serializer writes as an array is
            /// what is a collection, so that a value that is written as one text is never taken apart.
            /// </summary>
            private static IEnumerable<string> Texts(string key, object? value)
            {
                if (value == null)
                {
                    return Array.Empty<string>();
                }

                if (value is IApiValue carried)
                {
                    return Texts(key, carried.Written);
                }

                // Left out, it writes no pair and the application leaves what it reads into as it was; given null,
                // a pair with no text, which the application reads as null.
                if (value is IApiOptional optional)
                {
                    return optional switch
                    {
                        {IsGiven: false} => Array.Empty<string>(),
                        {Given: null} => new[] {string.Empty},
                        _ => Texts(key, optional.Given)
                    };
                }

                var written = JsonSerializer.SerializeToElement(value, value.GetType(), Options);

                return written.ValueKind switch
                {
                    JsonValueKind.Array => written.EnumerateArray()
                        .Select(element => Element(key, element, "the query string", "key"))
                        .ToArray(),
                    JsonValueKind.Object => throw new ArgumentException(
                        $"The value '{key}' cannot travel in the query string: it is written as an object, and the " +
                        "query string carries only values written as one text each."),
                    _ => new[] {Written(written)}
                };
            }

            /// <summary>
            /// The text an element of a collection travels as, where a collection travels as its elements under
            /// one repeated name: a query string or a form.
            /// </summary>
            private static string Element(string key, JsonElement element, string where, string repeated) =>
                element.ValueKind switch
            {
                JsonValueKind.Null => string.Empty,
                JsonValueKind.Array or JsonValueKind.Object => throw new ArgumentException(
                    $"The collection '{key}' cannot travel in {where}: an element of it is written as " +
                    $"{WrittenAs(element)}, and repeating the {repeated} carries only elements written as one text " +
                    "each."),
                _ => Written(element)
            };

            private static string WrittenAs(JsonElement written) =>
                written.ValueKind == JsonValueKind.Array ? "a collection" : "an object";

            private static string Written(JsonElement written) =>
                written.ValueKind == JsonValueKind.String ? written.GetString()! : written.GetRawText();
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

            /// <summary>The messages about each field of the request, keyed by the field.</summary>
            [JsonPropertyName("errors")]
            public Dictionary<string, string[]>? Errors { get; init; }
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
