import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API_URL } from './config';
import { AppRole, AuthResponse, UserDto } from './models';

const REFRESH_KEY = 'pulseboard.refreshToken';
const USER_KEY = 'pulseboard.user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly accessTokenSignal = signal<string | null>(null);
  readonly user = signal<UserDto | null>(this.readStoredUser());
  readonly isAuthenticated = computed(() => this.user() !== null);
  readonly role = computed<AppRole | null>(() => this.user()?.role ?? null);
  readonly isAdmin = computed(() => this.role() === 'Admin');

  get accessToken(): string | null { return this.accessTokenSignal(); }

  async login(email: string, password: string): Promise<void> {
    const auth = await firstValueFrom(this.http.post<AuthResponse>(`${API_URL}/auth/login`, { email, password }));
    this.storeSession(auth);
  }

  async register(email: string, password: string, displayName: string): Promise<void> {
    const auth = await firstValueFrom(
      this.http.post<AuthResponse>(`${API_URL}/auth/register`, { email, password, displayName }));
    this.storeSession(auth);
  }

  /** Exchanges the stored refresh token for a new session. Returns false when missing/expired. */
  async tryRefresh(): Promise<boolean> {
    const refreshToken = this.read(REFRESH_KEY);
    if (!refreshToken) return false;
    try {
      const auth = await firstValueFrom(this.http.post<AuthResponse>(`${API_URL}/auth/refresh`, { refreshToken }));
      this.storeSession(auth);
      return true;
    } catch {
      this.clearSession();
      return false;
    }
  }

  async logout(): Promise<void> {
    const refreshToken = this.read(REFRESH_KEY);
    if (refreshToken) {
      try { await firstValueFrom(this.http.post(`${API_URL}/auth/logout`, { refreshToken })); }
      catch { /* server treats logout as idempotent */ }
    }
    this.clearSession();
  }

  private storeSession(auth: AuthResponse): void {
    this.accessTokenSignal.set(auth.accessToken);
    this.user.set(auth.user);
    this.write(REFRESH_KEY, auth.refreshToken);
    this.write(USER_KEY, JSON.stringify(auth.user));
  }

  private clearSession(): void {
    this.accessTokenSignal.set(null);
    this.user.set(null);
    this.remove(REFRESH_KEY);
    this.remove(USER_KEY);
  }

  private readStoredUser(): UserDto | null {
    try {
      const raw = this.read(USER_KEY);
      return raw ? (JSON.parse(raw) as UserDto) : null;
    } catch { return null; }
  }

  private read(k: string): string | null { try { return localStorage.getItem(k); } catch { return null; } }
  private write(k: string, v: string): void { try { localStorage.setItem(k, v); } catch { /* ignore */ } }
  private remove(k: string): void { try { localStorage.removeItem(k); } catch { /* ignore */ } }
}
