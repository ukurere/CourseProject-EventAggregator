import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, catchError, of } from 'rxjs';
import { AuthService } from '../services/auth.service';

const ADMIN_EMAIL = 'adamyocardium@gmail.com';

export const adminGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  const allow = (email: string | undefined) => {
    if (email === ADMIN_EMAIL) return true;
    router.navigate(['/']);
    return false;
  };

  // Якщо юзер вже є в пам'яті — перевіряємо одразу
  if (auth.currentUser()) {
    return allow(auth.currentUser()!.email);
  }

  // Токен є, але currentUser порожній (наприклад, localStorage частково очищено)
  // → підтягуємо юзера з API і тоді перевіряємо
  return auth.me().pipe(
    map(user => allow(user.email)),
    catchError(() => {
      router.navigate(['/']);
      return of(false);
    })
  );
};
