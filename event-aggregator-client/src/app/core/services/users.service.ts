import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CreateUserRequest, UpdateUserRequest, User } from '../models/user.model';
import { EventItem, EventsResponse, EventStatus } from '../models/event.model';
import { Filter } from '../models/filter.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/users`;

  create(req: CreateUserRequest) {
    return this.http.post<User>(this.base, req);
  }

  getById(id: number) {
    return this.http.get<User>(`${this.base}/${id}`);
  }

  update(id: number, req: UpdateUserRequest) {
    return this.http.put(`${this.base}/${id}`, req);
  }

  getFilters(id: number) {
    return this.http.get<Filter[]>(`${this.base}/${id}/filters`);
  }

  addFilter(userId: number, filterId: number) {
    return this.http.post(`${this.base}/${userId}/filters/${filterId}`, {});
  }

  removeFilter(userId: number, filterId: number) {
    return this.http.delete(`${this.base}/${userId}/filters/${filterId}`);
  }

  getFeed(userId: number, page = 1, pageSize = 20) {
    return this.http.get<EventsResponse>(
      `${this.base}/${userId}/feed`,
      { params: { page, pageSize } }
    );
  }

  setEventStatus(userId: number, eventId: number, status: EventStatus) {
    return this.http.post(`${this.base}/${userId}/events/${eventId}/status`, status);
  }
}
