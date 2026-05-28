import api from './client'
import type { ChatRequest, ChatResponse, OllamaModel } from '../types/chat'

export const sendChatMessage = (req: ChatRequest, signal?: AbortSignal) =>
  api.post<ChatResponse>('/ai/chat', req, { signal }).then(r => r.data)

export const getModels = () =>
  api.get<{ models: OllamaModel[]; defaultModel: string }>('/ai/models').then(r => r.data)
