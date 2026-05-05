import { useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import {
  createModel,
  createProvider,
  getPreferredFormat,
  loadAiSettings,
  providerFormatOptions,
  reconcileAiSettings,
  saveAiSettings,
  type AiProvider,
  type AiProviderModel,
  type AiSettings,
  type ProviderFormat,
} from './aiSettingsStore'
import { testProviderConnection } from './aiClient'

export function AiSettingsPage() {
  const initialSettings = useMemo(() => loadAiSettings(), [])
  const [settings, setSettings] = useState<AiSettings>(initialSettings)
  const [selectedProviderId, setSelectedProviderId] = useState(
    initialSettings.defaultProviderId ?? initialSettings.providers[0]?.id ?? '',
  )
  const [testingModelId, setTestingModelId] = useState<string | null>(null)
  const [status, setStatus] = useState('')

  const selectedProvider =
    settings.providers.find((provider) => provider.id === selectedProviderId) ??
    settings.providers[0] ??
    null

  function updateSettings(nextSettings: AiSettings, nextSelectedProviderId = selectedProviderId) {
    const reconciled = reconcileAiSettings(nextSettings)

    setSettings(reconciled)
    setSelectedProviderId(nextSelectedProviderId || reconciled.providers[0]?.id || '')
    saveAiSettings(reconciled)
  }

  function addProvider() {
    const provider = createProvider()
    const nextSettings = settings.providers.length === 0
      ? {
          defaultModelId: provider.models[0]?.id ?? null,
          defaultProviderId: provider.id,
          providers: [provider],
        }
      : {
          ...settings,
          providers: [...settings.providers, provider],
        }

    updateSettings(nextSettings, provider.id)
    setStatus('Provider added.')
  }

  function updateProvider(providerId: string, updater: (provider: AiProvider) => AiProvider) {
    const nextProviders = settings.providers.map((provider) =>
      provider.id === providerId ? updater(provider) : provider,
    )

    updateSettings({
      ...settings,
      providers: nextProviders,
    }, providerId)
  }

  function deleteProvider(providerId: string) {
    const remainingProviders = settings.providers.filter((provider) => provider.id !== providerId)

    updateSettings({
      defaultModelId:
        settings.defaultProviderId === providerId ? null : settings.defaultModelId,
      defaultProviderId:
        settings.defaultProviderId === providerId ? null : settings.defaultProviderId,
      providers: remainingProviders,
    }, remainingProviders[0]?.id ?? '')
    setStatus('Provider deleted.')
  }

  function setDefaultProvider(providerId: string) {
    const provider = settings.providers.find((item) => item.id === providerId) ?? null
    const firstEnabledModel = provider?.models.find((model) => model.enabled) ?? null

    updateSettings({
      ...settings,
      defaultModelId: firstEnabledModel?.id ?? null,
      defaultProviderId: providerId,
      providers: settings.providers,
    }, providerId)
    setStatus('Default provider updated.')
  }

  async function testModelConnection(provider: AiProvider, model: AiProviderModel) {
    if (!provider.baseUrl.trim() || !provider.apiKey.trim()) {
      setStatus('Add a base URL and API key before testing the connection.')
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

  const defaultModelId = useMemo(() => {
    if (settings.defaultProviderId !== selectedProvider?.id) {
      return null
    }

    return settings.defaultModelId
  }, [selectedProvider?.id, settings.defaultModelId, settings.defaultProviderId])

  return (
    <section className="settings-page ai-settings-page" aria-label="AI settings">
      <header className="settings-page-header ai-settings-header">
        <Link aria-label="Back to settings" className="ai-settings-back-link" to="/settings">
          ←
        </Link>
        <div className="settings-page-header-title ai-settings-header-title">
          <h1>AI</h1>
        </div>
        <button className="primary-button" onClick={addProvider} type="button">
          Add provider
        </button>
      </header>

      {status ? <p className="settings-status">{status}</p> : null}

      <section className="provider-settings" aria-label="Provider settings">
        <nav className="provider-menu" aria-label="Providers">
          {settings.providers.length > 0 ? (
            settings.providers.map((provider) => (
              <button
                aria-current={provider.id === selectedProvider?.id ? 'page' : undefined}
                key={provider.id}
                onClick={() => setSelectedProviderId(provider.id)}
                type="button"
              >
                <span className="provider-menu-item-title">{provider.name}</span>
                <span className="provider-menu-item-meta">
                  {provider.enabled ? 'Enabled' : 'Disabled'}
                </span>
              </button>
            ))
          ) : (
            <p className="empty-settings-copy">Add a provider to start chatting.</p>
          )}
        </nav>

        <div className="provider-detail">
          {selectedProvider ? (
            <>
              <ProviderForm
                isDefault={settings.defaultProviderId === selectedProvider.id}
                onDelete={() => deleteProvider(selectedProvider.id)}
                onSetDefault={() => setDefaultProvider(selectedProvider.id)}
                onUpdate={(updater) => updateProvider(selectedProvider.id, updater)}
                provider={selectedProvider}
                setStatus={setStatus}
              />

              <section className="settings-panel">
                <div className="settings-section-header">
                  <div>
                    <h2>Models</h2>
                    <p>Display names are shown in chat. Model IDs are sent to the provider API.</p>
                  </div>
                  <button
                    onClick={() => {
                      updateProvider(selectedProvider.id, (provider) => ({
                        ...provider,
                        models: [...provider.models, createModel()],
                      }))
                      setStatus('Model added.')
                    }}
                    type="button"
                  >
                    Add model
                  </button>
                </div>

                <div className="settings-table-wrap">
                  <div className="settings-table ai-model-table">
                    <div className="settings-table-row settings-table-head">
                      <span>Default</span>
                      <span>Name</span>
                      <span>Model ID</span>
                      <span>Format</span>
                      <span>Stream</span>
                      <span>Image</span>
                      <span>Temp</span>
                      <span>Top-p</span>
                      <span>Max</span>
                      <span>Enabled</span>
                      <span>Action</span>
                    </div>

                    {selectedProvider.models.length > 0 ? (
                      selectedProvider.models.map((model) => (
                        <ProviderModelRow
                          defaultModelId={defaultModelId}
                          model={model}
                          onDelete={() => {
                            updateProvider(selectedProvider.id, (provider) => ({
                              ...provider,
                              models: provider.models.filter((item) => item.id !== model.id),
                            }))
                            setStatus('Model deleted.')
                          }}
                          onSetDefault={() => {
                            updateSettings({
                              ...settings,
                              defaultModelId: model.id,
                              defaultProviderId: selectedProvider.id,
                              providers: settings.providers,
                            }, selectedProvider.id)
                            setStatus('Default model updated.')
                          }}
                          onTest={() => void testModelConnection(selectedProvider, model)}
                          isTesting={testingModelId === model.id}
                          onUpdate={(nextModel) => {
                            updateProvider(selectedProvider.id, (provider) => ({
                              ...provider,
                              models: provider.models.map((item) =>
                                item.id === model.id ? nextModel : item,
                              ),
                            }))
                          }}
                          preferredFormat={getPreferredFormat(selectedProvider)}
                        />
                      ))
                    ) : (
                      <p className="empty-settings-copy table-empty">No models configured.</p>
                    )}
                  </div>
                </div>
              </section>
            </>
          ) : (
            <div className="settings-panel">
              <p className="empty-settings-copy">
                Add a provider, paste a base URL and API key, then configure one or more models.
              </p>
            </div>
          )}
        </div>
      </section>
    </section>
  )
}

function ProviderForm({
  isDefault,
  onDelete,
  onSetDefault,
  onUpdate,
  provider,
  setStatus,
}: {
  isDefault: boolean
  onDelete: () => void
  onSetDefault: () => void
  onUpdate: (updater: (provider: AiProvider) => AiProvider) => void
  provider: AiProvider
  setStatus: (status: string) => void
}) {
  const [draggedFormat, setDraggedFormat] = useState<ProviderFormat | null>(null)

  function updateField<K extends keyof AiProvider>(key: K, value: AiProvider[K]) {
    onUpdate((currentProvider) => ({
      ...currentProvider,
      [key]: value,
    }))
  }

  function toggleFormat(format: ProviderFormat) {
    onUpdate((currentProvider) => {
      const formats = currentProvider.formats.includes(format)
        ? currentProvider.formats.filter((item) => item !== format)
        : [...currentProvider.formats, format]
      const nextFormats: ProviderFormat[] = formats.length > 0 ? formats : ['chat-completions']

      return {
        ...currentProvider,
        formats: nextFormats,
        preferredFormat: nextFormats[0],
      }
    })
  }

  function moveFormat(sourceFormat: ProviderFormat, targetFormat: ProviderFormat) {
    onUpdate((currentProvider) => {
      if (
        sourceFormat === targetFormat ||
        !currentProvider.formats.includes(sourceFormat) ||
        !currentProvider.formats.includes(targetFormat)
      ) {
        return currentProvider
      }

      const nextFormats = [...currentProvider.formats]
      const sourceIndex = nextFormats.indexOf(sourceFormat)
      const targetIndex = nextFormats.indexOf(targetFormat)

      nextFormats.splice(sourceIndex, 1)
      nextFormats.splice(targetIndex, 0, sourceFormat)

      return {
        ...currentProvider,
        formats: nextFormats,
        preferredFormat: nextFormats[0],
      }
    })
  }

  function saveProvider(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setStatus('Provider saved.')
  }

  const orderedFormatOptions = [
    ...provider.formats
      .map((format) => providerFormatOptions.find((option) => option.value === format))
      .filter((option): option is (typeof providerFormatOptions)[number] => Boolean(option)),
    ...providerFormatOptions.filter((option) => !provider.formats.includes(option.value)),
  ]

  return (
    <form className="settings-panel provider-form-panel" onSubmit={saveProvider}>
      <div className="settings-section-header">
        <div>
          <h2>Provider</h2>
          <p>Keep credentials local in this browser. They are not sent to the CodeCafe backend.</p>
        </div>
        <div className="settings-actions">
          {!isDefault ? (
            <button onClick={onSetDefault} type="button">
              Set default
            </button>
          ) : (
            <span className="settings-readonly-badge">Default</span>
          )}
          <button onClick={onDelete} type="button">
            Delete
          </button>
        </div>
      </div>

      <div className="provider-config-grid">
        <label className="settings-field">
          <span>Name</span>
          <input
            onChange={(event) => updateField('name', event.target.value)}
            value={provider.name}
          />
        </label>

        <label className="settings-field">
          <span>Base URL</span>
          <input
            onChange={(event) => updateField('baseUrl', event.target.value)}
            placeholder="https://api.openai.com/v1"
            value={provider.baseUrl}
          />
        </label>

        <label className="settings-field full-width-field">
          <span>API Key</span>
          <input
            onChange={(event) => updateField('apiKey', event.target.value)}
            placeholder="sk-..."
            type="password"
            value={provider.apiKey}
          />
        </label>

        <fieldset className="settings-field full-width-field format-fieldset">
          <legend>Supported formats</legend>
          <div className="format-option-list compact-format-list">
            {orderedFormatOptions.map((option) => {
              const isEnabled = provider.formats.includes(option.value)
              const isDefault = getPreferredFormat(provider) === option.value

              return (
                <div
                  className={`format-option format-option-row${isEnabled ? ' enabled' : ''}`}
                  draggable={isEnabled}
                  key={option.value}
                  onDragEnd={() => setDraggedFormat(null)}
                  onDragOver={(event) => {
                    if (draggedFormat && draggedFormat !== option.value && isEnabled) {
                      event.preventDefault()
                    }
                  }}
                  onDragStart={() => setDraggedFormat(option.value)}
                  onDrop={() => {
                    if (draggedFormat && isEnabled) {
                      moveFormat(draggedFormat, option.value)
                    }
                    setDraggedFormat(null)
                  }}
                >
                  <label className="format-option-main">
                    <input
                      checked={isEnabled}
                      onChange={() => toggleFormat(option.value)}
                      type="checkbox"
                    />
                    <strong>{option.label}</strong>
                  </label>
                  <div className="format-option-meta">
                    {isDefault ? <span className="table-badge">Default</span> : null}
                    {isEnabled ? <span className="drag-hint">Drag</span> : null}
                  </div>
                </div>
              )
            })}
          </div>
        </fieldset>

        <label className="settings-checkbox-field">
          <input
            checked={provider.enabled}
            onChange={(event) => updateField('enabled', event.target.checked)}
            type="checkbox"
          />
          Enabled
        </label>
      </div>

      <div className="settings-actions">
        <button type="submit">Save provider</button>
      </div>
    </form>
  )
}

function ProviderModelRow({
  defaultModelId,
  isTesting,
  model,
  onDelete,
  onSetDefault,
  onTest,
  onUpdate,
  preferredFormat,
}: {
  defaultModelId: string | null
  isTesting: boolean
  model: AiProviderModel
  onDelete: () => void
  onSetDefault: () => void
  onTest: () => void
  onUpdate: (model: AiProviderModel) => void
  preferredFormat: ProviderFormat
}) {
  function updateField<K extends keyof AiProviderModel>(key: K, value: AiProviderModel[K]) {
    onUpdate({
      ...model,
      [key]: value,
    })
  }

  return (
    <div className="settings-table-row">
      <label className="table-checkbox">
        <input
          checked={defaultModelId === model.id}
          name={`default-${model.id}`}
          onChange={onSetDefault}
          type="radio"
        />
      </label>

      <input
        aria-label={`${model.id} display name`}
        onChange={(event) => updateField('name', event.target.value)}
        value={model.name}
      />

      <input
        aria-label={`${model.id} model id`}
        onChange={(event) => updateField('modelId', event.target.value)}
        value={model.modelId}
      />

      <span className="table-badge">
        {providerFormatOptions.find((option) => option.value === preferredFormat)?.label ?? preferredFormat}
      </span>

      <label className="table-checkbox">
        <input
          checked={model.supportsStreaming}
          onChange={(event) => updateField('supportsStreaming', event.target.checked)}
          type="checkbox"
        />
      </label>

      <label className="table-checkbox">
        <input
          checked={model.supportsImages}
          onChange={(event) => updateField('supportsImages', event.target.checked)}
          type="checkbox"
        />
      </label>

      <input
        aria-label={`${model.id} default temperature`}
        min={0}
        onChange={(event) => updateField('defaultTemperature', Number(event.target.value))}
        step={0.1}
        type="number"
        value={model.defaultTemperature}
      />

      <input
        aria-label={`${model.id} default top p`}
        max={1}
        min={0}
        onChange={(event) => updateField('defaultTopP', Number(event.target.value))}
        step={0.05}
        type="number"
        value={model.defaultTopP}
      />

      <input
        aria-label={`${model.id} default max output tokens`}
        min={1}
        onChange={(event) => updateField('defaultMaxOutputTokens', Number(event.target.value))}
        step={1}
        type="number"
        value={model.defaultMaxOutputTokens}
      />

      <label className="table-checkbox">
        <input
          checked={model.enabled}
          onChange={(event) => updateField('enabled', event.target.checked)}
          type="checkbox"
        />
      </label>

      <div className="table-actions">
        <button onClick={onTest} type="button">
          {isTesting ? 'Testing' : 'Test'}
        </button>
        <button onClick={onDelete} type="button">
          Delete
        </button>
      </div>
    </div>
  )
}
