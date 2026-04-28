import client from './client';
import type { ApiResponse, UserDto, UpdateUserDto } from '../types';

export function getUsers() {
  return client.get<ApiResponse<UserDto[]>>('/api/users');
}

export function getUser(id: number) {
  return client.get<ApiResponse<UserDto>>(`/api/users/${id}`);
}

export function updateUser(id: number, data: UpdateUserDto) {
  return client.put<ApiResponse<UserDto>>(`/api/users/${id}`, data);
}

export function deleteUser(id: number) {
  return client.delete<ApiResponse<boolean>>(`/api/users/${id}`);
}
