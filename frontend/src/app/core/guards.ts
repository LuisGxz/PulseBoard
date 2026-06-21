import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Requires a signed-in user; bounces to login with a returnUrl otherwise. */
export const authGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.accessToken || auth.isAuthenticated()) return true;
  if (await auth.tryRefresh()) return true;

  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** Keeps authenticated users away from the login/register screens. */
export const guestGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) return router.createUrlTree(['/dashboards']);
  if (await auth.tryRefresh()) return router.createUrlTree(['/dashboards']);
  return true;
};
