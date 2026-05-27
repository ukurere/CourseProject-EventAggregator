export interface FeedSourceInfo {
  name: string;
  language: string;
}

export interface EventItem {
  id: number;
  title: string;
  description: string;
  sourceUrl: string;
  imageUrl?: string;
  publishedDate: string;
  eventDate?: string;
  location?: string;
  category?: string;
  author?: string;
  source?: FeedSourceInfo;
}

export interface EventsResponse {
  total: number;
  page: number;
  pageSize: number;
  events: EventItem[];
}

export interface EventStatus {
  status?: string;
  isInCalendar: boolean;
}
