import { Routes, Route, Navigate } from 'react-router-dom'
import { AppShell } from './components/layout/AppShell'
import { ProtectedRoute } from './components/auth/ProtectedRoute'
import { DashboardPage } from './pages/DashboardPage'
import { TimeTrackingPage } from './pages/TimeTrackingPage'
import { ClientsPage } from './pages/ClientsPage'
import { InvoicesPage } from './pages/InvoicesPage'
import { ExpensesPage } from './pages/ExpensesPage'
import { ForecastPage } from './pages/ForecastPage'
import { SettlementPage } from './pages/SettlementPage'
import { DocumentsPage } from './pages/DocumentsPage'
import { TaxObligationsPage } from './pages/TaxObligationsPage'
import { ProfitabilityPage } from './pages/ProfitabilityPage'
import { WorkAnalyticsPage } from './pages/WorkAnalyticsPage'
import { CashFlowPage } from './pages/CashFlowPage'
import { CurrencyExposurePage } from './pages/CurrencyExposurePage'
import { ChatPage } from './pages/ChatPage'
import { SettingsPage } from './pages/SettingsPage'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AppShell />}>
          <Route index element={<DashboardPage />} />
          <Route path="time" element={<TimeTrackingPage />} />
          <Route path="clients" element={<ClientsPage />} />
          <Route path="invoices" element={<InvoicesPage />} />
          <Route path="expenses" element={<ExpensesPage />} />
          <Route path="forecast" element={<ForecastPage />} />
          <Route path="settlement" element={<SettlementPage />} />
          <Route path="documents" element={<DocumentsPage />} />
          <Route path="tax-obligations" element={<TaxObligationsPage />} />
          <Route path="profitability" element={<ProfitabilityPage />} />
          <Route path="work-analytics" element={<WorkAnalyticsPage />} />
          <Route path="cash-flow" element={<CashFlowPage />} />
          <Route path="currency-exposure" element={<CurrencyExposurePage />} />
          <Route path="chat" element={<ChatPage />} />
          <Route path="settings" element={<SettingsPage />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
