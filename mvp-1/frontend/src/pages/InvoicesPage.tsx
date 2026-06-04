import { useState } from 'react'
import { Plus } from 'lucide-react'
import { InvoiceList } from '../components/invoices/InvoiceList'
import { InvoiceBuilder } from '../components/invoices/InvoiceBuilder'

export function InvoicesPage() {
  const [showBuilder, setShowBuilder] = useState(false)

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Faktury</h1>
        <button onClick={() => setShowBuilder(true)}
          className="flex items-center gap-2 text-sm px-3 py-1.5 bg-blue-600 text-white rounded-lg hover:bg-blue-700">
          <Plus size={16} /> Nowa faktura
        </button>
      </div>
      <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">
        <InvoiceList />
      </div>
      {showBuilder && <InvoiceBuilder onClose={() => setShowBuilder(false)} />}
    </div>
  )
}
