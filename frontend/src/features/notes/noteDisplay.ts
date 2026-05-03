export function toDisplayName(value: string) {
  const withoutExtension = value.replace(/\.(md|markdown|txt)$/i, '')
  const withoutOrderPrefix = withoutExtension.replace(/^\d+[\s._-]+/, '')

  return withoutOrderPrefix
    .replace(/[-_]+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .replace(/\b\w/g, (letter) => letter.toUpperCase())
}

export function formatFileSize(sizeBytes: number) {
  if (sizeBytes < 1024) {
    return `${sizeBytes} B`
  }

  return `${(sizeBytes / 1024).toFixed(1)} KB`
}

export function formatReadingTime(content: string) {
  const latinWordCount = content.match(/[A-Za-z0-9]+(?:[-'][A-Za-z0-9]+)*/g)?.length ?? 0
  const cjkCharacterCount = content.match(/[\u4e00-\u9fff]/g)?.length ?? 0
  const estimatedWords = latinWordCount + cjkCharacterCount / 2
  const minutes = Math.max(1, Math.ceil(estimatedWords / 240))

  return `${minutes} min read`
}
