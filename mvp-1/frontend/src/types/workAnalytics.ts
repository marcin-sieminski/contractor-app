export interface WorkAnalyticsSummary {
  totalHours: number
  invoicedHours: number
  pendingHours: number
  invoicedPercent: number
  workedDays: number
  businessDaysInPeriod: number
  freeDays: number
  avgHoursPerWorkedDay: number
  avgHoursPerWeek: number
  unbilledOlderThan30: number
}

export interface MonthWork {
  month: number
  monthName: string
  totalHours: number
  invoicedHours: number
  pendingHours: number
  invoicedPercent: number
  workedDays: number
}

export interface DayWork {
  date: string
  hours: number
  hasInvoiced: boolean
}

export interface UnbilledAlert {
  entryId: string
  projectName: string
  clientName: string
  hours: number
  startedAt: string
  daysAgo: number
}

export interface WorkAnalytics {
  year: number
  summary: WorkAnalyticsSummary
  months: MonthWork[]
  days: DayWork[]
  unbilledAlerts: UnbilledAlert[]
}
