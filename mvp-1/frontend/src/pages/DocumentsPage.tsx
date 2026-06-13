import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { AlertTriangle, Download, FileSpreadsheet } from 'lucide-react'
import { downloadDocumentPdf, getFinancialDocument } from '../api/documents'
import { FinancialStatementTable } from '../components/documents/FinancialStatementTable'
import {
  GRANULARITY_LABELS,
  STATEMENT_TYPE_LABELS,
} from '../types/document'
import type { FinancialDocument, StatementGranularity, StatementType } from '../types/document'

const TYPES = Object.keys(STATEMENT_TYPE_LABELS) as StatementType[]
const GRANULARITIES = Object.keys(GRANULARITY_LABELS) as StatementGranularity[]

const fmt = (v: number) => v.toLocaleString('pl-PL', { maximumFractionDigits: 0 }) + ' zł'

/** Indeks kolumny podsumowania (roczna „Total" lub ostatnia migawka). */
const summaryIndex = (doc: FinancialDocument) => {
  const total = doc.columns.findIndex(c => c.kind === 'Total')
  return total >= 0 ? total : doc.columns.length - 1
}

const cellAt = (doc: FinancialDocument, rowKey: string, col: number): number | null => {
  const row = doc.rows.find(r => r.key === rowKey)
  return row ? row.values[col] ?? null : null
}

export function DocumentsPage() {
  const currentYear = new Date().getFullYear()
  const [year, setYear] = useState(currentYear)
  const [type, setType] = useState<StatementType>('income_statement')
  const [granularity, setGranularity] = useState<StatementGranularity>('monthly')

  const { data, isLoading, isError } = useQuery({
    queryKey: ['document', year, type, granularity],
    queryFn: () => getFinancialDocument(year, type, granularity),
    placeholderData: prev => prev,
  })

  const [downloading, setDownloading] = useState(false)
  const [downloadError, setDownloadError] = useState<string | null>(null)
  const download = async () => {
    setDownloading(true)
    setDownloadError(null)
    try {
      await downloadDocumentPdf(year, type, granularity)
    } catch (e: unknown) {
      let message = 'Nie udało się pobrać pliku PDF.'
      const response = (e as { response?: { data?: Blob } }).response
      if (response?.data instanceof Blob) {
        try {
          const detail = JSON.parse(await response.data.text())?.detail
          if (detail) message = detail
        } catch { /* zostaw komunikat ogólny */ }
      }
      setDownloadError(message)
    } finally {
      setDownloading(false)
    }
  }

  const sumCol = data ? summaryIndex(data) : 0

  return (
    <div className="p-6 max-w-6xl">
      <div className="flex items-center gap-2 mb-6">
        <FileSpreadsheet className="text-blue-600" size={24} />
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">Dokumenty</h1>
      </div>

      <div className="flex flex-wrap items-end gap-3 mb-6">
        <label className="flex flex-col text-xs font-medium text-gray-500 dark:text-gray-400">
          Rok
          <select
            value={year}
            onChange={e => setYear(Number(e.target.value))}
            className="mt-1 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm text-gray-900 dark:text-gray-100 bg-white dark:bg-gray-800"
          >
            {[4, 3, 2, 1, 0].map(d => currentYear - d).map(y => (
              <option key={y} value={y}>{y}</option>
            ))}
          </select>
        </label>

        <label className="flex flex-col text-xs font-medium text-gray-500 dark:text-gray-400">
          Dokument
          <div className="mt-1 flex rounded-lg border border-gray-300 dark:border-gray-600 overflow-hidden text-sm">
            {TYPES.map((t, i) => (
              <button
                key={t}
                onClick={() => setType(t)}
                className={`px-3 py-2 transition-colors whitespace-nowrap ${i > 0 ? 'border-l border-gray-300 dark:border-gray-600' : ''} ${
                  type === t
                    ? 'bg-blue-600 text-white'
                    : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'
                }`}
              >
                {STATEMENT_TYPE_LABELS[t]}
              </button>
            ))}
          </div>
        </label>

        <label className="flex flex-col text-xs font-medium text-gray-500 dark:text-gray-400">
          Ujęcie
          <div className="mt-1 flex rounded-lg border border-gray-300 dark:border-gray-600 overflow-hidden text-sm">
            {GRANULARITIES.map((g, i) => (
              <button
                key={g}
                onClick={() => setGranularity(g)}
                className={`px-3 py-2 transition-colors whitespace-nowrap ${i > 0 ? 'border-l border-gray-300 dark:border-gray-600' : ''} ${
                  granularity === g
                    ? 'bg-blue-600 text-white'
                    : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'
                }`}
              >
                {GRANULARITY_LABELS[g]}
              </button>
            ))}
          </div>
        </label>

        <button
          onClick={download}
          disabled={downloading || !data}
          className="inline-flex items-center gap-1.5 text-sm px-4 py-2 rounded-lg bg-blue-600 text-white hover:bg-blue-700 transition-colors disabled:opacity-50"
        >
          <Download size={15} />
          {downloading ? 'Generowanie…' : 'Pobierz PDF'}
        </button>
      </div>

      {isLoading && !data && (
        <div className="text-gray-400 dark:text-gray-500 text-sm py-12 text-center">Ładowanie…</div>
      )}
      {isError && (
        <div className="text-red-500 text-sm py-12 text-center">Nie udało się pobrać dokumentu.</div>
      )}

      {data && (
        <div className="space-y-4">
          <div className="flex items-baseline justify-between flex-wrap gap-2">
            <h2 className="text-lg font-semibold text-gray-800 dark:text-gray-100">{data.title}</h2>
            {!data.hasData && (
              <span className="text-xs text-gray-400 dark:text-gray-500">
                Brak faktur i wydatków w wybranym roku — wartości zerowe.
              </span>
            )}
          </div>

          {downloadError && <div className="text-sm text-red-600 dark:text-red-400">{downloadError}</div>}

          {data.dataWarnings.length > 0 && (
            <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-xl p-4 space-y-1">
              {data.dataWarnings.map((w, i) => (
                <div key={i} className="flex items-start gap-2 text-sm text-amber-700 dark:text-amber-400">
                  <AlertTriangle size={15} className="mt-0.5 shrink-0" />
                  {w}
                </div>
              ))}
            </div>
          )}

          {/* Skrót podsumowania (kolumna roczna / stan na koniec roku) */}
          {data.type === 'income_statement' ? (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <Kpi label="Przychody (rok)" value={fmt(cellAt(data, 'revenue', sumCol) ?? 0)} color="text-blue-600" />
              <Kpi label="Koszty (rok)" value={fmt(cellAt(data, 'costs', sumCol) ?? 0)} color="text-red-500" />
              <Kpi label="Wynik (zysk/strata)" value={fmt(cellAt(data, 'result', sumCol) ?? 0)} color="text-green-600" />
              <Kpi
                label="Marża"
                value={(cellAt(data, 'margin', sumCol) ?? 0).toLocaleString('pl-PL', { maximumFractionDigits: 1 }) + '%'}
                color="text-violet-600"
              />
            </div>
          ) : (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <Kpi label="Suma bilansowa (koniec roku)" value={fmt(cellAt(data, 'aktywa_razem', sumCol) ?? 0)} color="text-blue-600" />
              <Kpi label="Kapitał własny" value={fmt(cellAt(data, 'kapital_wlasny', sumCol) ?? 0)} color="text-green-600" />
            </div>
          )}

          <FinancialStatementTable doc={data} />

          <div className="bg-blue-50/50 dark:bg-blue-900/10 border border-blue-100 dark:border-blue-900 rounded-xl p-4">
            <div className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-2">Założenia i zastrzeżenia</div>
            <ul className="list-disc list-inside text-xs text-gray-600 dark:text-gray-400 space-y-1">
              {data.notes.map((n, i) => <li key={i}>{n}</li>)}
            </ul>
          </div>
        </div>
      )}
    </div>
  )
}

function Kpi({ label, value, color }: { label: string; value: string; color: string }) {
  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
      <div className="text-gray-500 dark:text-gray-400 text-sm mb-1">{label}</div>
      <div className={`text-2xl font-bold ${color}`}>{value}</div>
    </div>
  )
}
