# Backend AI and MAF Architecture

This document defines the backend architecture for CodeCafe's AI-native
workspace features.

The core rule is:

> CodeCafe builds all low-level AI capability on Microsoft Agent Framework
> (MAF), but CodeCafe domain, application, and API contracts do not expose MAF
> types directly.

MAF is the AI runtime. CodeCafe is the product model.

## Product Model

The workspace is the primary boundary for AI, memory, execution, code, notes,
and activity. Most backend capabilities should be scoped by `WorkspaceId`.

Expected product areas:

- Workspaces: project container, repository binding, branch, status, settings.
- Memory: decisions, architecture, topics, summaries, and durable context.
- Conversations: chat sessions with workspace context.
- Code: repository tree, file content, code insights, suggested improvements.
- Runs: preview environments, run history, steps, logs, reruns, health.
- Notes: authored knowledge, tags, pinned notes, AI summaries and connections.
- Tasks: planned work, status, AI-suggested tasks, run/code/memory links.
- Activity: unified audit trail for important workspace events.

## Layering Rule

The Clean Architecture dependency direction remains:

```text
CodeCafe.Domain
  <- CodeCafe.Application
      <- CodeCafe.Infrastructure
      <- CodeCafe.Api
```

`CodeCafe.Contracts` remains an API boundary contract project and should not
contain MAF types.

MAF references belong in infrastructure adapter code only. If a future feature
needs MAF concepts such as agents, sessions, tools, context providers, workflow
runs, or streaming updates, the application layer defines CodeCafe-owned
abstractions and infrastructure adapts those abstractions to MAF.

## Project Responsibilities

### CodeCafe.Domain

Owns CodeCafe business concepts and invariants.

Candidate modules:

- `Workspaces`
  - `Workspace`
  - `WorkspaceRepositoryBinding`
  - `WorkspaceStatus`
- `Memory`
  - `MemoryItem`
  - `MemoryItemKind`
  - `MemorySource`
- `Conversations`
  - `Conversation`
  - `ConversationMessage`
  - `ConversationStatus`
- `Runs`
  - `Run`
  - `RunEnvironment`
  - `RunStep`
  - `RunStatus`
- `Tasks`
  - `WorkspaceTask`
  - `TaskStatus`
- `Activity`
  - `ActivityEvent`
  - `ActivityEventKind`
- `Notes`
  - existing notes concepts should move here over time when the notes model is
    made persistent and workspace-scoped.

Domain must not reference:

- `Microsoft.Agents.*`
- `Microsoft.Extensions.AI`
- ASP.NET Core types
- provider SDKs
- file system or GitHub implementation details

### CodeCafe.Application

Owns use cases, ports, and orchestration expressed in CodeCafe terms.

Candidate services:

- `IWorkspaceService`
  - list, create, open, update settings, repository binding.
- `IWorkspaceContextService`
  - build the current workspace context summary for UI and AI.
- `IConversationService`
  - create conversation, list conversations, send message, stream response.
- `IAgentRuntime`
  - CodeCafe-owned abstraction for running AI agents.
- `IAgentSessionStore`
  - persist serialized agent session state without exposing MAF session types.
- `IAgentToolCatalog`
  - list tools available to a workspace and agent profile.
- `IMemoryService`
  - add, search, summarize, promote, and retire memory items.
- `ICodeWorkspaceService`
  - repository tree, file content, file search, code metadata.
- `IRunService`
  - create run, stream logs, list history, rerun, inspect steps.
- `IActivityService`
  - append and query workspace activity events.

Application-facing AI models should be CodeCafe-owned:

- `AgentProfile`
- `AgentConversation`
- `AgentMessage`
- `AgentRunRequest`
- `AgentRunUpdate`
- `AgentRunResult`
- `AgentToolDescriptor`
- `AgentSessionSnapshot`

These models may map closely to MAF, but they should not be aliases for MAF.

### CodeCafe.Infrastructure

Owns implementation details.

MAF integration should live under a dedicated namespace:

```text
CodeCafe.Infrastructure.AI.Maf
```

Expected adapters:

- `MafAgentRuntime : IAgentRuntime`
  - creates or resolves MAF `AIAgent` instances.
  - maps CodeCafe messages to MAF chat messages.
  - maps MAF streaming updates to `AgentRunUpdate`.
  - serializes and deserializes MAF sessions through `IAgentSessionStore`.
- `MafAgentFactory`
  - builds agent profiles using MAF `ChatClientAgentOptions`.
  - wires tools, context providers, history providers, and workflows.
- `WorkspaceMemoryContextProvider`
  - MAF context provider backed by CodeCafe memory APIs.
- `WorkspaceNotesContextProvider`
  - MAF context provider backed by notes APIs.
- `WorkspaceCodeContextProvider`
  - MAF context provider backed by repository/code APIs.
- `WorkspaceToolsProvider`
  - exposes CodeCafe actions as MAF tools.
- `MafWorkflowRuntime`
  - adapts MAF workflow execution to CodeCafe run/workflow concepts.

Provider SDKs also belong here:

- OpenAI or Azure OpenAI clients.
- GitHub clients.
- file system repository readers.
- preview environment/deployment runners.
- persistent stores.

### CodeCafe.Api

Owns HTTP transport, authentication, authorization, request validation, and DTO
mapping.

The API should expose CodeCafe contracts only.

Candidate endpoint groups:

- `/api/workspaces`
- `/api/workspaces/{workspaceId}/overview`
- `/api/workspaces/{workspaceId}/memory`
- `/api/workspaces/{workspaceId}/conversations`
- `/api/workspaces/{workspaceId}/conversations/{conversationId}/messages`
- `/api/workspaces/{workspaceId}/code/tree`
- `/api/workspaces/{workspaceId}/code/files`
- `/api/workspaces/{workspaceId}/runs`
- `/api/workspaces/{workspaceId}/notes`
- `/api/workspaces/{workspaceId}/activity`

Streaming agent responses can start with Server-Sent Events because they fit the
chat and run log model well. SignalR can be introduced later if bidirectional
collaboration becomes necessary.

## AI Runtime Design

### Agent Profiles

Agent behavior should be configured through CodeCafe profiles.

Initial profile examples:

- `workspace-assistant`
  - answers questions using workspace memory, tasks, notes, repo metadata, and
    recent activity.
- `code-assistant`
  - explains files, suggests improvements, and references repository context.
- `notes-assistant`
  - summarizes, expands, tags, and connects notes.
- `run-assistant`
  - explains failures, watches logs, and suggests remediation.

Profiles are resolved by CodeCafe and implemented by MAF. UI and API callers
should not need to know whether a profile is a chat agent, workflow agent, or
multi-agent orchestration.

### Sessions

Conversation state should be persistent and workspace-scoped.

CodeCafe should store:

- conversation metadata.
- user and assistant messages.
- serialized MAF session state as opaque JSON.
- agent profile id and version.
- workspace id.
- linked files, notes, memory items, tasks, and runs.

The serialized MAF session state is infrastructure data. Application code should
treat it as an opaque snapshot.

### Context

Workspace context should be assembled in layers:

1. workspace metadata.
2. memory summary.
3. relevant tasks.
4. relevant notes.
5. relevant files and code symbols.
6. recent runs and activity.
7. explicit user attachments or mentions.

Application services decide what context is available. MAF context providers
perform the runtime injection into agent calls.

### Tools

Tools should be CodeCafe-owned operations exposed to MAF through infrastructure
adapters.

Candidate tool groups:

- Memory tools:
  - search memory.
  - add decision.
  - summarize workspace memory.
- Notes tools:
  - search notes.
  - read note.
  - draft note.
  - suggest tags.
- Code tools:
  - list files.
  - read file.
  - search repository.
  - explain file.
- Task tools:
  - create task.
  - update task status.
  - link task to memory, note, run, or file.
- Run tools:
  - start preview run.
  - read run logs.
  - rerun.
  - inspect deployment step.

Tools that mutate state should pass through application services so permissions,
validation, and activity logging stay consistent.

### Workflows

MAF workflows should back larger AI operations, but CodeCafe should expose them
as CodeCafe workflow or run concepts.

Initial workflow candidates:

- workspace onboarding:
  - inspect repo, identify stack, create initial memory, suggest tasks.
- memory refresh:
  - summarize recent notes, runs, activity, and code changes.
- code review:
  - inspect files, produce suggestions, link findings to tasks.
- run failure diagnosis:
  - inspect logs and recent changes, propose next actions.

## Milestone Shape

The first backend milestone should create the right platform shape without
overbuilding every feature.

### Milestone 1: AI Platform Skeleton

- Add workspace-scoped domain and application abstractions.
- Add CodeCafe-owned agent runtime interfaces.
- Add infrastructure MAF adapter namespace.
- Add configuration options for provider selection and agent profiles.
- Add persistent conversation/session storage interface.
- Add API contracts for workspace chat and streaming updates.
- Implement one working `workspace-assistant` using MAF.
- Add activity events for conversation creation and agent responses.
- Keep notes integration read-only unless the notes model is intentionally
  refactored in the same milestone.

### Milestone 2: Workspace Context and Memory

- Add persistent workspace memory.
- Add memory search and summary.
- Add memory context provider for MAF.
- Add memory tools for controlled writes.
- Surface relevant memory in chat responses and overview APIs.

### Milestone 3: Code and Run Intelligence

- Add repository tree and file content services.
- Add code context provider and code tools.
- Add run entities, run logs, and run status.
- Add run assistant workflow for failure diagnosis.

### Milestone 4: Workflow Orchestration

- Add CodeCafe workflow definitions and run tracking.
- Use MAF workflows for multi-step AI operations.
- Add workflow run events to activity.
- Support retry, resume, and cancellation.

## Non-Goals

- Do not expose MAF types in API contracts.
- Do not put MAF references in Domain.
- Do not let notes, tasks, runs, or memory depend on MAF.
- Do not make the first integration depend on the frontend implementation.
- Do not implement broad mutation tools before permissions and audit events are
  in place.

## Open Decisions

- Primary AI provider for the first working adapter: OpenAI, Azure OpenAI, or
  configurable provider abstraction.
- Persistent store for conversations, serialized sessions, memory, and runs.
- Streaming transport: SSE first, SignalR later, or SignalR from the start.
- Whether workflow definitions are code-first, database-backed, or file-backed.
- How much GitHub integration belongs in the first milestone.
