import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar } from '@angular/material/snack-bar';
import { EventsService } from '../../core/services/events.service';
import { FiltersService } from '../../core/services/filters.service';
import { EventItem } from '../../core/models/event.model';
import { FilterGroup } from '../../core/models/filter.model';
import { EventCard } from '../../shared/components/event-card/event-card';

@Component({
  selector: 'app-feed',
  standalone: true,
  imports: [
    FormsModule,
    MatSidenavModule, MatListModule, MatCheckboxModule,
    MatButtonModule, MatIconModule, MatInputModule,
    MatFormFieldModule, MatProgressSpinnerModule,
    MatPaginatorModule, MatDividerModule,
    EventCard,
  ],
  templateUrl: './feed.html',
  styleUrl: './feed.scss',
})
export class Feed implements OnInit {
  private eventsService = inject(EventsService);
  private filtersService = inject(FiltersService);
  private snackBar = inject(MatSnackBar);

  events = signal<EventItem[]>([]);
  filterGroups = signal<FilterGroup[]>([]);
  loading = signal(false);
  total = signal(0);

  search = '';
  selectedCategory = '';
  selectedLanguage = '';
  page = 1;
  pageSize = 20;

  ngOnInit() {
    this.loadFilters();
    this.loadEvents();
  }

  loadFilters() {
    this.filtersService.getGrouped().subscribe(groups => this.filterGroups.set(groups));
  }

  loadEvents() {
    this.loading.set(true);
    this.eventsService.getEvents({
      category: this.selectedCategory || undefined,
      language: this.selectedLanguage || undefined,
      search: this.search || undefined,
      page: this.page,
      pageSize: this.pageSize,
    }).subscribe({
      next: res => {
        this.events.set(res.events);
        this.total.set(res.total);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Не вдалося завантажити події', 'ОК', { duration: 3000 });
      }
    });
  }

  onSearch() {
    this.page = 1;
    this.loadEvents();
  }

  onPageChange(e: PageEvent) {
    this.page = e.pageIndex + 1;
    this.pageSize = e.pageSize;
    this.loadEvents();
  }

  filterByCategory(category: string) {
    this.selectedCategory = this.selectedCategory === category ? '' : category;
    this.page = 1;
    this.loadEvents();
  }

  filterByLanguage(lang: string) {
    this.selectedLanguage = this.selectedLanguage === lang ? '' : lang;
    this.page = 1;
    this.loadEvents();
  }

  onMarkInteresting(event: EventItem) {
    this.snackBar.open(`«${event.title}» позначено як цікаве`, '', { duration: 2000 });
  }

  onAddToCalendar(event: EventItem) {
    this.snackBar.open(`«${event.title}» додано до календаря`, '', { duration: 2000 });
  }
}
