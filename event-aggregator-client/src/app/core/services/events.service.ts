import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { EventItem, EventsResponse, EventStatus } from '../models/event.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class EventsService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/events`;

  getEvents(params: {
    category?: string;
    language?: string;
    search?: string;
    page?: number;
    pageSize?: number;
  } = {}) {
    let httpParams = new HttpParams();
    if (params.category)  httpParams = httpParams.set('category', params.category);
    if (params.language)  httpParams = httpParams.set('language', params.language);
    if (params.search)    httpParams = httpParams.set('search', params.search);
    if (params.page)      httpParams = httpParams.set('page', params.page);
    if (params.pageSize)  httpParams = httpParams.set('pageSize', params.pageSize);

    return this.http.get<EventsResponse>(this.base, { params: httpParams });
  }

  getById(id: number) {
    return this.http.get<EventItem>(`${this.base}/${id}`);
  }
}
