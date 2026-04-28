import client from './client';
import type { ApiResponse, AuditLogDto } from '../types';

export function getAuditLogs() {
  return client.get<ApiResponse<AuditLogDto[]>>('/api/auditlog');
}

export function getAuditLogsByUser(userId: number) {
  return client.get<ApiResponse<AuditLogDto[]>>(`/api/auditlog/user/${userId}`);
}
