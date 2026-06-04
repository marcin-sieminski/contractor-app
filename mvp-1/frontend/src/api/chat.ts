import api from './client'
import type { AiModel, AiProvider, ChatRequest, ChatResponse } from '../types/chat'

export const sendChatMessage = (req: ChatRequest, signal?: AbortSignal) =>
  api.post<ChatResponse>('/ai/chat', req, { signal }).then(r => r.data)

export const getModels = () =>
  api.get<{ models: AiModel[]; defaultModel: string; defaultProvider: AiProvider }>('/ai/models').then(r => r.data)
