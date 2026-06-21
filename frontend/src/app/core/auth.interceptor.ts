import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from './auth.service';

/** Auth endpoints that are part of the credential flow — a 401 here must not trigger a refresh retry. */
const PUBLIC_AUTH_PATHS = ['/auth/login', '/auth/register', '/auth/refresh', '/auth/logout'];

/** Attaches the bearer token and transparently retries once after refreshing an expired session. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  const isCredentialFlow = PUBLIC_AUTH_PATHS.some(path => req.url.includes(path));
  const withToken = auth.accessToken
    ? req.clone({ setHeaders: { Authorization: `Bearer ${auth.accessToken}` } })
    : req;

  return next(withToken).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || isCredentialFlow) return throwError(() => error);

      return from(auth.tryRefresh()).pipe(
        switchMap(refreshed => {
          if (!refreshed) return throwError(() => error);
          const retried = req.clone({ setHeaders: { Authorization: `Bearer ${auth.accessToken}` } });
          return next(retried);
        }));
    }));
};
