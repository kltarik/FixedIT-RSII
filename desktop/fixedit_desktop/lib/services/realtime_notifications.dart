import 'package:flutter/foundation.dart';
import 'package:signalr_netcore/signalr_client.dart';

import '../core/api_client.dart';
import '../core/models.dart';
import 'admin_repository.dart';

enum RealtimeStatus { disconnected, connecting, connected, reconnecting, error }

class RealtimeNotifications extends ChangeNotifier {
  RealtimeNotifications({
    required this.repository,
    required this.tokenProvider,
  });

  final AdminRepository repository;
  final String? Function() tokenProvider;
  HubConnection? _connection;
  List<NotificationItem> _items = const [];
  RealtimeStatus _status = RealtimeStatus.disconnected;
  String? _error;
  bool _loading = false;

  List<NotificationItem> get items => _items;
  RealtimeStatus get status => _status;
  String? get error => _error;
  bool get loading => _loading;
  int get unreadCount => _items.where((item) => !item.isRead).length;

  Future<void> start() async {
    if (_connection != null) return;
    _loading = true;
    _error = null;
    _status = RealtimeStatus.connecting;
    notifyListeners();
    try {
      final initial = await repository.getNotifications();
      _items = initial.items;
      ApiClient.ensureConfigured();
      final connection = HubConnectionBuilder()
          .withUrl(
            '${ApiClient.normalizedBaseUrl}/hubs/notifications',
            options: HttpConnectionOptions(
              accessTokenFactory: () async => tokenProvider() ?? '',
            ),
          )
          .withAutomaticReconnect(
            retryDelays: [
              0,
              const Duration(seconds: 2).inMilliseconds,
              const Duration(seconds: 5).inMilliseconds,
              const Duration(seconds: 10).inMilliseconds,
            ],
          )
          .build();
      connection.on('ReceiveNotification', _handleNotification);
      connection.onreconnecting(({error}) {
        _status = RealtimeStatus.reconnecting;
        notifyListeners();
      });
      connection.onreconnected(({connectionId}) {
        _status = RealtimeStatus.connected;
        notifyListeners();
      });
      connection.onclose(({error}) {
        if (_connection != null) {
          _status = RealtimeStatus.disconnected;
          _error = 'Veza za obavijesti je prekinuta.';
          notifyListeners();
        }
      });
      _connection = connection;
      await _startConnection(connection);
      _status = RealtimeStatus.connected;
    } catch (exception) {
      final failedConnection = _connection;
      _connection = null;
      failedConnection?.off('ReceiveNotification');
      await failedConnection?.stop();
      _status = RealtimeStatus.error;
      _error = userFacingError(
        exception,
        fallback: 'Povezivanje s obavijestima nije uspjelo.',
      );
    } finally {
      _loading = false;
      notifyListeners();
    }
  }

  Future<void> _startConnection(HubConnection connection) async {
    Object? lastError;
    for (final delay in const [
      Duration.zero,
      Duration(seconds: 2),
      Duration(seconds: 5),
    ]) {
      if (delay != Duration.zero) {
        await Future<void>.delayed(delay);
      }
      try {
        await connection.start();
        return;
      } catch (exception) {
        lastError = exception;
        debugPrint('SignalR pokušaj povezivanja nije uspio: $exception');
      }
    }

    throw StateError('SignalR povezivanje nije uspjelo: $lastError');
  }

  Future<void> retry() async {
    await stop();
    await start();
  }

  Future<void> markRead(NotificationItem item) async {
    try {
      final updated = await repository.markNotificationRead(item.id);
      _items = _items
          .map((current) => current.id == updated.id ? updated : current)
          .toList();
      _error = null;
    } catch (exception) {
      _error = userFacingError(exception);
    }
    notifyListeners();
  }

  Future<void> markAllRead() async {
    try {
      await repository.markAllNotificationsRead();
      _items = _items
          .map(
            (item) => NotificationItem(
              id: item.id,
              title: item.title,
              body: item.body,
              isRead: true,
              createdAt: item.createdAt,
              type: item.type,
            ),
          )
          .toList();
      _error = null;
    } catch (exception) {
      _error = userFacingError(exception);
    }
    notifyListeners();
  }

  void _handleNotification(List<Object?>? arguments) {
    if (arguments == null || arguments.isEmpty || arguments.first is! Map) {
      return;
    }
    final notification = NotificationItem.fromJson(
      Map<String, dynamic>.from(arguments.first! as Map),
    );
    _items = [
      notification,
      ..._items.where((item) => item.id != notification.id),
    ];
    notifyListeners();
  }

  Future<void> stop() async {
    final connection = _connection;
    _connection = null;
    if (connection != null) {
      connection.off('ReceiveNotification');
      await connection.stop();
    }
    _items = const [];
    _status = RealtimeStatus.disconnected;
    _error = null;
    notifyListeners();
  }

  @override
  void dispose() {
    final connection = _connection;
    _connection = null;
    connection?.off('ReceiveNotification');
    connection?.stop();
    super.dispose();
  }
}
