import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

interface AuthResult {
  token: string;
  userId: string;
  name: string;
  email: string;
}

declare const google: any;

function waitForGoogleScript(): Promise<void> {
  return new Promise(resolve => {
    if ((window as any).google?.accounts?.id) {
      resolve();
      return;
    }
    const interval = setInterval(() => {
      if ((window as any).google?.accounts?.id) {
        clearInterval(interval);
        resolve();
      }
    }, 50);
  });
}

const STORAGE_KEY = 'chatapp_jwt';
const GOOGLE_CLIENT_ID = '1082735551021-df9ohvbt255b4anch08l5funnselerae.apps.googleusercontent.com';

const NAME_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';
const EMAIL_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress';

function decodeJwtClaims(token: string): { name: string | null; email: string | null; exp: number | null } {
  try {
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    return {
      name: payload[NAME_CLAIM] ?? payload.name ?? null,
      email: payload[EMAIL_CLAIM] ?? payload.email ?? null,
      exp: typeof payload.exp === 'number' ? payload.exp : null,
    };
  } catch {
    return { name: null, email: null, exp: null };
  }
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly isAuthenticated = signal(false);
  readonly userName = signal<string | null>(null);
  readonly userEmail = signal<string | null>(null);

  private expiryTimer: ReturnType<typeof setTimeout> | null = null;

  constructor(private http: HttpClient) {
    const existingToken = localStorage.getItem(STORAGE_KEY);
    if (existingToken) {
      const claims = decodeJwtClaims(existingToken);
      if (claims.exp !== null && claims.exp * 1000 <= Date.now()) {
        // Stored token already expired — treat as logged out
        localStorage.removeItem(STORAGE_KEY);
      } else {
        this.userName.set(claims.name);
        this.userEmail.set(claims.email);
        this.isAuthenticated.set(true);
        this.scheduleAutoLogout(claims.exp);
      }
    }
  }

  /** Logs out the moment the token expires, so the UI never sits on a dead session. */
  private scheduleAutoLogout(exp: number | null): void {
    if (this.expiryTimer) {
      clearTimeout(this.expiryTimer);
      this.expiryTimer = null;
    }
    if (exp === null) {
      return;
    }
    this.expiryTimer = setTimeout(() => this.logout(), exp * 1000 - Date.now());
  }

  getToken(): string | null {
    return localStorage.getItem(STORAGE_KEY);
  }

  async initGoogleButton(containerId: string): Promise<void> {
    await waitForGoogleScript();
    google.accounts.id.initialize({
      client_id: GOOGLE_CLIENT_ID,
      callback: (response: { credential: string }) => this.handleCredential(response.credential),
    });
    google.accounts.id.renderButton(
      document.getElementById(containerId),
      { theme: 'outline', size: 'large' }
    );
  }

  private async handleCredential(idToken: string): Promise<void> {
    const result = await firstValueFrom(
      this.http.post<AuthResult>('https://localhost:7129/auth/google', { idToken })
    );
    localStorage.setItem(STORAGE_KEY, result.token);
    this.userName.set(result.name);
    this.userEmail.set(result.email);
    this.isAuthenticated.set(true);
    this.scheduleAutoLogout(decodeJwtClaims(result.token).exp);
  }

  logout(): void {
    if (this.expiryTimer) {
      clearTimeout(this.expiryTimer);
      this.expiryTimer = null;
    }
    localStorage.removeItem(STORAGE_KEY);
    this.userName.set(null);
    this.userEmail.set(null);
    this.isAuthenticated.set(false);
  }
}
