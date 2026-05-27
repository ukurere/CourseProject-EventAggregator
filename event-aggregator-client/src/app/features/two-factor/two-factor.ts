import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-two-factor',
  standalone: true,
  imports: [
    FormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatButtonModule,
    MatIconModule, MatProgressSpinnerModule,
  ],
  templateUrl: './two-factor.html',
  styleUrl: './two-factor.scss',
})
export class TwoFactor implements OnInit {
  private auth   = inject(AuthService);
  private router = inject(Router);

  userId  = 0;
  code    = '';
  loading = signal(false);
  error   = signal('');

  ngOnInit() {
    const state = history.state as { userId?: number };
    if (!state?.userId) {
      this.router.navigate(['/login']);
      return;
    }
    this.userId = state.userId;
  }

  submit() {
    this.error.set('');
    this.loading.set(true);

    this.auth.verifyTwoFactor(this.userId, this.code).subscribe({
      next: () => { this.loading.set(false); this.router.navigate(['/']); },
      error: err => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Невірний код');
      },
    });
  }
}
