import client from './client';
import type { ApiResponse, LoginDto, RegisterDto, TokenResponseDto, RefreshTokenRequestDto } from '../types';

export function login(data: LoginDto) {
  return client.post<ApiResponse<TokenResponseDto>>('/api/auth/login', data);
}

export function register(data: RegisterDto) {
  return client.post<ApiResponse<TokenResponseDto>>('/api/auth/register', data);
}

export function refreshToken(data: RefreshTokenRequestDto) {
  return client.post<ApiResponse<TokenResponseDto>>('/api/auth/refresh-token', data);
}
