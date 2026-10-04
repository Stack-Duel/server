using Microsoft.AspNetCore.WebUtilities;
using StackDuel.Application.Configuration;
using StackDuel.Application.ExecutionEngine;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StackDuel.Infrastructure.ExecutionEngine.Judge0;

internal sealed class Judge0ExecutionEngineStrategy(HttpClient http, Judge0Options options) : IExecutionEngineStrategy
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private const int BatchSize = 20;

    public async Task<IReadOnlyList<ExecutionEngineResult>> SubmitBatchAsync(
        IReadOnlyList<ExecutionEngineSubmission> submissions,
        Guid? submissionId = null,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            List<ExecutionEngineResult> allResults = [];
            string? callbackUrl = BuildCallbackUrl(submissionId);

            foreach (var batch in submissions.Chunk(BatchSize))
            {
                var query = new Dictionary<string, string?>
                {
                    ["base64_encoded"] = options.IsEncoded.ToString().ToLowerInvariant(),
                    ["fields"] = "*",
                };

                string uri = QueryHelpers.AddQueryString("submissions/batch", query);

                var payload = new Judge0BatchRequest
                {
                    Submissions =
                    [
                        .. batch.Select(s => new Judge0SubmissionRequest
                        {
                            SourceCode = Encode(s.SourceCode),
                            LanguageId = s.LanguageId,
                            StdIn = Encode(s.Stdin),
                            AdditionalFiles = s.AdditionalFiles is null
                                ? null
                                : Convert.ToBase64String(s.AdditionalFiles),
                            CpuTimeLimit = s.TimeLimitMs.HasValue ? (double?)s.TimeLimitMs.Value / 1000.0 : null,
                            MemoryLimit = s.MemoryLimitKb,
                            CallbackUrl = callbackUrl,
                        }),
                    ],
                };

                var response = await http.PostAsJsonAsync(uri, payload, SerializerOptions, cancellationToken);
                await EnsureSuccessAsync(response, cancellationToken);

                var body = await response.Content.ReadFromJsonAsync<List<Judge0TokenResponse>>(
                    SerializerOptions,
                    cancellationToken
                );

                if (body is { Count: > 0 })
                {
                    allResults.AddRange(
                        body.Select(t => new ExecutionEngineResult(
                            Token: t.Token,
                            Stdout: null,
                            Stderr: null,
                            CompileOutput: null,
                            RuntimeMs: null,
                            MemoryUsedKb: null,
                            Status: ExecutionEngineResultStatus.Queued
                        ))
                    );
                }
            }

            return allResults;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"HTTP request failed: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON deserialization failed: {ex.Message}", ex);
        }
    }

    public async Task<IReadOnlyList<ExecutionEngineResult>> PollBatchAsync(
        IReadOnlyList<string> tokens,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            List<ExecutionEngineResult> allResults = [];

            foreach (string[] batch in tokens.Chunk(BatchSize))
            {
                var query = new Dictionary<string, string?>
                {
                    ["tokens"] = string.Join(",", batch),
                    ["base64_encoded"] = options.IsEncoded.ToString().ToLowerInvariant(),
                    ["fields"] = "token,stdout,stderr,compile_output,time,memory,status",
                };

                string uri = QueryHelpers.AddQueryString("submissions/batch", query);

                var response = await http.GetAsync(uri, cancellationToken);
                await EnsureSuccessAsync(response, cancellationToken);

                var batchResult = await response.Content.ReadFromJsonAsync<Judge0BatchGetResponse>(
                    SerializerOptions,
                    cancellationToken
                );

                if (batchResult?.Submissions is { Count: > 0 })
                {
                    allResults.AddRange(
                        batchResult.Submissions.Select(s => new ExecutionEngineResult(
                            Token: s.Token,
                            Stdout: Decode(s.Stdout),
                            Stderr: Decode(s.Stderr),
                            CompileOutput: Decode(s.CompileOutput),
                            RuntimeMs: s.Time.HasValue ? (int?)(s.Time.Value * 1000) : null,
                            MemoryUsedKb: s.Memory,
                            Status: MapStatus(s.Status?.Id ?? 0)
                        ))
                    );
                }
            }

            return allResults;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"HTTP request failed: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON deserialization failed: {ex.Message}", ex);
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException($"Judge0 returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
    }

    private string? BuildCallbackUrl(Guid? submissionId)
    {
        if (!options.UseCallback || submissionId is null)
            return null;

        var query = new Dictionary<string, string?>
        {
            ["key"] = options.CallbackSecret,
            ["submissionId"] = submissionId.Value.ToString(),
        };

        return QueryHelpers.AddQueryString($"{options.PublicBaseUrl.TrimEnd('/')}/api/v1/judge0/callback", query);
    }

    private string? Encode(string? value) =>
        value is null ? null
        : options.IsEncoded ? Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
        : value;

    private string? Decode(string? value)
    {
        if (value is null)
            return null;
        if (!options.IsEncoded)
            return value;

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch (FormatException)
        {
            return value;
        }
    }

    private static ExecutionEngineResult MapResult(Judge0SubmissionResponse s) =>
        new(
            Token: s.Token,
            Stdout: s.Stdout,
            Stderr: s.Stderr,
            CompileOutput: s.CompileOutput,
            RuntimeMs: s.Time.HasValue ? (int?)(s.Time.Value * 1000) : null,
            MemoryUsedKb: s.Memory,
            Status: MapStatus(s.Status?.Id ?? 0)
        );

    private static ExecutionEngineResultStatus MapStatus(int id) =>
        id switch
        {
            1 => ExecutionEngineResultStatus.Queued,
            2 => ExecutionEngineResultStatus.Processing,
            3 => ExecutionEngineResultStatus.Accepted,
            4 => ExecutionEngineResultStatus.WrongAnswer,
            5 => ExecutionEngineResultStatus.TimeLimitExceeded,
            6 => ExecutionEngineResultStatus.CompilationError,
            >= 7 and <= 12 => ExecutionEngineResultStatus.RuntimeError,
            _ => ExecutionEngineResultStatus.InternalError,
        };

    private sealed class Judge0BatchRequest
    {
        [JsonPropertyName("submissions")]
        public required IEnumerable<Judge0SubmissionRequest> Submissions { get; init; }
    }

    private sealed class Judge0SubmissionRequest
    {
        [JsonPropertyName("language_id")]
        public int LanguageId { get; init; }

        [JsonPropertyName("source_code")]
        public string? SourceCode { get; init; }

        [JsonPropertyName("stdin")]
        public string? StdIn { get; init; }

        [JsonPropertyName("additional_files")]
        public string? AdditionalFiles { get; init; }

        [JsonPropertyName("expected_output")]
        public string? ExpectedOutput { get; init; }

        [JsonPropertyName("cpu_time_limit")]
        public double? CpuTimeLimit { get; init; }

        [JsonPropertyName("memory_limit")]
        public int? MemoryLimit { get; init; }

        [JsonPropertyName("callback_url")]
        public string? CallbackUrl { get; init; }
    }

    private sealed record Judge0TokenResponse(string Token);

    private sealed class Judge0BatchGetResponse
    {
        public List<Judge0SubmissionResponse> Submissions { get; set; } = [];
    }

    private sealed class Judge0SubmissionResponse
    {
        public string Token { get; set; } = string.Empty;
        public string? Stdout { get; set; }
        public string? Stderr { get; set; }
        public string? CompileOutput { get; set; }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public double? Time { get; set; }
        public int? Memory { get; set; }
        public Judge0StatusResponse? Status { get; set; }
    }

    private sealed class Judge0StatusResponse
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}