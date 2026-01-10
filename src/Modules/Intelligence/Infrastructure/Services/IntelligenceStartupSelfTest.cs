using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Infrastructure.Services.Dispatching;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vitarish.Intelligence.Infrastructure.Services
{
    /// <summary>
    /// Startup self-test for the Intelligence module that validates AI provider readiness at application boot.
    /// Uses WebM instead of WAV for audio, given that the backend supports WebM.  
    /// </summary>
    public sealed class IntelligenceStartupSelfTest : IHostedService
    {
        private readonly IOptionsMonitor<AiOptions> _aiOptions;
        private readonly ModelRegistry _modelRegistry;
        private readonly IResponder _responder;
        private readonly ITranscriber _transcriber;
        private readonly ILogger<IntelligenceStartupSelfTest> _logger;
        private AiOptions CurrentOptions => _aiOptions.CurrentValue;

        private const string SelfTestPrompt = "Say 'ok'";
        private static readonly byte[] DummyAudioData = GenerateDummyWebmData();

        public IntelligenceStartupSelfTest(
            IOptionsMonitor<AiOptions> aiOptions,
            ModelRegistry modelRegistry,
            IResponder responder,
            ITranscriber transcriber,
            ILogger<IntelligenceStartupSelfTest> logger)
        {
            _aiOptions = aiOptions;
            _modelRegistry = modelRegistry;
            _responder = responder;
            _transcriber = transcriber;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting Intelligence module self-test...");

                ValidateDefaultModels();
                //await ProbeResponderAsync(cancellationToken).ConfigureAwait(false);
                //await ProbeTranscriberAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Intelligence module startup self-test completed successfully.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Intelligence module startup self-test failed.");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void ValidateDefaultModels()
        {
            string defaultResponseModel = CurrentOptions.DefaultResponseModel;
            string defaultTranscriptionModel = CurrentOptions.DefaultTranscriptionModel;

            _logger.LogDebug(
                "Validating default models - Response: {ResponseModel}, Transcription: {TranscriptionModel}",
                defaultResponseModel,
                defaultTranscriptionModel);

            string? responseProvider = _modelRegistry.GetProviderForModel(defaultResponseModel);
            if (string.IsNullOrEmpty(responseProvider))
            {
                throw new InvalidOperationException(
                    $"Default response model '{defaultResponseModel}' is not mapped to any known provider in ModelRegistry.");
            }

            _logger.LogDebug("Default response model '{Model}' mapped to provider '{Provider}'", defaultResponseModel, responseProvider);

            string? transcriptionProvider = _modelRegistry.GetProviderForModel(defaultTranscriptionModel);
            if (string.IsNullOrEmpty(transcriptionProvider))
            {
                throw new InvalidOperationException(
                    $"Default transcription model '{defaultTranscriptionModel}' is not mapped to any known provider in ModelRegistry.");
            }

            _logger.LogDebug("Default transcription model '{Model}' mapped to provider '{Provider}'", defaultTranscriptionModel, transcriptionProvider);

            AvailableModels availableModels = _modelRegistry.GetAvailableModels();

            bool responseModelAvailable = availableModels.ResponderModels.Any(m =>
                string.Equals(m.Id, defaultResponseModel, StringComparison.OrdinalIgnoreCase));

            if (!responseModelAvailable)
            {
                _logger.LogWarning(
                    "Default response model '{Model}' exists in registry but provider '{Provider}' may not be configured. " +
                    "The model may not be available at runtime.",
                    defaultResponseModel,
                    responseProvider);
            }

            bool transcriptionModelAvailable = availableModels.TranscriberModels.Any(m =>
                string.Equals(m.Id, defaultTranscriptionModel, StringComparison.OrdinalIgnoreCase));

            if (!transcriptionModelAvailable)
            {
                _logger.LogWarning(
                    "Default transcription model '{Model}' exists in registry but provider '{Provider}' may not be configured. " +
                    "The model may not be available at runtime.",
                    defaultTranscriptionModel,
                    transcriptionProvider);
            }
        }

        private async Task ProbeResponderAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Probing responder with minimal text prompt...");

            try
            {
                ResponderOptions options = new()
                {
                    Model = CurrentOptions.DefaultResponseModel
                };

                IReadOnlyList<ResponderInput> input = new[] { ResponderInput.TextInput(SelfTestPrompt) };

                IReadOnlyList<ResponderOutput> response = await _responder.GenerateAsync(input, options, cancellationToken)
                    .ConfigureAwait(false);

                if (response.Count == 0)
                {
                    throw new InvalidOperationException("Responder returned empty response during self-test probe.");
                }

                _logger.LogDebug("Responder probe completed successfully with {OutputCount} output(s).", response.Count);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Responder probe failed for model '{Model}'.", CurrentOptions.DefaultResponseModel);
                throw;
            }
        }

        private async Task ProbeTranscriberAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Probing transcriber with dummy WebM audio stream...");

            try
            {
                await using MemoryStream audioStream = new(DummyAudioData, writable: false);

                string? result = await _transcriber.TranscribeAsync(
                    audioStream,
                    "audio/webm",
                    language: null,
                    modelId: CurrentOptions.DefaultTranscriptionModel,
                    cancellationToken).ConfigureAwait(false);

                _logger.LogDebug("Transcriber probe completed successfully. Result length: {Length}", result?.Length ?? 0);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transcriber probe failed for model '{Model}'.", CurrentOptions.DefaultTranscriptionModel);
                throw;
            }
        }

        /// <summary>
        /// Generates a minimal valid WebM file with silent audio (or very short audio).
        /// This is a stub — you must fill in a correct WebM container + audio codec header + payload, or embed a small pre-generated WebM resource.
        /// </summary>
        private static byte[] GenerateDummyWebmData()
        {
            return IntelligenceResource.audio_hello_world_webm;
        }
    }
}
