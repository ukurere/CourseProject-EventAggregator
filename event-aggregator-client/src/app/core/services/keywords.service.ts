import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface UserKeyword {
  id: number;
  keyword: string;
}

@Injectable({ providedIn: 'root' })
export class KeywordsService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  getAll(userId: number) {
    return this.http.get<UserKeyword[]>(`${this.base}/users/${userId}/keywords`);
  }

  add(userId: number, keyword: string) {
    return this.http.post<UserKeyword>(`${this.base}/users/${userId}/keywords`, { keyword });
  }

  delete(userId: number, id: number) {
    return this.http.delete<void>(`${this.base}/users/${userId}/keywords/${id}`);
  }
}
