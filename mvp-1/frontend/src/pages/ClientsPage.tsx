import { useState } from 'react'
import { Plus } from 'lucide-react'
import { ClientList } from '../components/clients/ClientList'
import { ClientForm } from '../components/clients/ClientForm'

export function ClientsPage() {
  const [showForm, setShowForm] = useState(false)

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-2xl font-bold">Klienci</h1>
        <button onClick={() => setShowForm(true)}
          className="flex items-center gap-2 text-sm px-3 py-1.5 bg-blue-600 text-white rounded-lg hover:bg-blue-700">
          <Plus size={16} /> Dodaj klienta
        </button>
      </div>
      <div className="bg-white rounded-xl border border-gray-200">
        <ClientList />
      </div>
      {showForm && <ClientForm onClose={() => setShowForm(false)} />}
    </div>
  )
}
