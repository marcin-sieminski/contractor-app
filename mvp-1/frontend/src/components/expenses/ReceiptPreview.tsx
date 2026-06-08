import { useEffect, useState } from 'react'
import { Loader2, X } from 'lucide-react'
import { getReceiptBlob } from '../../api/expenses'
import type { Expense } from '../../types/expense'

interface Props {
  expense: Expense
  onClose: () => void
}

/** Modal z podglądem oryginalnego skanu. Pobiera plik jako Blob (z Bearer) i pokazuje przez objectURL. */
export function ReceiptPreview({ expense, onClose }: Props) {
  const [url, setUrl] = useState<string | null>(null)
  const [isPdf, setIsPdf] = useState(false)
  const [error, setError] = useState(false)

  useEffect(() => {
    let objectUrl: string | null = null
    let cancelled = false
    getReceiptBlob(expense.id)
      .then(blob => {
        if (cancelled) return
        setIsPdf(blob.type === 'application/pdf')
        objectUrl = URL.createObjectURL(blob)
        setUrl(objectUrl)
      })
      .catch(() => !cancelled && setError(true))
    return () => {
      cancelled = true
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [expense.id])

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4" onClick={onClose}>
      <div
        className="bg-white dark:bg-gray-800 rounded-xl shadow-xl max-w-2xl w-full max-h-[90vh] overflow-auto"
        onClick={e => e.stopPropagation()}
      >
        <div className="flex items-center justify-between px-5 py-3 border-b border-gray-100 dark:border-gray-700">
          <span className="text-sm font-medium text-gray-700 dark:text-gray-200 truncate">
            Skan: {expense.description}
          </span>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 p-1">
            <X size={18} />
          </button>
        </div>
        <div className="p-4 flex items-center justify-center min-h-[200px]">
          {error
            ? <p className="text-red-600 dark:text-red-400 text-sm">Nie udało się wczytać skanu.</p>
            : url
              ? isPdf
                ? <iframe src={url} title="Skan paragonu (PDF)" className="w-full h-[75vh] rounded border-0" />
                : <img src={url} alt="Skan paragonu" className="max-w-full max-h-[75vh] object-contain rounded" />
              : <Loader2 className="animate-spin text-gray-400" size={28} />}
        </div>
      </div>
    </div>
  )
}
