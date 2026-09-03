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
  List<NotificationItem> items = const [];
  bool loading = false;
  bool connected = false;
  String? error;

  Future<void> start() async {
    if (_connection != null) return;
    loading = true;
    error = null;
    notifyListeners();
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
      items = (await repository.getNotifications()).items;
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
      connection.onreconnected(({connectionId}) {
        connected = true;
        notifyListeners();
      });
      connection.onclose(({error}) {
        connected = false;
        this.error = 'Veza za obavijesti je prekinuta.';
        notifyListeners();
      });
      _connection = connection;
      await connection.start();
      connected = true;
    } catch (e) {
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

  Future<void> _receive(List<Object?>? args) async {
    if (args == null || args.isEmpty || args.first is! Map) return;
    final item = NotificationItem.fromJson(
      Map<String, dynamic>.from(args.first! as Map),
    );
    items = [item, ...items.where((e) => e.id != item.id)];
    notifyListeners();
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
  }

  Future<void> markRead(NotificationItem item) async {
    await repository.markNotificationRead(item.id);
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

  Future<void> start() async {
    if (_connection != null) return;
    try {
      final history = await repository.getMessages(conversationId);
      messages = history.items.reversed.toList();
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
