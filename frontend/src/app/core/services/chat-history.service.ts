import { Injectable, inject } from '@angular/core';
import {
  ChatMessage,
  ConversationSummary,
  StoredChatMessage,
  StoredConversation
} from '../models/chat';
import { AuthService } from './auth.service';

const STORAGE_PREFIX = 'trip-planner-chat-history';
const TITLE_LIMIT = 52;
const PREVIEW_LIMIT = 84;

/**
 * Persists conversations in the browser, scoped to the signed-in user.
 * The n8n agent keeps its own memory per session id, so re-opening a saved
 * conversation keeps the agent's context intact.
 */
@Injectable({ providedIn: 'root' })
export class ChatHistoryService {
  private readonly auth = inject(AuthService);

  /** Newest first, excluding conversations that never got a user message. */
  list(): ConversationSummary[] {
    return this.readAll()
      .filter((conversation) => conversation.messages.some((message) => message.role === 'user'))
      .sort((a, b) => Date.parse(b.updatedAt) - Date.parse(a.updatedAt))
      .map(({ messages, ...summary }) => summary);
  }

  load(id: string): ChatMessage[] {
    const conversation = this.readAll().find((item) => item.id === id);
    if (!conversation) {
      return [];
    }

    return conversation.messages.map((message) => ({
      ...message,
      createdAt: new Date(message.createdAt)
    }));
  }

  save(id: string, messages: ChatMessage[]): void {
    const stored: StoredChatMessage[] = messages.map((message) => ({
      id: message.id,
      role: message.role,
      text: message.text,
      createdAt: message.createdAt.toISOString()
    }));

    // A brand-new thread only holds the welcome message; nothing to remember yet.
    if (!stored.some((message) => message.role === 'user')) {
      return;
    }

    const conversation: StoredConversation = {
      id,
      title: buildTitle(stored),
      preview: buildPreview(stored),
      messageCount: stored.length,
      updatedAt: stored[stored.length - 1].createdAt,
      messages: stored
    };

    const conversations = this.readAll().filter((item) => item.id !== id);
    conversations.push(conversation);
    this.writeAll(conversations);
  }

  remove(id: string): void {
    this.writeAll(this.readAll().filter((conversation) => conversation.id !== id));
  }

  clear(): void {
    this.writeAll([]);
  }

  private storageKey(): string {
    return `${STORAGE_PREFIX}:${this.auth.user()?.id ?? 'anonymous'}`;
  }

  private readAll(): StoredConversation[] {
    try {
      const raw = localStorage.getItem(this.storageKey());
      if (!raw) {
        return [];
      }

      const parsed = JSON.parse(raw) as unknown;
      return Array.isArray(parsed) ? (parsed as StoredConversation[]) : [];
    } catch {
      return [];
    }
  }

  private writeAll(conversations: StoredConversation[]): void {
    try {
      localStorage.setItem(this.storageKey(), JSON.stringify(conversations));
    } catch {
      // Storage can be unavailable (private mode); the chat itself keeps working.
    }
  }
}

function buildTitle(messages: StoredChatMessage[]): string {
  const firstUserMessage = messages.find((message) => message.role === 'user')?.text.trim();
  return truncate(firstUserMessage || 'New conversation', TITLE_LIMIT);
}

function buildPreview(messages: StoredChatMessage[]): string {
  const lastMessage = [...messages].reverse().find((message) => message.text.trim());
  return truncate(lastMessage?.text.trim() ?? '', PREVIEW_LIMIT);
}

function truncate(value: string, limit: number): string {
  const singleLine = value.replace(/\s+/g, ' ');
  return singleLine.length > limit ? `${singleLine.slice(0, limit - 1).trimEnd()}…` : singleLine;
}
