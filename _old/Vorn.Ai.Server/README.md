# Vorn.Ai.Server

## AI Provider Configuration

`Vorn.Ai.Server` supports multiple AI providers for chat and response generation. Configure the provider in `appsettings.json` under the `Vorn:Ai` section:

### OpenAI Configuration (Default)

```json
{
  "Vorn": {
    "Ai": {
      "Provider": "OpenAi",
      "ApiKey": "<openai-api-key>",
      "ResponseModel": "gpt-5-nano-2025-08-07",
      "TranscriptionModel": "gpt-4o-mini-transcribe"
    }
  }
}
```

### Ollama Configuration

```json
{
  "Vorn": {
    "Ai": {
      "Provider": "Ollama",
      "Ollama": {
        "BaseUrl": "http://localhost:11434",
        "DefaultModel": "llama3.2",
        "DefaultTranscriberModel": "whisper"
      }
    }
  }
}
```

* **Provider**: Set to `OpenAi` or `Ollama` to choose the AI provider (default: `OpenAi`)
* **Ollama.BaseUrl**: The URL of your Ollama server (default: `http://localhost:11434`)
* **Ollama.DefaultModel**: The default model to use when none is specified for chat responses (default: `llama3.2`)
* **Ollama.DefaultTranscriberModel**: The default speech-to-text model to use when clients do not specify one (default: `whisper`)

## Google Speech-to-Text configuration

`Vorn.Ai.Server` can dispatch audio transcription requests to Google Cloud Speech alongside OpenAI. Configure the Google client in `appsettings.json` (or another configuration provider) under the `Vorn:Ai:GoogleSpeech` section:

```json
{
  "Vorn": {
    "Ai": {
      "ApiKey": "<openai-api-key>",
      "TranscriptionModel": "gpt-4o-mini-transcribe",
      "GoogleSpeech": {
        "ProjectId": "my-gcp-project",
        "Location": "us-central1",
        "RecognizerId": "my-recognizer",
        "CredentialsPath": "/secrets/google-service-account.json",
        "DefaultLanguageCode": "en-US",
        "DefaultModelId": "long",
        "SupportedModels": ["long", "medical_dictation"]
      }
    }
  }
}
```

* **ProjectId**/**Location** identify the Google Cloud project and Speech region that hosts the recognizer.
* **RecognizerId** should point to an existing Speech V2 recognizer resource.
* **CredentialsPath** is optional when the environment already exposes `GOOGLE_APPLICATION_CREDENTIALS`.
* **DefaultModelId** is used when clients do not specify a model. Add any additional model identifiers you want to expose to clients to `SupportedModels` (for example `medical_dictation`).

Clients can request a specific Google model by setting `TranscriberOptions.ModelId` before starting a transcription session. When no model is supplied, the dispatcher falls back to the OpenAI model configured in `Vorn:Ai:TranscriptionModel`.
