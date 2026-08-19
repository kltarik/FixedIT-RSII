import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../screens/auth_screen.dart';
import '../screens/mobile_shell.dart';
import '../services/auth_service.dart';
import '../services/mobile_repository.dart';
import '../services/realtime_service.dart';
import 'routes.dart';
import 'theme.dart';

class FixedItMobileApp extends StatelessWidget {
  const FixedItMobileApp({super.key});
  @override
  Widget build(BuildContext context) {
    final auth = context.read<AuthService>();
    return Provider(
      create: (_) => MobileRepository(auth.api),
      child: MaterialApp(
        title: 'FixedIT',
        debugShowCheckedModeBanner: false,
        theme: buildMobileTheme(),
        onGenerateRoute: AppRoutes.generate,
        home: Consumer<AuthService>(
          builder: (context, auth, _) => AnimatedSwitcher(
            duration: const Duration(milliseconds: 320),
            child: auth.authenticated
                ? const _RealtimeHost(key: ValueKey('home'))
                : const AuthLanding(key: ValueKey('auth')),
          ),
        ),
      ),
    );
  }
}

class _RealtimeHost extends StatefulWidget {
  const _RealtimeHost({super.key});
  @override
  State<_RealtimeHost> createState() => _RealtimeHostState();
}

class _RealtimeHostState extends State<_RealtimeHost> {
  late final RealtimeNotifications service;
  @override
  void initState() {
    super.initState();
    final auth = context.read<AuthService>();
    service = RealtimeNotifications(
      context.read<MobileRepository>(),
      () => auth.token,
    );
    service.start();
  }

  @override
  void dispose() {
    service.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) =>
      ChangeNotifierProvider.value(value: service, child: const MobileShell());
}
