# Geek Academy workshop Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-12

## Active Technologies

- C# on .NET 10 (`net10.0`) + `Azure.AI.OpenAI` 2.1.0, `Microsoft.Agents.AI.OpenAI` 1.1.0, `Microsoft.Agents.AI.Workflows` 1.1.0, `Microsoft.Extensions.AI`, existing configuration packages (001-support-request-flow)

## Project Structure

```text
support-agent-csharp/
specs/
```

## Commands

- `dotnet build support-agent-csharp/support-agent-csharp.csproj`
- `dotnet run --project support-agent-csharp/support-agent-csharp.csproj`

## Code Style

C# on .NET 10 (`net10.0`): Follow standard conventions

## Recent Changes

- 001-support-request-flow: Planned a hybrid Azure OpenAI + MAF workflow in `support-agent-csharp/` with deterministic policy gating and AI-based intake/drafting

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
