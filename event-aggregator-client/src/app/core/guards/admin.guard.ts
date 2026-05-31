import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

const ADMIN_EMAIL = 'adamyocardium@gmail.com';

export const adminGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  if (auth.currentUser()?.email === ADMIN_EMAIL) return true;

  router.navigate(['/']);
  return false;
};
