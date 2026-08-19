import 'package:dio/dio.dart';

class ApiException implements Exception {
  const ApiException(this.message, {this.statusCode});

  final String message;
  final int? statusCode;

  factory ApiException.fromDio(DioException exception) {
    final data = exception.response?.data;
    var message = 'API zahtjev nije uspio. Provjerite vezu i pokušajte ponovo.';
    if (data is Map && data['message'] is String) {
      message = data['message'] as String;
    } else if (exception.type == DioExceptionType.connectionTimeout ||
        exception.type == DioExceptionType.receiveTimeout) {
      message = 'API nije odgovorio na vrijeme.';
    } else if (exception.type == DioExceptionType.connectionError) {
      message = 'Nije moguće uspostaviti vezu sa API servisom.';
    } else if (exception.response?.statusCode case final int status) {
      message = 'API je vratio HTTP $status.';
    }
    return ApiException(message, statusCode: exception.response?.statusCode);
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
    required String? Function() tokenProvider,
    VoidCallback? onUnauthorized,
  }) : dio = Dio(
         BaseOptions(
           baseUrl: apiBaseUrl,
           connectTimeout: const Duration(seconds: 10),
           receiveTimeout: const Duration(seconds: 30),
           followRedirects: true,
           maxRedirects: 3,
           validateStatus: (status) =>
               status != null && status >= 200 && status < 300,
           headers: const {'Accept': 'application/json'},
         ),
       ) {
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          final token = tokenProvider();
          if (token != null && token.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          handler.next(options);
        },
        onError: (error, handler) {
          if (error.response?.statusCode == 401) {
            onUnauthorized?.call();
          }
          handler.next(error);
        },
      ),
    );
  }

  static const String apiBaseUrl = String.fromEnvironment('API_BASE_URL');
  static String get normalizedBaseUrl => apiBaseUrl.endsWith('/')
      ? apiBaseUrl.substring(0, apiBaseUrl.length - 1)
      : apiBaseUrl;
  final Dio dio;

  static void ensureConfigured() {
    if (apiBaseUrl.trim().isEmpty) {
      throw const ApiException(
        'API_BASE_URL nije postavljen. Pokrenite aplikaciju sa --dart-define=API_BASE_URL=...',
      );
    }
    final uri = Uri.tryParse(apiBaseUrl);
    if (uri == null || !uri.hasScheme || !uri.hasAuthority) {
      throw const ApiException(
        'API_BASE_URL mora biti ispravan apsolutni URL.',
      );
    }
  }

  Future<Response<T>> request<T>(
    Future<Response<T>> Function() operation,
  ) async {
    ensureConfigured();
    try {
      return await operation();
    } on DioException catch (error) {
      throw ApiException.fromDio(error);
    }
  }
}

typedef VoidCallback = void Function();
