import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.getToken();

  if (token) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }

  return next(req).pipe(
    catchError((err: unknown) => {
      // Backstop: token rejected by the server (expired, invalid) — back to login
      if (err instanceof HttpErrorResponse && err.status === 401 && auth.isAuthenticated()) {
        auth.logout();
      }
      return throwError(() => err);
    })
  );
};
