# Geek Academy Workshop — Building Agentic Systems with a Spec-Driven Approach

This repo contains the lab skeletons for the Geek Academy agentic AI workshop.

## Getting Started

1. Pick a skeleton: **C#** (`support-agent-csharp/`) or **Python** (`support-agent-python/`).

  C# requires the .NET 10 SDK.
  Python requires Python 3.10 or newer.

2. Add your Azure OpenAI credentials:

  **C#** — create `support-agent-csharp/appsettings.Development.json`.

   ```json
   {
     "ModelName": "gpt-4o",
     "Endpoint": "https://your-resource.openai.azure.com/",
     "ApiKey": "your-api-key"
   }
   ```

  **Python** — copy `.env.example` to `.env` in `support-agent-python/` and fill in the values.

   ```
   AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
   AZURE_OPENAI_API_KEY=your-api-key
   AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o
   ```

3. Start with [lab1.md](lab1.md).
