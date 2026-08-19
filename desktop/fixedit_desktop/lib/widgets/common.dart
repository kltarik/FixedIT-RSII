import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../app/theme.dart';
import '../core/api_client.dart';

String userError(Object error) => userFacingError(error);

final dateFormat = DateFormat('dd.MM.yyyy');
final dateTimeFormat = DateFormat('dd.MM.yyyy HH:mm');
final moneyFormat = NumberFormat.currency(
  locale: 'bs_BA',
  symbol: '',
  decimalDigits: 2,
);

class PageHeading extends StatelessWidget {
  const PageHeading({
    super.key,
    required this.title,
    required this.subtitle,
    this.actions = const [],
  });
  final String title;
  final String subtitle;
  final List<Widget> actions;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: 22),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: Theme.of(context).textTheme.displaySmall),
              const SizedBox(height: 6),
              Text(
                subtitle,
                style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                  color: ink.withValues(alpha: 0.66),
                ),
              ),
            ],
          ),
        ),
        ...actions,
      ],
    ),
  );
}

class LoadingPanel extends StatelessWidget {
  const LoadingPanel({super.key, this.label = 'Učitavanje podataka...'});
  final String label;
  @override
  Widget build(BuildContext context) => Center(
    child: Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        const CircularProgressIndicator(),
        const SizedBox(height: 14),
        Text(label),
      ],
    ),
  );
}

class ErrorPanel extends StatelessWidget {
  const ErrorPanel({super.key, required this.message, required this.onRetry});
  final String message;
  final VoidCallback onRetry;
  @override
  Widget build(BuildContext context) => Center(
    child: Card(
      child: Padding(
        padding: const EdgeInsets.all(28),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 460),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(
                Icons.cloud_off_outlined,
                size: 42,
                color: Theme.of(context).colorScheme.error,
              ),
              const SizedBox(height: 12),
              Text(message, textAlign: TextAlign.center),
              const SizedBox(height: 18),
              FilledButton.icon(
                onPressed: onRetry,
                icon: const Icon(Icons.refresh),
                label: const Text('Pokušaj ponovo'),
              ),
            ],
          ),
        ),
      ),
    ),
  );
}

class EmptyPanel extends StatelessWidget {
  const EmptyPanel({
    super.key,
    required this.message,
    this.icon = Icons.inbox_outlined,
  });
  final String message;
  final IconData icon;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(36),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 48, color: ink.withValues(alpha: 0.35)),
          const SizedBox(height: 12),
          Text(
            message,
            style: Theme.of(context).textTheme.titleMedium,
            textAlign: TextAlign.center,
          ),
        ],
      ),
    ),
  );
}

class PagedFooter extends StatelessWidget {
  const PagedFooter({
    super.key,
    required this.page,
    required this.pageCount,
    required this.total,
    required this.onPage,
  });
  final int page;
  final int pageCount;
  final int total;
  final ValueChanged<int> onPage;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(top: 14),
    child: Row(
      children: [
        Text('$total zapisa'),
        const Spacer(),
        IconButton(
          onPressed: page > 1 ? () => onPage(page - 1) : null,
          icon: const Icon(Icons.chevron_left),
        ),
        Text('Stranica $page / $pageCount'),
        IconButton(
          onPressed: page < pageCount ? () => onPage(page + 1) : null,
          icon: const Icon(Icons.chevron_right),
        ),
      ],
    ),
  );
}

class StatusPill extends StatelessWidget {
  const StatusPill({super.key, required this.label, this.positive = true});
  final String label;
  final bool positive;
  @override
  Widget build(BuildContext context) {
    final color = positive ? success : Theme.of(context).colorScheme.error;
    return DecoratedBox(
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(99),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
        child: Text(
          label,
          style: TextStyle(
            color: color,
            fontWeight: FontWeight.w700,
            fontSize: 12,
          ),
        ),
      ),
    );
  }
}

class DateFilterButton extends StatelessWidget {
  const DateFilterButton({
    super.key,
    required this.label,
    required this.value,
    required this.onChanged,
  });
  final String label;
  final DateTime? value;
  final ValueChanged<DateTime?> onChanged;

  @override
  Widget build(BuildContext context) => OutlinedButton.icon(
    onPressed: () async {
      final selected = await showDatePicker(
        context: context,
        initialDate: value ?? DateTime.now(),
        firstDate: DateTime(2020),
        lastDate: DateTime.now().add(const Duration(days: 365)),
      );
      if (selected != null) onChanged(selected);
    },
    onLongPress: value == null ? null : () => onChanged(null),
    icon: const Icon(Icons.calendar_month_outlined),
    label: Text(value == null ? label : '$label: ${dateFormat.format(value!)}'),
  );
}

Future<void> showApiErrorDialog(
  BuildContext context,
  String message, {
  String title = 'Operacija nije uspjela',
}) {
  return showDialog<void>(
    context: context,
    builder: (context) => AlertDialog(
      title: Text(title),
      content: Text(message),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Zatvori'),
        ),
      ],
    ),
  );
}

Widget scrollableTable(DataTable table) =>
    SingleChildScrollView(scrollDirection: Axis.horizontal, child: table);
