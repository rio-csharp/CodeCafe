import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { isLocalEnvironment } from '../../app/runtimeEnvironment'
import { useTheme } from '../../app/useTheme'
import { getNotesSettings, updateNotesSettings } from './notesSettingsApi'

export function SettingsPage() {
  const { setTheme, theme } = useTheme()
  const [rootPath, setRootPath] = useState('')
  const [status, setStatus] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const canEditNotesSettings = isLocalEnvironment()

  useEffect(() => {
    let ignore = false

    async function load() {
      try {
        const settings = await getNotesSettings()

        if (!ignore) {
          setRootPath(settings.rootPath)
        }
      } catch {
        if (!ignore) {
          setStatus('Unable to load notes settings.')
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

  async function saveNotesSettings(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!canEditNotesSettings) {
      return
    }

    try {
      setStatus('')
      const settings = await updateNotesSettings({ rootPath })
      setRootPath(settings.rootPath)
      setStatus('Notes settings saved.')
    } catch {
      setStatus('Save failed.')
    }
  }

  return (
    <section className="settings-page" aria-label="Settings">
      <p className="eyebrow settings-eyebrow">Settings</p>

      {status ? <p className="settings-status">{status}</p> : null}

      <section className="settings-panel settings-inline-panel">
        <div className="settings-inline-row">
          <h2>Appearance</h2>
          <div className="theme-segmented-control" role="group" aria-label="Theme">
            <ThemeButton active={theme === 'dark'} label="Dark" onClick={() => setTheme('dark')} />
            <ThemeButton active={theme === 'light'} label="Light" onClick={() => setTheme('light')} />
            <ThemeButton active={theme === 'e-ink'} label="E-ink" onClick={() => setTheme('e-ink')} />
          </div>
        </div>

        <form className="settings-inline-row" onSubmit={(event) => void saveNotesSettings(event)}>
          <h2>Notes</h2>
          <div className="settings-inline-field-group">
            <label className="settings-inline-field">
              <span className="sr-only">Notes root path</span>
              <input
                aria-label="Notes root path"
                disabled={isLoading || !canEditNotesSettings}
                name="rootPath"
                onChange={(event) => setRootPath(event.target.value)}
                placeholder="/srv/codecafe/notes"
                readOnly={!canEditNotesSettings}
                value={rootPath}
              />
            </label>
            {canEditNotesSettings ? (
              <button disabled={isLoading} type="submit">
                Save
              </button>
            ) : (
              <span className="settings-readonly-badge">Read-only</span>
            )}
          </div>
        </form>

        <Link className="settings-inline-row settings-link-row" to="/settings/ai">
          <h2>AI</h2>
          <span className="settings-link-copy">Providers, formats, models, and capabilities</span>
        </Link>
      </section>
    </section>
  )
}

function ThemeButton({
  active,
  label,
  onClick,
}: {
  active: boolean
  label: string
  onClick: () => void
}) {
  return (
    <button
      aria-pressed={active}
      className="theme-option"
      onClick={onClick}
      type="button"
    >
      {label}
    </button>
  )
}
