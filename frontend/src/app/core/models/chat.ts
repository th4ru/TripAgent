export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  text: string;
  createdAt: Date;
}

export interface ChatRequest {
  message: string;
  sessionId: string;
}

export interface ChatResponse {
  reply: string;
  sessionId?: string;
}

/** Message shape stored in localStorage (dates are serialized). */
export interface StoredChatMessage {
  id: string;
  role: 'user' | 'assistant';
  text: string;
  createdAt: string;
}

/** Lightweight row rendered in the chat history drawer. */
export interface ConversationSummary {
  id: string;
  title: string;
  preview: string;
  messageCount: number;
  updatedAt: string;
}

export interface StoredConversation extends ConversationSummary {
  messages: StoredChatMessage[];
}
