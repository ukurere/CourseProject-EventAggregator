import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Filter, FilterGroup, FilterType } from '../models/filter.model';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

const TYPE_LABELS: Record<FilterType, string> = {
  Artist: 'Виконавці',
  EventType: 'Типи подій',
  City: 'Міста',
  Technology: 'Технології',
};

@Injectable({ providedIn: 'root' })
export class FiltersService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/filters`;

  getAll() {
    return this.http.get<Filter[]>(this.base);
  }

  getGrouped() {
    return this.getAll().pipe(
      map(filters => {
        const groups = new Map<FilterType, Filter[]>();
        for (const f of filters) {
          if (!groups.has(f.type)) groups.set(f.type, []);
          groups.get(f.type)!.push(f);
        }
        return Array.from(groups.entries()).map(([type, items]): FilterGroup => ({
          type,
          label: TYPE_LABELS[type] ?? type,
          filters: items,
        }));
      })
    );
  }

  create(name: string, type: FilterType, searchKeyword: string) {
    return this.http.post<Filter>(this.base, { name, type, searchKeyword });
  }

  delete(id: number) {
    return this.http.delete(`${this.base}/${id}`);
  }
}
