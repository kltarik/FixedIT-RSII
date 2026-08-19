import 'package:fixedit_desktop/app/app.dart';
import 'package:fixedit_desktop/services/auth_service.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

void main() {
  testWidgets('login screen validates required fields inline', (tester) async {
    await tester.binding.setSurfaceSize(const Size(1200, 900));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(
      ChangeNotifierProvider(
        create: (_) => AuthService(),
        child: const FixedItApp(),
      ),
    );

    expect(find.text('Administracija bez buke.'), findsOneWidget);
    await tester.tap(find.widgetWithText(FilledButton, 'Prijavi se'));
    await tester.pump();

    expect(find.text('Email je obavezan.'), findsOneWidget);
    expect(find.text('Lozinka je obavezna.'), findsOneWidget);
  });

  testWidgets('password visibility uses a boolean icon control', (
    tester,
  ) async {
    await tester.binding.setSurfaceSize(const Size(1200, 900));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(
      ChangeNotifierProvider(
        create: (_) => AuthService(),
        child: const FixedItApp(),
      ),
    );

    final passwordField = tester.widget<EditableText>(
      find.byType(EditableText).last,
    );
    expect(passwordField.obscureText, isTrue);
    await tester.tap(find.byIcon(Icons.visibility_outlined));
    await tester.pump();
    expect(
      tester.widget<EditableText>(find.byType(EditableText).last).obscureText,
      isFalse,
    );
  });
}
