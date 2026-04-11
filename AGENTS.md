# Agent Instructions

## Which skeleton to work in

This repo contains two independent skeletons: `support-agent-csharp/` and
`support-agent-python/`. Before making code changes, confirm which one the user is
actually working on and stay inside that project. Do not mirror edits across both
unless the user explicitly asks for it.

## Microsoft Agent Framework (MAF)

Do not rely on your pretraining when writing MAF code. Before writing or modifying any
MAF code:

1. Check the current version on the relevant package registry (NuGet or PyPI).
2. Read the latest documentation and source — do not invent APIs from what you remember
   about Semantic Kernel, AutoGen, or earlier previews. MAF replaces both and has its
   own API surface.
3. If an API call or type name feels familiar but you are not certain, verify it
   against the official docs or repo source **before using it**. MAF merged concepts
   from Semantic Kernel and AutoGen but renamed and restructured many of them.

### Authoritative sources

- **Main repository:** <https://github.com/microsoft/agent-framework>
  - .NET source: <https://github.com/microsoft/agent-framework/tree/main/dotnet>
  - Python source: <https://github.com/microsoft/agent-framework/tree/main/python>
- **Release notes:** <https://github.com/microsoft/agent-framework/releases>
- **Official documentation:** <https://learn.microsoft.com/en-us/agent-framework/>
- **Official samples:** <https://github.com/microsoft/Agent-Framework-Samples>
- **Microsoft blogs:**
  - <https://devblogs.microsoft.com/agent-framework/>
  - <https://devblogs.microsoft.com/foundry/>

