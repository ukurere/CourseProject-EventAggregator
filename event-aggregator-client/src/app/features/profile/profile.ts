import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDatepickerModule, MatCalendarCellClassFunction } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { QRCodeComponent } from 'angularx-qrcode';
import { AuthService } from '../../core/services/auth.service';
import { UsersService } from '../../core/services/users.service';
import { FiltersService } from '../../core/services/filters.service';
import { Filter, FilterGroup } from '../../core/models/filter.model';
import { EventItem } from '../../core/models/event.model';
import { User } from '../../core/models/user.model';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    FormsModule, DatePipe, RouterLink, QRCodeComponent,
    MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatChipsModule,
    MatDividerModule, MatProgressSpinnerModule, MatTabsModule,
    MatDatepickerModule, MatNativeDateModule,
  ],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class Profile implements OnInit {
  private auth           = inject(AuthService);
  private usersService   = inject(UsersService);
  private filtersService = inject(FiltersService);
  private snackBar       = inject(MatSnackBar);

  user           = signal<User | null>(null);
  allGroups      = signal<FilterGroup[]>([]);
  userFilterIds  = signal<Set<number>>(new Set());
  savedEvents    = signal<EventItem[]>([]);
  calendarEvents = signal<EventItem[]>([]);
  loading        = signal(true);

  // 2FA
  twoFactorEnabled = signal(false);
  qrUri            = signal('');
  twoFaCode        = '';
  twoFaLoading     = signal(false);

  // Календар
  selectedDate = signal<Date | null>(null);

  eventsForDate = computed(() => {
    const d   = this.selectedDate();
    const all = this.calendarEvents();
    if (!d) return all;
    return all.filter(e => {
      const ev = new Date(e.eventDate ?? e.publishedDate);
      return ev.getFullYear() === d.getFullYear() &&
             ev.getMonth()    === d.getMonth()    &&
             ev.getDate()     === d.getDate();
    });
  });

  dateClass: MatCalendarCellClassFunction<Date> = (date, view) => {
    if (view !== 'month') return '';
    return this.calendarEvents().some(e => {
      const ev = new Date(e.eventDate ?? e.publishedDate);
      return ev.getFullYear() === date.getFullYear() &&
             ev.getMonth()    === date.getMonth()    &&
             ev.getDate()     === date.getDate();
    }) ? 'has-event' : '';
  };

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
    this.usersService.getSavedEvents(this.userId).subscribe(e => this.savedEvents.set(e));
    this.usersService.getCalendarEvents(this.userId).subscribe(e => this.calendarEvents.set(e));
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

  onDateSelected(date: Date | null) {
    const cur = this.selectedDate();
    if (cur && date && cur.toDateString() === date.toDateString()) {
      this.selectedDate.set(null); // повторний клік — скидає фільтр
    } else {
      this.selectedDate.set(date);
    }
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
