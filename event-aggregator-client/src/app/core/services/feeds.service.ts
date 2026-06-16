import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface SourceStats {
  id: number;
  name: string;
  category: string;
  language: string;
  isActive: boolean;
  lastFetched: string | null;
  eventCount: number;
}

export interface AdminStats {
  totalEvents: number;
  totalUsers: number;
  activeSources: number;
  sources: SourceStats[];
}

export interface AdminUser {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  isBanned: boolean;
  reportFrequency: string;
  telegramNotificationsEnabled: boolean;
  telegramChatId: number | null;
  filterCount: number;
  savedCount: number;
  calendarCount: number;
}

export interface AdminUserDetail extends AdminUser {
  lastReportSent: string | null;
  twoFactorEnabled: boolean;
  telegramUsername: string | null;
  filters: { id: number; name: string; type: string }[];
}

@Injectable({ providedIn: 'root' })
export class FeedsService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}`;

  getStats() {
    return this.http.get<AdminStats>(`${this.base}/admin/stats`);
  }

  refreshAll() {
    return this.http.post<{ message: string }>(`${this.base}/feeds/refresh`, {});
  }

  refreshOne(id: number) {
    return this.http.post<{ message: string; lastFetched: string }>(`${this.base}/feeds/${id}/refresh`, {});
  }

  toggleActive(id: number) {
    return this.http.patch<{ id: number; isActive: boolean }>(`${this.base}/admin/feeds/${id}/toggle`, {});
  }

  getUsers() {
    return this.http.get<AdminUser[]>(`${this.base}/admin/users`);
  }

  getUserDetail(id: number) {
    return this.http.get<AdminUserDetail>(`${this.base}/admin/users/${id}`);
  }

  toggleBan(id: number) {
    return this.http.post<{ id: number; isBanned: boolean }>(`${this.base}/admin/users/${id}/toggle-ban`, {});
  }

  deleteUser(id: number) {
    return this.http.delete<void>(`${this.base}/admin/users/${id}`);
  }
}
