namespace CodeCafe.Application.Ai;

// The relay software in front of the model, if any. Generic means a first-party endpoint or an
// unknown relay; named relays get a compatibility adapter bundling their known quirks (see
// Infrastructure/Ai). Application code always speaks canonical API shapes regardless.
public enum AiRelayKind
{
    Generic,
    NewApi,
}
