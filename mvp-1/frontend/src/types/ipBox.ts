export interface UnfilledIpEntryDto {
  timeEntryId: string
  projectName: string
  startedAt: string
  durationMinutes: number | null
  description: string
}

export interface MonthIpDataDto {
  month: number
  monthName: string
  ipHours: number
  totalHours: number
  ipRevenuePln: number
  totalRevenuePln: number
  qualifyingRevenuePln: number
}

export interface IpBoxProgressDto {
  year: number
  nexusCoefficient: number
  totalHours: number
  ipHours: number
  ipHoursPercent: number
  totalRevenuePln: number
  ipRevenuePln: number
  qualifyingRevenuePln: number
  taxSavingsPln: number
  projectedYearSavingsPln: number
  monthlyData: MonthIpDataDto[]
  unfilledEntries: UnfilledIpEntryDto[]
}
