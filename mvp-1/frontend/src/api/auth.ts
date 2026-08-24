import api from './client'

export interface AuthResponse {
  token: string
  email: string
  expiresAt: string
  displayName?: string | null
}

export const authApi = {
  login: (email: string, password: string) =>
    api.post<AuthResponse>('/auth/login', { email, password }).then(r => r.data),

  register: (email: string, password: string, confirmPassword: string) =>
    api.post<AuthResponse>('/auth/register', { email, password, confirmPassword }).then(r => r.data),
}
