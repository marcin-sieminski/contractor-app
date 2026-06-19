import { useState } from 'react'
import { Plus } from 'lucide-react'
import { TimerWidget } from '../components/timer/TimerWidget'
import { TimeEntryList } from '../components/time-entries/TimeEntryList'
import { ManualEntryForm } from '../components/time-entries/ManualEntryForm'

export function TimeTrackingPage() {
  const [showManual, setShowManual] = useState(false)

  return (
    <div className="p-4 md:p-6">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Czas pracy</h1>
        <button onClick={() => setShowManual(true)}
          className="flex items-center gap-2 text-sm px-3 py-1.5 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800">
          <Plus size={16} /> Dodaj ręcznie
        </button>
      </div>
      <div className="mb-4">
        <TimerWidget />
      </div>
      <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">
        <TimeEntryList />
      </div>
      {showManual && <ManualEntryForm onClose={() => setShowManual(false)} />}
    </div>
  )
}
