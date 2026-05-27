export type FilterType = 'Artist' | 'EventType' | 'City' | 'Technology';

export interface Filter {
  id: number;
  name: string;
  type: FilterType;
  searchKeyword: string;
}

export interface FilterGroup {
  type: FilterType;
  label: string;
  filters: Filter[];
}
