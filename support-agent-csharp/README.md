# Support Agent C#

This project implements the `001-support-request-flow` feature as a Microsoft Agent Framework workflow over Azure OpenAI.

## Configuration

Prerequisite: install the .NET 10 SDK.

1. Copy `appsettings.json` to `appsettings.Development.json`.
	On macOS/Linux: `cp appsettings.json appsettings.Development.json`
	In PowerShell: `Copy-Item appsettings.json appsettings.Development.json`
2. Fill in your Azure OpenAI endpoint, API key, and deployment name.
3. Keep the config keys in the current shape:

```json
{
	"ModelName": "gpt-4.1",
	"Endpoint": "https://your-resource.openai.azure.com/",
	"ApiKey": "your-api-key"
}
```

## Run

```sh
dotnet build support-agent-csharp.csproj
dotnet run --project support-agent-csharp.csproj
```

Paste one customer message and finish the input with `---`.

## Implemented Flow

1. Normalize the request and detect deterministic handbook-relevant signals.
2. Run AI intake classification with structured output.
3. Apply deterministic handbook routing rules.
4. Draft customer-facing text with AI where needed.
5. Simulate clarification, refund/cancellation, or escalation artifacts locally.
6. Assemble the final `SupportRequestResult` and render any artifact preview.

## Primary Scenarios

1. API access on Basic -> direct reply
2. First-month refund -> refund handling
3. Ambiguous billing confusion -> clarification email
4. Manager request or chargeback threat -> escalation
5. Cross-account disclosure request -> refusal without sharing information
