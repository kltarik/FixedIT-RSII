import 'package:fixedit_mobile/app/app.dart';
import 'package:fixedit_mobile/app/routes.dart';
import 'package:fixedit_mobile/services/auth_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

void main() {
  testWidgets('onboarding opens the authentication flow', (tester) async {
    await tester.pumpWidget(
      ChangeNotifierProvider(
        create: (_) => AuthService(),
        child: const FixedItMobileApp(),
      ),
    );
    expect(find.text('Popravke, bez nagađanja.'), findsOneWidget);
    await tester.tap(find.text('Započni'));
    await tester.pumpAndSettle();
    expect(find.text('Prijava'), findsOneWidget);
    expect(find.text('Registracija'), findsOneWidget);
  });

  testWidgets('named routes handle missing arguments safely', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        onGenerateRoute: AppRoutes.generate,
        home: Builder(
          builder: (context) => FilledButton(
            onPressed: () =>
                Navigator.pushNamed(context, AppRoutes.professional),
            child: const Text('Otvori'),
          ),
        ),
      ),
    );

    await tester.tap(find.text('Otvori'));
    await tester.pumpAndSettle();

    expect(find.text('Nepoznata stranica'), findsOneWidget);
  });
}
