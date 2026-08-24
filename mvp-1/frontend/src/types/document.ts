export type StatementType = 'income_statement' | 'balance_sheet'
export type StatementGranularity = 'monthly' | 'quarterly' | 'annual'

// Wartości enumów serializowane przez backend jako nazwy składowych (PascalCase).
export type StatementColumnKind = 'Period' | 'Total'
export type StatementRowStyle = 'Item' | 'Subtotal' | 'Total' | 'Section' | 'Memo'

export const STATEMENT_TYPE_LABELS: Record<StatementType, string> = {
  income_statement: 'Rachunek wyników',
  balance_sheet: 'Bilans (uproszczony)',
}

export const GRANULARITY_LABELS: Record<StatementGranularity, string> = {
  monthly: 'Miesięcznie',
  quarterly: 'Kwartalnie',
  annual: 'Rocznie',
}

export interface StatementColumn {
  key: string
  label: string
  kind: StatementColumnKind
}

export interface StatementRow {
  key: string
  label: string
  indent: number
  style: StatementRowStyle
  values: (number | null)[]
  description?: string | null
  isPercent: boolean
}

export interface FinancialDocument {
  year: number
  type: StatementType
  typeLabel: string
  granularity: StatementGranularity
  title: string
  columns: StatementColumn[]
  rows: StatementRow[]
  notes: string[]
  dataWarnings: string[]
  hasData: boolean
}
