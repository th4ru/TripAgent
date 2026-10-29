export interface AuthUser {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
}

export interface AuthResult {
  token: string;
  expiresAtUtc: string;
  user: AuthUser;
}

export interface LoginRequest {
  emailOrUserName: string;
  password: string;
}

export interface RegisterRequest {
  userName: string;
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
}
