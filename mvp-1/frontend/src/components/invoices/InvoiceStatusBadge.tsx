type Status = 'Draft' | 'Submitted' | 'Accepted' | 'Rejected'

const colors: Record<Status, string> = {
  Draft: 'bg-gray-100 text-gray-600',
  Submitted: 'bg-yellow-100 text-yellow-700',
  Accepted: 'bg-green-100 text-green-700',
  Rejected: 'bg-red-100 text-red-700'
}

const labels: Record<Status, string> = {
  Draft: 'Szkic',
  Submitted: 'Wysłana',
  Accepted: 'Zatwierdzona',
  Rejected: 'Odrzucona'
}

export function InvoiceStatusBadge({ status }: { status: string }) {
  const s = status as Status
  return <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${colors[s] ?? 'bg-gray-100 text-gray-500'}`}>{labels[s] ?? status}</span>
}
