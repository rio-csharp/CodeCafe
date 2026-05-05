export type ProviderFormat = 'chat-completions' | 'responses' | 'anthropic' | 'gemini'

export type AiProviderModel = {
  defaultMaxOutputTokens: number
  defaultTemperature: number
  defaultTopP: number
  enabled: boolean
  id: string
  modelId: string
  name: string
  supportsImages: boolean
  supportsStreaming: boolean
}

export type AiProvider = {
  apiKey: string
  baseUrl: string
  enabled: boolean
  formats: ProviderFormat[]
  id: string
  models: AiProviderModel[]
  name: string
  preferredFormat: ProviderFormat
}

export type AiSettings = {
  defaultModelId: string | null
  defaultProviderId: string | null
  providers: AiProvider[]
}

const storageKey = 'codecafe-ai-settings'

export const providerFormatOptions: Array<{
  description: string
  label: string
  value: ProviderFormat
}> = [
  {
    description: 'OpenAI-compatible /v1/chat/completions requests.',
    label: 'Chat Completions',
    value: 'chat-completions',
  },
  {
    description: 'OpenAI-compatible /v1/responses requests.',
    label: 'Responses API',
    value: 'responses',
  },
  {
    description: 'Anthropic Claude-style messages API payloads.',
    label: 'Claude',
    value: 'anthropic',
  },
  {
    description: 'Google Gemini-style generateContent payloads.',
    label: 'Gemini',
    value: 'gemini',
  },
]

export function createProvider(): AiProvider {
  return {
    apiKey: '',
    baseUrl: '',
    enabled: true,
    formats: ['chat-completions'],
    id: crypto.randomUUID(),
    models: [createModel()],
    name: 'New provider',
    preferredFormat: 'chat-completions',
  }
}

export function createModel(): AiProviderModel {
  return {
    defaultMaxOutputTokens: 2048,
    defaultTemperature: 0.7,
    defaultTopP: 1,
    enabled: true,
    id: crypto.randomUUID(),
    modelId: '',
    name: 'New model',
    supportsImages: false,
    supportsStreaming: true,
  }
}

export function getEmptyAiSettings(): AiSettings {
  return {
    defaultModelId: null,
    defaultProviderId: null,
    providers: [],
  }
}

export function loadAiSettings(): AiSettings {
  if (typeof window === 'undefined') {
    return getEmptyAiSettings()
  }

  const rawValue = window.localStorage.getItem(storageKey)

  if (!rawValue) {
    return getEmptyAiSettings()
  }

  try {
    const parsed = JSON.parse(rawValue) as Partial<AiSettings>
    const providers = Array.isArray(parsed.providers)
      ? parsed.providers
          .map(normalizeProvider)
          .filter((provider): provider is AiProvider => provider !== null)
      : []
    const defaultProviderId =
      typeof parsed.defaultProviderId === 'string' ? parsed.defaultProviderId : null
    const defaultModelId =
      typeof parsed.defaultModelId === 'string' ? parsed.defaultModelId : null

    return reconcileAiSettings({
      defaultModelId,
      defaultProviderId,
      providers,
    })
  } catch {
    return getEmptyAiSettings()
  }
}

export function saveAiSettings(settings: AiSettings) {
  if (typeof window === 'undefined') {
    return
  }

  window.localStorage.setItem(storageKey, JSON.stringify(reconcileAiSettings(settings)))
}

export function reconcileAiSettings(settings: AiSettings): AiSettings {
  const enabledProviders = settings.providers.filter((provider) => provider.enabled)
  const defaultProvider =
    enabledProviders.find((provider) => provider.id === settings.defaultProviderId) ??
    enabledProviders[0] ??
    null

  const enabledModels =
    defaultProvider?.models.filter((model) => model.enabled) ??
    []
  const defaultModel =
    enabledModels.find((model) => model.id === settings.defaultModelId) ??
    enabledModels[0] ??
    null

  return {
    defaultModelId: defaultModel?.id ?? null,
    defaultProviderId: defaultProvider?.id ?? null,
    providers: settings.providers,
  }
}

export function getDefaultProvider(settings: AiSettings) {
  return settings.providers.find((provider) => provider.id === settings.defaultProviderId) ?? null
}

export function getDefaultModel(settings: AiSettings) {
  const provider = getDefaultProvider(settings)

  return provider?.models.find((model) => model.id === settings.defaultModelId) ?? null
}

export function getPreferredFormat(provider: AiProvider): ProviderFormat {
  return provider.formats[0] ?? 'chat-completions'
}

function normalizeProvider(value: unknown): AiProvider | null {
  if (!isRecord(value)) {
    return null
  }

  const id = typeof value.id === 'string' ? value.id : crypto.randomUUID()
  const name = typeof value.name === 'string' ? value.name : 'Provider'
  const baseUrl = typeof value.baseUrl === 'string' ? value.baseUrl : ''
  const apiKey = typeof value.apiKey === 'string' ? value.apiKey : ''
  const enabled = typeof value.enabled === 'boolean' ? value.enabled : true
  const formats: ProviderFormat[] = Array.isArray(value.formats)
    ? value.formats.filter(isProviderFormat)
    : ['chat-completions']
  const preferredFormat: ProviderFormat =
    isProviderFormat(value.preferredFormat) &&
    formats.includes(value.preferredFormat as ProviderFormat)
      ? (value.preferredFormat as ProviderFormat)
      : ((formats[0] as ProviderFormat | undefined) ?? 'chat-completions')
  const orderedFormats: ProviderFormat[] = [
    preferredFormat,
    ...formats.filter((format) => format !== preferredFormat),
  ]
  const models = Array.isArray(value.models)
    ? value.models
        .map(normalizeModel)
        .filter((model): model is AiProviderModel => model !== null)
    : []

  return {
    apiKey,
    baseUrl,
    enabled,
    formats: (orderedFormats.length > 0 ? orderedFormats : ['chat-completions']) as ProviderFormat[],
    id,
    models,
    name,
    preferredFormat: orderedFormats[0] ?? 'chat-completions',
  }
}

function normalizeModel(value: unknown): AiProviderModel | null {
  if (!isRecord(value)) {
    return null
  }

  return {
    defaultMaxOutputTokens:
      typeof value.defaultMaxOutputTokens === 'number' ? value.defaultMaxOutputTokens : 2048,
    defaultTemperature:
      typeof value.defaultTemperature === 'number' ? value.defaultTemperature : 0.7,
    defaultTopP: typeof value.defaultTopP === 'number' ? value.defaultTopP : 1,
    enabled: typeof value.enabled === 'boolean' ? value.enabled : true,
    id: typeof value.id === 'string' ? value.id : crypto.randomUUID(),
    modelId: typeof value.modelId === 'string' ? value.modelId : '',
    name: typeof value.name === 'string' ? value.name : 'Model',
    supportsImages: typeof value.supportsImages === 'boolean' ? value.supportsImages : false,
    supportsStreaming: typeof value.supportsStreaming === 'boolean' ? value.supportsStreaming : true,
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function isProviderFormat(value: unknown): value is ProviderFormat {
  return (
    value === 'chat-completions' ||
    value === 'responses' ||
    value === 'anthropic' ||
    value === 'gemini'
  )
}
