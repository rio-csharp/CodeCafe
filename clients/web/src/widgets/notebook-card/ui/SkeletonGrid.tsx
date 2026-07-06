function SkeletonCard() {
  return (
    <div className="rounded-xl border border-border-subtle bg-surface p-5 animate-pulse">
      <div className="flex items-start gap-4">
        <div className="h-10 w-10 rounded-lg bg-surface-active shrink-0" />
        <div className="flex-1 space-y-2 min-w-0">
          <div className="h-4 w-28 bg-surface-active rounded" />
          <div className="h-3 w-full bg-surface-active rounded" />
          <div className="h-3 w-2/3 bg-surface-active rounded" />
        </div>
      </div>
      <div className="mt-4 flex items-center justify-between">
        <div className="h-3 w-16 bg-surface-active rounded" />
        <div className="h-3 w-20 bg-surface-active rounded" />
      </div>
    </div>
  )
}

interface SkeletonGridProps {
  count?: number
}

export default function SkeletonGrid({ count = 4 }: SkeletonGridProps) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      {Array.from({ length: count }).map((_, i) => (
        <SkeletonCard key={i} />
      ))}
    </div>
  )
}
