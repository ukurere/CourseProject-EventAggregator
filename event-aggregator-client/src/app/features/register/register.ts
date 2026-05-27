import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    FormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatButtonModule,
    MatIconModule, MatProgressSpinnerModule,
  ],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  private auth   = inject(AuthService);
  private router = inject(Router);

  firstName = '';
  lastName  = '';
  email     = '';
  password  = '';
  loading   = signal(false);
  error     = signal('');
  hidePass  = true;

  submit() {
    this.error.set('');
    this.loading.set(true);

    this.auth.register({ firstName: this.firstName, lastName: this.lastName,
                         email: this.email, password: this.password })
      .subscribe({
        next: () => { this.loading.set(false); this.router.navigate(['/']); },
        error: err => {
          this.loading.set(false);
          this.error.set(err.error?.message ?? 'Помилка реєстрації');
        },
      });
  }
}
