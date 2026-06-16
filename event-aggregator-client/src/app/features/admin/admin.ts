import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDialog } from '@angular/material/dialog';
import { FeedsService, AdminStats, SourceStats, AdminUser } from '../../core/services/feeds.service';
import { UserDetailDialog } from './user-detail-dialog';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [
    DatePipe, DecimalPipe,
    MatCardModule, MatTableModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatProgressBarModule, MatChipsModule,
    MatTooltipModule, MatDividerModule, MatSlideToggleModule, MatTabsModule,
  ],
  templateUrl: './admin.html',
  styleUrl:    './admin.scss',
})
export class Admin implements OnInit {
  private feedsService = inject(FeedsService);
  private snackBar     = inject(MatSnackBar);
  private dialog       = inject(MatDialog);

  stats         = signal<AdminStats | null>(null);
  loading       = signal(true);
  refreshingAll = signal(false);
  refreshingId  = signal<number | null>(null);
  togglingId    = signal<number | null>(null);

  users        = signal<AdminUser[]>([]);
  usersLoading = signal(true);

  readonly sourceColumns = ['name', 'category', 'language', 'events', 'lastFetched', 'status', 'actions'];
  readonly userColumns   = ['avatar', 'name', 'email', 'filters', 'telegram', 'status', 'actions'];

  ngOnInit() {
    this.loadStats();
    this.loadUsers();
  }

  loadStats() {
    this.loading.set(true);
    this.feedsService.getStats().subscribe({
      next: s  => { this.stats.set(s); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  loadUsers() {
    this.usersLoading.set(true);
    this.feedsService.getUsers().subscribe({
      next: u  => { this.users.set(u); this.usersLoading.set(false); },
      error: () => this.usersLoading.set(false),
    });
  }

  refreshAll() {
    this.refreshingAll.set(true);
    this.feedsService.refreshAll().subscribe({
      next: r => {
        this.refreshingAll.set(false);
        this.snackBar.open(r.message, '', { duration: 3000 });
        this.loadStats();
      },
      error: () => {
        this.refreshingAll.set(false);
        this.snackBar.open('Помилка оновлення', 'ОК', { duration: 3000 });
      },
    });
  }

  refreshOne(source: SourceStats) {
    this.refreshingId.set(source.id);
    this.feedsService.refreshOne(source.id).subscribe({
      next: r => {
        this.refreshingId.set(null);
        this.snackBar.open(r.message, '', { duration: 3000 });
        source.lastFetched = r.lastFetched;
        const s = this.stats();
        if (s) this.stats.set({ ...s });
      },
      error: () => {
        this.refreshingId.set(null);
        this.snackBar.open('Помилка оновлення', 'ОК', { duration: 3000 });
      },
    });
  }

  toggleActive(source: SourceStats) {
    this.togglingId.set(source.id);
    this.feedsService.toggleActive(source.id).subscribe({
      next: r => {
        source.isActive = r.isActive;
        this.togglingId.set(null);
        const s = this.stats();
        if (s) {
          s.activeSources += r.isActive ? 1 : -1;
          this.stats.set({ ...s });
        }
      },
      error: () => {
        this.togglingId.set(null);
        this.snackBar.open('Помилка зміни статусу', 'ОК', { duration: 3000 });
      },
    });
  }

  openUser(user: AdminUser) {
    const ref = this.dialog.open(UserDetailDialog, {
      data: { ...user },
      width: '480px',
    });

    ref.afterClosed().subscribe(result => {
      if (!result) return;
      if (result.action === 'delete') {
        this.users.update(list => list.filter(u => u.id !== user.id));
        const s = this.stats();
        if (s) this.stats.set({ ...s, totalUsers: s.totalUsers - 1 });
      } else if (result.action === 'ban') {
        this.users.update(list =>
          list.map(u => u.id === user.id ? { ...u, isBanned: result.isBanned } : u)
        );
      }
    });
  }

  initials(u: AdminUser) {
    return (u.firstName[0] + u.lastName[0]).toUpperCase();
  }

  timeAgo(date: string | null): string {
    if (!date) return 'Ніколи';
    const diff = Date.now() - new Date(date).getTime();
    const m = Math.floor(diff / 60_000);
    if (m < 1)  return 'Щойно';
    if (m < 60) return `${m} хв тому`;
    const h = Math.floor(m / 60);
    if (h < 24) return `${h} год тому`;
    return `${Math.floor(h / 24)} д тому`;
  }

  get maxEvents(): number {
    return Math.max(...(this.stats()?.sources.map(s => s.eventCount) ?? [1]));
  }
}
