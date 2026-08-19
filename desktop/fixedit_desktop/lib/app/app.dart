import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../screens/admin_shell.dart';
import '../screens/login_screen.dart';
import '../services/auth_service.dart';
import 'theme.dart';

class FixedItApp extends StatelessWidget {
  const FixedItApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'FixedIT administracija',
      debugShowCheckedModeBanner: false,
      theme: buildFixedItTheme(),
      home: Consumer<AuthService>(
        builder: (context, auth, _) => AnimatedSwitcher(
          duration: const Duration(milliseconds: 360),
          child: auth.isAuthenticated
              ? const AdminShell(key: ValueKey('admin-shell'))
              : const LoginScreen(key: ValueKey('login-screen')),
        ),
      ),
    );
  }
}
