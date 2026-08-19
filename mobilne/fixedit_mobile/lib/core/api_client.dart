import 'package:dio/dio.dart';

class ApiException implements Exception {
  const ApiException(this.message, {this.statusCode});
  final String message;
  final int? statusCode;
  factory ApiException.fromDio(DioException e) {
    final data = e.response?.data;
    var message = 'Zahtjev nije uspio. Provjerite internet vezu.';
    if (data is Map && data['message'] is String) {
      message = data['message'] as String;
    }
    if (e.type == DioExceptionType.connectionError) {
      message = 'API servis nije dostupan.';
    }
    if (e.type == DioExceptionType.connectionTimeout) {
      message = 'API nije odgovorio na vrijeme.';
    }
    return ApiException(message, statusCode: e.response?.statusCode);
  }
  @override
  String toString() => message;
}

String userFacingError(
  Object error, {
  String fallback = 'Operacija nije uspjela. Pokušajte ponovo.',
}) => error is ApiException ? error.message : fallback;

class ApiClient {
  ApiClient({
    required this.tokenProvider,
    required this.refreshAccessToken,
    required this.onSessionExpired,
  }) : dio = Dio(
         BaseOptions(
           baseUrl: apiBaseUrl,
           connectTimeout: const Duration(seconds: 12),
           receiveTimeout: const Duration(seconds: 30),
           followRedirects: true,
           maxRedirects: 3,
           validateStatus: (s) => s != null && s >= 200 && s < 300,
           headers: const {'Accept': 'application/json'},
         ),
       ) {
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          final token = tokenProvider();
          if (token != null) options.headers['Authorization'] = 'Bearer $token';
          handler.next(options);
        },
        onError: (error, handler) async {
          final canRefresh =
              error.response?.statusCode == 401 &&
              error.requestOptions.extra['skipRefresh'] != true &&
              error.requestOptions.extra['retried'] != true;
          if (canRefresh) {
            final token = await refreshAccessToken();
            if (token != null) {
              try {
                final request = error.requestOptions;
                request.extra['retried'] = true;
                request.headers['Authorization'] = 'Bearer $token';
                return handler.resolve(await dio.fetch(request));
              } on DioException catch (retryError) {
                return handler.next(retryError);
              }
            }
            onSessionExpired();
          }
          handler.next(error);
        },
      ),
    );
  }

  static const apiBaseUrl = String.fromEnvironment('API_BASE_URL');
  static String get normalizedBaseUrl => apiBaseUrl.endsWith('/')
      ? apiBaseUrl.substring(0, apiBaseUrl.length - 1)
      : apiBaseUrl;
  final String? Function() tokenProvider;
  final Future<String?> Function() refreshAccessToken;
  final void Function() onSessionExpired;
  final Dio dio;

  static void ensureConfigured() {
    final uri = Uri.tryParse(apiBaseUrl);
    if (uri == null || !uri.hasScheme || !uri.hasAuthority) {
      throw const ApiException('API_BASE_URL nije ispravno postavljen.');
    }
  }

  Future<Response<T>> call<T>(Future<Response<T>> Function() operation) async {
    ensureConfigured();
    try {
      return await operation();
    } on DioException catch (e) {
      throw ApiException.fromDio(e);
    }
  }
}
