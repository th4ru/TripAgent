import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthResult, AuthUser, LoginRequest, RegisterRequest } from '../models/auth';

const STORAGE_KEY = 'trip-planner-auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly state = signal<AuthResult | null>(readStored());

  readonly isAuthenticated = computed(() => {
    const value = this.state();
    if (!value) {
      return false;
    }

    const expiresAt = new Date(value.expiresAtUtc).getTime();
    return Number.isFinite(expiresAt) && expiresAt > Date.now();
  });

  readonly user = computed<AuthUser | null>(() => (this.isAuthenticated() ? this.state()!.user : null));

  readonly token = computed<string | null>(() => (this.isAuthenticated() ? this.state()!.token : null));

  readonly displayName = computed(() => {
    const user = this.user();
    if (!user) {
      return '';
    }

    const fullName = `${user.firstName} ${user.lastName}`.trim();
    return fullName || user.userName;
  });

  login(request: LoginRequest): Observable<AuthResult> {
    return this.http
      .post<AuthResult>('/api/auth/login', request)
      .pipe(tap((result) => this.store(result)));
  }

  register(request: RegisterRequest): Observable<AuthResult> {
    return this.http
      .post<AuthResult>('/api/auth/register', request)
      .pipe(tap((result) => this.store(result)));
  }

  signOut(redirectTo: string | null = '/login'): void {
    this.state.set(null);

    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      // Storage can be unavailable (private mode); the in-memory state is already cleared.
    }

    if (redirectTo) {
      void this.router.navigateByUrl(redirectTo);
    }
  }

  private store(result: AuthResult): void {
    this.state.set(result);

    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(result));
    } catch {
      // Ignore storage failures; the session still works until refresh.
    }
  }
}

function readStored(): AuthResult | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }

    const parsed = JSON.parse(raw) as AuthResult;
    if (!parsed?.token || !parsed?.user || !parsed?.expiresAtUtc) {
      return null;
    }

    return parsed;
  } catch {
    return null;
  }
}
