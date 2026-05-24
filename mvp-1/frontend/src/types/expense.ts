export type ExpenseCategory =
  | 'Software' | 'Hardware' | 'Office' | 'Training'
  | 'Travel' | 'Phone' | 'Insurance' | 'Accounting'
  | 'Marketing' | 'Other'

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
  createdAt: string
}
