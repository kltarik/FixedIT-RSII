import 'package:flutter/foundation.dart';

import '../core/api_client.dart';
import '../core/models.dart';

class AuthService extends ChangeNotifier {
  AuthService() {
    api = ApiClient(tokenProvider: () => token, onUnauthorized: expireSession);
  }

  late final ApiClient api;
  AuthSession? _session;
  bool _isLoading = false;
  String? _error;

  String? get token => _session?.token;
  AuthUser? get user => _session?.user;
  bool get isAuthenticated => _session != null;
  bool get isLoading => _isLoading;
  String? get error => _error;

  Future<bool> login(String email, String password) async {
    _isLoading = true;
    _error = null;
    notifyListeners();
    try {
      final response = await api.request<Json>(
        () => api.dio.post<Json>(
          '/api/auth/login',
          data: {'email': email.trim(), 'password': password},
        ),
      );
      final session = AuthSession.fromJson(response.data ?? const {});
      if (!session.user.roles.contains('Admin')) {
        _error = 'Desktop aplikacija je dostupna samo administratorima.';
        return false;
      }
      _session = session;
      return true;
    } on ApiException catch (exception) {
      _error = exception.message;
      return false;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> logout() async {
    try {
      if (_session != null) {
        await api.request<void>(() => api.dio.post<void>('/api/auth/logout'));
      }
    } on ApiException {
      // Local session must be cleared even if the server is unavailable.
    } finally {
      _session = null;
      _error = null;
      notifyListeners();
    }
  }

  void expireSession() {
    if (_session == null) return;
    _session = null;
    _error = 'Sesija je istekla. Prijavite se ponovo.';
    notifyListeners();
  }
}
