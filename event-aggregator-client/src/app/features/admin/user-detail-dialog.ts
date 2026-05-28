import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FeedsService, AdminUser, AdminUserDetail } from '../../core/services/feeds.service';

@Component({
  selector: 'app-user-detail-dialog',
  standalone: true,
  imports: [
    DatePipe,
    MatDialogModule, MatButtonModule, MatChipsModule,
    MatIconModule, MatProgressSpinnerModule, MatDividerModule,
  ],
  templateUrl: './user-detail-dialog.html',
  styleUrl: './user-detail-dialog.scss',
})
export class UserDetailDialog implements OnInit {
  dialogRef   = inject<MatDialogRef<UserDetailDialog>>(MatDialogRef);
  data        = inject<AdminUser>(MAT_DIALOG_DATA);
  private svc = inject(FeedsService);
  private sb  = inject(MatSnackBar);

  detail  = signal<AdminUserDetail | null>(null);
  loading = signal(true);
  acting  = signal(false);

  get initials() {
    return (this.data.firstName[0] + this.data.lastName[0]).toUpperCase();
  }

  ngOnInit() {
    this.svc.getUserDetail(this.data.id).subscribe({
      next: d  => { this.detail.set(d); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  toggleBan() {
    this.acting.set(true);
    this.svc.toggleBan(this.data.id).subscribe({
      next: r => {
        this.data.isBanned = r.isBanned;
        const d = this.detail();
        if (d) this.detail.set({ ...d, isBanned: r.isBanned });
        this.acting.set(false);
        this.sb.open(r.isBanned ? 'Користувача заблоковано' : 'Користувача розблоковано', '', { duration: 3000 });
        this.dialogRef.close({ action: 'ban', isBanned: r.isBanned });
      },
      error: () => { this.acting.set(false); this.sb.open('Помилка', 'ОК', { duration: 3000 }); },
    });
  }

  confirmDelete() {
    if (!confirm(`Видалити користувача ${this.data.firstName} ${this.data.lastName}? Цю дію не можна скасувати.`)) return;
    this.acting.set(true);
    this.svc.deleteUser(this.data.id).subscribe({
      next: () => {
        this.acting.set(false);
        this.sb.open('Користувача видалено', '', { duration: 3000 });
        this.dialogRef.close({ action: 'delete' });
      },
      error: () => { this.acting.set(false); this.sb.open('Помилка видалення', 'ОК', { duration: 3000 }); },
    });
  }
}
