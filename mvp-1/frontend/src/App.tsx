import { Routes, Route } from 'react-router-dom'
import { AppShell } from './components/layout/AppShell'
import { DashboardPage } from './pages/DashboardPage'
import { TimeTrackingPage } from './pages/TimeTrackingPage'
import { ClientsPage } from './pages/ClientsPage'
import { InvoicesPage } from './pages/InvoicesPage'

export default function App() {
  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route index element={<DashboardPage />} />
        <Route path="time" element={<TimeTrackingPage />} />
        <Route path="clients" element={<ClientsPage />} />
        <Route path="invoices" element={<InvoicesPage />} />
      </Route>
    </Routes>
  )
}
