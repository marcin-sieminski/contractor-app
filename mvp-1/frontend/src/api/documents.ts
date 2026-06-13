import api from './client'
import type { FinancialDocument, StatementGranularity, StatementType } from '../types/document'

export const getFinancialDocument = (
  year: number,
  type: StatementType,
  granularity: StatementGranularity,
) =>
  api
    .get<FinancialDocument>(`/documents/${year}`, { params: { type, granularity } })
    .then(r => r.data)

/** Pobiera PDF zestawienia i zapisuje jako plik w przeglądarce. */
export const downloadDocumentPdf = async (
  year: number,
  type: StatementType,
  granularity: StatementGranularity,
) => {
  const response = await api.get(`/documents/${year}/pdf`, {
    params: { type, granularity },
    responseType: 'blob',
  })
  const disposition: string = response.headers['content-disposition'] ?? ''
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition)
  const fileName = match?.[1] ? decodeURIComponent(match[1]) : `dokument_${year}.pdf`

  const url = URL.createObjectURL(response.data as Blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  URL.revokeObjectURL(url)
}
