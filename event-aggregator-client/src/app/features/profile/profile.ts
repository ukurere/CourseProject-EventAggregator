import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { UsersService } from '../../core/services/users.service';
import { FiltersService } from '../../core/services/filters.service';
import { Filter, FilterGroup } from '../../core/models/filter.model';
import { User } from '../../core/models/user.model';

const DEMO_USER_ID = 1;

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatChipsModule,
    MatDividerModule, MatProgressSpinnerModule,
  ],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class Profile implements OnInit {
  private usersService = inject(UsersService);
  private filtersService = inject(FiltersService);
  private snackBar = inject(MatSnackBar);

  user = signal<User | null>(null);
  allGroups = signal<FilterGroup[]>([]);
  userFilterIds = signal<Set<number>>(new Set());
  loading = signal(true);

  ngOnInit() {
    this.usersService.getById(DEMO_USER_ID).subscribe({
      next: u => { this.user.set(u); this.userFilterIds.set(new Set(u.filters.map(f => f.id))); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
    this.filtersService.getGrouped().subscribe(g => this.allGroups.set(g));
  }

  isSubscribed(filterId: number) {
    return this.userFilterIds().has(filterId);
  }

  toggleFilter(filter: Filter) {
    const ids = this.userFilterIds();
    if (ids.has(filter.id)) {
      this.usersService.removeFilter(DEMO_USER_ID, filter.id).subscribe(() => {
        this.userFilterIds.set(new Set([...ids].filter(id => id !== filter.id)));
        this.snackBar.open(`«${filter.name}» видалено з підписок`, '', { duration: 2000 });
      });
    } else {
      this.usersService.addFilter(DEMO_USER_ID, filter.id).subscribe(() => {
        this.userFilterIds.set(new Set([...ids, filter.id]));
        this.snackBar.open(`«${filter.name}» додано до підписок`, '', { duration: 2000 });
      });
    }
  }

  saveProfile() {
    const u = this.user();
    if (!u) return;
    this.usersService.update(DEMO_USER_ID, {
      firstName: u.firstName,
      lastName: u.lastName,
      reportFrequency: u.reportFrequency,
    }).subscribe(() => this.snackBar.open('Профіль збережено', '', { duration: 2000 }));
  }
}
