import { Injectable } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError, BehaviorSubject } from 'rxjs';
import { LoginResponse } from './models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject = new BehaviorSubject<LoginResponse | null>(this.getStoredUser());
  private tokenSubject = new BehaviorSubject<string | null>(localStorage.getItem('token'));

  currentUser$ = this.currentUserSubject.asObservable();
  token$ = this.tokenSubject.asObservable();

  constructor(private router: Router) {}

  get currentUser(): LoginResponse | null {
    return this.currentUserSubject.value;
  }

  get token(): string | null {
    return this.tokenSubject.value;
  }

  login(user: LoginResponse) {
    localStorage.setItem('token', user.accessToken);
    localStorage.setItem('user', JSON.stringify(user));
    this.tokenSubject.next(user.accessToken);
    this.currentUserSubject.next(user);
    this.redirectByRole(user.roleName);
  }

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    this.tokenSubject.next(null);
    this.currentUserSubject.next(null);
    if (this.router.url !== '/login') {
      this.router.navigate(['/login']);
    }
  }

  isAuthenticated(): boolean {
    return !!this.token && !!this.currentUser && !this.isTokenExpired();
  }

  getRole(): string {
    return this.currentUser?.roleName || '';
  }

  getUserId(): number {
    return this.currentUser?.userId || 0;
  }

  getUserDisplayName(): string {
    return this.currentUser?.displayName || this.currentUser?.userName || 'User';
  }

  dashboardPath(roleName?: string): string {
    switch (roleName || this.getRole()) {
      case 'Attendee':
        return '/attendee';
      case 'EventManager':
        return '/manager';
      case 'BusinessManagement':
        return '/business';
      case 'Administrator':
        return '/admin';
      default:
        return '/events';
    }
  }

  redirectByRole(roleName?: string) {
    this.router.navigateByUrl(this.dashboardPath(roleName));
  }

  private isTokenExpired(): boolean {
    const expiresAt = this.currentUser?.expiresAtUtc;
    if (!expiresAt) return false;
    const expiryTime = new Date(expiresAt).getTime();
    if (isNaN(expiryTime)) return false;
    return expiryTime <= Date.now();
  }

  private getStoredUser(): LoginResponse | null {
    const data = localStorage.getItem('user');
    if (!data) return null;
    try {
      const user = JSON.parse(data) as LoginResponse;
      return user && user.accessToken ? user : null;
    } catch {
      return null;
    }
  }
}

export const roleGuard = (allowedRoles: string[]): CanActivateFn => {
  return () => {
    const token = localStorage.getItem('token');
    const userData = localStorage.getItem('user');

    if (!token || !userData) {
      if (window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
      return false;
    }

    try {
      const user = JSON.parse(userData) as LoginResponse;
      const role = user?.roleName || '';
      if (allowedRoles.includes(role)) {
        return true;
      }
    } catch {
      // Invalid user data
    }

    // Role not allowed -> redirect to login
    window.location.href = '/login';
    return false;
  };
};

export const jwtInterceptor: HttpInterceptorFn = (req: HttpRequest<unknown>, next: HttpHandlerFn) => {
  const token = localStorage.getItem('token');
  let authReq = req;

  if (token) {
    authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !req.url.includes('/auth/login')) {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        if (window.location.pathname !== '/login') {
          window.location.href = '/login';
        }
      }
      return throwError(() => error);
    })
  );
};