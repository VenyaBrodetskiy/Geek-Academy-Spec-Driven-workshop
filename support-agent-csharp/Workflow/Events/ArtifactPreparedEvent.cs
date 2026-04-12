using Microsoft.Agents.AI.Workflows;
using SupportAgent.Models;

namespace SupportAgent.Workflow.Events;

internal sealed class ArtifactPreparedEvent(SimulatedArtifact artifact) : WorkflowEvent(artifact)
{
    public SimulatedArtifact Artifact { get; } = artifact;
}