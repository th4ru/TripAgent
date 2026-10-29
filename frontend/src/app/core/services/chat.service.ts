import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ChatResponse } from '../models/chat';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);

  send(message: string, sessionId: string): Observable<ChatResponse> {
    return this.http.post<ChatResponse>('/api/chat', { message, sessionId });
  }
}
