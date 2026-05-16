export interface LineItem {
  lineNumber: number
  description: string
  quantity: number
  unit: string
  unitPrice: number
  vatRate: number
  netAmount: number
  grossAmount: number
}

export interface Invoice {
  id: string
  invoiceNumber: string
  clientName: string
  clientNip: string
  issueDate: string
  serviceDate: string
  dueDate: string
  status: 'Draft' | 'Submitted' | 'Accepted' | 'Rejected'
  vatTreatment: string
  currency: string
  exchangeRate?: number
  exchangeRateDate?: string
  exchangeRateTableNumber?: string
  totalNet: number
  totalVat: number
  totalGross: number
  ksefReferenceNumber?: string
  ksefSubmittedAt?: string
  ksefError?: string
  lineItems: LineItem[]
}
