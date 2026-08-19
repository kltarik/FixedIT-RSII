import 'dart:typed_data';

import 'package:dio/dio.dart';

import '../core/api_client.dart';
import '../core/models.dart';

class DashboardData {
  const DashboardData(this.stats, this.report);
  final AdminStats stats;
  final FinancialReport report;
}

class AdminRepository {
  const AdminRepository(this.api);
  final ApiClient api;

  Future<DashboardData> getDashboard() async {
    final results = await Future.wait<Object>([
      getStats(),
      getFinancialReport(pageSize: 5),
    ]);
    return DashboardData(
      results[0] as AdminStats,
      results[1] as FinancialReport,
    );
  }

  Future<AdminStats> getStats() async {
    final response = await api.request<Json>(
      () => api.dio.get<Json>('/api/admin/stats'),
    );
    return AdminStats.fromJson(response.data ?? const {});
  }

  Future<PagedResult<AdminUserRecord>> getUsers({int page = 1}) async {
    final response = await api.request<Json>(
      () => api.dio.get<Json>(
        '/api/admin/users',
        queryParameters: {'page': page, 'pageSize': 20},
      ),
    );
    return PagedResult.fromJson(
      response.data ?? const {},
      AdminUserRecord.fromJson,
    );
  }

  Future<AdminUserRecord> setUserActive(String id, bool isActive) async {
    final response = await api.request<Json>(
      () => api.dio.put<Json>(
        '/api/admin/users/$id/activate',
        data: {'isActive': isActive},
      ),
    );
    return AdminUserRecord.fromJson(response.data ?? const {});
  }

  Future<void> deleteUser(String id) async {
    await api.request<void>(() => api.dio.delete<void>('/api/admin/users/$id'));
  }

  Future<PagedResult<ProfessionalRecord>> getProfessionals({
    int page = 1,
  }) async {
    final response = await api.request<Json>(
      () => api.dio.get<Json>(
        '/api/professionals',
        queryParameters: {'page': page, 'pageSize': 20},
      ),
    );
    return PagedResult.fromJson(
      response.data ?? const {},
      ProfessionalRecord.fromJson,
    );
  }

  Future<ProfessionalRecord> setProfessionalVerified(
    int id,
    bool isVerified,
  ) async {
    final response = await api.request<Json>(
      () => api.dio.put<Json>(
        '/api/admin/professionals/$id/verification',
        data: {'isVerified': isVerified},
      ),
    );
    return ProfessionalRecord.fromJson(response.data ?? const {});
  }

  Future<PagedResult<JobRecord>> getJobs({int page = 1}) async {
    final response = await api.request<Json>(
      () => api.dio.get<Json>(
        '/api/jobs',
        queryParameters: {'page': page, 'pageSize': 20},
      ),
    );
    return PagedResult.fromJson(response.data ?? const {}, JobRecord.fromJson);
  }

  Future<PagedResult<ReservationRecord>> getReservations({int page = 1}) async {
    final response = await api.request<Json>(
      () => api.dio.get<Json>(
        '/api/admin/reservations',
        queryParameters: {'page': page, 'pageSize': 20},
      ),
    );
    return PagedResult.fromJson(
      response.data ?? const {},
      ReservationRecord.fromJson,
    );
  }

  Future<ReservationRecord> setReservationStatus(
    int id,
    int status, {
    String? reason,
  }) async {
    final response = await api.request<Json>(
      () => api.dio.put<Json>(
        '/api/admin/reservations/$id/status',
        data: {'status': status, 'cancellationReason': reason},
      ),
    );
    return ReservationRecord.fromJson(response.data ?? const {});
  }

  Future<PagedResult<AuditEntry>> getAuditLogs({
    int page = 1,
    String? action,
    String? entityType,
    DateTime? from,
    DateTime? to,
  }) async {
    final response = await api.request<Json>(
      () => api.dio.get<Json>(
        '/api/admin/audit-logs',
        queryParameters: {
          'page': page,
          'pageSize': 20,
          if (action != null && action.isNotEmpty) 'action': action,
          if (entityType != null && entityType.isNotEmpty)
            'entityType': entityType,
          if (from != null) 'dateFrom': from.toUtc().toIso8601String(),
          if (to != null) 'dateTo': to.toUtc().toIso8601String(),
        },
      ),
    );
    return PagedResult.fromJson(response.data ?? const {}, AuditEntry.fromJson);
  }

  Future<FinancialReport> getFinancialReport({
    int page = 1,
    int pageSize = 20,
    DateTime? from,
    DateTime? to,
    int? categoryId,
  }) async {
    final response = await api.request<Json>(
      () => api.dio.get<Json>(
        '/api/reports/financial',
        queryParameters: _reportParameters(
          page: page,
          pageSize: pageSize,
          from: from,
          to: to,
          categoryId: categoryId,
        ),
      ),
    );
    return FinancialReport.fromJson(response.data ?? const {});
  }

  Future<Uint8List> downloadFinancialReport({
    DateTime? from,
    DateTime? to,
    int? categoryId,
  }) async {
    final response = await api.request<List<int>>(
      () => api.dio.get<List<int>>(
        '/api/reports/financial/pdf',
        queryParameters: _reportParameters(
          from: from,
          to: to,
          categoryId: categoryId,
        ),
        options: Options(
          responseType: ResponseType.bytes,
          followRedirects: true,
          maxRedirects: 3,
        ),
      ),
    );
    return Uint8List.fromList(response.data ?? const []);
  }

  Future<PagedResult<NotificationItem>> getNotifications() async {
    final response = await api.request<Json>(
      () => api.dio.get<Json>(
        '/api/notifications',
        queryParameters: const {'page': 1, 'pageSize': 50},
      ),
    );
    return PagedResult.fromJson(
      response.data ?? const {},
      NotificationItem.fromJson,
    );
  }

  Future<NotificationItem> markNotificationRead(int id) async {
    final response = await api.request<Json>(
      () => api.dio.put<Json>('/api/notifications/$id/read'),
    );
    return NotificationItem.fromJson(response.data ?? const {});
  }

  Future<void> markAllNotificationsRead() async {
    await api.request<void>(
      () => api.dio.put<void>('/api/notifications/read-all'),
    );
  }

  Map<String, dynamic> _reportParameters({
    int? page,
    int? pageSize,
    DateTime? from,
    DateTime? to,
    int? categoryId,
  }) => {
    'page': ?page,
    'pageSize': ?pageSize,
    if (from != null) 'from': _dateOnly(from),
    if (to != null) 'to': _dateOnly(to),
    'categoryId': ?categoryId,
  };

  String _dateOnly(DateTime value) =>
      '${value.year.toString().padLeft(4, '0')}-${value.month.toString().padLeft(2, '0')}-${value.day.toString().padLeft(2, '0')}';
}
