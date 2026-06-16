import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DigestService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/digest`;

  sendNow(userId: number) {
    return this.http.post<{ message: string }>(`${this.base}/send/${userId}`, {});
  }

  sendTelegramNow(userId: number) {
    return this.http.post<{ message: string }>(`${this.base}/send-telegram/${userId}`, {});
  }
}
