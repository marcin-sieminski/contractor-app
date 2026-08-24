export type TaxPaymentType = 'PIT' | 'ZusSocial' | 'ZusHealth' | 'VAT'

export interface TaxPaymentRecord {
  id: string
  year: number
  month: number
  type: TaxPaymentType
  amount: number
  paidAt: string | null
  notes: string | null
}

export interface MonthObligation {
  month: number
  monthName: string
  isActual: boolean
  // Naliczone
  pitDue: number
  zusSocialDue: number
  zusHealthDue: number
  vatDue: number
  totalDue: number
  // Terminy
  zusDueDate: string
  pitDueDate: string
  vatDueDate: string
  // Zapłacone
  payments: TaxPaymentRecord[]
  pitPaid: number
  zusSocialPaid: number
  zusHealthPaid: number
  vatPaid: number
  totalPaid: number
  // Status: future | paid | partial | due | overdue
  status: 'future' | 'paid' | 'partial' | 'due' | 'overdue'
}

export interface TaxObligationsSummary {
  totalPitDue: number
  totalZusSocialDue: number
  totalZusHealthDue: number
  totalVatDue: number
  totalDue: number
  totalPitPaid: number
  totalZusSocialPaid: number
  totalZusHealthPaid: number
  totalVatPaid: number
  totalPaid: number
  totalRemaining: number
}

export interface TaxObligations {
  year: number
  taxForm: string
  zusStage: string
  summary: TaxObligationsSummary
  months: MonthObligation[]
}
