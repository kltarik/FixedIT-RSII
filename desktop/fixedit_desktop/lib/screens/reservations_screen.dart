import 'package:flutter/material.dart';

import '../core/models.dart';
import '../services/admin_repository.dart';
import '../widgets/common.dart';

class ReservationsScreen extends StatefulWidget {
  const ReservationsScreen({super.key, required this.repository});
  final AdminRepository repository;
  @override
  State<ReservationsScreen> createState() => _ReservationsScreenState();
}

class _ReservationsScreenState extends State<ReservationsScreen> {
  PagedResult<ReservationRecord>? _result;
  String? _error;
  int _page = 1;
  int? _statusFilter;
  int? _busyId;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load([int? page]) async {
    setState(() {
      _page = page ?? _page;
      _result = null;
      _error = null;
    });
    try {
      final result = await widget.repository.getReservations(
        page: _page,
        status: _statusFilter,
      );
      if (mounted) setState(() => _result = result);
    } catch (exception) {
      if (mounted) setState(() => _error = userError(exception));
    }
  }

  Future<void> _changeStatus(ReservationRecord reservation) async {
    final selection = await showDialog<_StatusSelection>(
      context: context,
      builder: (context) => _StatusDialog(reservation: reservation),
    );
    if (selection == null) return;
    setState(() => _busyId = reservation.id);
    try {
      final updated = await widget.repository.setReservationStatus(
        reservation.id,
        selection.status,
        reason: selection.reason,
      );
      if (mounted) {
        setState(
          () => _result = PagedResult(
            items: _result!.items
                .map((item) => item.id == updated.id ? updated : item)
                .toList(),
            total: _result!.total,
            page: _result!.page,
            pageSize: _result!.pageSize,
          ),
        );
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Status rezervacije je ažuriran.')),
        );
      }
    } catch (exception) {
      if (mounted) {
        await showApiErrorDialog(
          context,
          userError(exception),
          title: 'Status nije promijenjen',
        );
      }
    } finally {
      if (mounted) setState(() => _busyId = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_error != null) return ErrorPanel(message: _error!, onRetry: _load);
    if (_result == null) {
      return const LoadingPanel(label: 'Učitavanje rezervacija...');
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeading(
          title: 'Rezervacije',
          subtitle:
              'Admin override poštuje obavezni state machine bez preskakanja koraka.',
          actions: [
            IconButton.filledTonal(
              onPressed: _load,
              icon: const Icon(Icons.refresh),
            ),
          ],
        ),
        SizedBox(
          width: 250,
          child: Align(
            alignment: Alignment.centerLeft,
            child: SizedBox(
              width: 250,
              child: DropdownButtonFormField<int?>(
                initialValue: _statusFilter,
                decoration: const InputDecoration(
                  labelText: 'Status rezervacije',
                ),
                items: [
                  const DropdownMenuItem(
                    value: null,
                    child: Text('Svi statusi'),
                  ),
                  for (var status = 1; status <= 5; status++)
                    DropdownMenuItem(
                      value: status,
                      child: Text(reservationStatusName(status)),
                    ),
                ],
                onChanged: (value) {
                  setState(() => _statusFilter = value);
                  _load(1);
                },
              ),
            ),
          ),
        ),
        const SizedBox(height: 14),
        if (_result!.items.isEmpty)
          const Expanded(
            child: EmptyPanel(message: 'Nema rezervacija za odabrani status.'),
          )
        else
          Expanded(
            child: Card(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(16),
                child: scrollableTable(
                  DataTable(
                    columns: const [
                      DataColumn(label: Text('ID')),
                      DataColumn(label: Text('Klijent')),
                      DataColumn(label: Text('Profesionalac')),
                      DataColumn(label: Text('Termin')),
                      DataColumn(label: Text('Cijena')),
                      DataColumn(label: Text('Plaćeno')),
                      DataColumn(label: Text('Status')),
                      DataColumn(label: Text('Akcija')),
                    ],
                    rows: [
                      for (final reservation in _result!.items)
                        DataRow(
                          cells: [
                            DataCell(Text('#${reservation.id}')),
                            DataCell(
                              Tooltip(
                                message: reservation.serviceDescription,
                                child: Text(reservation.clientName),
                              ),
                            ),
                            DataCell(Text(reservation.professionalName)),
                            DataCell(
                              Text(
                                dateTimeFormat.format(reservation.scheduledAt),
                              ),
                            ),
                            DataCell(
                              Text(moneyFormat.format(reservation.totalPrice)),
                            ),
                            DataCell(
                              Checkbox(
                                value: reservation.isPaid,
                                onChanged: null,
                              ),
                            ),
                            DataCell(
                              StatusPill(
                                label: reservationStatusName(
                                  reservation.status,
                                ),
                                positive: reservation.status != 5,
                              ),
                            ),
                            DataCell(
                              _busyId == reservation.id
                                  ? const SizedBox(
                                      width: 20,
                                      height: 20,
                                      child: CircularProgressIndicator(
                                        strokeWidth: 2,
                                      ),
                                    )
                                  : IconButton(
                                      tooltip: 'Promijeni status',
                                      onPressed:
                                          allowedNextReservationStatuses(
                                            reservation.status,
                                          ).isEmpty
                                          ? null
                                          : () => _changeStatus(reservation),
                                      icon: const Icon(Icons.swap_horiz),
                                    ),
                            ),
                          ],
                        ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        PagedFooter(
          page: _result!.page,
          pageCount: _result!.pageCount,
          total: _result!.total,
          onPage: _load,
        ),
      ],
    );
  }
}

class _StatusSelection {
  const _StatusSelection(this.status, this.reason);
  final int status;
  final String? reason;
}

class _StatusDialog extends StatefulWidget {
  const _StatusDialog({required this.reservation});
  final ReservationRecord reservation;
  @override
  State<_StatusDialog> createState() => _StatusDialogState();
}

class _StatusDialogState extends State<_StatusDialog> {
  final _formKey = GlobalKey<FormState>();
  final _reasonController = TextEditingController();
  int? _status;

  @override
  void dispose() {
    _reasonController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final allowed = allowedNextReservationStatuses(widget.reservation.status);
    return AlertDialog(
      title: Text('Rezervacija #${widget.reservation.id}'),
      content: SizedBox(
        width: 440,
        child: Form(
          key: _formKey,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              DropdownButtonFormField<int>(
                initialValue: _status,
                decoration: const InputDecoration(labelText: 'Novi status'),
                items: allowed
                    .map(
                      (status) => DropdownMenuItem(
                        value: status,
                        child: Text(reservationStatusName(status)),
                      ),
                    )
                    .toList(),
                onChanged: (value) => setState(() => _status = value),
                validator: (value) =>
                    value == null ? 'Odaberite novi status.' : null,
              ),
              if (_status == 5) ...[
                const SizedBox(height: 14),
                TextFormField(
                  controller: _reasonController,
                  maxLength: 500,
                  maxLines: 3,
                  decoration: const InputDecoration(
                    labelText: 'Razlog otkazivanja',
                  ),
                  validator: (value) => (value?.trim().isEmpty ?? true)
                      ? 'Razlog otkazivanja je obavezan.'
                      : null,
                ),
              ],
            ],
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Odustani'),
        ),
        FilledButton(
          onPressed: () {
            if (!_formKey.currentState!.validate()) return;
            Navigator.pop(
              context,
              _StatusSelection(
                _status!,
                _status == 5 ? _reasonController.text.trim() : null,
              ),
            );
          },
          child: const Text('Potvrdi prijelaz'),
        ),
      ],
    );
  }
}
