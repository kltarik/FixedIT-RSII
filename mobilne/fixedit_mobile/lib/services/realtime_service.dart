import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:signalr_netcore/signalr_client.dart';

import '../core/api_client.dart';
import '../core/models.dart';
import 'mobile_repository.dart';

class RealtimeNotifications extends ChangeNotifier {
  RealtimeNotifications(this.repository, this.tokenProvider);
  final MobileRepository repository;
  final String? Function() tokenProvider;
  final _local = FlutterLocalNotificationsPlugin();
  HubConnection? _connection;
  bool _localNotificationsReady = false;
  List<NotificationItem> items = const [];
  bool loading = false;
  bool connected = false;
  String? error;
  int? _serverUnreadCount;
  int get unreadCount =>
      _serverUnreadCount ?? items.where((item) => !item.isRead).length;
  int _page = 1;
  int _pageCount = 1;
  bool loadingMore = false;
  bool get canLoadMore => _page < _pageCount;

  Future<void> start() async {
    if (_connection != null) return;
    loading = true;
    error = null;
    notifyListeners();
    try {
      await _initializeLocalNotifications();
      await _reloadPersistedNotifications();
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
      connection.on('ReceiveNotification', _receive);
      connection.onreconnecting(({error}) {
        connected = false;
        notifyListeners();
      });
      connection.onreconnected(({connectionId}) async {
        connected = true;
        await _reloadPersistedNotifications();
        notifyListeners();
      });
      connection.onclose(({error}) {
        connected = false;
        this.error = 'Veza za obavijesti je prekinuta.';
        notifyListeners();
      });
      _connection = connection;
      await _startConnection(connection);
      connected = true;
    } catch (e) {
      debugPrint('SignalR povezivanje za obavijesti nije uspjelo: $e');
      error = userFacingError(
        e,
        fallback: 'Povezivanje s obavijestima nije uspjelo.',
      );
      _connection = null;
    } finally {
      loading = false;
      notifyListeners();
    }
  }

  Future<void> _reloadPersistedNotifications() async {
    try {
      final results = await Future.wait<Object>([
        repository.getNotifications(),
        repository.getUnreadNotificationCount(),
      ]);
      final page = results[0] as Paged<NotificationItem>;
      items = page.items;
      _page = page.page;
      _pageCount = page.pageCount;
      _serverUnreadCount = results[1] as int;
      error = null;
    } catch (exception) {
      error = userFacingError(
        exception,
        fallback: 'Propuštene obavijesti trenutno nije moguće učitati.',
      );
    }
  }

  Future<void> loadMore() async {
    if (loadingMore || !canLoadMore) return;
    loadingMore = true;
    notifyListeners();
    try {
      final result = await repository.getNotifications(page: _page + 1);
      final ids = items.map((item) => item.id).toSet();
      items = [
        ...items,
        ...result.items.where((item) => !ids.contains(item.id)),
      ];
      _page = result.page;
      _pageCount = result.pageCount;
    } catch (exception) {
      error = userFacingError(exception);
    } finally {
      loadingMore = false;
      notifyListeners();
    }
  }

  Future<void> _initializeLocalNotifications() async {
    try {
      await _local.initialize(
        const InitializationSettings(
          android: AndroidInitializationSettings('ic_notification'),
        ),
      );
      await _local
          .resolvePlatformSpecificImplementation<
            AndroidFlutterLocalNotificationsPlugin
          >()
          ?.requestNotificationsPermission();
      _localNotificationsReady = true;
    } catch (exception) {
      _localNotificationsReady = false;
      debugPrint('Lokalne Android obavijesti nisu dostupne: $exception');
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

  Future<void> _receive(List<Object?>? args) async {
    if (args == null || args.isEmpty || args.first is! Map) return;
    final item = NotificationItem.fromJson(
      Map<String, dynamic>.from(args.first! as Map),
    );
    NotificationItem? previous;
    for (final current in items) {
      if (current.id == item.id) {
        previous = current;
        break;
      }
    }
    final previousUnreadCount = unreadCount;
    items = [item, ...items.where((e) => e.id != item.id)];
    if (!item.isRead && (previous == null || previous.isRead)) {
      _serverUnreadCount = previousUnreadCount + 1;
    }
    notifyListeners();
    if (_localNotificationsReady) {
      try {
        await _local.show(
          item.id,
          item.title,
          item.body,
          const NotificationDetails(
            android: AndroidNotificationDetails(
              'fixedit_updates',
              'FixedIT obavijesti',
              channelDescription: 'Rezervacije, poruke i plaćanja',
              importance: Importance.high,
              priority: Priority.high,
            ),
          ),
        );
      } catch (exception) {
        debugPrint('Lokalna obavijest nije prikazana: $exception');
      }
    }
  }

  Future<void> markRead(NotificationItem item) async {
    await repository.markNotificationRead(item.id);
    if (!item.isRead && unreadCount > 0) {
      _serverUnreadCount = unreadCount - 1;
    }
    items = items
        .map(
          (current) => current.id == item.id
              ? NotificationItem(
                  current.id,
                  current.title,
                  current.body,
                  true,
                  current.createdAt,
                  current.type,
                )
              : current,
        )
        .toList();
    notifyListeners();
  }

  Future<void> markAllRead() async {
    await repository.markAllNotificationsRead();
    items = items
        .map(
          (item) => NotificationItem(
            item.id,
            item.title,
            item.body,
            true,
            item.createdAt,
            item.type,
          ),
        )
        .toList();
    _serverUnreadCount = 0;
    notifyListeners();
  }

  Future<void> retry() async {
    await stop();
    await start();
  }

  Future<void> stop() async {
    final c = _connection;
    _connection = null;
    c?.off('ReceiveNotification');
    await c?.stop();
    connected = false;
    items = const [];
    _serverUnreadCount = null;
    _page = 1;
    _pageCount = 1;
    _localNotificationsReady = false;
    notifyListeners();
  }

  @override
  void dispose() {
    final c = _connection;
    _connection = null;
    c?.off('ReceiveNotification');
    c?.stop();
    super.dispose();
  }
}

class ChatSession extends ChangeNotifier {
  ChatSession(
    this.repository,
    this.tokenProvider,
    this.conversationId,
    this.currentUserId,
  );
  final MobileRepository repository;
  final String? Function() tokenProvider;
  final int conversationId;
  final String currentUserId;
  HubConnection? _connection;
  List<ChatMessage> messages = const [];
  bool loading = true;
  bool connected = false;
  String? error;
  int _page = 1;
  int _pageCount = 1;
  bool loadingOlder = false;
  bool get canLoadOlder => _page < _pageCount;

  Future<void> start() async {
    if (_connection != null) return;
    try {
      final history = await repository.getMessages(conversationId);
      messages = history.items.reversed.toList();
      _page = history.page;
      _pageCount = history.pageCount;
      final c = HubConnectionBuilder()
          .withUrl(
            '${ApiClient.normalizedBaseUrl}/hubs/chat',
            options: HttpConnectionOptions(
              accessTokenFactory: () async => tokenProvider() ?? '',
            ),
          )
          .withAutomaticReconnect()
          .build();
      c.on('ReceiveMessage', _receive);
      c.onreconnected(({connectionId}) async {
        connected = true;
        await c.invoke('JoinConversation', args: [conversationId]);
        await _reloadLatestMessages();
        notifyListeners();
      });
      c.onreconnecting(({error}) {
        connected = false;
        notifyListeners();
      });
      c.onclose(({error}) {
        connected = false;
        this.error = 'Veza za razgovor je prekinuta.';
        notifyListeners();
      });
      _connection = c;
      await c.start();
      await c.invoke('JoinConversation', args: [conversationId]);
      connected = true;
    } catch (e) {
      error = userFacingError(
        e,
        fallback: 'Povezivanje s razgovorom nije uspjelo.',
      );
      await _stopConnection();
    } finally {
      loading = false;
      notifyListeners();
    }
  }

  Future<void> _reloadLatestMessages() async {
    final latest = await repository.getMessages(conversationId);
    _mergeMessages(latest.items);
    _pageCount = latest.pageCount;
  }

  Future<void> loadOlder() async {
    if (loadingOlder || !canLoadOlder) return;
    loadingOlder = true;
    notifyListeners();
    try {
      final history = await repository.getMessages(
        conversationId,
        page: _page + 1,
      );
      _mergeMessages(history.items);
      _page = history.page;
      _pageCount = history.pageCount;
    } catch (exception) {
      error = userFacingError(exception);
    } finally {
      loadingOlder = false;
      notifyListeners();
    }
  }

  void _mergeMessages(Iterable<ChatMessage> incoming) {
    final byId = {for (final message in messages) message.id: message};
    for (final message in incoming) {
      byId[message.id] = message;
    }
    messages = byId.values.toList()
      ..sort((left, right) {
        final byTime = left.sentAt.compareTo(right.sentAt);
        return byTime != 0 ? byTime : left.id.compareTo(right.id);
      });
  }

  Future<void> retry() async {
    loading = true;
    error = null;
    notifyListeners();
    await _stopConnection();
    await start();
  }

  void _receive(List<Object?>? args) {
    if (args == null || args.isEmpty || args.first is! Map) return;
    final message = ChatMessage.fromJson(
      Map<String, dynamic>.from(args.first! as Map),
    );
    if (!messages.any((e) => e.id == message.id)) {
      messages = [...messages, message];
    }
    notifyListeners();
  }

  Future<void> send(String content) async {
    if (content.trim().isEmpty || _connection == null) return;
    try {
      await _connection!.invoke(
        'SendMessage',
        args: [conversationId, content.trim()],
      );
    } catch (e) {
      error = userFacingError(e);
      notifyListeners();
    }
  }

  @override
  void dispose() {
    final c = _connection;
    _connection = null;
    c?.off('ReceiveMessage');
    c?.stop();
    super.dispose();
  }

  Future<void> _stopConnection() async {
    final c = _connection;
    _connection = null;
    c?.off('ReceiveMessage');
    await c?.stop();
    connected = false;
  }
}
