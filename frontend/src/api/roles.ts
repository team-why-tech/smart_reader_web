import client from './client';
import type { ApiResponse, RoleDto, CreateRoleDto } from '../types';

export function getRoles() {
  return client.get<ApiResponse<RoleDto[]>>('/api/roles');
}

export function getRole(id: number) {
  return client.get<ApiResponse<RoleDto>>(`/api/roles/${id}`);
}

export function createRole(data: CreateRoleDto) {
  return client.post<ApiResponse<RoleDto>>('/api/roles', data);
}

export function updateRole(id: number, data: CreateRoleDto) {
  return client.put<ApiResponse<RoleDto>>(`/api/roles/${id}`, data);
}

export function deleteRole(id: number) {
  return client.delete<ApiResponse<boolean>>(`/api/roles/${id}`);
}
