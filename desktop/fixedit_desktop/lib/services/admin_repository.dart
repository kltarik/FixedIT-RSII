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

  Future<PagedResult<CountryRecord>> getCountries({
    int page = 1,
    String? search,
  }) async {
    final response = await api.request<Json>(
      () => api.dio.get<Json>(
        '/api/admin/reference-data/countries',
        queryParameters: {
          'page': page,
          'pageSize': 20,
          if (search?.isNotEmpty ?? false) 'search': search,
        },
      ),
    );
    return PagedResult.fromJson(
      response.data ?? const {},
      CountryRecord.fromJson,
    );
  }

  Future<CountryRecord> saveCountry({
    int? id,
    required String name,
    required String code,
  }) async {
    final data = {'name': name, 'code': code};
    final response = await api.request<Json>(
      () => id == null
          ? api.dio.post<Json>(
              '/api/admin/reference-data/countries',
              data: data,
            )
          : api.dio.put<Json>(
              '/api/admin/reference-data/countries/$id',
              data: data,
            ),
    );
    return CountryRecord.fromJson(response.data ?? const {});
  }

  Future<void> deleteCountry(int id) => api.request<void>(
    () => api.dio.delete<void>('/api/admin/reference-data/countries/$id'),
  );

  Future<PagedResult<CityRecord>> getCities({
    int page = 1,
    String? search,
  }) async {
    final response = await api.request<Json>(
      () => api.dio.get<Json>(
        '/api/admin/reference-data/cities',
        queryParameters: {
          'page': page,
          'pageSize': 20,
          if (search?.isNotEmpty ?? false) 'search': search,
        },
      ),
    );
    return PagedResult.fromJson(response.data ?? const {}, CityRecord.fromJson);
  }

  Future<CityRecord> saveCity({
    int? id,
    required String name,
    required int countryId,
  }) async {
    final data = {'name': name, 'countryId': countryId};
    final response = await api.request<Json>(
      () => id == null
          ? api.dio.post<Json>('/api/admin/reference-data/cities', data: data)
          : api.dio.put<Json>(
              '/api/admin/reference-data/cities/$id',
              data: data,
            ),
    );
    return CityRecord.fromJson(response.data ?? const {});
  }

  Future<void> deleteCity(int id) => api.request<void>(
    () => api.dio.delete<void>('/api/admin/reference-data/cities/$id'),
  );

  Future<PagedResult<CategoryRecord>> getCategories({
    int page = 1,
    String? search,
  }) async {
    final response = await api.request<Json>(
      () => api.dio.get<Json>(
        '/api/admin/reference-data/categories',
        queryParameters: {
          'page': page,
          'pageSize': 20,
          if (search?.isNotEmpty ?? false) 'search': search,
        },
      ),
    );
    return PagedResult.fromJson(
      response.data ?? const {},
      CategoryRecord.fromJson,
    );
  }

  Future<CategoryRecord> saveCategory({
    int? id,
    required String name,
    required String description,
    String? iconUrl,
  }) async {
    final data = {
      'name': name,
      'description': description,
      'iconUrl': iconUrl?.isEmpty ?? true ? null : iconUrl,
    };
    final response = await api.request<Json>(
      () => id == null
          ? api.dio.post<Json>(
              '/api/admin/reference-data/categories',
              data: data,
            )
          : api.dio.put<Json>(
              '/api/admin/reference-data/categories/$id',
              data: data,
            ),
    );
    return CategoryRecord.fromJson(response.data ?? const {});
  }

  Future<void> deleteCategory(int id) => api.request<void>(
    () => api.dio.delete<void>('/api/admin/reference-data/categories/$id'),
  );

  Future<List<ReservationStatusRecord>> getReservationStatuses() async {
    final response = await api.request<List<dynamic>>(
      () => api.dio.get<List<dynamic>>(
        '/api/admin/reference-data/reservation-statuses',
      ),
    );
    return (response.data ?? const [])
        .whereType<Map>()
        .map(
          (item) =>
              ReservationStatusRecord.fromJson(Map<String, dynamic>.from(item)),
        )
        .toList();
  }

  Future<ReservationStatusRecord> updateReservationStatus({
    required int id,
    required String name,
    required String description,
  }) async {
    final response = await api.request<Json>(
      () => api.dio.put<Json>(
        '/api/admin/reference-data/reservation-statuses/$id',
        data: {'name': name, 'description': description},
      ),
    );
    return ReservationStatusRecord.fromJson(response.data ?? const {});
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

  Future<AdminUserRecord> updateUser({
    required String id,
    required String firstName,
    required String lastName,
    String? phoneNumber,
    required int cityId,
  }) async {
    final response = await api.request<Json>(
      () => api.dio.put<Json>(
        '/api/admin/users/$id',
        data: {
          'firstName': firstName,
          'lastName': lastName,
          'phoneNumber': phoneNumber?.isEmpty ?? true ? null : phoneNumber,
          'cityId': cityId,
        },
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

  Future<ProfessionalRecord> updateProfessional({
    required int id,
    required String bio,
    required double hourlyRate,
    required int yearsOfExperience,
    required List<int> categoryIds,
  }) async {
    final response = await api.request<Json>(
      () => api.dio.put<Json>(
        '/api/admin/professionals/$id',
        data: {
          'bio': bio,
          'hourlyRate': hourlyRate,
          'yearsOfExperience': yearsOfExperience,
          'categoryIds': categoryIds,
        },
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
