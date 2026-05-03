import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import {
  createAiProvider,
  createAiProviderModel,
  deleteAiProvider,
  deleteAiProviderModel,
  listAiProviders,
  updateAiProvider,
  updateAiProviderModel,
} from './aiProviderApi'
import type { AiProvider, AiProviderModel } from './aiProviderApi'

export function SettingsPage() {
  return (
    <section className="settings-page" aria-label="Settings">
      <header>
        <p className="eyebrow">Settings</p>
        <h2>Application settings</h2>
      </header>

      <section className="settings-panel">
        <div className="settings-panel-header">
          <h3>Settings</h3>
          <p>Select a settings section from the sidebar.</p>
        </div>
      </section>
    </section>
  )
}

export function AiSettingsPage() {
  const [providers, setProviders] = useState<AiProvider[]>([])
  const [selectedProviderId, setSelectedProviderId] = useState('')
  const [status, setStatus] = useState('')
  const [isLoading, setIsLoading] = useState(true)

  const selectedProvider = providers.find((provider) => provider.id === selectedProviderId) ?? providers[0] ?? null

  async function reloadProviders(nextProviderId = selectedProviderId) {
    const nextProviders = await listAiProviders()
    const nextProvider = nextProviders.find((provider) => provider.id === nextProviderId) ?? nextProviders[0] ?? null

    setProviders(nextProviders)
    setSelectedProviderId(nextProvider?.id ?? '')
  }

  useEffect(() => {
    let ignore = false

    async function load() {
      try {
        const loadedProviders = await listAiProviders()

        if (ignore) {
          return
        }

        setProviders(loadedProviders)
        setSelectedProviderId(loadedProviders[0]?.id ?? '')
      } catch {
        if (!ignore) {
          setStatus('Unable to load providers.')
        }
      } finally {
        if (!ignore) {
          setIsLoading(false)
        }
      }
    }

    void load()

    return () => {
      ignore = true
    }
  }, [])

  async function runAction(action: () => Promise<void>, successMessage: string) {
    try {
      setStatus('')
      await action()
      setStatus(successMessage)
    } catch {
      setStatus('Save failed.')
    }
  }

  async function addProvider(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    const provider = await createAiProvider({
      apiKey: getNullableString(form, 'apiKey'),
      baseUrl: getString(form, 'baseUrl'),
      enabled: getBoolean(form, 'enabled'),
      name: getString(form, 'name'),
    })

    await reloadProviders(provider.id)
    event.currentTarget.reset()
  }

  return (
    <section className="settings-page compact-settings-page" aria-label="AI settings">
      <section className="settings-section" aria-label="AI configuration">
        {status ? <p className="settings-status">{status}</p> : null}

        <section className="provider-settings simple-provider-settings" aria-label="Provider settings">
          <nav className="provider-menu" aria-label="Providers">
            {providers.map((provider) => (
              <button
                aria-current={provider.id === selectedProvider?.id ? 'page' : undefined}
                key={provider.id}
                onClick={() => setSelectedProviderId(provider.id)}
                type="button"
              >
                {provider.name}
              </button>
            ))}
          </nav>

          <div className="provider-detail">
            {isLoading ? (
              <p className="empty-settings-copy">Loading providers.</p>
            ) : selectedProvider ? (
              <ProviderDetail
                key={selectedProvider.id}
                onReload={(providerId) => reloadProviders(providerId)}
                provider={selectedProvider}
                runAction={runAction}
              />
            ) : (
              <p className="empty-settings-copy">No providers configured.</p>
            )}

            <form className="settings-table provider-add-table" onSubmit={(event) => void runAction(() => addProvider(event), 'Provider added.')}>
              <div className="settings-table-row settings-table-head">
                <span>Name</span>
                <span>Base URL</span>
                <span>API Key</span>
                <span>Enabled</span>
                <span>Action</span>
              </div>
              <div className="settings-table-row">
                <input aria-label="New provider name" name="name" placeholder="Custom provider" />
                <input aria-label="New provider base URL" name="baseUrl" placeholder="https://..." />
                <input aria-label="New provider API Key" name="apiKey" placeholder="sk-..." type="password" />
                <label className="table-checkbox">
                  <input name="enabled" type="checkbox" />
                </label>
                <button type="submit">Add provider</button>
              </div>
            </form>
          </div>
        </section>
      </section>
    </section>
  )
}

function ProviderDetail({
  onReload,
  provider,
  runAction,
}: {
  onReload: (providerId?: string) => Promise<void>
  provider: AiProvider
  runAction: (action: () => Promise<void>, successMessage: string) => Promise<void>
}) {
  async function saveProvider(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)

    await updateAiProvider(provider.id, {
      apiKey: getNullableString(form, 'apiKey'),
      baseUrl: getString(form, 'baseUrl'),
      enabled: getBoolean(form, 'enabled'),
      name: getString(form, 'name'),
    })
    await onReload(provider.id)
  }

  async function removeProvider() {
    await deleteAiProvider(provider.id)
    await onReload()
  }

  async function addModel(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)

    await createAiProviderModel(provider.id, {
      displayName: getString(form, 'displayName'),
      enabled: getBoolean(form, 'enabled'),
      kind: getModelKind(form),
      modelId: getString(form, 'modelId'),
    })
    await onReload(provider.id)
    event.currentTarget.reset()
  }

  return (
    <>
      <form className="provider-config-grid" onSubmit={(event) => void runAction(() => saveProvider(event), 'Provider saved.')}>
        <label className="settings-field">
          <span>Name</span>
          <input defaultValue={provider.name} name="name" />
        </label>
        <label className="settings-field">
          <span>Base URL</span>
          <input defaultValue={provider.baseUrl} name="baseUrl" />
        </label>
        <label className="settings-field">
          <span>API Key</span>
          <input defaultValue={provider.apiKey ?? ''} name="apiKey" type="password" />
        </label>
        <label className="settings-checkbox-field">
          <input defaultChecked={provider.enabled} name="enabled" type="checkbox" /> Enabled
        </label>
        <div className="settings-actions full-width-field">
          <button type="submit">Save provider</button>
          <button onClick={() => void runAction(removeProvider, 'Provider deleted.')} type="button">
            Delete provider
          </button>
        </div>
      </form>

      <div className="settings-table-wrap">
        <form className="settings-table simple-model-table add-row-table" onSubmit={(event) => void runAction(() => addModel(event), 'Model added.')}>
          <div className="settings-table-row settings-table-head">
            <span>Model ID</span>
            <span>Display</span>
            <span>Kind</span>
            <span>Enabled</span>
            <span>Action</span>
          </div>
          <div className="settings-table-row">
            <input aria-label="Model ID" name="modelId" placeholder="gpt-4.1" />
            <input aria-label="Display name" name="displayName" placeholder="GPT-4.1" />
            <select aria-label="Model kind" name="kind" defaultValue="Official">
              <option value="Official">Official</option>
              <option value="Custom">Custom</option>
            </select>
            <label className="table-checkbox">
              <input defaultChecked name="enabled" type="checkbox" />
            </label>
            <button type="submit">Add model</button>
          </div>
        </form>
      </div>

      <div className="settings-table-wrap">
        <div className="settings-table simple-model-table">
          <div className="settings-table-row settings-table-head">
            <span>Model ID</span>
            <span>Display</span>
            <span>Kind</span>
            <span>Enabled</span>
            <span>Action</span>
          </div>
          {provider.models.length > 0 ? (
            provider.models.map((model) => (
              <ProviderModelRow
                key={model.id}
                model={model}
                onReload={() => onReload(provider.id)}
                providerId={provider.id}
                runAction={runAction}
              />
            ))
          ) : (
            <p className="empty-settings-copy table-empty">No models configured.</p>
          )}
        </div>
      </div>
    </>
  )
}

function ProviderModelRow({
  model,
  onReload,
  providerId,
  runAction,
}: {
  model: AiProviderModel
  onReload: () => Promise<void>
  providerId: string
  runAction: (action: () => Promise<void>, successMessage: string) => Promise<void>
}) {
  async function saveModel(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)

    await updateAiProviderModel(providerId, model.id, {
      displayName: getString(form, 'displayName'),
      enabled: getBoolean(form, 'enabled'),
      kind: getModelKind(form),
      modelId: getString(form, 'modelId'),
    })
    await onReload()
  }

  async function removeModel() {
    await deleteAiProviderModel(providerId, model.id)
    await onReload()
  }

  return (
    <form className="settings-table-row" onSubmit={(event) => void runAction(() => saveModel(event), 'Model saved.')}>
      <input aria-label={`${model.modelId} model ID`} defaultValue={model.modelId} name="modelId" />
      <input aria-label={`${model.modelId} display name`} defaultValue={model.displayName} name="displayName" />
      <select aria-label={`${model.modelId} kind`} defaultValue={model.kind} name="kind">
        <option value="Official">Official</option>
        <option value="Custom">Custom</option>
      </select>
      <label className="table-checkbox">
        <input defaultChecked={model.enabled} name="enabled" type="checkbox" />
      </label>
      <div className="table-actions">
        <button type="submit">Save</button>
        <button onClick={() => void runAction(removeModel, 'Model deleted.')} type="button">
          Delete
        </button>
      </div>
    </form>
  )
}

function getString(form: FormData, key: string) {
  return String(form.get(key) ?? '').trim()
}

function getNullableString(form: FormData, key: string) {
  const value = getString(form, key)

  return value.length > 0 ? value : null
}

function getBoolean(form: FormData, key: string) {
  return form.has(key)
}

function getModelKind(form: FormData) {
  return getString(form, 'kind') === 'Official' ? 'Official' : 'Custom'
}
