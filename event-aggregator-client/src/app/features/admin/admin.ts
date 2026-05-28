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
import { FeedsService, AdminStats, SourceStats } from '../../core/services/feeds.service';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [
    DatePipe, DecimalPipe,
    MatCardModule, MatTableModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatProgressBarModule, MatChipsModule,
    MatTooltipModule, MatDividerModule,
  ],
  templateUrl: './admin.html',
  styleUrl:    './admin.scss',
})
export class Admin implements OnInit {
  private feedsService = inject(FeedsService);
  private snackBar     = inject(MatSnackBar);

  stats         = signal<AdminStats | null>(null);
  loading       = signal(true);
  refreshingAll = signal(false);
  refreshingId  = signal<number | null>(null);

  readonly columns = ['name', 'category', 'language', 'events', 'lastFetched', 'status', 'actions'];

  ngOnInit() { this.loadStats(); }

  loadStats() {
    this.loading.set(true);
    this.feedsService.getStats().subscribe({
      next: s  => { this.stats.set(s); this.loading.set(false); },
      error: () => this.loading.set(false),
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
        this.loadStats();
      },
      error: () => {
        this.refreshingId.set(null);
        this.snackBar.open('Помилка оновлення', 'ОК', { duration: 3000 });
      },
    });
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
