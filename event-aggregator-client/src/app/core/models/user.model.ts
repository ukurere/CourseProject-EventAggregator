import { Filter } from './filter.model';

export interface User {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  photoUrl?: string;
  reportFrequency: string;
  lastReportSent?: string;
  filters: Filter[];
}

export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  reportFrequency?: string;
}

export interface UpdateUserRequest {
  firstName?: string;
  lastName?: string;
  photoUrl?: string;
  reportFrequency?: string;
}
