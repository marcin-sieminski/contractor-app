import type { SettlementLine, SettlementResult } from '../../types/settlement'

const fmt = (v: number) =>
  v.toLocaleString('pl-PL', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' zł'

/** Tabela pozycji rozliczenia z opisami umiejscowienia w formularzu + karta wyniku. */
export function SettlementResultTable({ lines, result, formCode }: {
  lines: SettlementLine[]
  result: SettlementResult
  formCode: string
}) {
  const isRefund = result.doZaplaty <= 0 && result.nadplata > 0
  return (
    <div className="space-y-4">
      <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden">
        <div className="px-4 py-3 border-b border-gray-100 dark:border-gray-700 text-sm font-semibold text-gray-700 dark:text-gray-200">
          Pozycje zeznania {formCode}
        </div>
        <table className="w-full text-sm">
          <thead>
            <tr className="text-xs text-gray-500 dark:text-gray-400 border-b border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
              <th className="text-left font-medium px-4 py-2">Pozycja</th>
              <th className="text-right font-medium px-4 py-2 whitespace-nowrap">Kwota</th>
            </tr>
          </thead>
          <tbody>
            {lines.map(line => (
              <tr
                key={line.boxId}
                className={`border-b border-gray-50 dark:border-gray-700/50 last:border-0 ${
                  line.boxId === 'PODATEK' ? 'bg-blue-50/50 dark:bg-blue-900/10 font-semibold' : ''
                }`}
              >
                <td className="px-4 py-2.5">
                  <div className="text-gray-900 dark:text-gray-100">{line.label}</div>
                  {line.description && (
                    <div className="text-xs text-gray-400 dark:text-gray-500 mt-0.5">{line.description}</div>
                  )}
                </td>
                <td className="px-4 py-2.5 text-right tabular-nums align-top text-gray-900 dark:text-gray-100 whitespace-nowrap">
                  {fmt(line.value)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className={`rounded-xl border p-5 ${
        isRefund
          ? 'bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800'
          : 'bg-amber-50 dark:bg-amber-900/20 border-amber-200 dark:border-amber-800'
      }`}>
        <div className="text-sm text-gray-600 dark:text-gray-300 mb-1">
          {isRefund ? 'Nadpłata (do zwrotu lub zaliczenia)' : 'Do zapłaty do urzędu skarbowego'}
        </div>
        <div className={`text-3xl font-bold ${
          isRefund ? 'text-green-700 dark:text-green-400' : 'text-amber-700 dark:text-amber-400'
        }`}>
          {fmt(isRefund ? result.nadplata : result.doZaplaty)}
        </div>
        <div className="text-xs text-gray-500 dark:text-gray-400 mt-2">
          Podatek należny {fmt(result.podatekNalezny)} · zaliczki wpłacone {fmt(result.zaliczkiWplacone)} ·
          efektywna stawka od przychodu {result.efektywnaStawkaOdPrzychodu.toLocaleString('pl-PL')}%
        </div>
      </div>
    </div>
  )
}
