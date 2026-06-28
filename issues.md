![security-high](https://www.gstatic.com/codereviewagent/security-high-priority.svg) ![high](https://www.gstatic.com/codereviewagent/high-priority.svg)

Using `AddSlidingWindowLimiter` directly registers a global rate limiter for the specified policy name. This means the limit of 10 (or 5) requests per minute is shared globally across all users and IP addresses. If a single user or an automated scanner triggers this limit, all other users will be blocked from logging in, creating a severe Denial of Service (DoS) vulnerability.

To fix this, use `AddPolicy` and partition the rate limiter by a client identifier (such as the remote IP address).

```c#
            _ = options.AddPolicy(AuthenticationRateLimitPolicies.PasswordLogin, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        SegmentsPerWindow = 6
                    }));

            _ = options.AddPolicy(AuthenticationRateLimitPolicies.OtpBegin, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 5,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        SegmentsPerWindow = 6
                    }));

            _ = options.AddPolicy(AuthenticationRateLimitPolicies.OtpComplete, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        SegmentsPerWindow = 6
                    }));

            _ = options.AddPolicy(AuthenticationRateLimitPolicies.Passkey, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        PermitLimit = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        SegmentsPerWindow = 6
                    }));
```

![security-high](https://www.gstatic.com/codereviewagent/security-high-priority.svg) ![high](https://www.gstatic.com/codereviewagent/high-priority.svg)

Using `stackalloc` with an unbounded user-supplied input length (`code.Length`) can lead to a `StackOverflowException` if an attacker sends an extremely long string. Since stack overflow exceptions cannot be caught, this will crash the entire application process, leading to a Denial of Service (DoS) vulnerability.

To mitigate this, add a length check to reject or skip processing for excessively long codes, or only use `stackalloc` when the length is within a safe threshold (e.g., <= 128 characters).

```c#
    private static string NormalizeVerificationCode(string code)
    {
        if (code.Length > 128)
        {
            return code;
        }

        Span<char> normalized = stackalloc char[code.Length];
        for (int index = 0; index < code.Length; index++)
        {
            normalized[index] = code[index] switch
            {
                >= '۰' and <= '۹' => (char)('0' + code[index] - '۰'),
                >= '٠' and <= '٩' => (char)('0' + code[index] - '٠'),
                _ => code[index]
            };
        }

        return new string(normalized);
    }
```


![medium](https://www.gstatic.com/codereviewagent/medium-priority.svg)

If the `try` block fails because the request was cancelled (meaning `cancellationToken` is already triggered), passing `cancellationToken` to `store.ReplaceAsync` will cause it to immediately throw a `TaskCanceledException` and fail. As a result, the challenge will never be updated to `Cancelled` in the database, leaving an active/pending challenge that could potentially be abused later.

To ensure the cancellation state is reliably persisted even when the request is cancelled, use `CancellationToken.None` (or a separate token with a short timeout) for the database update.

```c#
        catch
        {
            challenge.Cancel();
            await store.ReplaceAsync(challenge, CancellationToken.None);
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, "Failed to send verification code."));
        }
```


![medium](https://www.gstatic.com/codereviewagent/medium-priority.svg)

If the `try` block fails because the request was cancelled (meaning `cancellationToken` is already triggered), passing `cancellationToken` to `store.ReplaceAsync` will cause it to immediately throw a `TaskCanceledException` and fail. As a result, the challenge will never be updated to `Cancelled` in the database, leaving an active/pending challenge that could potentially be abused later.

To ensure the cancellation state is reliably persisted even when the request is cancelled, use `CancellationToken.None` (or a separate token with a short timeout) for the database update.

```c#
        catch
        {
            challenge.Cancel();
            await store.ReplaceAsync(challenge, CancellationToken.None);
            return new AuthenticationResult<ChallengeResult>.Failure(new(AuthenticationFailureReason.ProviderFailure, "Failed to send verification code."));
        }
```