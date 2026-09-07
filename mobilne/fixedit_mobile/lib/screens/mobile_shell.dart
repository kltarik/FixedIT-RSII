import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../services/auth_service.dart';
import '../services/mobile_repository.dart';
import '../services/realtime_service.dart';
import 'earnings_screen.dart';
import 'home_screen.dart';
import 'jobs_screen.dart';
import 'notifications_screen.dart';
import 'profile_screen.dart';
import 'reservations_screen.dart';
import 'search_screen.dart';

class MobileShell extends StatefulWidget {
  const MobileShell({super.key});
  @override
  State<MobileShell> createState() => _MobileShellState();
}

class _MobileShellState extends State<MobileShell> {
  int index = 0;
  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthService>();
    final professional = auth.user!.isProfessional;
    final repo = context.read<MobileRepository>();
    final pages = professional
        ? [
            HomeScreen(repository: repo, client: false),
            JobsScreen(repository: repo, client: false),
            ReservationsScreen(repository: repo, professional: true),
            EarningsScreen(repository: repo),
            ProfileScreen(repository: repo, professional: true),
          ]
        : [
            HomeScreen(repository: repo, client: true),
            SearchScreen(repository: repo),
            JobsScreen(repository: repo, client: true),
            ReservationsScreen(repository: repo, professional: false),
            ProfileScreen(repository: repo, professional: false),
          ];
    final destinations = professional
        ? const [
            (Icons.home_outlined, 'Početna'),
            (Icons.work_outline, 'Oglasi'),
            (Icons.event_note_outlined, 'Termini'),
            (Icons.payments_outlined, 'Zarada'),
            (Icons.person_outline, 'Profil'),
          ]
        : const [
            (Icons.home_outlined, 'Početna'),
            (Icons.search, 'Pretraga'),
            (Icons.work_outline, 'Oglasi'),
            (Icons.event_note_outlined, 'Rezervacije'),
            (Icons.person_outline, 'Profil'),
          ];
    final realtime = context.watch<RealtimeNotifications>();
    return Scaffold(
      appBar: AppBar(
        title: Text('Zdravo, ${auth.user!.firstName}'),
        actions: [
          IconButton(
            onPressed: () => Navigator.push(
              context,
              MaterialPageRoute(
                builder: (_) => NotificationsScreen(service: realtime),
              ),
            ),
            icon: Badge(
              isLabelVisible: realtime.unreadCount > 0,
              label: Text('${realtime.unreadCount}'),
              child: Icon(
                realtime.connected
                    ? Icons.notifications_active_outlined
                    : Icons.notifications_outlined,
              ),
            ),
          ),
          IconButton(
            onPressed: () async {
              await realtime.stop();
              if (context.mounted) await context.read<AuthService>().logout();
            },
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: SafeArea(
        child: IndexedStack(index: index, children: pages),
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: index,
        onDestinationSelected: (value) => setState(() => index = value),
        destinations: destinations
            .map((e) => NavigationDestination(icon: Icon(e.$1), label: e.$2))
            .toList(),
      ),
    );
  }
}
