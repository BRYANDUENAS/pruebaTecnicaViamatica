import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { LoginResponse, MenuItem, User } from './models';

const STORAGE_KEY = 'viamatica.session';

interface Session {
  token: string;
  expiresAt: string;
  user: User;
  menu: MenuItem[];
}

/**
 * Sesión del usuario. Las claims del JWT viajan cifradas, así que los datos
 * del usuario y su menú se toman de la respuesta del login, no del token.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly session = signal<Session | null>(this.restore());

  readonly user = () => this.session()?.user ?? null;
  readonly menu = () => this.session()?.menu ?? [];
  readonly token = () => this.session()?.token ?? null;

  constructor(private http: HttpClient, private router: Router) {}

  isAuthenticated(): boolean {
    const session = this.session();
    return !!session && new Date(session.expiresAt).getTime() > Date.now();
  }

  hasRole(roles: string[]): boolean {
    const user = this.user();
    return !!user && roles.includes(user.rolName);
  }

  login(username: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', { username, password }).pipe(
      tap((response) => {
        this.session.set(response);
        sessionStorage.setItem(STORAGE_KEY, JSON.stringify(response));
      }),
    );
  }

  recoverPassword(body: { username: string; email: string; identification: string; newPassword: string }) {
    return this.http.post<{ message: string }>('/api/auth/recover-password', body);
  }

  /** Indica si el servidor puede enviar la contraseña temporal por correo. */
  recoveryOptions() {
    return this.http.get<{ emailEnabled: boolean }>('/api/auth/recovery-options');
  }

  forgotPassword(body: { username: string; email: string }) {
    return this.http.post<{ message: string }>('/api/auth/forgot-password', body);
  }

  /** Cierra la sesión en el servidor (libera la caja) y limpia el navegador. */
  logout(): void {
    if (this.token()) {
      this.http.post('/api/auth/logout', {}).subscribe({ complete: () => this.clear(), error: () => this.clear() });
    } else {
      this.clear();
    }
  }

  /** Limpia la sesión local sin llamar al servidor (token vencido o inválido). */
  clear(): void {
    this.session.set(null);
    sessionStorage.removeItem(STORAGE_KEY);
    this.router.navigate(['/login']);
  }

  private restore(): Session | null {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as Session) : null;
    } catch {
      return null;
    }
  }
}
