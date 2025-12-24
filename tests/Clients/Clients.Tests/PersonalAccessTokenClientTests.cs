using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Interface.Identity.Services;
using Xunit;

namespace Vitarish.Clients.Interface.Identity.Tests;

public sealed class PersonalAccessTokenClientTests
{
    private static readonly ILogger<PersonalAccessTokenClient> Logger = NullLogger<PersonalAccessTokenClient>.Instance;

    [Theory]
    [InlineData("")]
#pragma warning disable CS8625
    [InlineData(null)]
#pragma warning restore CS8625
    public async Task CreateTokenAsync_ThrowsArgumentExceptionOrDerived_WhenUserIdNullOrEmpty(string? userId)
    {
        var clientFactory = new Mock<IIdentityClientService>(MockBehavior.Strict);
        var cache = new Mock<HybridCache>(MockBehavior.Strict);

        var sut = new PersonalAccessTokenClient(clientFactory.Object, Logger, cache.Object);

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            sut.CreateTokenAsync(userId!, lifetimeDays: 1, scopes: null, isReferenceToken: false, CancellationToken.None));
    }


    [Fact]
    public async Task CreateTokenAsync_Throws_WhenLifetimeDaysNull()
    {
        var clientFactory = new Mock<IIdentityClientService>(MockBehavior.Strict);
        var cache = new Mock<HybridCache>(MockBehavior.Strict);

        var sut = new PersonalAccessTokenClient(clientFactory.Object, Logger, cache.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.CreateTokenAsync("user-1", lifetimeDays: 30, scopes: null, isReferenceToken: false, CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateTokenAsync_Throws_WhenLifetimeDaysLessThanOne(int lifetimeDays)
    {
        var clientFactory = new Mock<IIdentityClientService>(MockBehavior.Strict);
        var cache = new Mock<HybridCache>(MockBehavior.Strict);

        var sut = new PersonalAccessTokenClient(clientFactory.Object, Logger, cache.Object);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.CreateTokenAsync("user-1", lifetimeDays, scopes: null, isReferenceToken: false, CancellationToken.None));
    }

    [Fact]
    public async Task CreateTokenAsync_ReturnsCachedValue_WhenHybridCacheHit()
    {
        var clientFactory = new Mock<IIdentityClientService>(MockBehavior.Strict);

        var cache = new Mock<HybridCache>(MockBehavior.Strict);
        cache.Setup(c => c.GetOrCreateAsync<
                (IIdentityClientService, ILogger<PersonalAccessTokenClient>, string, int, string[]?, bool),
                string>(
                It.IsAny<string>(),
                It.IsAny<(IIdentityClientService, ILogger<PersonalAccessTokenClient>, string, int, string[]?, bool)>(),
                It.IsAny<Func<(IIdentityClientService, ILogger<PersonalAccessTokenClient>, string, int, string[]?, bool), CancellationToken, ValueTask<string>>>(),
                It.IsAny<HybridCacheEntryOptions?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string>("cached-token"));

        var sut = new PersonalAccessTokenClient(clientFactory.Object, Logger, cache.Object);

        var token = await sut.CreateTokenAsync("user-1", 7, new[] { "a", "b" }, false, CancellationToken.None);

        Assert.Equal("cached-token", token);
        clientFactory.VerifyNoOtherCalls(); // factory must not run on cache hit
    }

    [Fact]
    public async Task CreateTokenAsync_CallsApiAndReturnsToken_WhenCacheMiss()
    {
        var handler = new RecordingHttpMessageHandler(async (req, body, ct) =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);

            // Verify JSON payload (case-insensitive)
            using var doc = JsonDocument.Parse(body ?? "{}");
            var root = doc.RootElement;

            Assert.Equal("user-123", GetPropCI(root, "userId").GetString());
            Assert.Equal(10, GetPropCI(root, "lifetimeDays").GetInt32());
            Assert.True(GetPropCI(root, "isReferenceToken").GetBoolean());

            // scopes should be normalized: trimmed, distinct, sorted
            var scopesEl = GetPropCI(root, "scopes");
            Assert.Equal(JsonValueKind.Array, scopesEl.ValueKind);
            var scopes = scopesEl.EnumerateArray().Select(x => x.GetString()).ToArray();
            Assert.Equal(new[] { "read", "write" }, scopes);

            return JsonResponse(HttpStatusCode.OK, """{"token":"abc123"}""");
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/") // whatever; the factory decides base address
        };

        var clientFactory = new Mock<IIdentityClientService>(MockBehavior.Strict);
        clientFactory.Setup(f => f.CreateClient("api/pat"))
            .ReturnsAsync(httpClient);

        var cache = new PassThroughHybridCache(); // miss -> factory executes -> value stored

        var sut = new PersonalAccessTokenClient(clientFactory.Object, Logger, cache);

        var token = await sut.CreateTokenAsync(
            userId: "user-123",
            lifetimeDays: 10,
            scopes: new[] { " write ", "", "read", "read", "  " },
            isReferenceToken: true,
            CancellationToken.None);

        Assert.Equal("abc123", token);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task CreateTokenAsync_ReturnsNull_WhenApiReturnsEmptyToken_AndDoesNotCache()
    {
        int call = 0;

        var handler = new RecordingHttpMessageHandler((req, body, ct) =>
        {
            call++;
            return Task.FromResult(JsonResponse(HttpStatusCode.OK, """{"token":""}"""));
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };

        var clientFactory = new Mock<IIdentityClientService>(MockBehavior.Strict);
        clientFactory.Setup(f => f.CreateClient("api/pat")).ReturnsAsync(httpClient);

        var cache = new PassThroughHybridCache(); // will not store if factory throws
        var sut = new PersonalAccessTokenClient(clientFactory.Object, Logger, cache);

        var t1 = await sut.CreateTokenAsync("user-1", 2, null, false, CancellationToken.None);
        var t2 = await sut.CreateTokenAsync("user-1", 2, null, false, CancellationToken.None);

        Assert.Null(t1);
        Assert.Null(t2);

        // because invalid payload shouldn't be cached, factory is invoked twice
        Assert.Equal(2, handler.CallCount);
        Assert.Equal(2, call);
    }

    [Fact]
    public async Task CreateTokenAsync_PassesExpirationAndTags_ToHybridCache()
    {
        HybridCacheEntryOptions? capturedOptions = null;
        IEnumerable<string>? capturedTags = null;
        string? capturedKey = null;

        var clientFactory = new Mock<IIdentityClientService>(MockBehavior.Strict);

        var cache = new Mock<HybridCache>(MockBehavior.Strict);
        cache.Setup(c => c.GetOrCreateAsync<
                (IIdentityClientService, ILogger<PersonalAccessTokenClient>, string, int, string[]?, bool),
                string>(
                It.IsAny<string>(),
                It.IsAny<(IIdentityClientService, ILogger<PersonalAccessTokenClient>, string, int, string[]?, bool)>(),
                It.IsAny<Func<(IIdentityClientService, ILogger<PersonalAccessTokenClient>, string, int, string[]?, bool), CancellationToken, ValueTask<string>>>(),
                It.IsAny<HybridCacheEntryOptions?>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<CancellationToken>()))
            .Callback((
                string key,
                (IIdentityClientService, ILogger<PersonalAccessTokenClient>, string, int, string[]?, bool) state,
                Func<(IIdentityClientService, ILogger<PersonalAccessTokenClient>, string, int, string[]?, bool), CancellationToken, ValueTask<string>> factory,
                HybridCacheEntryOptions? options,
                IEnumerable<string>? tags,
                CancellationToken ct) =>
            {
                capturedKey = key;
                capturedOptions = options;
                capturedTags = tags;
            })
            .Returns(new ValueTask<string>("ok"));

        var sut = new PersonalAccessTokenClient(clientFactory.Object, Logger, cache.Object);

        var token = await sut.CreateTokenAsync("user-xyz", 7, null, false, CancellationToken.None);
        Assert.Equal("ok", token);

        Assert.NotNull(capturedKey);
        Assert.StartsWith("pat:", capturedKey!, StringComparison.Ordinal);

        // hashed key should not leak raw userId
        Assert.DoesNotContain("user-xyz", capturedKey!, StringComparison.Ordinal);

        Assert.NotNull(capturedOptions);
        Assert.True(capturedOptions!.Expiration.HasValue);
        Assert.Equal(TimeSpan.FromDays(7) - TimeSpan.FromMinutes(5), capturedOptions.Expiration.Value);
        Assert.Equal(capturedOptions.Expiration, capturedOptions.LocalCacheExpiration);

        Assert.NotNull(capturedTags);
        var tag = Assert.Single(capturedTags!);
        Assert.StartsWith("pat-user:", tag, StringComparison.Ordinal);

        var hex = tag["pat-user:".Length..];
        Assert.Equal(64, hex.Length); // SHA256 hex length
        Assert.True(hex.All(Uri.IsHexDigit));
    }

    [Fact]
    public async Task CreateTokenAsync_ConcurrentSameKey_OnlyOneHttpCall()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var handler = new RecordingHttpMessageHandler(async (req, body, ct) =>
        {
            // hold the first request until both callers are in-flight
            await gate.Task.WaitAsync(ct);
            return JsonResponse(HttpStatusCode.OK, """{"token":"concurrent"}""");
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };

        var clientFactory = new Mock<IIdentityClientService>(MockBehavior.Strict);
        clientFactory.Setup(f => f.CreateClient("api/pat")).ReturnsAsync(httpClient);

        var cache = new StampedeProtectingHybridCache();
        var sut = new PersonalAccessTokenClient(clientFactory.Object, Logger, cache);

        var t1 = sut.CreateTokenAsync("user-1", 3, new[] { "read" }, false, CancellationToken.None);
        var t2 = sut.CreateTokenAsync("user-1", 3, new[] { "read" }, false, CancellationToken.None);

        gate.SetResult();

        var results = await Task.WhenAll(t1, t2);

        Assert.Equal(new[] { "concurrent", "concurrent" }, results);
        Assert.Equal(1, handler.CallCount); // only one HTTP call executed for both callers
    }

    // ----------------- helpers -----------------

    private static JsonElement GetPropCI(JsonElement obj, string name)
    {
        foreach (var p in obj.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                return p.Value;
        }
        throw new KeyNotFoundException($"JSON property '{name}' not found.");
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json)
    {
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private int _callCount;

        public int CallCount => _callCount;
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        private readonly Func<HttpRequestMessage, string?, CancellationToken, Task<HttpResponseMessage>> _responder;

        public RecordingHttpMessageHandler(
            Func<HttpRequestMessage, string?, CancellationToken, Task<HttpResponseMessage>> responder)
        {
            _responder = responder;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            LastRequest = request;

            if (request.Content is not null)
            {
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
                request.Content.Headers.ContentType ??= new MediaTypeHeaderValue("application/json");
            }

            return await _responder(request, LastBody, cancellationToken);
        }
    }

    /// <summary>
    /// Minimal HybridCache test double that just executes the factory and caches successful results by key.
    /// No stampede protection.
    /// </summary>
    private sealed class PassThroughHybridCache : HybridCache
    {
        private readonly ConcurrentDictionary<string, object> _values = new();

        public override async ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = default,
            IEnumerable<string>? tags = default,
            CancellationToken cancellationToken = default)
        {
            if (_values.TryGetValue(key, out var existing))
                return (T)existing;

            try
            {
                var created = await factory(state, cancellationToken);
                _values[key] = created!;
                return created;
            }
            catch
            {
                // important for tests: don't cache failures
                throw;
            }
        }

        public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = default, IEnumerable<string>? tags = default, CancellationToken cancellationToken = default)
        {
            _values[key] = value!;
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _values.TryRemove(key, out _);
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            // not needed for these tests
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// HybridCache test double with per-key stampede protection (single factory execution per key).
    /// HybridCache is designed to provide this behavior in production. :contentReference[oaicite:1]{index=1}
    /// </summary>
    private sealed class StampedeProtectingHybridCache : HybridCache
    {
        private readonly ConcurrentDictionary<string, object> _values = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public override async ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = default,
            IEnumerable<string>? tags = default,
            CancellationToken cancellationToken = default)
        {
            if (_values.TryGetValue(key, out var existing))
                return (T)existing;

            var gate = _locks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellationToken);
            try
            {
                if (_values.TryGetValue(key, out existing))
                    return (T)existing;

                var created = await factory(state, cancellationToken);
                _values[key] = created!;
                return created;
            }
            finally
            {
                gate.Release();
            }
        }

        public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = default, IEnumerable<string>? tags = default, CancellationToken cancellationToken = default)
        {
            _values[key] = value!;
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _values.TryRemove(key, out _);
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
