import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { AuthResponse, AuthUser, LoginRequest, RegisterRequest, TwoFactorRequired } from '../models/auth.model';
import { environment } from '../../../environments/environment';

const TOKEN_KEY = 'ea_token';
const USER_KEY  = 'ea_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http   = inject(HttpClient);
  private router = inject(Router);
  private base   = `${environment.apiUrl}/auth`;

  currentUser = signal<AuthUser | null>(this.loadUser());
  token       = signal<string | null>(localStorage.getItem(TOKEN_KEY));

  get isLoggedIn() { return !!this.token(); }

  register(req: RegisterRequest) {
    return this.http.post<AuthResponse>(`${this.base}/register`, req).pipe(
      tap(res => this.saveSession(res))
    );
  }

  login(req: LoginRequest) {
    return this.http.post<AuthResponse | TwoFactorRequired>(`${this.base}/login`, req);
  }

  verifyTwoFactor(userId: number, code: string) {
    return this.http.post<AuthResponse>(`${this.base}/login/2fa`, { userId, code }).pipe(
      tap(res => this.saveSession(res))
    );
  }

  me() {
    return this.http.get<AuthUser>(`${this.base}/me`).pipe(
      tap(user => {
        this.currentUser.set(user);
        localStorage.setItem(USER_KEY, JSON.stringify(user));
      })
    );
  }

  setupTwoFactor() {
    return this.http.post<{ otpauthUri: string }>(`${this.base}/setup-2fa`, {});
  }

  confirmTwoFactor(code: string) {
    return this.http.post<{ message: string }>(`${this.base}/confirm-2fa`, { code }).pipe(
      tap(() => {
        const user = this.currentUser();
        if (user) {
          const updated = { ...user, twoFactorEnabled: true };
          this.currentUser.set(updated);
          localStorage.setItem(USER_KEY, JSON.stringify(updated));
        }
      })
    );
  }

  disableTwoFactor() {
    return this.http.delete<{ message: string }>(`${this.base}/2fa`).pipe(
      tap(() => {
        const user = this.currentUser();
        if (user) {
          const updated = { ...user, twoFactorEnabled: false };
          this.currentUser.set(updated);
          localStorage.setItem(USER_KEY, JSON.stringify(updated));
        }
      })
    );
  }

  logout() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.token.set(null);
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  private saveSession(res: AuthResponse) {
    localStorage.setItem(TOKEN_KEY, res.token);
    localStorage.setItem(USER_KEY, JSON.stringify(res.user));
    this.token.set(res.token);
    this.currentUser.set(res.user);
  }

  private loadUser(): AuthUser | null {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) : null;
  }
}
