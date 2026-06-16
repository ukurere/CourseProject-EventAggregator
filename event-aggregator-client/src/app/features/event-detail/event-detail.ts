import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../core/services/auth.service';
import { EventsService } from '../../core/services/events.service';
import { UsersService } from '../../core/services/users.service';
import { EventItem } from '../../core/models/event.model';

@Component({
  selector: 'app-event-detail',
  standalone: true,
  imports: [
    DatePipe, RouterLink,
    MatButtonModule, MatIconModule, MatChipsModule,
    MatProgressSpinnerModule, MatDividerModule, MatTooltipModule,
  ],
  templateUrl: './event-detail.html',
  styleUrl: './event-detail.scss',
})
export class EventDetail implements OnInit {
  private route        = inject(ActivatedRoute);
  private eventsService = inject(EventsService);
  private usersService  = inject(UsersService);
  private auth          = inject(AuthService);
  private sanitizer     = inject(DomSanitizer);
  private snackBar      = inject(MatSnackBar);

  event      = signal<EventItem | null>(null);
  loading    = signal(true);
  saved      = signal(false);
  inCalendar = signal(false);

  get userId() { return this.auth.currentUser()?.id ?? 0; }

  safeContent = computed((): SafeHtml | null => {
    const e = this.event();
    if (!e) return null;
    const html = e.contentHtml || e.description;
    return this.sanitizer.bypassSecurityTrustHtml(html);
  });

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    this.eventsService.getById(id).subscribe({
      next: e  => { this.event.set(e); this.loading.set(false); },
      error: () => this.loading.set(false),
    });

    this.usersService.getSavedEvents(this.userId)
      .subscribe(evs => { if (evs.some(e => e.id === id)) this.saved.set(true); });

    this.usersService.getCalendarEvents(this.userId)
      .subscribe(evs => { if (evs.some(e => e.id === id)) this.inCalendar.set(true); });
  }

  toggleSaved() {
    const ev = this.event();
    if (!ev) return;

    if (this.saved()) {
      this.usersService.removeEventStatus(this.userId, ev.id).subscribe(() => {
        this.saved.set(false);
        this.snackBar.open('Прибрано зі збережених', '', { duration: 2000 });
      });
    } else {
      this.usersService.setEventStatus(this.userId, ev.id, { status: 'Interesting', isInCalendar: false })
        .subscribe(() => {
          this.saved.set(true);
          this.snackBar.open('Збережено ★', '', { duration: 2000 });
        });
    }
  }

  toggleCalendar() {
    const ev = this.event();
    if (!ev) return;

    if (this.inCalendar()) {
      this.usersService.setEventStatus(this.userId, ev.id, { isInCalendar: false })
        .subscribe(() => {
          this.inCalendar.set(false);
          this.snackBar.open('Прибрано з календаря', '', { duration: 2000 });
        });
    } else {
      this.usersService.setEventStatus(this.userId, ev.id, { isInCalendar: true })
        .subscribe(() => {
          this.inCalendar.set(true);
          this.snackBar.open('Додано до календаря 📅', '', { duration: 2000 });
        });
    }
  }
}
