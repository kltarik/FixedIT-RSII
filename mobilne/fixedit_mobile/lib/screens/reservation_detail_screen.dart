import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../app/routes.dart';
import '../core/models.dart';
import '../services/auth_service.dart';
import '../services/mobile_repository.dart';
import '../widgets/common.dart';

class ReservationDetailScreen extends StatefulWidget {
  const ReservationDetailScreen({super.key, required this.reservationId});
  final int reservationId;
  @override
  State<ReservationDetailScreen> createState() =>
      _ReservationDetailScreenState();
}

class _ReservationDetailScreenState extends State<ReservationDetailScreen> {
  Reservation? item;
  String? error;
  bool busy = false;
  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    try {
      final value = await context.read<MobileRepository>().getReservation(
        widget.reservationId,
      );
      if (mounted) {
        setState(() {
          item = value;
          error = null;
        });
      }
    } catch (e) {
      if (mounted) setState(() => error = userError(e));
    }
  }

  @override
  Widget build(BuildContext context) {
    if (error != null) {
      return Scaffold(appBar: AppBar(), body: ErrorView(error!, load));
    }
    if (item == null) {
      return Scaffold(appBar: AppBar(), body: const LoadingView());
    }
    final r = item!;
    final auth = context.read<AuthService>().user!;
    final professional = auth.isProfessional;
    return Scaffold(
      appBar: AppBar(title: Text('Rezervacija #${r.id}')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(18),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          reservationStatus(r.status),
                          style: Theme.of(context).textTheme.headlineMedium,
                        ),
                      ),
                      Checkbox(value: r.isPaid, onChanged: null),
                    ],
                  ),
                  Text(
                    professional
                        ? 'Klijent: ${r.clientName}'
                        : 'Profesionalac: ${r.professionalName}',
                  ),
                  const SizedBox(height: 10),
                  Text(r.description),
                  Text('Kategorija: ${r.categoryName}'),
                  const Divider(height: 28),
                  Text('Termin: ${dateTime.format(r.scheduledAt)}'),
                  Text('Trajanje: ${r.duration} minuta'),
                  Text('Cijena: ${money.format(r.price)} EUR'),
                  if (r.reason != null) Text('Razlog: ${r.reason}'),
                ],
              ),
            ),
          ),
          const SizedBox(height: 12),
          FilledButton.tonalIcon(
            onPressed: () =>
                Navigator.pushNamed(context, AppRoutes.chat, arguments: r.id),
            icon: const Icon(Icons.chat_bubble_outline),
            label: const Text('Otvori chat'),
          ),
          const SizedBox(height: 8),
          ..._actions(r, professional),
          if (r.reviewId != null) ...[
            const SizedBox(height: 16),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Recenzija: ${r.reviewRating} / 5',
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                    const SizedBox(height: 6),
                    Text(r.reviewComment ?? ''),
                    if (r.reviewCreatedAt != null)
                      Text(
                        dateTime.format(r.reviewCreatedAt!),
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                  ],
                ),
              ),
            ),
          ],
          if (r.statusHistory.isNotEmpty) ...[
            const SizedBox(height: 16),
            Text(
              'Tok rezervacije',
              style: Theme.of(context).textTheme.titleLarge,
            ),
            for (final history in r.statusHistory)
              ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.history),
                title: Text(reservationStatus(history.newStatus)),
                subtitle: Text(
                  '${dateTime.format(history.changedAt)}${history.reason == null ? '' : '\nRazlog: ${history.reason}'}',
                ),
              ),
          ],
          const SizedBox(height: 80),
        ],
      ),
    );
  }

  List<Widget> _actions(Reservation r, bool professional) {
    final actions = <Widget>[];
    if (professional && r.status == 1) {
      actions.add(_action('Prihvati rezervaciju', 'accept', Icons.check));
    }
    if (professional && r.status == 2) {
      actions.add(_action('Započni rad', 'start', Icons.play_arrow));
    }
    if (professional && r.status == 3) {
      actions.add(_action('Označi završeno', 'complete', Icons.task_alt));
    }
    if (!professional && r.status == 4 && !r.isPaid) {
      actions.add(
        Padding(
          padding: const EdgeInsets.only(top: 8),
          child: FilledButton.icon(
            onPressed: () async {
              await Navigator.pushNamed(
                context,
                AppRoutes.payment,
                arguments: r.id,
              );
              if (mounted) load();
            },
            icon: const Icon(Icons.paypal),
            label: const Text('Plati putem PayPal-a'),
          ),
        ),
      );
    }
    if (!professional && r.status == 4 && r.reviewId == null) {
      actions.add(
        Padding(
          padding: const EdgeInsets.only(top: 8),
          child: FilledButton.tonalIcon(
            onPressed: _review,
            icon: const Icon(Icons.star_outline),
            label: const Text('Ostavi recenziju'),
          ),
        ),
      );
    }
    final canCancel = r.status == 1 || (!professional && r.status == 2);
    if (canCancel) {
      actions.add(
        Padding(
          padding: const EdgeInsets.only(top: 8),
          child: OutlinedButton.icon(
            onPressed: busy ? null : _cancel,
            icon: const Icon(Icons.cancel_outlined),
            label: const Text('Otkaži'),
          ),
        ),
      );
    }
    return actions;
  }

  Widget _action(String label, String action, IconData icon) => Padding(
    padding: const EdgeInsets.only(top: 8),
    child: FilledButton.icon(
      onPressed: busy ? null : () => _transition(action),
      icon: Icon(icon),
      label: Text(label),
    ),
  );
  Future<void> _transition(String action, {String? reason}) async {
    setState(() => busy = true);
    try {
      item = await context.read<MobileRepository>().transitionReservation(
        widget.reservationId,
        action,
        reason: reason,
      );
    } catch (e) {
      if (mounted) await showFailure(context, e);
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> _cancel() async {
    final controller = TextEditingController();
    final reason = await showDialog<String>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Razlog otkazivanja'),
        content: TextField(
          controller: controller,
          maxLines: 3,
          decoration: const InputDecoration(labelText: 'Razlog'),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Odustani'),
          ),
          FilledButton(
            onPressed: () {
              if (controller.text.trim().isNotEmpty) {
                Navigator.pop(context, controller.text.trim());
              }
            },
            child: const Text('Otkaži rezervaciju'),
          ),
        ],
      ),
    );
    controller.dispose();
    if (reason != null) await _transition('cancel', reason: reason);
  }

  Future<void> _review() async {
    final repository = context.read<MobileRepository>();
    final value = await showDialog<(int, String)>(
      context: context,
      builder: (_) => const _ReviewDialog(),
    );
    if (value == null) return;
    try {
      await repository.createReview(widget.reservationId, value.$1, value.$2);
      await load();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Recenzija je objavljena.')),
        );
      }
    } catch (e) {
      if (mounted) await showFailure(context, e);
    }
  }
}

class _ReviewDialog extends StatefulWidget {
  const _ReviewDialog();
  @override
  State<_ReviewDialog> createState() => _ReviewDialogState();
}

class _ReviewDialogState extends State<_ReviewDialog> {
  int rating = 5;
  final comment = TextEditingController();
  final key = GlobalKey<FormState>();
  @override
  void dispose() {
    comment.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AlertDialog(
    title: const Text('Ocijenite profesionalca'),
    content: Form(
      key: key,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          DropdownButtonFormField<int>(
            initialValue: rating,
            decoration: const InputDecoration(labelText: 'Ocjena'),
            items: [1, 2, 3, 4, 5]
                .map(
                  (e) =>
                      DropdownMenuItem(value: e, child: Text('$e ${'★' * e}')),
                )
                .toList(),
            onChanged: (v) => setState(() => rating = v ?? rating),
          ),
          const SizedBox(height: 10),
          TextFormField(
            controller: comment,
            maxLines: 3,
            decoration: const InputDecoration(labelText: 'Komentar'),
            validator: (v) =>
                (v?.trim().isEmpty ?? true) ? 'Komentar je obavezan.' : null,
          ),
        ],
      ),
    ),
    actions: [
      TextButton(
        onPressed: () => Navigator.pop(context),
        child: const Text('Odustani'),
      ),
      FilledButton(
        onPressed: () {
          if (key.currentState!.validate()) {
            Navigator.pop(context, (rating, comment.text.trim()));
          }
        },
        child: const Text('Objavi'),
      ),
    ],
  );
}
