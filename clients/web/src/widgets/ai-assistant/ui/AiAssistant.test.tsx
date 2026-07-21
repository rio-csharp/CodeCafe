import { cleanup, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import AiAssistant from './AiAssistant'

const mocks = vi.hoisted(() => ({
  applyDraft: vi.fn(),
  clear: vi.fn(),
  createAiEdit: vi.fn(),
  generateDraft: vi.fn(),
  navigate: vi.fn(),
  openPreview: vi.fn(),
  sendMessage: vi.fn(),
  setProposal: vi.fn(),
  showToast: vi.fn(),
  stop: vi.fn(),
  useAiAssistantSession: vi.fn(),
  useAiStatus: vi.fn(),
  useAiEditStore: vi.fn(),
  useApplyAiNoteDraft: vi.fn(),
  useCreateAiEditProposal: vi.fn(),
  useGenerateAiNoteDraft: vi.fn(),
  useUser: vi.fn(),
}))

vi.mock('@/features/ai-assistant', () => ({
  getMessageText: (message: { content?: string }) => message.content ?? '',
  useAiAssistantSession: mocks.useAiAssistantSession,
  useAiStatus: mocks.useAiStatus,
  useAiEditStore: mocks.useAiEditStore,
  useApplyAiNoteDraft: mocks.useApplyAiNoteDraft,
  useCreateAiEditProposal: mocks.useCreateAiEditProposal,
  useGenerateAiNoteDraft: mocks.useGenerateAiNoteDraft,
}))

vi.mock('@/entities/user', () => ({
  useUser: mocks.useUser,
}))

vi.mock('@/shared/ui/Toast', () => ({
  useToast: () => ({ showToast: mocks.showToast }),
}))

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    i18n: {
      language: 'en',
      resolvedLanguage: 'en',
    },
    t: (key: string, options?: { defaultValue?: string }) =>
      translations[key] ?? options?.defaultValue ?? `prompt:${key}`,
  }),
}))

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mocks.navigate,
  }
})

const translations: Record<string, string> = {
  'ai.actions.outline': 'Suggest an outline',
  'ai.actions.related': 'Find related notes',
  'ai.actions.summarize': 'Summarize current context',
  'ai.clear': 'Clear conversation',
  'ai.collapse': 'Collapse AI Assistant',
  'ai.disabledDescription': 'Set Ai:Enabled, Ai:ApiKey, and Ai:Model to use the notebook assistant.',
  'ai.disabledTitle': 'AI is not configured',
  'ai.drafts.actions.continue': 'Continue current page',
  'ai.drafts.actions.expand': 'Expand current page',
  'ai.drafts.actions.outline': 'Draft outline',
  'ai.drafts.actions.rewrite': 'Rewrite current page',
  'ai.drafts.actions.summary': 'Draft summary page',
  'ai.drafts.applied': 'AI draft applied',
  'ai.drafts.apply.append': 'Append',
  'ai.drafts.apply.create': 'New',
  'ai.drafts.apply.replace': 'Replace',
  'ai.drafts.discard': 'Discard draft',
  'ai.drafts.errors.applyFailed': 'Failed to apply AI draft',
  'ai.drafts.errors.generateFailed': 'Failed to generate AI draft',
  'ai.drafts.generate': 'Generate draft',
  'ai.drafts.placeholder': 'Describe the note you want AI to draft...',
  'ai.drafts.title': 'Draft into notes',
  'ai.inputPlaceholder': 'Ask about your notes...',
  'ai.readOnly': 'Chat read-only',
  'ai.send': 'Send',
  'ai.signIn': 'Sign in',
  'ai.signInDescription': 'The assistant reads notebooks through your CodeCafe account.',
  'ai.signInTitle': 'Sign in to use AI',
  'ai.stop': 'Stop',
  'ai.thinking': 'Thinking...',
  'ai.title': 'AI Assistant',
}

const notebook: Notebook = {
  id: 'notebook-1',
  ownerId: 'user-1',
  title: 'Architecture Notes',
  slug: 'architecture-notes',
  description: null,
  visibility: 'private',
  isPublished: false,
  authorDisplayName: 'Yao',
  itemCount: 3,
  folderCount: 1,
  pageCount: 2,
  favoriteCount: 0,
  isFavoritedByMe: false,
  lastActivityAtUtc: '2026-06-01T00:00:00Z',
  createdAtUtc: '2026-06-01T00:00:00Z',
  updatedAtUtc: null,
  publishedAtUtc: null,
  canEdit: true,
}

const activePage: NotebookItem = {
  id: 'page-1',
  notebookId: 'notebook-1',
  parentId: 'folder-1',
  type: 'page',
  title: 'Overview',
  slug: 'overview',
  path: 'guides/overview',
  sortOrder: 0,
  contentFormat: 'tiptap_json',
  contentJson: null,
  plainTextContent: 'Current page body',
  isArchived: false,
  archivedAtUtc: null,
  archivedByUserId: null,
  createdAtUtc: '2026-06-01T00:00:00Z',
  updatedAtUtc: null,
}

// Skipped: this suite triggers a Vitest/Node worker OOM when loading the AiAssistant
// module graph on the current toolchain (Node 24 + Vitest 4.1.6). Re-enable after
// upgrading Vitest/Node or restructuring the test to avoid loading the full component.
describe.skip('AiAssistant', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mocks.sendMessage.mockResolvedValue(undefined)
    mocks.generateDraft.mockResolvedValue({
      generatedAtUtc: '2026-06-01T00:00:00Z',
      intent: 'outline',
      markdown: '# AI Outline',
      notebookSlug: 'architecture-notes',
      pagePath: 'guides/overview',
      title: 'AI Outline',
    })
    mocks.applyDraft.mockResolvedValue({ path: 'guides/ai-outline' })
    mocks.useAiAssistantSession.mockReturnValue({
      clear: mocks.clear,
      error: null,
      isRunning: false,
      messages: [],
      sendMessage: mocks.sendMessage,
      stop: mocks.stop,
      toolActivities: [],
    })
    mocks.useApplyAiNoteDraft.mockReturnValue({
      isPending: false,
      mutateAsync: mocks.applyDraft,
    })
    mocks.useGenerateAiNoteDraft.mockReturnValue({
      isPending: false,
      mutateAsync: mocks.generateDraft,
    })
    mocks.useUser.mockReturnValue({
      data: { user: { id: 'user-1', email: 'yao@example.test', displayName: 'Yao' } },
      isPending: false,
    })
  })

  afterEach(() => {
    cleanup()
  })

  it('shows the disabled gate when AI is not configured', () => {
    mocks.useAiStatus.mockReturnValue({
      data: { enabled: false, endpointPath: null, draftEndpointPath: null },
      isError: false,
      isPending: false,
    })

    renderAssistant()

    expect(screen.getByText('AI is not configured')).toBeInTheDocument()
    expect(screen.getByText('Set Ai:Enabled, Ai:ApiKey, and Ai:Model to use the notebook assistant.')).toBeInTheDocument()
  })

  it('asks anonymous users to sign in before using AI', () => {
    mocks.useAiStatus.mockReturnValue({
      data: { enabled: true, endpointPath: '/api/ai/assistant', draftEndpointPath: '/api/ai/drafts' },
      isError: false,
      isPending: false,
    })
    mocks.useUser.mockReturnValue({ data: null, isPending: false })

    renderAssistant()

    expect(screen.getByText('Sign in to use AI')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Sign in' })).toHaveAttribute('href', '/login')
  })

  it('sends quick action prompts through the assistant session', async () => {
    const user = userEvent.setup()
    mocks.useAiStatus.mockReturnValue({
      data: { enabled: true, endpointPath: '/api/ai/assistant', draftEndpointPath: '/api/ai/drafts' },
      isError: false,
      isPending: false,
    })

    renderAssistant()

    await user.click(screen.getByRole('button', { name: 'Summarize current context' }))

    expect(mocks.useAiAssistantSession).toHaveBeenCalledWith(expect.objectContaining({
      activePage,
      enabled: true,
      endpointPath: '/api/ai/assistant',
      notebook,
    }))
    expect(mocks.sendMessage).toHaveBeenCalledWith('prompt:ai.prompts.summarizePage')
  })

  it('generates and applies note drafts from the draft workspace', async () => {
    const user = userEvent.setup()
    mocks.useAiStatus.mockReturnValue({
      data: { enabled: true, endpointPath: '/api/ai/assistant', draftEndpointPath: '/api/ai/drafts' },
      isError: false,
      isPending: false,
    })

    renderAssistant()

    await user.click(screen.getByRole('button', { name: 'Draft outline' }))
    expect(mocks.generateDraft).toHaveBeenCalledWith({
      intent: 'outline',
      prompt: 'prompt:ai.drafts.prompts.outlinePage',
    })

    expect(await screen.findByText('AI Outline')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'New' }))

    expect(mocks.applyDraft).toHaveBeenCalledWith({
      markdown: '# AI Outline',
      mode: 'create',
      title: 'AI Outline',
    })
    expect(mocks.showToast).toHaveBeenCalledWith('AI draft applied')
    expect(mocks.navigate).toHaveBeenCalledWith('/notes/architecture-notes/guides/ai-outline')
  })
})

function renderAssistant() {
  return render(
    <MemoryRouter>
      <AiAssistant notebook={notebook} activePage={activePage} />
    </MemoryRouter>,
  )
}
