export default function CodesIllustration() {
  return (
    <div className="relative w-36 h-28 shrink-0">
      <div className="absolute inset-0 bg-white rounded-xl border border-gray-100 shadow-sm p-2 flex flex-col gap-1">
        <div className="flex items-center gap-1 text-[10px] text-gray-300 font-mono">
          <span>&lt;/&gt;</span>
        </div>
        {[1, 2, 3, 4].map((i) => (
          <div key={i} className="flex items-center gap-1.5">
            <span className="text-[9px] text-gray-300 w-3 text-right">{i}</span>
            <div
              className={`h-0.5 rounded-full ${
                i === 1 ? 'w-14 bg-brand-brown/20' : i === 3 ? 'w-10 bg-brand-brown/20' : 'w-12 bg-gray-100'
              }`}
            />
          </div>
        ))}
      </div>
      <div className="absolute -right-2 -top-1 w-24 h-20 bg-white rounded-lg border border-gray-100 shadow-md p-2">
        <div className="flex items-center gap-1 text-[10px] text-gray-300 font-mono mb-1">
          <span>&lt;/&gt;</span>
        </div>
        {[1, 2, 3].map((i) => (
          <div key={i} className="flex items-center gap-1.5 mb-1">
            <span className="text-[9px] text-gray-300 w-3 text-right">{i}</span>
            <div
              className={`h-0.5 rounded-full ${
                i === 2 ? 'w-8 bg-brand-brown/30' : 'w-10 bg-gray-100'
              }`}
            />
          </div>
        ))}
      </div>
    </div>
  )
}
