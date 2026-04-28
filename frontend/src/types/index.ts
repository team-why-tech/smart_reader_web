// Generic API response wrapper matching backend ApiResponse<T>
export interface ApiResponse<T> {
  success: boolean;
  message: string | null;
  data: T | null;
  errors: string[] | null;
}

// Auth
export interface LoginDto {
  email: string;
  password: string;
}

export interface RegisterDto {
  name: string;
  email: string;
  password: string;
}

export interface TokenResponseDto {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface RefreshTokenRequestDto {
  refreshToken: string;
}

// Users
export interface UserDto {
  id: number;
  name: string;
  email: string;
  roleName: string;
  isActive: boolean;
  createdAt: string;
}

export interface UpdateUserDto {
  name?: string;
  email?: string;
  roleId?: number;
  isActive?: boolean;
}

export interface CreateUserDto {
  name: string;
  email: string;
  password: string;
  roleId: number;
}

// Roles
export interface RoleDto {
  id: number;
  name: string;
  description: string | null;
}

export interface CreateRoleDto {
  name: string;
  description?: string;
}

// Audit Logs
export interface AuditLogDto {
  id: number;
  userId: number | null;
  action: string;
  entityName: string;
  entityId: number | null;
  timestamp: string;
  details: string | null;
}

// Auth context user (decoded from JWT)
export interface AuthUser {
  id: string;
  name: string;
  email: string;
  role: string;
}
