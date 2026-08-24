import { useState, useId, FormEvent } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()
  const emailId = useId()
  const passwordId = useId()
  const confirmId = useId()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [errors, setErrors] = useState<string[]>([])
  const [loading, setLoading] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setErrors([])

    if (password !== confirmPassword) {
      setErrors(['Hasła nie są zgodne.'])
      return
    }

    setLoading(true)
    try {
      await register(email, password, confirmPassword)
      navigate('/', { replace: true })
    } catch (err: any) {
      const data = err?.response?.data
      if (data?.errors) setErrors(data.errors)
      else if (data?.error) setErrors([data.error])
      else setErrors(['Wystąpił błąd rejestracji.'])
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-950 flex items-center justify-center p-4">
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 w-full max-w-md p-8">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100 mb-2">ContractorHub</h1>
        <p className="text-gray-500 dark:text-gray-400 mb-8">Utwórz nowe konto</p>

        {errors.length > 0 && (
          <div role="alert" className="mb-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-400 text-sm space-y-1">
            {errors.map((e, i) => <p key={i}>{e}</p>)}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label htmlFor={emailId} className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Email</label>
            <input
              id={emailId}
              type="email"
              autoComplete="username"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
              className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="jan@kowalski.pl"
            />
          </div>

          <div>
            <label htmlFor={passwordId} className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">
              Hasło <span className="text-gray-500 dark:text-gray-400 font-normal">(min. 8 znaków)</span>
            </label>
            <input
              id={passwordId}
              type="password"
              autoComplete="new-password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
              minLength={8}
              className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="••••••••"
            />
          </div>

          <div>
            <label htmlFor={confirmId} className="block text-sm font-medium text-gray-700 dark:text-gray-200 mb-1">Powtórz hasło</label>
            <input
              id={confirmId}
              type="password"
              autoComplete="new-password"
              value={confirmPassword}
              onChange={e => setConfirmPassword(e.target.value)}
              required
              className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
              placeholder="••••••••"
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full py-2 px-4 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 font-medium"
          >
            {loading ? 'Rejestracja...' : 'Zarejestruj się'}
          </button>
        </form>

        <p className="mt-6 text-center text-sm text-gray-500 dark:text-gray-400">
          Masz już konto?{' '}
          <Link to="/login" className="text-blue-600 dark:text-blue-400 hover:underline font-medium">
            Zaloguj się
          </Link>
        </p>
      </div>
    </div>
  )
}
