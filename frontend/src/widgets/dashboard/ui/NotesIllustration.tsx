export default function NotesIllustration() {
  return (
    <div className="relative w-36 h-28 shrink-0">
      <div className="absolute inset-0 bg-surface rounded-xl border border-border-subtle shadow-sm p-2.5 flex flex-col gap-1.5">
        <div className="flex items-center gap-1.5">
          <div className="h-2 w-2 rounded-full bg-brand-brown/30" />
          <div className="h-1.5 w-14 bg-surface-active rounded-full" />
        </div>
        <div className="h-px bg-surface-hover" />
        <div className="flex items-center gap-1.5">
          <div className="h-2 w-2 rounded-full bg-surface-active" />
          <div className="h-1.5 w-20 bg-surface-active rounded-full" />
        </div>
        <div className="flex items-center gap-1.5">
          <div className="h-2 w-2 rounded-full bg-surface-active" />
          <div className="h-1.5 w-16 bg-surface-active rounded-full" />
        </div>
        <div className="flex items-center gap-1.5">
          <div className="h-2 w-2 rounded-full bg-surface-active" />
          <div className="h-1.5 w-12 bg-surface-active rounded-full" />
        </div>
      </div>
      {/* Overlapping card effect */}
      <div className="absolute -right-2 -top-1 w-24 h-20 bg-surface rounded-lg border border-border-subtle shadow-md p-2 flex flex-col gap-1">
        <div className="h-1 w-8 bg-brand-brown/30 rounded-full" />
        <div className="h-px bg-surface-hover" />
        <div className="flex items-center gap-1.5">
          <div className="h-1.5 w-1.5 rounded-full bg-brand-brown/20" />
          <div className="h-1 w-10 bg-surface-active rounded-full" />
        </div>
        <div className="flex items-center gap-1.5">
          <div className="h-1.5 w-1.5 rounded-full bg-surface-active" />
          <div className="h-1 w-8 bg-surface-active rounded-full" />
        </div>
      </div>
    </div>
  )
}
