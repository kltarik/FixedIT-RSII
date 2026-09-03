import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

import '../core/api_client.dart';
import '../core/models.dart';

class AuthService extends ChangeNotifier {
  AuthService() {
    api = ApiClient(
      tokenProvider: () => token,
      refreshAccessToken: _refresh,
      onSessionExpired: _expire,
    );
  }
  late final ApiClient api;
  AuthSession? _session;
  Future<String?>? _refreshing;
  bool loading = false;
  String? error;

  AuthUser? get user => _session?.user;
  String? get token => _session?.token;
  bool get authenticated => _session != null;

  Future<bool> login(String email, String password) => _authenticate(
    '/api/auth/login',
    {'email': email.trim(), 'password': password},
  );
  Future<bool> register({
    required String email,
    required String password,
    required String firstName,
    required String lastName,
    required String role,
    required int cityId,
  }) => _authenticate('/api/auth/register', {
    'email': email.trim(),
    'password': password,
    'firstName': firstName.trim(),
    'lastName': lastName.trim(),
    'role': role,
    'cityId': cityId,
  });

  Future<String> requestPasswordReset(String email) async {
    final response = await api.call<Json>(
      () => api.dio.post<Json>(
        '/api/auth/forgot-password',
        data: {'email': email.trim()},
        options: Options(extra: const {'skipRefresh': true}),
      ),
    );
    return jString(
      response.data ?? const {},
      'message',
      'Ako nalog postoji, kod je poslan na email.',
    );
  }

  Future<void> resetPassword({
    required String email,
    required String code,
    required String newPassword,
  }) async {
    await api.call<void>(
      () => api.dio.post<void>(
        '/api/auth/reset-password',
        data: {
          'email': email.trim(),
          'code': code.trim(),
          'newPassword': newPassword,
        },
        options: Options(extra: const {'skipRefresh': true}),
      ),
    );
  }

  Future<bool> _authenticate(String path, Json body) async {
    loading = true;
    error = null;
    notifyListeners();
    try {
      final response = await api.call<Json>(
        () => api.dio.post<Json>(
          path,
          data: body,
          options: Options(extra: const {'skipRefresh': true}),
        ),
      );
      _session = AuthSession.fromJson(response.data ?? const {});
      return true;
    } on ApiException catch (e) {
      error = e.message;
      return false;
    } finally {
      loading = false;
      notifyListeners();
    }
  }

  Future<String?> _refresh() {
    final activeRefresh = _refreshing;
    if (activeRefresh != null) return activeRefresh;

    final refresh = _performRefresh();
    _refreshing = refresh;
    return refresh.whenComplete(() {
      if (identical(_refreshing, refresh)) {
        _refreshing = null;
      }
    });
  }

  Future<String?> _performRefresh() async {
    final current = _session;
    if (current == null) return null;
    try {
      final response = await api.dio.post<Json>(
        '/api/auth/refresh',
        options: Options(
          headers: {'X-Refresh-Token': current.refreshToken},
          extra: const {'skipRefresh': true},
        ),
      );
      _session = AuthSession.fromJson(response.data ?? const {});
      notifyListeners();
      return _session!.token;
    } on Object {
      return null;
    }
  }

  Future<void> logout() async {
    try {
      if (_session != null) {
        await api.call<void>(() => api.dio.post<void>('/api/auth/logout'));
      }
    } on ApiException {
      // Local session state must still be cleared if server logout is unavailable.
    }
    _session = null;
    error = null;
    notifyListeners();
  }

  void _expire() {
    if (_session == null) return;
    _session = null;
    error = 'Sesija je istekla. Prijavite se ponovo.';
    notifyListeners();
  }
}
