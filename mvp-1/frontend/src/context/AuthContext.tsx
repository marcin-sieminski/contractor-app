import { createContext, useContext, useState, useEffect, ReactNode } from 'react'
import { authApi, AuthResponse } from '../api/auth'

interface AuthContextValue {
  user: { email: string; displayName: string | null } | null
  isAuthenticated: boolean
  isLoading: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, confirmPassword: string) => Promise<void>
  logout: () => void
  updateDisplayName: (displayName: string | null) => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

const TOKEN_KEY = 'auth_token'
const EMAIL_KEY = 'auth_email'
const DISPLAY_NAME_KEY = 'auth_display_name'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<{ email: string; displayName: string | null } | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const token = localStorage.getItem(TOKEN_KEY)
    const email = localStorage.getItem(EMAIL_KEY)
    if (token && email) {
      const displayName = localStorage.getItem(DISPLAY_NAME_KEY)
      setUser({ email, displayName })
    }
    setIsLoading(false)
  }, [])

  function storeAuth(data: AuthResponse) {
    localStorage.setItem(TOKEN_KEY, data.token)
    localStorage.setItem(EMAIL_KEY, data.email)
    const displayName = data.displayName ?? null
    if (displayName) {
      localStorage.setItem(DISPLAY_NAME_KEY, displayName)
    } else {
      localStorage.removeItem(DISPLAY_NAME_KEY)
    }
    setUser({ email: data.email, displayName })
  }

  async function login(email: string, password: string) {
    const data = await authApi.login(email, password)
    storeAuth(data)
  }

  async function register(email: string, password: string, confirmPassword: string) {
    const data = await authApi.register(email, password, confirmPassword)
    storeAuth(data)
  }

  function logout() {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(EMAIL_KEY)
    localStorage.removeItem(DISPLAY_NAME_KEY)
    setUser(null)
  }

  function updateDisplayName(displayName: string | null) {
    if (!user) return
    if (displayName) {
      localStorage.setItem(DISPLAY_NAME_KEY, displayName)
    } else {
      localStorage.removeItem(DISPLAY_NAME_KEY)
    }
    setUser({ ...user, displayName })
  }

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, isLoading, login, register, logout, updateDisplayName }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
