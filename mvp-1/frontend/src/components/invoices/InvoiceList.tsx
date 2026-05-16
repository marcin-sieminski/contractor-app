import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getInvoices, submitToKsef } from '../../api/invoices'
import { InvoiceStatusBadge } from './InvoiceStatusBadge'
import { InvoiceDetailModal } from './InvoiceDetailModal'
import { InvoiceEditModal } from './InvoiceEditModal'
import { Send, Eye, Pencil } from 'lucide-react'

export function InvoiceList() {
  const qc = useQueryClient()
  const { data: invoices = [], isLoading } = useQuery({ queryKey: ['invoices'], queryFn: () => getInvoices() })

  const [detailId, setDetailId] = useState<string | null>(null)
  const [editId, setEditId] = useState<string | null>(null)

  const submitMutation = useMutation({
    mutationFn: submitToKsef,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['invoices'] })
  })

  if (isLoading) return <div className="text-gray-500 p-4 text-sm">Ładowanie...</div>
  if (invoices.length === 0)
    return <div className="text-gray-400 text-sm p-4 text-center">Brak faktur.</div>

  return (
    <>
      <div className="divide-y divide-gray-100">
        {invoices.map(inv => (
          <div key={inv.id} className="px-4 py-3 hover:bg-gray-50 flex items-center gap-3">
            <div className="flex-1">
              <div className="flex items-center gap-2">
                <span className="font-medium text-sm">{inv.invoiceNumber}</span>
                <InvoiceStatusBadge status={inv.status} />
                {inv.ksefReferenceNumber && (
                  <span className="text-xs text-green-600 font-mono" title="Numer KSeF">{inv.ksefReferenceNumber.slice(0, 20)}…</span>
                )}
              </div>
              <div className="text-gray-500 text-xs mt-0.5">{inv.clientName} · {inv.issueDate}</div>
              {inv.ksefError && <div className="text-red-500 text-xs mt-0.5">{inv.ksefError}</div>}
            </div>
            <div className="text-right">
              <div className="font-semibold text-sm">{inv.totalGross.toFixed(2)} {inv.currency}</div>
              <div className="text-gray-400 text-xs">netto: {inv.totalNet.toFixed(2)}</div>
            </div>
            <div className="flex items-center gap-1 ml-2">
              <button
                onClick={() => setDetailId(inv.id)}
                title="Szczegóły faktury"
                className="text-gray-400 hover:text-gray-700 p-1.5 rounded-lg hover:bg-gray-100"
              >
                <Eye size={14} />
              </button>
              {inv.status === 'Draft' && (
                <>
                  <button
                    onClick={() => setEditId(inv.id)}
                    title="Edytuj fakturę"
                    className="text-gray-400 hover:text-blue-600 p-1.5 rounded-lg hover:bg-blue-50"
                  >
                    <Pencil size={14} />
                  </button>
                  <button
                    onClick={() => submitMutation.mutate(inv.id)}
                    disabled={submitMutation.isPending}
                    title="Wyślij do KSeF"
                    className="bg-blue-600 text-white px-3 py-1.5 rounded-lg text-xs flex items-center gap-1 hover:bg-blue-700 disabled:opacity-50"
                  >
                    <Send size={12} /> KSeF
                  </button>
                </>
              )}
            </div>
          </div>
        ))}
      </div>

      {detailId && (
        <InvoiceDetailModal
          invoiceId={detailId}
          onClose={() => setDetailId(null)}
          onEdit={() => { const id = detailId; setDetailId(null); setEditId(id) }}
        />
      )}

      {editId && (
        <InvoiceEditModal
          invoiceId={editId}
          onClose={() => setEditId(null)}
          onSaved={() => setEditId(null)}
        />
      )}
    </>
  )
}
