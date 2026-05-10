export const aiSettingsStorageKey = 'codecafe-ai-settings'

export const providerFormatOptions = [
  {
    label: 'Chat Completions',
    value: 'chat-completions',
  },
  {
    label: 'Anthropic Messages',
    value: 'anthropic-messages',
  },
] as const

export type AiProviderFormat = (typeof providerFormatOptions)[number]['value']

export type AiProviderModel = {
  defaultMaxOutputTokens: number
  defaultTemperature: number
  defaultTopP: number
  enabled: boolean
  id: string
  maxContextTokens: number
  maxOutputTokens: number
  modelId: string
  name: string
  supportsJsonOutput: boolean
  supportsStreaming: boolean
  supportsThinking: boolean
  supportsToolCalls: boolean
}

export type AiProvider = {
  apiKey: string
  baseUrl: string
  enabled: boolean
  formats: AiProviderFormat[]
  id: string
  models: AiProviderModel[]
  name: string
  preferredFormat: AiProviderFormat
}

export type AiSettings = {
  defaultModelId: string | null
  defaultProviderId: string | null
  providers: AiProvider[]
}

export type AiModelOption = {
  label: string
  model: AiProviderModel
  provider: AiProvider
  value: string
}

export function loadAiSettings(): AiSettings {
  if (typeof window === 'undefined') {
    return createDefaultAiSettings()
  }

  const rawValue = window.localStorage.getItem(aiSettingsStorageKey)

  if (!rawValue) {
    return createDefaultAiSettings()
  }

  try {
    const parsed = JSON.parse(rawValue) as Partial<AiSettings>

    if (!Array.isArray(parsed.providers)) {
      return createDefaultAiSettings()
    }

    const providers = parsed.providers
      .map(normalizeProvider)
      .filter((provider): provider is AiProvider => provider !== null)

    if (providers.length === 0) {
      return createDefaultAiSettings()
    }

    const defaultProvider =
      providers.find((provider) => provider.id === parsed.defaultProviderId) ??
      providers.find((provider) => provider.enabled) ??
      providers[0]
    const defaultModel =
      defaultProvider.models.find((model) => model.id === parsed.defaultModelId && model.enabled) ??
      defaultProvider.models.find((model) => model.enabled) ??
      defaultProvider.models[0] ??
      null

    return {
      defaultModelId: defaultModel?.id ?? null,
      defaultProviderId: defaultProvider?.id ?? null,
      providers,
    }
  } catch {
    return createDefaultAiSettings()
  }
}

export function saveAiSettings(settings: AiSettings) {
  if (typeof window === 'undefined') {
    return
  }

  window.localStorage.setItem(aiSettingsStorageKey, JSON.stringify(settings))
}

export function getDefaultProvider(settings: AiSettings) {
  return (
    settings.providers.find((provider) => provider.id === settings.defaultProviderId) ??
    settings.providers.find((provider) => provider.enabled) ??
    settings.providers[0] ??
    null
  )
}

export function getDefaultModel(settings: AiSettings) {
  const provider = getDefaultProvider(settings)

  if (!provider) {
    return null
  }

  return (
    provider.models.find((model) => model.id === settings.defaultModelId && model.enabled) ??
    provider.models.find((model) => model.enabled) ??
    provider.models[0] ??
    null
  )
}

export function getEnabledModelOptions(settings: AiSettings): AiModelOption[] {
  return settings.providers
    .filter((provider) => provider.enabled)
    .flatMap((provider) =>
      provider.models
        .filter((model) => model.enabled)
        .map((model) => ({
          label: model.name,
          model,
          provider,
          value: toModelOptionValue(provider.id, model.id),
        })),
    )
}

export function getDefaultModelOptionValue(settings: AiSettings) {
  const provider = getDefaultProvider(settings)
  const model = getDefaultModel(settings)

  return provider && model ? toModelOptionValue(provider.id, model.id) : null
}

export function resolveModelSelection(
  settings: AiSettings,
  currentValue: string | null,
) {
  const enabledModelOptions = getEnabledModelOptions(settings)

  if (currentValue && enabledModelOptions.some((option) => option.value === currentValue)) {
    return currentValue
  }

  return getDefaultModelOptionValue(settings) ?? enabledModelOptions[0]?.value ?? null
}

export function getPreferredFormat(provider: AiProvider) {
  return provider.preferredFormat
}

export function toModelOptionValue(providerId: string, modelId: string) {
  return `${providerId}:${modelId}`
}

function createDefaultAiSettings(): AiSettings {
  return {
    defaultModelId: 'deepseek-v4-pro',
    defaultProviderId: 'deepseek',
    providers: [
      {
        apiKey: '',
        baseUrl: 'https://api.deepseek.com',
        enabled: true,
        formats: ['chat-completions', 'anthropic-messages'],
        id: 'deepseek',
        models: [
          {
            defaultMaxOutputTokens: 8192,
            defaultTemperature: 0.7,
            defaultTopP: 1,
            enabled: true,
            id: 'deepseek-v4-pro',
            maxContextTokens: 1_000_000,
            maxOutputTokens: 384_000,
            modelId: 'deepseek-v4-pro',
            name: 'DeepSeek V4 Pro',
            supportsJsonOutput: true,
            supportsStreaming: true,
            supportsThinking: true,
            supportsToolCalls: true,
          },
          {
            defaultMaxOutputTokens: 8192,
            defaultTemperature: 0.7,
            defaultTopP: 1,
            enabled: true,
            id: 'deepseek-v4-flash',
            maxContextTokens: 1_000_000,
            maxOutputTokens: 384_000,
            modelId: 'deepseek-v4-flash',
            name: 'DeepSeek V4 Flash',
            supportsJsonOutput: true,
            supportsStreaming: true,
            supportsThinking: true,
            supportsToolCalls: true,
          },
        ],
        name: 'DeepSeek',
        preferredFormat: 'chat-completions',
      },
      {
        apiKey: '',
        baseUrl: 'https://api.minimax.io/v1',
        enabled: false,
        formats: ['chat-completions'],
        id: 'minimax',
        models: [
          {
            defaultMaxOutputTokens: 8192,
            defaultTemperature: 0.7,
            defaultTopP: 1,
            enabled: true,
            id: 'minimax-m2.7',
            maxContextTokens: 204_800,
            maxOutputTokens: 128_000,
            modelId: 'MiniMax-M2.7',
            name: 'MiniMax M2.7',
            supportsJsonOutput: true,
            supportsStreaming: true,
            supportsThinking: true,
            supportsToolCalls: true,
          },
          {
            defaultMaxOutputTokens: 8192,
            defaultTemperature: 0.7,
            defaultTopP: 1,
            enabled: true,
            id: 'minimax-m2.7-highspeed',
            maxContextTokens: 204_800,
            maxOutputTokens: 128_000,
            modelId: 'MiniMax-M2.7-highspeed',
            name: 'MiniMax M2.7 Highspeed',
            supportsJsonOutput: true,
            supportsStreaming: true,
            supportsThinking: true,
            supportsToolCalls: true,
          },
          {
            defaultMaxOutputTokens: 8192,
            defaultTemperature: 0.7,
            defaultTopP: 1,
            enabled: true,
            id: 'minimax-m2.5',
            maxContextTokens: 204_800,
            maxOutputTokens: 128_000,
            modelId: 'MiniMax-M2.5',
            name: 'MiniMax M2.5',
            supportsJsonOutput: true,
            supportsStreaming: true,
            supportsThinking: true,
            supportsToolCalls: true,
          },
          {
            defaultMaxOutputTokens: 8192,
            defaultTemperature: 0.7,
            defaultTopP: 1,
            enabled: true,
            id: 'minimax-m2.5-highspeed',
            maxContextTokens: 204_800,
            maxOutputTokens: 128_000,
            modelId: 'MiniMax-M2.5-highspeed',
            name: 'MiniMax M2.5 Highspeed',
            supportsJsonOutput: true,
            supportsStreaming: true,
            supportsThinking: true,
            supportsToolCalls: true,
          },
          {
            defaultMaxOutputTokens: 8192,
            defaultTemperature: 0.7,
            defaultTopP: 1,
            enabled: true,
            id: 'minimax-m2.1',
            maxContextTokens: 204_800,
            maxOutputTokens: 128_000,
            modelId: 'MiniMax-M2.1',
            name: 'MiniMax M2.1',
            supportsJsonOutput: true,
            supportsStreaming: true,
            supportsThinking: true,
            supportsToolCalls: true,
          },
          {
            defaultMaxOutputTokens: 8192,
            defaultTemperature: 0.7,
            defaultTopP: 1,
            enabled: true,
            id: 'minimax-m2.1-highspeed',
            maxContextTokens: 204_800,
            maxOutputTokens: 128_000,
            modelId: 'MiniMax-M2.1-highspeed',
            name: 'MiniMax M2.1 Highspeed',
            supportsJsonOutput: true,
            supportsStreaming: true,
            supportsThinking: true,
            supportsToolCalls: true,
          },
          {
            defaultMaxOutputTokens: 8192,
            defaultTemperature: 0.7,
            defaultTopP: 1,
            enabled: true,
            id: 'minimax-m2',
            maxContextTokens: 204_800,
            maxOutputTokens: 128_000,
            modelId: 'MiniMax-M2',
            name: 'MiniMax M2',
            supportsJsonOutput: true,
            supportsStreaming: true,
            supportsThinking: true,
            supportsToolCalls: true,
          },
        ],
        name: 'MiniMax',
        preferredFormat: 'chat-completions',
      },
    ],
  }
}

function normalizeProvider(value: unknown): AiProvider | null {
  if (typeof value !== 'object' || value === null) {
    return null
  }

  const provider = value as Record<string, unknown>

  if (
    typeof provider.id !== 'string' ||
    typeof provider.name !== 'string' ||
    typeof provider.baseUrl !== 'string' ||
    typeof provider.apiKey !== 'string' ||
    !Array.isArray(provider.models)
  ) {
    return null
  }

  const models = provider.models
    .map(normalizeModel)
    .filter((model): model is AiProviderModel => model !== null)

  if (models.length === 0) {
    return null
  }

  return {
    apiKey: provider.apiKey,
    baseUrl: provider.baseUrl,
    enabled: provider.enabled !== false,
    formats: ['chat-completions', 'anthropic-messages'],
    id: provider.id,
    models,
    name: provider.name,
    preferredFormat: 'chat-completions',
  }
}

function normalizeModel(value: unknown): AiProviderModel | null {
  if (typeof value !== 'object' || value === null) {
    return null
  }

  const model = value as Record<string, unknown>

  if (
    typeof model.id !== 'string' ||
    typeof model.name !== 'string' ||
    typeof model.modelId !== 'string'
  ) {
    return null
  }

  return {
    defaultMaxOutputTokens:
      typeof model.defaultMaxOutputTokens === 'number' ? model.defaultMaxOutputTokens : 8192,
    defaultTemperature:
      typeof model.defaultTemperature === 'number' ? model.defaultTemperature : 0.7,
    defaultTopP: typeof model.defaultTopP === 'number' ? model.defaultTopP : 1,
    enabled: model.enabled !== false,
    id: model.id,
    maxContextTokens:
      typeof model.maxContextTokens === 'number' ? model.maxContextTokens : 1_000_000,
    maxOutputTokens:
      typeof model.maxOutputTokens === 'number' ? model.maxOutputTokens : 384_000,
    modelId: model.modelId,
    name: model.name,
    supportsJsonOutput: model.supportsJsonOutput !== false,
    supportsStreaming: model.supportsStreaming !== false,
    supportsThinking: model.supportsThinking !== false,
    supportsToolCalls: model.supportsToolCalls !== false,
  }
}
