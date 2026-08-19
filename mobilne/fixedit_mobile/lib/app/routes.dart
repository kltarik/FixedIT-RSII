import 'package:flutter/material.dart';

import '../screens/chat_screen.dart';
import '../screens/payment_screen.dart';
import '../screens/professional_detail_screen.dart';
import '../screens/reservation_detail_screen.dart';

abstract final class AppRoutes {
  static const professional = '/professional';
  static const reservation = '/reservation';
  static const chat = '/chat';
  static const payment = '/payment';

  static Route<dynamic> generate(RouteSettings settings) {
    final args = settings.arguments;
    return MaterialPageRoute(
      settings: settings,
      builder: (_) => switch (settings.name) {
        professional when args is int => ProfessionalDetailScreen(
          professionalId: args,
        ),
        reservation when args is int => ReservationDetailScreen(
          reservationId: args,
        ),
        chat when args is int => ChatScreen(reservationId: args),
        payment when args is int => PaymentScreen(reservationId: args),
        _ => const MissingRouteScreen(),
      },
    );
  }
}

class MissingRouteScreen extends StatelessWidget {
  const MissingRouteScreen({super.key});
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Nepoznata stranica')),
    body: Center(
      child: FilledButton.icon(
        onPressed: () => Navigator.maybePop(context),
        icon: const Icon(Icons.arrow_back),
        label: const Text('Nazad'),
      ),
    ),
  );
}
