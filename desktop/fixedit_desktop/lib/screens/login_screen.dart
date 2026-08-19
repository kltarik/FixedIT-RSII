import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../app/theme.dart';
import '../core/api_client.dart';
import '../services/auth_service.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    await context.read<AuthService>().login(
      _emailController.text,
      _passwordController.text,
    );
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthService>();
    return Scaffold(
      body: DecoratedBox(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [Color(0xFFF8F2E6), Color(0xFFDDE9E2)],
          ),
        ),
        child: Stack(
          children: [
            Positioned(
              right: -90,
              top: -120,
              child: _Orb(size: 380, color: coral.withValues(alpha: 0.13)),
            ),
            Positioned(
              left: -100,
              bottom: -150,
              child: _Orb(size: 430, color: ink.withValues(alpha: 0.1)),
            ),
            Center(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(32),
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 460),
                  child: Card(
                    child: Padding(
                      padding: const EdgeInsets.all(38),
                      child: Form(
                        key: _formKey,
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            Row(
                              children: [
                                const _BrandMark(),
                                const SizedBox(width: 14),
                                Text(
                                  'FixedIT',
                                  style: Theme.of(
                                    context,
                                  ).textTheme.headlineMedium,
                                ),
                              ],
                            ),
                            const SizedBox(height: 34),
                            Text(
                              'Administracija bez buke.',
                              style: Theme.of(context).textTheme.displaySmall,
                            ),
                            const SizedBox(height: 10),
                            Text(
                              'Prijavite se administratorskim nalogom za pregled operacija, rezervacija i finansija.',
                              style: Theme.of(context).textTheme.bodyLarge,
                            ),
                            const SizedBox(height: 30),
                            TextFormField(
                              controller: _emailController,
                              keyboardType: TextInputType.emailAddress,
                              autofillHints: const [AutofillHints.email],
                              decoration: const InputDecoration(
                                labelText: 'Email',
                                prefixIcon: Icon(Icons.alternate_email),
                              ),
                              validator: (value) {
                                final email = value?.trim() ?? '';
                                if (email.isEmpty) return 'Email je obavezan.';
                                if (!RegExp(
                                  r'^[^@\s]+@[^@\s]+\.[^@\s]+$',
                                ).hasMatch(email)) {
                                  return 'Unesite ispravnu email adresu.';
                                }
                                return null;
                              },
                              onFieldSubmitted: (_) => _submit(),
                            ),
                            const SizedBox(height: 16),
                            TextFormField(
                              controller: _passwordController,
                              obscureText: _obscurePassword,
                              autofillHints: const [AutofillHints.password],
                              decoration: InputDecoration(
                                labelText: 'Lozinka',
                                prefixIcon: const Icon(Icons.lock_outline),
                                suffixIcon: IconButton(
                                  onPressed: () => setState(
                                    () => _obscurePassword = !_obscurePassword,
                                  ),
                                  icon: Icon(
                                    _obscurePassword
                                        ? Icons.visibility_outlined
                                        : Icons.visibility_off_outlined,
                                  ),
                                ),
                              ),
                              validator: (value) => (value?.isEmpty ?? true)
                                  ? 'Lozinka je obavezna.'
                                  : null,
                              onFieldSubmitted: (_) => _submit(),
                            ),
                            if (auth.error != null) ...[
                              const SizedBox(height: 14),
                              Text(
                                auth.error!,
                                style: TextStyle(
                                  color: Theme.of(context).colorScheme.error,
                                ),
                              ),
                            ],
                            const SizedBox(height: 22),
                            FilledButton(
                              onPressed: auth.isLoading ? null : _submit,
                              child: auth.isLoading
                                  ? const SizedBox(
                                      width: 22,
                                      height: 22,
                                      child: CircularProgressIndicator(
                                        strokeWidth: 2,
                                        color: Colors.white,
                                      ),
                                    )
                                  : const Text('Prijavi se'),
                            ),
                            const SizedBox(height: 18),
                            Text(
                              ApiClient.apiBaseUrl.isEmpty
                                  ? 'API URL nije konfigurisan'
                                  : 'API: ${ApiClient.apiBaseUrl}',
                              textAlign: TextAlign.center,
                              style: Theme.of(context).textTheme.bodySmall
                                  ?.copyWith(
                                    color: ink.withValues(alpha: 0.55),
                                  ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _BrandMark extends StatelessWidget {
  const _BrandMark();
  @override
  Widget build(BuildContext context) => Container(
    width: 48,
    height: 48,
    decoration: BoxDecoration(
      color: ink,
      borderRadius: BorderRadius.circular(15),
    ),
    child: const Icon(Icons.home_repair_service_rounded, color: Colors.white),
  );
}

class _Orb extends StatelessWidget {
  const _Orb({required this.size, required this.color});
  final double size;
  final Color color;
  @override
  Widget build(BuildContext context) => Container(
    width: size,
    height: size,
    decoration: BoxDecoration(shape: BoxShape.circle, color: color),
  );
}
