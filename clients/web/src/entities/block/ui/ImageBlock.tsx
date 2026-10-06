import type { ImageContent } from '../model/types'

/** Meaningful images carry alt text; decorative ones opt out with an empty alt. */
export function ImageBlock({ content }: { content: ImageContent }) {
  return (
    <figure className="flex flex-col gap-2">
      <img
        src={content.url}
        alt={content.isDecorative ? '' : (content.alt ?? '')}
        loading="lazy"
        className="rounded-xl border border-line"
      />
      {content.caption === null ? null : (
        <figcaption className="text-sm text-muted">{content.caption}</figcaption>
      )}
    </figure>
  )
}
