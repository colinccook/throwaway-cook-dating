export const API_BASE_URL = '/api';

export interface SignUpData {
  email: string;
  password: string;
  displayName: string;
  dateOfBirth: string;
  gender: string;
  preferredGender?: string;
  minAge: number;
  maxAge: number;
  maxDistanceKm: number;
}

export interface SignInData {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  userId: string;
  email: string;
}

export interface ProfileDto {
  userId: string;
  displayName: string;
  bio: string;
  dateOfBirth: string;
  gender: string;
  preferredGender?: string;
  minAge: number;
  maxAge: number;
  maxDistanceKm: number;
  photoUrls: string[];
  lookingStatus: string;
}

export interface UpdateProfileData {
  displayName?: string;
  bio?: string;
  dateOfBirth?: string;
  gender?: string;
  preferredGender?: string;
  minAge?: number;
  maxAge?: number;
  maxDistanceKm?: number;
}

function authHeaders(): Record<string, string> {
  const token = localStorage.getItem('auth_token');
  return token ? { Authorization: `Bearer ${token}` } : {};
}

export async function apiFetch<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...authHeaders(),
      ...options?.headers,
    },
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(body || `API error: ${response.status} ${response.statusText}`);
  }

  return response.json() as Promise<T>;
}

export function signUp(data: SignUpData): Promise<AuthResponse> {
  return apiFetch<AuthResponse>('/auth/signup', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export function signIn(data: SignInData): Promise<AuthResponse> {
  return apiFetch<AuthResponse>('/auth/signin', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export function getProfile(): Promise<ProfileDto> {
  return apiFetch<ProfileDto>('/profile');
}

export function updateProfile(data: UpdateProfileData): Promise<ProfileDto> {
  return apiFetch<ProfileDto>('/profile', {
    method: 'PUT',
    body: JSON.stringify(data),
  });
}

export async function setLookingStatus(status: string): Promise<void> {
  await apiFetch<void>('/profile/status', {
    method: 'PUT',
    body: JSON.stringify({ status }),
  });
}
