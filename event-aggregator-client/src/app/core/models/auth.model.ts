export interface AuthResponse {
  token: string;
  user: AuthUser;
}

export interface TwoFactorRequired {
  requiresTwoFactor: true;
  userId: number;
}

export interface AuthUser {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  photoUrl?: string;
  reportFrequency: string;
  twoFactorEnabled: boolean;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}
