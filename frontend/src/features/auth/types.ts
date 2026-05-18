export interface User {
  id: string
  email: string
  displayName: string
}

export interface AuthResponse {
  user: User
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterRequest {
  email: string
  password: string
  displayName: string
}
