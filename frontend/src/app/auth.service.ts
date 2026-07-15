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

function decodeJwtName(token: string): string | null {
  try {
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    return payload[NAME_CLAIM] ?? payload.name ?? null;
  } catch {
    return null;
  }
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly isAuthenticated = signal(!!localStorage.getItem(STORAGE_KEY));
  readonly userName = signal<string | null>(null);

  constructor(private http: HttpClient) {
    const existingToken = localStorage.getItem(STORAGE_KEY);
    if (existingToken) {
      this.userName.set(decodeJwtName(existingToken));
    }
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
    this.isAuthenticated.set(true);
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.userName.set(null);
    this.isAuthenticated.set(false);
  }
}
