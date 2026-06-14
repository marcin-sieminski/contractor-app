export type EventStatus = 'ok' | 'tight' | 'danger' | 'overdue' | 'paid'
export type ObligationStatus = 'ok' | 'tight' | 'danger'

export interface CashFlowEvent {
  date: string
  kind: 'invoice' | 'tax'
  label: string
  amount: number
  status: EventStatus
}

export interface CashFlowDay {
  date: string
  dayOfWeek: string
  inflows: number
  outflows: number
  balance: number
  isObligationDay: boolean
  obligationStatus: ObligationStatus | null
}

export interface CashFlowForecast {
  fromDate: string
  toDate: string
  startingBalance: number
  totalExpectedInflows: number
  totalObligations: number
  totalAlreadyPaid: number
  projectedEndBalance: number
  monthlyBufferRecommended: number
  avgMonthlyObligations: number
  avgDsodays: number
  days: CashFlowDay[]
  events: CashFlowEvent[]
}
