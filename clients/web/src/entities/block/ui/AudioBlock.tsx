import type { AudioContent } from '../model/types'

export function AudioBlock({ content }: { content: AudioContent }) {
  return (
    <audio controls preload="metadata" className="w-full">
      <source src={content.url} type={content.mimeType} />
    </audio>
  )
}
