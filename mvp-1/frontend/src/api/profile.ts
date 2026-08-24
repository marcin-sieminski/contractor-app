import api from './client'

export interface ProfileResponse {
  email: string
  displayName: string | null
}

export const profileApi = {
  getProfile: () =>
    api.get<ProfileResponse>('/profile').then(r => r.data),

  updateProfile: (displayName: string | null) =>
    api.put<ProfileResponse>('/profile', { displayName }).then(r => r.data),
}
