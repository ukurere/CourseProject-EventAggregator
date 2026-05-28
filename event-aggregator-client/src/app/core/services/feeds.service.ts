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
    return this.http.post<{ message: string }>(`${this.base}/feeds/${id}/refresh`, {});
  }
}
