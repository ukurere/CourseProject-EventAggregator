import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { QRCodeComponent } from 'angularx-qrcode';
import { AuthService } from '../../core/services/auth.service';
import { UsersService } from '../../core/services/users.service';
import { FiltersService } from '../../core/services/filters.service';
import { Filter, FilterGroup } from '../../core/models/filter.model';
import { User } from '../../core/models/user.model';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    FormsModule, QRCodeComponent,
    MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatChipsModule,
    MatDividerModule, MatProgressSpinnerModule,
  ],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class Profile implements OnInit {
  private auth          = inject(AuthService);
  private usersService  = inject(UsersService);
  private filtersService = inject(FiltersService);
  private snackBar      = inject(MatSnackBar);

  user          = signal<User | null>(null);
  allGroups     = signal<FilterGroup[]>([]);
  userFilterIds = signal<Set<number>>(new Set());
  loading       = signal(true);

  // 2FA
  twoFactorEnabled = signal(false);
  qrUri            = signal('');
  twoFaCode        = '';
  twoFaLoading     = signal(false);

  get userId() { return this.auth.currentUser()?.id ?? 0; }

  ngOnInit() {
    this.usersService.getById(this.userId).subscribe({
      next: u => {
        this.user.set(u);
        this.userFilterIds.set(new Set(u.filters.map(f => f.id)));
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
    this.filtersService.getGrouped().subscribe(g => this.allGroups.set(g));
    this.twoFactorEnabled.set(this.auth.currentUser()?.twoFactorEnabled ?? false);
  }

  isSubscribed(filterId: number) { return this.userFilterIds().has(filterId); }

  toggleFilter(filter: Filter) {
    const ids = this.userFilterIds();
    if (ids.has(filter.id)) {
      this.usersService.removeFilter(this.userId, filter.id).subscribe(() => {
        this.userFilterIds.set(new Set([...ids].filter(id => id !== filter.id)));
        this.snackBar.open(`«${filter.name}» видалено з підписок`, '', { duration: 2000 });
      });
    } else {
      this.usersService.addFilter(this.userId, filter.id).subscribe(() => {
        this.userFilterIds.set(new Set([...ids, filter.id]));
        this.snackBar.open(`«${filter.name}» додано до підписок`, '', { duration: 2000 });
      });
    }
  }

  saveProfile() {
    const u = this.user();
    if (!u) return;
    this.usersService.update(this.userId, {
      firstName: u.firstName,
      lastName: u.lastName,
      reportFrequency: u.reportFrequency,
    }).subscribe(() => this.snackBar.open('Профіль збережено', '', { duration: 2000 }));
  }

  // ── 2FA ──────────────────────────────────────────────────────────────────

  setupTwoFactor() {
    this.twoFaLoading.set(true);
    this.auth.setupTwoFactor().subscribe({
      next: res => { this.qrUri.set(res.otpauthUri); this.twoFaLoading.set(false); },
      error: ()  => this.twoFaLoading.set(false),
    });
  }

  confirmTwoFactor() {
    this.twoFaLoading.set(true);
    this.auth.confirmTwoFactor(this.twoFaCode).subscribe({
      next: () => {
        this.twoFaLoading.set(false);
        this.twoFactorEnabled.set(true);
        this.qrUri.set('');
        this.twoFaCode = '';
        this.snackBar.open('2FA увімкнено', '', { duration: 3000 });
      },
      error: err => {
        this.twoFaLoading.set(false);
        this.snackBar.open(err.error?.message ?? 'Невірний код', 'ОК', { duration: 3000 });
      },
    });
  }

  disableTwoFactor() {
    this.auth.disableTwoFactor().subscribe({
      next: () => {
        this.twoFactorEnabled.set(false);
        this.snackBar.open('2FA вимкнено', '', { duration: 3000 });
      },
    });
  }
}
