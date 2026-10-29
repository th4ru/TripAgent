import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const token = auth.token();

  const authorized = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(authorized).pipe(
    catchError((error: HttpErrorResponse) => {
      const isAuthEndpoint =
        request.url.includes('/api/auth/login') || request.url.includes('/api/auth/register');

      if (error.status === 401 && !isAuthEndpoint && auth.isAuthenticated()) {
        auth.signOut('/login');
      }

      return throwError(() => error);
    })
  );
};
