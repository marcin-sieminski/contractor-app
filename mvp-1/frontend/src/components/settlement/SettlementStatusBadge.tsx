import { CheckCircle2, PencilLine } from 'lucide-react'
import type { SettlementStatus } from '../../types/settlement'

export function SettlementStatusBadge({ status, finalizedAt }: { status: SettlementStatus; finalizedAt?: string | null }) {
  if (status === 'final') {
    const date = finalizedAt ? new Date(finalizedAt).toLocaleDateString('pl-PL') : null
    return (
      <span className="inline-flex items-center gap-1.5 text-xs font-medium px-2.5 py-1 rounded-full bg-green-50 dark:bg-green-900/30 text-green-700 dark:text-green-400 border border-green-200 dark:border-green-800">
        <CheckCircle2 size={13} />
        Zatwierdzone{date ? ` · ${date}` : ''}
      </span>
    )
  }
  return (
    <span className="inline-flex items-center gap-1.5 text-xs font-medium px-2.5 py-1 rounded-full bg-amber-50 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 border border-amber-200 dark:border-amber-800">
      <PencilLine size={13} />
      Robocze
    </span>
  )
}
