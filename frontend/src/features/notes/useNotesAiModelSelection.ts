import { useEffect, useMemo, useState } from 'react'
import {
  getEnabledModelOptions,
  loadAiSettings,
  resolveModelSelection,
  type AiSettings,
} from '../ai/aiSettingsStore'

export function useNotesAiModelSelection() {
  const [aiSettings, setAiSettings] = useState<AiSettings>(() => loadAiSettings())
  const [selectedModelValue, setSelectedModelValue] = useState<string | null>(() =>
    resolveModelSelection(loadAiSettings(), null),
  )

  const enabledModelOptions = useMemo(() => getEnabledModelOptions(aiSettings), [aiSettings])

  const selectedModelOption = useMemo(() => {
    const resolvedValue = resolveModelSelection(aiSettings, selectedModelValue)

    return enabledModelOptions.find((option) => option.value === resolvedValue) ?? null
  }, [aiSettings, enabledModelOptions, selectedModelValue])

  useEffect(() => {
    const syncSettings = () => {
      const nextSettings = loadAiSettings()
      setAiSettings(nextSettings)
      setSelectedModelValue((currentValue) => resolveModelSelection(nextSettings, currentValue))
    }

    window.addEventListener('storage', syncSettings)
    window.addEventListener('focus', syncSettings)

    return () => {
      window.removeEventListener('storage', syncSettings)
      window.removeEventListener('focus', syncSettings)
    }
  }, [])

  return {
    enabledModelOptions,
    selectedModel: selectedModelOption?.model ?? null,
    selectedModelOption,
    selectedModelValue,
    selectedProvider: selectedModelOption?.provider ?? null,
    setSelectedModelValue,
  }
}
