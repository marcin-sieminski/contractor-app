export type ExpenseCategory =
  | 'Software' | 'Hardware' | 'Office' | 'Training'
  | 'Travel' | 'Phone' | 'Insurance' | 'Accounting'
  | 'Marketing' | 'Other' | 'Literature'

export const EXPENSE_CATEGORY_LABELS: Record<ExpenseCategory, string> = {
  Software:   'Oprogramowanie',
  Hardware:   'Sprzęt',
  Office:     'Biuro i wyposażenie',
  Training:   'Szkolenia i kursy',
  Travel:     'Podróże',
  Phone:      'Telefon i internet',
  Insurance:  'Ubezpieczenie',
  Accounting: 'Księgowość',
  Marketing:  'Marketing',
  Other:      'Inne',
  Literature: 'Literatura',
}

export interface Expense {
  id: string
  date: string
  category: ExpenseCategory
  description: string
  amount: number
  currency: string
  exchangeRate?: number
  amountPLN: number
  isVatDeductible: boolean
  receiptNumber?: string
  vendorName?: string
  vendorNip?: string
  netAmount?: number
  vatAmount?: number
  receiptId?: string
  hasReceipt: boolean
  createdAt: string
}

export type OcrProvider = 'claude' | 'ollama'

/** Dane rozpoznane ze skanu paragonu/faktury (wszystkie pola opcjonalne). */
export interface ReceiptExtraction {
  date?: string
  vendorName?: string
  vendorNip?: string
  grossAmount?: number
  netAmount?: number
  vatAmount?: number
  currency?: string
  receiptNumber?: string
  isVatDeductible?: boolean
  category?: ExpenseCategory
  categoryConfidence?: number
}

export interface ScanReceiptResult {
  receiptId: string
  extracted: ReceiptExtraction
}
