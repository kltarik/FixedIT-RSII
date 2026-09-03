import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../app/theme.dart';
import '../services/admin_repository.dart';
import '../services/auth_service.dart';
import '../services/realtime_notifications.dart';
import 'audit_logs_screen.dart';
import 'dashboard_screen.dart';
import 'jobs_screen.dart';
import 'professionals_screen.dart';
import 'reference_data_screen.dart';
import 'reports_screen.dart';
import 'reviews_screen.dart';
import 'reservations_screen.dart';
import 'users_screen.dart';

class AdminShell extends StatefulWidget {
  const AdminShell({super.key});

  @override
  State<AdminShell> createState() => _AdminShellState();
}

class _AdminShellState extends State<AdminShell> {
  int _selectedIndex = 0;
  late final AdminRepository _repository;
  late final RealtimeNotifications _notifications;

  static const _destinations = [
    (Icons.space_dashboard_outlined, 'Pregled'),
    (Icons.people_outline, 'Korisnici'),
    (Icons.verified_user_outlined, 'Profesionalci'),
    (Icons.work_outline, 'Oglasi'),
    (Icons.event_available_outlined, 'Rezervacije'),
    (Icons.dataset_outlined, 'Referentni podaci'),
    (Icons.reviews_outlined, 'Recenzije'),
    (Icons.manage_search_outlined, 'Evidencija aktivnosti'),
    (Icons.assessment_outlined, 'Izvještaji'),
  ];

  @override
  void initState() {
    super.initState();
    final auth = context.read<AuthService>();
    _repository = AdminRepository(auth.api);
    _notifications = RealtimeNotifications(
      repository: _repository,
      tokenProvider: () => auth.token,
    );
    _notifications.start();
  }

  @override
  void dispose() {
    _notifications.dispose();
    super.dispose();
  }

  List<Widget> get _pages => [
    DashboardScreen(repository: _repository),
    UsersScreen(repository: _repository),
    ProfessionalsScreen(repository: _repository),
    JobsScreen(repository: _repository),
    ReservationsScreen(repository: _repository),
    ReferenceDataScreen(repository: _repository),
    ReviewsScreen(repository: _repository),
    AuditLogsScreen(repository: _repository),
    ReportsScreen(repository: _repository),
  ];

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider.value(
      value: _notifications,
      child: Scaffold(
        body: LayoutBuilder(
          builder: (context, constraints) {
            final compact = constraints.maxWidth < 980;
            return Row(
              children: [
                _Navigation(
                  compact: compact,
                  selectedIndex: _selectedIndex,
                  onSelected: (index) => setState(() => _selectedIndex = index),
                ),
                Expanded(
                  child: Column(
                    children: [
                      _TopBar(
                        onNotifications: () => _showNotifications(context),
                      ),
                      Expanded(
                        child: AnimatedSwitcher(
                          duration: const Duration(milliseconds: 260),
                          child: Padding(
                            key: ValueKey(_selectedIndex),
                            padding: EdgeInsets.all(compact ? 20 : 30),
                            child: _pages[_selectedIndex],
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            );
          },
        ),
      ),
    );
  }

  Future<void> _showNotifications(BuildContext context) => showDialog<void>(
    context: context,
    builder: (_) => ChangeNotifierProvider.value(
      value: _notifications,
      child: const _NotificationDialog(),
    ),
  );
}

class _Navigation extends StatelessWidget {
  const _Navigation({
    required this.compact,
    required this.selectedIndex,
    required this.onSelected,
  });
  final bool compact;
  final int selectedIndex;
  final ValueChanged<int> onSelected;

  @override
  Widget build(BuildContext context) => Container(
    width: compact ? 86 : 232,
    color: ink,
    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 22),
    child: Column(
      children: [
        Row(
          mainAxisAlignment: compact
              ? MainAxisAlignment.center
              : MainAxisAlignment.start,
          children: [
            Container(
              width: 44,
              height: 44,
              decoration: BoxDecoration(
                color: coral,
                borderRadius: BorderRadius.circular(14),
              ),
              child: const Icon(
                Icons.home_repair_service_rounded,
                color: Colors.white,
              ),
            ),
            if (!compact) ...[
              const SizedBox(width: 12),
              const Text(
                'FixedIT',
                style: TextStyle(
                  fontFamily: 'Georgia',
                  fontWeight: FontWeight.bold,
                  fontSize: 23,
                  color: Colors.white,
                ),
              ),
            ],
          ],
        ),
        const SizedBox(height: 34),
        for (
          var index = 0;
          index < _AdminShellState._destinations.length;
          index++
        )
          Padding(
            padding: const EdgeInsets.only(bottom: 7),
            child: _NavItem(
              compact: compact,
              icon: _AdminShellState._destinations[index].$1,
              label: _AdminShellState._destinations[index].$2,
              selected: selectedIndex == index,
              onTap: () => onSelected(index),
            ),
          ),
      ],
    ),
  );
}

class _NavItem extends StatelessWidget {
  const _NavItem({
    required this.compact,
    required this.icon,
    required this.label,
    required this.selected,
    required this.onTap,
  });
  final bool compact;
  final IconData icon;
  final String label;
  final bool selected;
  final VoidCallback onTap;
  @override
  Widget build(BuildContext context) => Tooltip(
    message: compact ? label : '',
    child: Material(
      color: selected
          ? Colors.white.withValues(alpha: 0.13)
          : Colors.transparent,
      borderRadius: BorderRadius.circular(12),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: EdgeInsets.symmetric(
            horizontal: compact ? 0 : 14,
            vertical: 13,
          ),
          child: Row(
            mainAxisAlignment: compact
                ? MainAxisAlignment.center
                : MainAxisAlignment.start,
            children: [
              Icon(icon, color: selected ? Colors.white : Colors.white60),
              if (!compact) ...[
                const SizedBox(width: 13),
                Text(
                  label,
                  style: TextStyle(
                    color: selected ? Colors.white : Colors.white70,
                    fontWeight: selected ? FontWeight.w700 : FontWeight.w500,
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    ),
  );
}

class _TopBar extends StatelessWidget {
  const _TopBar({required this.onNotifications});
  final VoidCallback onNotifications;
  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthService>();
    final notifications = context.watch<RealtimeNotifications>();
    return Container(
      height: 76,
      padding: const EdgeInsets.symmetric(horizontal: 28),
      decoration: const BoxDecoration(
        color: Color(0xFFFFFCF6),
        border: Border(bottom: BorderSide(color: sand)),
      ),
      child: Row(
        children: [
          Text(
            DateFormat('dd.MM.yyyy').format(DateTime.now()),
            style: const TextStyle(color: ink, fontWeight: FontWeight.w600),
          ),
          const Spacer(),
          IconButton(
            tooltip: 'Obavijesti',
            onPressed: onNotifications,
            icon: Badge(
              isLabelVisible: notifications.unreadCount > 0,
              label: Text('${notifications.unreadCount}'),
              child: Icon(
                notifications.status == RealtimeStatus.connected
                    ? Icons.notifications_active_outlined
                    : Icons.notifications_outlined,
              ),
            ),
          ),
          const SizedBox(width: 12),
          CircleAvatar(
            backgroundColor: coral.withValues(alpha: 0.16),
            child: Text(
              (auth.user?.firstName.isNotEmpty ?? false)
                  ? auth.user!.firstName[0].toUpperCase()
                  : 'A',
            ),
          ),
          const SizedBox(width: 10),
          Text(
            auth.user?.fullName ?? 'Administrator',
            style: const TextStyle(fontWeight: FontWeight.w700),
          ),
          const SizedBox(width: 8),
          IconButton(
            tooltip: 'Odjava',
            onPressed: () async {
              await notifications.stop();
              if (context.mounted) await context.read<AuthService>().logout();
            },
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
    );
  }
}

class _NotificationDialog extends StatelessWidget {
  const _NotificationDialog();
  @override
  Widget build(BuildContext context) {
    final notifications = context.watch<RealtimeNotifications>();
    return AlertDialog(
      title: Row(
        children: [
          const Expanded(child: Text('Obavijesti')),
          if (notifications.status == RealtimeStatus.connected)
            const Icon(Icons.wifi, color: success, size: 20),
        ],
      ),
      content: SizedBox(
        width: 560,
        height: 520,
        child: notifications.loading
            ? const Center(child: CircularProgressIndicator())
            : notifications.error != null && notifications.items.isEmpty
            ? Center(child: Text(notifications.error!))
            : notifications.items.isEmpty
            ? const Center(child: Text('Nema nepročitanih obavijesti.'))
            : ListView.separated(
                itemCount: notifications.items.length,
                separatorBuilder: (_, _) => const Divider(height: 1),
                itemBuilder: (context, index) {
                  final item = notifications.items[index];
                  return ListTile(
                    leading: Icon(
                      item.isRead
                          ? Icons.notifications_none
                          : Icons.notifications_active,
                      color: item.isRead ? null : coral,
                    ),
                    title: Text(
                      item.title,
                      style: TextStyle(
                        fontWeight: item.isRead
                            ? FontWeight.w500
                            : FontWeight.w800,
                      ),
                    ),
                    subtitle: Text(
                      '${item.body}\n${DateFormat('dd.MM.yyyy HH:mm').format(item.createdAt)}',
                    ),
                    isThreeLine: true,
                    onTap: item.isRead
                        ? null
                        : () => notifications.markRead(item),
                  );
                },
              ),
      ),
      actions: [
        if (notifications.status == RealtimeStatus.error ||
            notifications.status == RealtimeStatus.disconnected)
          TextButton.icon(
            onPressed: notifications.loading ? null : notifications.retry,
            icon: const Icon(Icons.refresh),
            label: const Text('Ponovo poveži'),
          ),
        TextButton(
          onPressed: notifications.unreadCount == 0
              ? null
              : notifications.markAllRead,
          child: const Text('Označi sve pročitano'),
        ),
        FilledButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Zatvori'),
        ),
      ],
    );
  }
}
