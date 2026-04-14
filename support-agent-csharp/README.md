# Support Agent C#

Prerequisite: install the .NET 10 SDK.

1. Copy `appsettings.json` to `appsettings.Development.json`.
	On macOS/Linux: `cp appsettings.json appsettings.Development.json`
	In PowerShell: `Copy-Item appsettings.json appsettings.Development.json`
2. Fill in your Azure OpenAI endpoint, API key, and deployment name.
3. Run:

```sh
dotnet run --project support-agent-csharp.csproj
```
