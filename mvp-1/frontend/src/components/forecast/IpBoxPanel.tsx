import { Sparkles } from 'lucide-react'
import type { IpBoxScenario } from '../../types/forecast'

const fmt = (v: number) =>
  v.toLocaleString('pl-PL', { maximumFractionDigits: 0 }) + ' PLN'

export function IpBoxPanel({ ipBox }: { ipBox: IpBoxScenario }) {
  const positive = ipBox.annualSavings > 0

  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-5 mb-6">
      <div className="flex items-center gap-2 mb-1">
        <Sparkles className="text-violet-600" size={18} />
        <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">Scenariusz IP Box</h2>
        <span className="text-xs text-gray-400 dark:text-gray-500">
          {ipBox.qualifyingPercent.toLocaleString('pl-PL', { maximumFractionDigits: 0 })}% dochodu kwalifikowanego ·
          Nexus {ipBox.nexusCoefficient.toLocaleString('pl-PL', { maximumFractionDigits: 2 })}
        </span>
      </div>
      <div
        className={`text-sm font-medium mb-4 ${
          positive ? 'text-green-700 dark:text-green-500' : 'text-red-600 dark:text-red-400'
        }`}
      >
        {ipBox.verdict}
      </div>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-5">
        <Kpi
          label="Oszczędność roczna"
          value={fmt(ipBox.annualSavings)}
          color={positive ? 'text-green-600' : 'text-red-500'}
          sub={`${fmt(ipBox.monthlySavings)}/mies.`}
        />
        <Kpi
          label="Efektywna stawka"
          value={`${ipBox.effectiveRateWith.toLocaleString('pl-PL', { maximumFractionDigits: 1 })}%`}
          color="text-violet-600"
          sub={`bez IP Box: ${ipBox.effectiveRateWithout.toLocaleString('pl-PL', { maximumFractionDigits: 1 })}%`}
        />
        <Kpi
          label="Obciążenia z IP Box"
          value={fmt(ipBox.totalObligationsWith)}
          color="text-amber-600"
          sub={`bez IP Box: ${fmt(ipBox.totalObligationsWithout)}`}
        />
        <Kpi
          label="Przepływ netto z IP Box"
          value={fmt(ipBox.netCashFlowWith)}
          color="text-green-600"
        />
      </div>

      <div className="overflow-x-auto mb-5">
        <table className="w-full text-sm">
          <thead>
            <tr className="text-xs text-gray-500 dark:text-gray-400 border-b border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
              <th className="text-left font-medium px-3 py-2">Pozycja (rok)</th>
              <th className="text-right font-medium px-3 py-2">Bez IP Box</th>
              <th className="text-right font-medium px-3 py-2">Z IP Box</th>
              <th className="text-right font-medium px-3 py-2">Różnica</th>
            </tr>
          </thead>
          <tbody className="text-gray-900 dark:text-gray-100">
            <Row
              label="PIT"
              without={ipBox.annualPitWithout}
              withIp={ipBox.annualPitWith}
            />
            <Row
              label="Łączne obciążenia (PIT+ZUS+VAT)"
              without={ipBox.totalObligationsWithout}
              withIp={ipBox.totalObligationsWith}
            />
            <tr className="border-b border-gray-50 dark:border-gray-700/50 last:border-0">
              <td className="px-3 py-2">Efektywna stawka</td>
              <td className="px-3 py-2 text-right tabular-nums">
                {ipBox.effectiveRateWithout.toLocaleString('pl-PL', { maximumFractionDigits: 1 })}%
              </td>
              <td className="px-3 py-2 text-right tabular-nums">
                {ipBox.effectiveRateWith.toLocaleString('pl-PL', { maximumFractionDigits: 1 })}%
              </td>
              <td className="px-3 py-2 text-right tabular-nums text-green-700 dark:text-green-500">
                {(ipBox.effectiveRateWith - ipBox.effectiveRateWithout).toLocaleString('pl-PL', {
                  maximumFractionDigits: 1,
                  signDisplay: 'exceptZero',
                })}
                pp
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div className="bg-violet-50/50 dark:bg-violet-900/10 border border-violet-100 dark:border-violet-900 rounded-lg p-4">
        <div className="text-sm font-semibold text-gray-700 dark:text-gray-200 mb-2">Warunki kwalifikacji IP Box</div>
        <ul className="list-disc list-inside text-xs text-gray-600 dark:text-gray-400 space-y-1">
          {ipBox.conditions.map((c, i) => <li key={i}>{c}</li>)}
        </ul>
      </div>
    </div>
  )
}

function Row({ label, without, withIp }: { label: string; without: number; withIp: number }) {
  const diff = without - withIp
  return (
    <tr className="border-b border-gray-50 dark:border-gray-700/50 last:border-0">
      <td className="px-3 py-2">{label}</td>
      <td className="px-3 py-2 text-right tabular-nums">{fmt(without)}</td>
      <td className="px-3 py-2 text-right tabular-nums">{fmt(withIp)}</td>
      <td className={`px-3 py-2 text-right tabular-nums font-medium ${diff >= 0 ? 'text-green-700 dark:text-green-500' : 'text-red-600 dark:text-red-400'}`}>
        {diff.toLocaleString('pl-PL', { maximumFractionDigits: 0, signDisplay: 'exceptZero' })} PLN
      </td>
    </tr>
  )
}

function Kpi({ label, value, color, sub }: { label: string; value: string; color: string; sub?: string }) {
  return (
    <div className="bg-gray-50 dark:bg-gray-900/40 border border-gray-200 dark:border-gray-700 rounded-lg p-4">
      <div className="text-gray-500 dark:text-gray-400 text-xs mb-1">{label}</div>
      <div className={`text-xl font-bold ${color}`}>{value}</div>
      {sub && <div className="text-xs text-gray-400 dark:text-gray-500 mt-1">{sub}</div>}
    </div>
  )
}
