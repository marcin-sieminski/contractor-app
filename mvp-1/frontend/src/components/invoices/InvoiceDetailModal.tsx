import { useQuery } from '@tanstack/react-query'
import { getInvoiceById, getInvoiceXml } from '../../api/invoices'
import { InvoiceStatusBadge } from './InvoiceStatusBadge'
import { X, Copy } from 'lucide-react'
import { useState } from 'react'

interface Props {
  invoiceId: string
  onClose: () => void
  onEdit?: () => void
}

export function InvoiceDetailModal({ invoiceId, onClose, onEdit }: Props) {
  const { data: invoice, isLoading } = useQuery({
    queryKey: ['invoice', invoiceId],
    queryFn: () => getInvoiceById(invoiceId)
  })
  const [showXml, setShowXml] = useState(false)
  const { data: xml } = useQuery({
    queryKey: ['invoiceXml', invoiceId],
    queryFn: () => getInvoiceXml(invoiceId),
    enabled: showXml
  })

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-xl shadow-2xl w-full max-w-2xl max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <div className="flex items-center gap-3">
            <h2 className="text-lg font-semibold">
              {isLoading ? 'Ładowanie...' : invoice?.invoiceNumber}
            </h2>
            {invoice && <InvoiceStatusBadge status={invoice.status} />}
          </div>
          <div className="flex items-center gap-2">
            {invoice?.status === 'Draft' && onEdit && (
              <button
                onClick={onEdit}
                className="text-sm px-3 py-1.5 border border-gray-300 rounded-lg hover:bg-gray-50"
              >
                Edytuj
              </button>
            )}
            <button onClick={onClose} className="text-gray-400 hover:text-gray-600 p-1">
              <X size={20} />
            </button>
          </div>
        </div>

        {/* Content */}
        <div className="overflow-y-auto flex-1 px-6 py-4 space-y-5">
          {isLoading && <div className="text-gray-400 text-sm text-center py-8">Ładowanie...</div>}
          {invoice && (
            <>
              {/* Parties */}
              <div className="grid grid-cols-2 gap-4">
                <div className="bg-gray-50 rounded-lg p-3">
                  <div className="text-xs text-gray-400 uppercase tracking-wide mb-1">Nabywca</div>
                  <div className="font-medium text-sm">{invoice.clientName}</div>
                  <div className="text-xs text-gray-500">NIP: {invoice.clientNip}</div>
                </div>
                <div className="bg-gray-50 rounded-lg p-3">
                  <div className="text-xs text-gray-400 uppercase tracking-wide mb-1">Daty</div>
                  <div className="text-xs text-gray-600 space-y-0.5">
                    <div>Wystawienia: <span className="font-medium">{invoice.issueDate}</span></div>
                    <div>Sprzedaży: <span className="font-medium">{invoice.serviceDate}</span></div>
                    <div>Płatności: <span className="font-medium">{invoice.dueDate}</span></div>
                  </div>
                </div>
              </div>

              {/* VAT / Currency info */}
              <div className="flex gap-3 text-xs">
                <span className="bg-blue-50 text-blue-700 px-2 py-1 rounded">
                  VAT: {invoice.vatTreatment === 'Domestic23' ? '23%' : invoice.vatTreatment === 'ReverseCharge' ? 'Odwrotne obciążenie' : 'np.'}
                </span>
                <span className="bg-gray-100 text-gray-600 px-2 py-1 rounded">
                  Waluta: {invoice.currency}
                </span>
                {invoice.exchangeRate && (
                  <span className="bg-yellow-50 text-yellow-700 px-2 py-1 rounded">
                    Kurs NBP: {invoice.exchangeRate} ({invoice.exchangeRateDate}, tab. {invoice.exchangeRateTableNumber})
                  </span>
                )}
              </div>

              {/* Line items */}
              <div>
                <div className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">Pozycje</div>
                <table className="w-full text-sm">
                  <thead>
                    <tr className="text-xs text-gray-400 border-b border-gray-100">
                      <th className="text-left pb-1 font-medium">Opis</th>
                      <th className="text-right pb-1 font-medium w-16">Ilość</th>
                      <th className="text-right pb-1 font-medium w-20">Cena/j.</th>
                      <th className="text-right pb-1 font-medium w-8">VAT</th>
                      <th className="text-right pb-1 font-medium w-24">Netto</th>
                      <th className="text-right pb-1 font-medium w-24">Brutto</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-50">
                    {invoice.lineItems.map(l => (
                      <tr key={l.lineNumber}>
                        <td className="py-1.5 pr-2">{l.description}</td>
                        <td className="text-right py-1.5 font-mono text-xs">{l.quantity} {l.unit}</td>
                        <td className="text-right py-1.5 font-mono text-xs">{l.unitPrice.toFixed(2)}</td>
                        <td className="text-right py-1.5 text-xs text-gray-500">
                          {l.vatRate === 0 ? '0%' : `${(l.vatRate * 100).toFixed(0)}%`}
                        </td>
                        <td className="text-right py-1.5 font-mono text-xs">{l.netAmount.toFixed(2)}</td>
                        <td className="text-right py-1.5 font-mono text-xs font-medium">{l.grossAmount.toFixed(2)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Totals */}
              <div className="border-t border-gray-100 pt-3 space-y-1 text-sm">
                <div className="flex justify-between text-gray-500">
                  <span>Wartość netto</span>
                  <span className="font-mono">{invoice.totalNet.toFixed(2)} {invoice.currency}</span>
                </div>
                <div className="flex justify-between text-gray-500">
                  <span>Podatek VAT</span>
                  <span className="font-mono">{invoice.totalVat.toFixed(2)} {invoice.currency}</span>
                </div>
                <div className="flex justify-between font-semibold text-base pt-1 border-t border-gray-200">
                  <span>Do zapłaty</span>
                  <span className="font-mono">{invoice.totalGross.toFixed(2)} {invoice.currency}</span>
                </div>
              </div>

              {/* KSeF info */}
              {(invoice.ksefReferenceNumber || invoice.ksefError) && (
                <div className={`rounded-lg p-3 text-sm ${invoice.ksefReferenceNumber ? 'bg-green-50' : 'bg-red-50'}`}>
                  <div className="text-xs font-semibold uppercase tracking-wide mb-1 text-gray-500">KSeF</div>
                  {invoice.ksefReferenceNumber && (
                    <div className="flex items-center gap-2">
                      <span className="text-green-700 font-mono text-xs break-all">{invoice.ksefReferenceNumber}</span>
                      <button
                        onClick={() => navigator.clipboard.writeText(invoice.ksefReferenceNumber!)}
                        className="text-green-600 hover:text-green-800 flex-shrink-0"
                        title="Kopiuj"
                      >
                        <Copy size={12} />
                      </button>
                    </div>
                  )}
                  {invoice.ksefSubmittedAt && (
                    <div className="text-xs text-gray-500 mt-0.5">
                      Wysłano: {new Date(invoice.ksefSubmittedAt).toLocaleString('pl-PL')}
                    </div>
                  )}
                  {invoice.ksefError && (
                    <div className="text-red-600 text-xs">{invoice.ksefError}</div>
                  )}
                </div>
              )}

              {/* XML viewer */}
              <div>
                <button
                  onClick={() => setShowXml(v => !v)}
                  className="text-xs text-gray-400 hover:text-gray-600 underline"
                >
                  {showXml ? 'Ukryj XML FA(3)' : 'Pokaż XML FA(3)'}
                </button>
                {showXml && xml && (
                  <pre className="mt-2 bg-gray-900 text-green-400 text-xs p-3 rounded-lg overflow-auto max-h-64 font-mono">
                    {xml}
                  </pre>
                )}
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
