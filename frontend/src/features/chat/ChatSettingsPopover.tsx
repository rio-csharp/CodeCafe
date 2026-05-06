import type { AiProviderModel } from '../ai/aiSettingsStore'
import type { ChatPreferences } from './chatTypes'

export function ChatSettingsPopover({
  chatPreferences,
  isOpen,
  onClose,
  onPreferencesChange,
  selectedModel,
}: {
  chatPreferences: ChatPreferences
  isOpen: boolean
  onClose: () => void
  onPreferencesChange: (updater: (currentPreferences: ChatPreferences) => ChatPreferences) => void
  selectedModel: AiProviderModel | null
}) {
  if (!isOpen) {
    return null
  }

  return (
    <>
      <button
        aria-label="Close chat settings"
        className="chat-settings-backdrop"
        onClick={onClose}
        type="button"
      />
      <section className="chat-settings-popover" aria-label="Chat controls">
        <label className="chat-setting-field chat-setting-field-wide">
          <span>System prompt</span>
          <textarea
            onChange={(event) =>
              onPreferencesChange((currentPreferences) => ({
                ...currentPreferences,
                systemPrompt: event.target.value,
              }))
            }
            placeholder="Set the assistant behavior for this workspace..."
            rows={3}
            value={chatPreferences.systemPrompt}
          />
        </label>

        <label className="chat-setting-field chat-setting-field-compact">
          <span>Temperature</span>
          <input
            max={2}
            min={0}
            onChange={(event) =>
              onPreferencesChange((currentPreferences) => ({
                ...currentPreferences,
                temperature: event.target.value ? Number(event.target.value) : null,
              }))
            }
            step={0.1}
            type="number"
            value={chatPreferences.temperature ?? selectedModel?.defaultTemperature ?? 0.7}
          />
        </label>

        <label className="chat-setting-field chat-setting-field-compact">
          <span>Max output tokens</span>
          <input
            max={selectedModel?.maxOutputTokens ?? 32768}
            min={1}
            onChange={(event) =>
              onPreferencesChange((currentPreferences) => ({
                ...currentPreferences,
                maxOutputTokens: event.target.value ? Number(event.target.value) : null,
              }))
            }
            step={1}
            type="number"
            value={chatPreferences.maxOutputTokens ?? selectedModel?.defaultMaxOutputTokens ?? 2048}
          />
        </label>
      </section>
    </>
  )
}
