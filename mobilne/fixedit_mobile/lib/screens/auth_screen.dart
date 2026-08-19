import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../app/theme.dart';
import '../core/models.dart';
import '../services/auth_service.dart';
import '../services/mobile_repository.dart';

class AuthLanding extends StatefulWidget {
  const AuthLanding({super.key});
  @override
  State<AuthLanding> createState() => _AuthLandingState();
}

class _AuthLandingState extends State<AuthLanding> {
  bool started = false;
  @override
  Widget build(BuildContext context) => started
      ? const AuthScreen()
      : Scaffold(
          body: DecoratedBox(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [Color(0xFFFFF2DC), Color(0xFFD8E9E6)],
              ),
            ),
            child: SafeArea(
              child: Padding(
                padding: const EdgeInsets.all(28),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Spacer(),
                    Container(
                      width: 70,
                      height: 70,
                      decoration: BoxDecoration(
                        color: midnight,
                        borderRadius: BorderRadius.circular(22),
                      ),
                      child: const Icon(
                        Icons.home_repair_service_rounded,
                        color: Colors.white,
                        size: 36,
                      ),
                    ),
                    const SizedBox(height: 28),
                    Text(
                      'Popravke, bez nagađanja.',
                      style: Theme.of(
                        context,
                      ).textTheme.displaySmall?.copyWith(fontSize: 43),
                    ),
                    const SizedBox(height: 14),
                    const Text(
                      'Pronađite provjerene profesionalce, dogovorite termin, razgovarajte i platite sigurno na jednom mjestu.',
                      style: TextStyle(fontSize: 18, height: 1.45),
                    ),
                    const Spacer(),
                    FilledButton.icon(
                      onPressed: () => setState(() => started = true),
                      icon: const Icon(Icons.arrow_forward),
                      label: const Text('Započni'),
                    ),
                    const SizedBox(height: 12),
                  ],
                ),
              ),
            ),
          ),
        );
}

class AuthScreen extends StatefulWidget {
  const AuthScreen({super.key});
  @override
  State<AuthScreen> createState() => _AuthScreenState();
}

class _AuthScreenState extends State<AuthScreen>
    with SingleTickerProviderStateMixin {
  late final TabController tabs;
  @override
  void initState() {
    super.initState();
    tabs = TabController(length: 2, vsync: this);
  }

  @override
  void dispose() {
    tabs.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: const Text('FixedIT'),
      bottom: TabBar(
        controller: tabs,
        tabs: const [
          Tab(text: 'Prijava'),
          Tab(text: 'Registracija'),
        ],
      ),
    ),
    body: TabBarView(
      controller: tabs,
      children: const [_LoginForm(), _RegisterForm()],
    ),
  );
}

class _LoginForm extends StatefulWidget {
  const _LoginForm();
  @override
  State<_LoginForm> createState() => _LoginFormState();
}

class _LoginFormState extends State<_LoginForm> {
  final key = GlobalKey<FormState>();
  final email = TextEditingController();
  final password = TextEditingController();
  bool obscure = true;
  @override
  void dispose() {
    email.dispose();
    password.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthService>();
    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: Form(
        key: key,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const SizedBox(height: 24),
            Text(
              'Dobro došli nazad',
              style: Theme.of(context).textTheme.headlineMedium,
            ),
            const SizedBox(height: 24),
            TextFormField(
              controller: email,
              keyboardType: TextInputType.emailAddress,
              decoration: const InputDecoration(
                labelText: 'Email',
                prefixIcon: Icon(Icons.alternate_email),
              ),
              validator: _emailValidator,
            ),
            const SizedBox(height: 14),
            TextFormField(
              controller: password,
              obscureText: obscure,
              decoration: InputDecoration(
                labelText: 'Lozinka',
                prefixIcon: const Icon(Icons.lock_outline),
                suffixIcon: IconButton(
                  onPressed: () => setState(() => obscure = !obscure),
                  icon: Icon(obscure ? Icons.visibility : Icons.visibility_off),
                ),
              ),
              validator: (v) =>
                  (v?.isEmpty ?? true) ? 'Lozinka je obavezna.' : null,
            ),
            if (auth.error != null)
              Padding(
                padding: const EdgeInsets.only(top: 12),
                child: Text(
                  auth.error!,
                  style: TextStyle(color: Theme.of(context).colorScheme.error),
                ),
              ),
            const SizedBox(height: 20),
            FilledButton(
              onPressed: auth.loading
                  ? null
                  : () {
                      if (key.currentState!.validate()) {
                        auth.login(email.text, password.text);
                      }
                    },
              child: auth.loading
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Text('Prijavi se'),
            ),
          ],
        ),
      ),
    );
  }
}

class _RegisterForm extends StatefulWidget {
  const _RegisterForm();
  @override
  State<_RegisterForm> createState() => _RegisterFormState();
}

class _RegisterFormState extends State<_RegisterForm> {
  final key = GlobalKey<FormState>();
  final email = TextEditingController();
  final password = TextEditingController();
  final first = TextEditingController();
  final last = TextEditingController();
  String role = 'Client';
  int? city;
  late final Future<ReferenceData> reference;
  @override
  void initState() {
    super.initState();
    reference = context.read<MobileRepository>().getReferenceData();
  }

  @override
  void dispose() {
    email.dispose();
    password.dispose();
    first.dispose();
    last.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => FutureBuilder<ReferenceData>(
    future: reference,
    builder: (context, snapshot) {
      if (!snapshot.hasData) {
        return const Center(child: CircularProgressIndicator());
      }
      final auth = context.watch<AuthService>();
      return SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Form(
          key: key,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(
                'Kreirajte nalog',
                style: Theme.of(context).textTheme.headlineMedium,
              ),
              const SizedBox(height: 20),
              Row(
                children: [
                  Expanded(
                    child: TextFormField(
                      controller: first,
                      decoration: const InputDecoration(labelText: 'Ime'),
                      validator: _required,
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: TextFormField(
                      controller: last,
                      decoration: const InputDecoration(labelText: 'Prezime'),
                      validator: _required,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: email,
                keyboardType: TextInputType.emailAddress,
                decoration: const InputDecoration(labelText: 'Email'),
                validator: _emailValidator,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: password,
                obscureText: true,
                decoration: const InputDecoration(
                  labelText: 'Lozinka (min. 8 znakova)',
                ),
                validator: (v) => (v?.length ?? 0) < 8
                    ? 'Lozinka mora imati najmanje 8 znakova.'
                    : null,
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<String>(
                initialValue: role,
                decoration: const InputDecoration(labelText: 'Uloga'),
                items: const [
                  DropdownMenuItem(value: 'Client', child: Text('Klijent')),
                  DropdownMenuItem(
                    value: 'Professional',
                    child: Text('Profesionalac'),
                  ),
                ],
                onChanged: (v) => setState(() => role = v ?? role),
              ),
              const SizedBox(height: 12),
              DropdownButtonFormField<int>(
                initialValue: city,
                decoration: const InputDecoration(labelText: 'Grad'),
                items: snapshot.data!.cities
                    .map(
                      (e) => DropdownMenuItem(value: e.id, child: Text(e.name)),
                    )
                    .toList(),
                onChanged: (v) => setState(() => city = v),
                validator: (v) => v == null ? 'Odaberite grad.' : null,
              ),
              if (auth.error != null)
                Padding(
                  padding: const EdgeInsets.only(top: 12),
                  child: Text(
                    auth.error!,
                    style: TextStyle(
                      color: Theme.of(context).colorScheme.error,
                    ),
                  ),
                ),
              const SizedBox(height: 20),
              FilledButton(
                onPressed: auth.loading
                    ? null
                    : () {
                        if (key.currentState!.validate()) {
                          auth.register(
                            email: email.text,
                            password: password.text,
                            firstName: first.text,
                            lastName: last.text,
                            role: role,
                            cityId: city!,
                          );
                        }
                      },
                child: const Text('Registruj se'),
              ),
            ],
          ),
        ),
      );
    },
  );
}

String? _required(String? value) =>
    (value?.trim().isEmpty ?? true) ? 'Polje je obavezno.' : null;
String? _emailValidator(String? value) {
  final email = value?.trim() ?? '';
  if (email.isEmpty) return 'Email je obavezan.';
  if (!RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(email)) {
    return 'Email nije ispravan.';
  }
  return null;
}
