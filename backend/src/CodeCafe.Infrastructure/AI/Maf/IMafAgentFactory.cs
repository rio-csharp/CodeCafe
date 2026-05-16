using Microsoft.Agents.AI;

namespace CodeCafe.Infrastructure.AI.Maf;

internal interface IMafAgentFactory
{
    AIAgent CreateAgent(string profileId);
}
