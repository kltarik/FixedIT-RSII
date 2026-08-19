import 'package:flutter/material.dart';

import '../services/realtime_service.dart';
import '../widgets/common.dart';

class NotificationsScreen extends StatelessWidget {
  const NotificationsScreen({super.key, required this.service});
  final RealtimeNotifications service;

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: service,
      builder: (context, _) => Scaffold(
        appBar: AppBar(
          title: const Text('Obavijesti'),
          actions: [
            TextButton(
              onPressed: service.items.isEmpty ? null : service.markAllRead,
              child: const Text('Pročitaj sve'),
            ),
          ],
        ),
        body: service.loading
            ? const LoadingView()
            : service.items.isEmpty
            ? EmptyView(
                service.error ?? 'Nema nepročitanih obavijesti.',
                icon: Icons.notifications_none,
              )
            : ListView.separated(
                padding: const EdgeInsets.all(14),
                itemCount: service.items.length,
                separatorBuilder: (_, _) => const Divider(),
                itemBuilder: (_, index) {
                  final item = service.items[index];
                  return ListTile(
                    onTap: () => service.markRead(item),
                    leading: const CircleAvatar(
                      child: Icon(Icons.notifications_outlined),
                    ),
                    title: Text(item.title),
                    subtitle: Text(
                      '${item.body}\n${dateTime.format(item.createdAt)}',
                    ),
                    isThreeLine: true,
                  );
                },
              ),
        floatingActionButton: service.connected
            ? null
            : FloatingActionButton.extended(
                onPressed: service.retry,
                icon: const Icon(Icons.refresh),
                label: const Text('Poveži'),
              ),
      ),
    );
  }
}
