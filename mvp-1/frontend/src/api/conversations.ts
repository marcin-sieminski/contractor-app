import api from './client'
import type { ConversationDetail, ConversationSummary } from '../types/chat'

export const listConversations = () =>
  api.get<ConversationSummary[]>('/ai/conversations').then(r => r.data)

export const getConversation = (id: string) =>
  api.get<ConversationDetail>(`/ai/conversations/${id}`).then(r => r.data)

export const deleteConversation = (id: string) =>
  api.delete(`/ai/conversations/${id}`).then(r => r.data)
