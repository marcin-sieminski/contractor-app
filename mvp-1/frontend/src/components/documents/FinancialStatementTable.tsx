import type { FinancialDocument } from '../../types/document'

const fmtMoney = (v: number, decimals: number) =>
  v.toLocaleString('pl-PL', { minimumFractionDigits: decimals, maximumFractionDigits: decimals })

const fmtPercent = (v: number) =>
  v.toLocaleString('pl-PL', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + '%'

/** Renderuje zestawienie finansowe jako matrycę pozycje × okresy (rachunek wyników / bilans). */
export function FinancialStatementTable({ doc }: { doc: FinancialDocument }) {
  const decimals = doc.granularity === 'monthly' ? 0 : 2
  const colCount = doc.columns.length

  const fmtCell = (v: number | null, isPercent: boolean) =>
    v === null ? '—' : isPercent ? fmtPercent(v) : fmtMoney(v, decimals)

  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden">
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="text-xs text-gray-500 dark:text-gray-400 border-b border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
              <th className="text-left font-medium px-3 py-2 whitespace-nowrap">Pozycja</th>
              {doc.columns.map(c => (
                <th
                  key={c.key}
                  className={`text-right font-medium px-3 py-2 whitespace-nowrap ${
                    c.kind === 'Total' ? 'text-blue-700 dark:text-blue-400' : ''
                  }`}
                >
                  {c.label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {doc.rows.map(row => {
              if (row.style === 'Section') {
                return (
                  <tr key={row.key} className="bg-gray-100 dark:bg-gray-900/70">
                    <td
                      colSpan={colCount + 1}
                      className="px-3 py-2 font-bold uppercase tracking-wide text-xs text-gray-600 dark:text-gray-300"
                    >
                      {row.label}
                    </td>
                  </tr>
                )
              }

              const isTotal = row.style === 'Total'
              const isMemo = row.style === 'Memo'
              const rowCls = isTotal ? 'bg-blue-50/50 dark:bg-blue-900/10' : ''
              const labelCls =
                isTotal
                  ? 'font-bold text-gray-900 dark:text-gray-100'
                  : row.style === 'Subtotal'
                    ? 'font-semibold text-gray-900 dark:text-gray-100'
                    : isMemo
                      ? 'italic text-gray-500 dark:text-gray-400'
                      : 'text-gray-800 dark:text-gray-200'

              return (
                <tr
                  key={row.key}
                  className={`border-b border-gray-50 dark:border-gray-700/50 last:border-0 ${rowCls}`}
                >
                  <td className="px-3 py-2" style={{ paddingLeft: 12 + row.indent * 16 }}>
                    <div className={labelCls}>{row.label}</div>
                    {row.description && (
                      <div className="text-xs text-gray-400 dark:text-gray-500 mt-0.5">{row.description}</div>
                    )}
                  </td>
                  {doc.columns.map((c, idx) => {
                    const v = row.values[idx] ?? null
                    const negative = v !== null && v < 0
                    return (
                      <td
                        key={c.key}
                        className={`px-3 py-2 text-right tabular-nums whitespace-nowrap ${
                          isTotal ? 'font-bold' : row.style === 'Subtotal' ? 'font-semibold' : ''
                        } ${
                          negative
                            ? 'text-red-600 dark:text-red-400'
                            : isMemo
                              ? 'italic text-gray-500 dark:text-gray-400'
                              : 'text-gray-900 dark:text-gray-100'
                        }`}
                      >
                        {fmtCell(v, row.isPercent)}
                      </td>
                    )
                  })}
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
  )
}
