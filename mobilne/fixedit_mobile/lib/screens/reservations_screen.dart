import 'package:flutter/material.dart';

import '../app/routes.dart';
import '../core/models.dart';
import '../services/mobile_repository.dart';
import '../widgets/common.dart';

class ReservationsScreen extends StatefulWidget {
  const ReservationsScreen({
    super.key,
    required this.repository,
    required this.professional,
  });
  final MobileRepository repository;
  final bool professional;
  @override
  State<ReservationsScreen> createState() => _ReservationsScreenState();
}

class _ReservationsScreenState extends State<ReservationsScreen> {
  Paged<Reservation>? result;
  String? error;
  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load([int page = 1]) async {
    setState(() {
      result = null;
      error = null;
    });
    try {
      final value = await widget.repository.getReservations(page: page);
      if (mounted) setState(() => result = value);
    } catch (e) {
      if (mounted) setState(() => error = userError(e));
    }
  }

  @override
  Widget build(BuildContext context) {
    if (error != null) return ErrorView(error!, load);
    if (result == null) return const LoadingView();
    return RefreshIndicator(
      onRefresh: load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          ScreenTitle(
            widget.professional ? 'Moji termini' : 'Moje rezervacije',
            'Status, detalji, chat i plaćanje.',
          ),
          if (result!.items.isEmpty)
            const EmptyView('Nema rezervacija.')
          else
            ...result!.items.map(
              (r) => Card(
                child: ListTile(
                  onTap: () async {
                    await Navigator.pushNamed(
                      context,
                      AppRoutes.reservation,
                      arguments: r.id,
                    );
                    if (mounted) load();
                  },
                  leading: CircleAvatar(child: Text('#${r.id}')),
                  title: Text(
                    widget.professional ? r.clientName : r.professionalName,
                  ),
                  subtitle: Text(
                    '${dateTime.format(r.scheduledAt)}\n${r.description}',
                  ),
                  isThreeLine: true,
                  trailing: Chip(label: Text(r.statusName)),
                ),
              ),
            ),
          PageControls(
            page: result!.page,
            pageCount: result!.pageCount,
            onPageChanged: load,
          ),
          const SizedBox(height: 90),
        ],
      ),
    );
  }
}
