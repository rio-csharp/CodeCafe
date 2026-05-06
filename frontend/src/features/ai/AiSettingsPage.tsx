import { useMemo, useState } from 'react'
import {
  getPreferredFormat,
  loadAiSettings,
  providerFormatOptions,
  saveAiSettings,
  type AiProvider,
  type AiProviderModel,
  type AiSettings,
} from './aiSettingsStore'
import { testProviderConnection } from './aiClient'

export function AiSettingsPage() {
  const initialSettings = useMemo(() => loadAiSettings(), [])
  const [settings, setSettings] = useState<AiSettings>(initialSettings)
  const [selectedProviderId, setSelectedProviderId] = useState(
    initialSettings.defaultProviderId ?? initialSettings.providers[0]?.id ?? '',
  )
  const [status, setStatus] = useState('')
  const [testingModelId, setTestingModelId] = useState<string | null>(null)

  const selectedProvider =
    settings.providers.find((provider) => provider.id === selectedProviderId) ??
    settings.providers[0] ??
    null

  function persist(nextSettings: AiSettings) {
    setSettings(nextSettings)
    saveAiSettings(nextSettings)
  }

  function updateProvider(providerId: string, updater: (provider: AiProvider) => AiProvider) {
    persist({
      ...settings,
      providers: settings.providers.map((provider) =>
        provider.id === providerId ? updater(provider) : provider,
      ),
    })
  }

  function updateModel(providerId: string, modelId: string, updater: (model: AiProviderModel) => AiProviderModel) {
    updateProvider(providerId, (provider) => ({
      ...provider,
      models: provider.models.map((model) => (model.id === modelId ? updater(model) : model)),
    }))
  }

  function setDefaultModel(providerId: string, modelId: string) {
    persist({
      ...settings,
      defaultModelId: modelId,
      defaultProviderId: providerId,
    })
    setStatus('Default model updated.')
  }

  async function testModel(provider: AiProvider, model: AiProviderModel) {
    if (!provider.apiKey.trim()) {
      setStatus('Add your DeepSeek API key first.')
      return
    }

    setTestingModelId(model.id)
    setStatus(`Testing ${model.name}...`)

    try {
      const result = await testProviderConnection({
        model,
        provider,
      })

      setStatus(result.message)
    } catch (error) {
      setStatus(error instanceof Error ? error.message : 'Connection test failed.')
    } finally {
      setTestingModelId(null)
    }
  }

  if (!selectedProvider) {
    return null
  }

  const preferredFormat =
    providerFormatOptions.find((option) => option.value === getPreferredFormat(selectedProvider))?.label ??
    getPreferredFormat(selectedProvider)
  const supportedFormats = selectedProvider.formats
    .map((format) => providerFormatOptions.find((option) => option.value === format)?.label ?? format)
    .join(', ')

  return (
    <section className="settings-page ai-settings-page" aria-label="AI settings">
      <header className="settings-page-header ai-settings-header">
        <div className="settings-page-header-title ai-settings-header-title">
          <h1>AI</h1>
        </div>
      </header>

      {status ? <p className="settings-status">{status}</p> : null}

      <section className="provider-settings" aria-label="Provider settings">
        <nav className="provider-menu" aria-label="Providers">
          {settings.providers.map((provider) => (
            <button
              aria-current={provider.id === selectedProvider.id ? 'page' : undefined}
              key={provider.id}
              onClick={() => setSelectedProviderId(provider.id)}
              type="button"
            >
              <span className="provider-menu-item-title">{provider.name}</span>
              <span className="provider-menu-item-meta">
                {provider.enabled ? 'Enabled' : 'Disabled'}
              </span>
            </button>
          ))}
        </nav>

        <div className="provider-detail">
          <section className="settings-panel">
            <div className="settings-section-header">
              <div>
                <h2>{selectedProvider.name}</h2>
                <p>Built-in provider configuration. Only your API key and model switches are editable.</p>
              </div>
            </div>

            <div className="provider-config-grid">
              <label className="settings-field">
                <span>Provider</span>
                <input disabled value={selectedProvider.name} />
              </label>

              <label className="settings-field">
                <span>Base URL</span>
                <input disabled value={selectedProvider.baseUrl} />
              </label>

              <label className="settings-field">
                <span>Default format</span>
                <input disabled value={preferredFormat} />
              </label>

              <label className="settings-field">
                <span>Supported formats</span>
                <input disabled value={supportedFormats} />
              </label>

              <label className="settings-checkbox-field">
                <input
                  checked={selectedProvider.enabled}
                  onChange={(event) => {
                    updateProvider(selectedProvider.id, (provider) => ({
                      ...provider,
                      enabled: event.target.checked,
                    }))
                    setStatus('Provider updated.')
                  }}
                  type="checkbox"
                />
                Enabled
              </label>

              <label className="settings-field full-width-field">
                <span>API Key</span>
                <input
                  onChange={(event) => {
                    updateProvider(selectedProvider.id, (provider) => ({
                      ...provider,
                      apiKey: event.target.value,
                    }))
                    setStatus('')
                  }}
                  placeholder="Paste your DeepSeek API key"
                  type="password"
                  value={selectedProvider.apiKey}
                />
              </label>
            </div>
          </section>

          <section className="settings-panel">
            <div className="settings-section-header">
              <div>
                <h2>Models</h2>
                <p>These are the built-in official DeepSeek models available in CodeCafe right now.</p>
              </div>
            </div>

            <div className="settings-table-wrap">
              <div className="settings-table ai-model-table ai-model-table-compact">
                <div className="settings-table-row settings-table-head">
                  <span>Default</span>
                  <span>Name</span>
                  <span>Model ID</span>
                  <span>Context</span>
                  <span>Max output</span>
                  <span>JSON</span>
                  <span>Tools</span>
                  <span>Thinking</span>
                  <span>Stream</span>
                  <span>Enabled</span>
                  <span>Action</span>
                </div>

                {selectedProvider.models.map((model) => (
                  <div className="settings-table-row" key={model.id}>
                    <label className="table-checkbox">
                      <input
                        checked={
                          settings.defaultProviderId === selectedProvider.id &&
                          settings.defaultModelId === model.id
                        }
                        name={`default-${model.id}`}
                        onChange={() => setDefaultModel(selectedProvider.id, model.id)}
                        type="radio"
                      />
                    </label>

                    <input aria-label={`${model.id} display name`} disabled value={model.name} />

                    <input aria-label={`${model.id} model id`} disabled value={model.modelId} />

                    <span className="table-metric">{formatTokenCount(model.maxContextTokens)}</span>

                    <span className="table-metric">{formatTokenCount(model.maxOutputTokens)}</span>

                    <span className="table-capability-pill">
                      {model.supportsJsonOutput ? 'Yes' : 'No'}
                    </span>

                    <span className="table-capability-pill">
                      {model.supportsToolCalls ? 'Yes' : 'No'}
                    </span>

                    <span className="table-capability-pill">
                      {model.supportsThinking ? 'Yes' : 'No'}
                    </span>

                    <label className="table-checkbox">
                      <input
                        checked={model.supportsStreaming}
                        onChange={(event) => {
                          updateModel(selectedProvider.id, model.id, (currentModel) => ({
                            ...currentModel,
                            supportsStreaming: event.target.checked,
                          }))
                          setStatus('Model updated.')
                        }}
                        type="checkbox"
                      />
                    </label>

                    <label className="table-checkbox">
                      <input
                        checked={model.enabled}
                        onChange={(event) => {
                          updateModel(selectedProvider.id, model.id, (currentModel) => ({
                            ...currentModel,
                            enabled: event.target.checked,
                          }))
                          setStatus('Model updated.')
                        }}
                        type="checkbox"
                      />
                    </label>

                    <div className="table-actions">
                      <button onClick={() => void testModel(selectedProvider, model)} type="button">
                        {testingModelId === model.id ? 'Testing' : 'Test'}
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </section>
        </div>
      </section>
    </section>
  )
}

function formatTokenCount(value: number) {
  if (value >= 1_000_000) {
    return `${value / 1_000_000}M`
  }

  if (value >= 1_000) {
    return `${Math.round(value / 1_000)}K`
  }

  return String(value)
}
