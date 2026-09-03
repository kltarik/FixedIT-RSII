import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../app/theme.dart';
import '../core/api_client.dart';

String userError(Object error) => userFacingError(error);

final money = NumberFormat.currency(
  locale: 'bs_BA',
  symbol: '',
  decimalDigits: 2,
);
final shortDate = DateFormat('dd.MM.yyyy');
final shortTime = DateFormat('HH:mm');
final dateTime = DateFormat('dd.MM.yyyy HH:mm');

String? resolveNetworkUrl(String? value) {
  if (value == null || value.trim().isEmpty) {
    return null;
  }

  final uri = Uri.tryParse(value);
  if (uri != null && uri.hasScheme) {
    return value;
  }

  if (ApiClient.apiBaseUrl.isEmpty) {
    return null;
  }

  return Uri.parse('${ApiClient.normalizedBaseUrl}/').resolve(value).toString();
}

class ScreenTitle extends StatelessWidget {
  const ScreenTitle(this.title, this.subtitle, {super.key, this.action});
  final String title;
  final String subtitle;
  final Widget? action;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.fromLTRB(4, 10, 4, 18),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: Theme.of(context).textTheme.headlineMedium),
              const SizedBox(height: 4),
              Text(
                subtitle,
                style: TextStyle(color: midnight.withValues(alpha: .65)),
              ),
            ],
          ),
        ),
        ?action,
      ],
    ),
  );
}

class LoadingView extends StatelessWidget {
  const LoadingView({super.key});
  @override
  Widget build(BuildContext context) =>
      const Center(child: CircularProgressIndicator());
}

class EmptyView extends StatelessWidget {
  const EmptyView(this.message, {super.key, this.icon = Icons.inbox_outlined});
  final String message;
  final IconData icon;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(30),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 52, color: midnight.withValues(alpha: .3)),
          const SizedBox(height: 10),
          Text(
            message,
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.titleMedium,
          ),
        ],
      ),
    ),
  );
}

class ErrorView extends StatelessWidget {
  const ErrorView(this.message, this.retry, {super.key});
  final String message;
  final VoidCallback retry;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            Icons.cloud_off,
            size: 48,
            color: Theme.of(context).colorScheme.error,
          ),
          const SizedBox(height: 10),
          Text(message, textAlign: TextAlign.center),
          const SizedBox(height: 14),
          FilledButton.tonalIcon(
            onPressed: retry,
            icon: const Icon(Icons.refresh),
            label: const Text('Pokušaj ponovo'),
          ),
        ],
      ),
    ),
  );
}

class NetworkAvatar extends StatelessWidget {
  const NetworkAvatar({
    super.key,
    this.url,
    required this.name,
    this.radius = 25,
  });
  final String? url;
  final String name;
  final double radius;
  @override
  Widget build(BuildContext context) {
    final resolved = resolveNetworkUrl(url);
    return CircleAvatar(
      radius: radius,
      backgroundColor: mist,
      backgroundImage: resolved == null
          ? null
          : CachedNetworkImageProvider(resolved),
      child: resolved == null
          ? Text(
              name.isEmpty ? '?' : name[0].toUpperCase(),
              style: const TextStyle(
                fontWeight: FontWeight.bold,
                color: midnight,
              ),
            )
          : null,
    );
  }
}

Future<void> showFailure(BuildContext context, Object error) =>
    showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Operacija nije uspjela'),
        content: Text(userError(error)),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Zatvori'),
          ),
        ],
      ),
    );
