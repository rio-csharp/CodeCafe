function SkeletonCard() {
  return (
    <div className="rounded-xl border border-gray-100 bg-white p-5 animate-pulse">
      <div className="flex items-start gap-4">
        <div className="h-10 w-10 rounded-lg bg-gray-100 shrink-0" />
        <div className="flex-1 space-y-2 min-w-0">
          <div className="h-4 w-28 bg-gray-100 rounded" />
          <div className="h-3 w-full bg-gray-100 rounded" />
          <div className="h-3 w-2/3 bg-gray-100 rounded" />
        </div>
      </div>
      <div className="mt-4 flex items-center justify-between">
        <div className="h-3 w-16 bg-gray-100 rounded" />
        <div className="h-3 w-20 bg-gray-100 rounded" />
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
