import 'package:fixedit_mobile/core/api_client.dart';
import 'package:fixedit_mobile/core/models.dart';
import 'package:fixedit_mobile/services/mobile_repository.dart';
import 'package:fixedit_mobile/services/realtime_service.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('badge broji samo nepročitane obavijesti', () {
    final api = ApiClient(
      tokenProvider: () => null,
      refreshAccessToken: () async => null,
      onSessionExpired: () {},
    );
    final service = RealtimeNotifications(MobileRepository(api), () => null);
    final createdAt = DateTime.utc(2026, 9, 7, 10);
    service.items = [
      NotificationItem(1, 'Nova', 'Nepročitana', false, createdAt, 1),
      NotificationItem(2, 'Stara', 'Pročitana', true, createdAt, 1),
    ];

    expect(service.items.length, 2);
    expect(service.unreadCount, 1);
  });
}
