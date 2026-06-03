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
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDatepickerModule, MatCalendarCellClassFunction } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { QRCodeComponent } from 'angularx-qrcode';
import { AuthService } from '../../core/services/auth.service';
import { UsersService } from '../../core/services/users.service';
import { FiltersService } from '../../core/services/filters.service';
import { DigestService } from '../../core/services/digest.service';
import { KeywordsService, UserKeyword } from '../../core/services/keywords.service';
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
    MatDividerModule, MatSlideToggleModule, MatProgressSpinnerModule, MatTabsModule,
    MatDatepickerModule, MatNativeDateModule, MatTooltipModule,
  ],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class Profile implements OnInit {
  private auth           = inject(AuthService);
  private usersService   = inject(UsersService);
  private filtersService = inject(FiltersService);
  private digestService  = inject(DigestService);
  private keywordsService = inject(KeywordsService);
  private snackBar       = inject(MatSnackBar);

  user           = signal<User | null>(null);
  allGroups      = signal<FilterGroup[]>([]);
  userFilterIds  = signal<Set<number>>(new Set());
  savedEvents    = signal<EventItem[]>([]);
  calendarEvents = signal<EventItem[]>([]);
  loading        = signal(true);

  // Ключові слова
  userKeywords   = signal<UserKeyword[]>([]);
  newKeyword     = '';

  // Send now
  sendingEmail    = signal(false);
  sendingTelegram = signal(false);

  // Telegram
  tgEnabled  = signal(false);
  tgUsername = signal('');
  tgChatId   = signal<number | null>(null);
  tgSaving   = signal(false);

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
        this.tgEnabled.set(u.telegramNotificationsEnabled);
        this.tgUsername.set(u.telegramUsername ?? '');
        this.tgChatId.set(u.telegramChatId ?? null);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
    this.filtersService.getGrouped().subscribe(g => this.allGroups.set(g));
    this.twoFactorEnabled.set(this.auth.currentUser()?.twoFactorEnabled ?? false);
    this.usersService.getSavedEvents(this.userId).subscribe(e => this.savedEvents.set(e));
    this.usersService.getCalendarEvents(this.userId).subscribe(e => this.calendarEvents.set(e));
    this.keywordsService.getAll(this.userId).subscribe(k => this.userKeywords.set(k));
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

  saveTelegram() {
    this.tgSaving.set(true);
    this.usersService.updateTelegram(this.userId, {
      telegramNotificationsEnabled: this.tgEnabled(),
      telegramUsername: this.tgUsername().trim().replace(/^@/, '') || undefined,
    }).subscribe({
      next: res => {
        this.tgChatId.set(res.telegramChatId ?? null);
        this.tgSaving.set(false);
        this.snackBar.open('Telegram-налаштування збережено', '', { duration: 2500 });
      },
      error: () => {
        this.tgSaving.set(false);
        this.snackBar.open('Помилка збереження', 'ОК', { duration: 3000 });
      },
    });
  }

  onDateSelected(date: Date | null) {
    const cur = this.selectedDate();
    if (cur && date && cur.toDateString() === date.toDateString()) {
      this.selectedDate.set(null); // повторний клік — скидає фільтр
    } else {
      this.selectedDate.set(date);
    }
  }

  // ── Ключові слова ────────────────────────────────────────────────────────

  addKeyword() {
    const kw = this.newKeyword.trim();
    if (!kw) return;
    this.keywordsService.add(this.userId, kw).subscribe({
      next: k => {
        this.userKeywords.update(list => [...list, k]);
        this.newKeyword = '';
        this.snackBar.open(`«${k.keyword}» додано`, '', { duration: 2000 });
      },
      error: err => this.snackBar.open(err.error?.error ?? 'Помилка', 'ОК', { duration: 3000 }),
    });
  }

  removeKeyword(kw: UserKeyword) {
    this.keywordsService.delete(this.userId, kw.id).subscribe({
      next: () => {
        this.userKeywords.update(list => list.filter(k => k.id !== kw.id));
        this.snackBar.open(`«${kw.keyword}» видалено`, '', { duration: 2000 });
      },
    });
  }

  onKeywordEnter(event: KeyboardEvent) {
    if (event.key === 'Enter') this.addKeyword();
  }

  // ── Send now ─────────────────────────────────────────────────────────────

  sendEmailNow() {
    this.sendingEmail.set(true);
    this.digestService.sendNow(this.userId).subscribe({
      next: res => {
        this.sendingEmail.set(false);
        this.snackBar.open(res.message, '', { duration: 3000 });
      },
      error: err => {
        this.sendingEmail.set(false);
        this.snackBar.open(err.error?.error ?? 'Помилка відправки', 'ОК', { duration: 4000 });
      },
    });
  }

  sendTelegramNow() {
    this.sendingTelegram.set(true);
    this.digestService.sendTelegramNow(this.userId).subscribe({
      next: res => {
        this.sendingTelegram.set(false);
        this.snackBar.open(res.message, '', { duration: 3000 });
      },
      error: err => {
        this.sendingTelegram.set(false);
        this.snackBar.open(err.error?.error ?? 'Помилка відправки', 'ОК', { duration: 4000 });
      },
    });
  }

  // ── 2FA ──────────────────────────────────────────────────────────────────

  setupTwoFactor() {
    this.twoFaLoading.set(true);
    this.auth.setupTwoFactor().subscribe({
      next: res => { this.qrUri.set(res.otpauthUri); this.twoFaLoading.set(false); },
      error: err => {
        this.twoFaLoading.set(false);
        const msg = err.status === 401
          ? 'Сесія закінчилась — увійдіть знову'
          : (err.error?.message ?? 'Помилка налаштування 2FA');
        this.snackBar.open(msg, 'ОК', { duration: 4000 });
      },
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
