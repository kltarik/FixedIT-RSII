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
  int? _categoryFilter;
  int? _cityFilter;
  DateTime? _fromFilter;
  DateTime? _toFilter;
  int? _busyId;
  List<ReservationStatusRecord> _statuses = const [];
  List<CategoryRecord> _categories = const [];
  List<CityRecord> _cities = const [];

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
      final values = await Future.wait<Object>([
        widget.repository.getReservations(
          page: _page,
          status: _statusFilter,
          categoryId: _categoryFilter,
          cityId: _cityFilter,
          from: _fromFilter,
          to: _toFilter,
        ),
        widget.repository.getReservationStatuses(),
        widget.repository.getAllCategories(),
        widget.repository.getAllCities(),
      ]);
      if (mounted) {
        setState(() {
          _result = values[0] as PagedResult<ReservationRecord>;
          _statuses = values[1] as List<ReservationStatusRecord>;
          _categories = (values[2] as PagedResult<CategoryRecord>).items;
          _cities = (values[3] as PagedResult<CityRecord>).items;
        });
      }
    } catch (exception) {
      if (mounted) setState(() => _error = userError(exception));
    }
  }

  Future<void> _changeStatus(ReservationRecord reservation) async {
    final allowedStatuses = _activeTransitions(reservation.status);
    if (allowedStatuses.isEmpty) return;
    final selection = await showDialog<_StatusSelection>(
      context: context,
      builder: (context) => _StatusDialog(
        reservation: reservation,
        statusNames: {for (final status in _statuses) status.id: status.name},
        allowedStatuses: allowedStatuses,
      ),
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

  List<int> _activeTransitions(int currentStatus) {
    return activeNextReservationStatuses(currentStatus, _statuses);
  }

  Future<void> _pickDate(bool start) async {
    final selected = await showDatePicker(
      context: context,
      initialDate: start
          ? (_fromFilter ?? DateTime.now())
          : (_toFilter ?? _fromFilter ?? DateTime.now()),
      firstDate: start ? DateTime(2020) : (_fromFilter ?? DateTime(2020)),
      lastDate: DateTime.now().add(const Duration(days: 3650)),
    );
    if (selected == null) return;
    setState(() {
      if (start) {
        _fromFilter = selected;
      } else {
        _toFilter = selected;
      }
    });
    await _load(1);
  }

  void _clearFilters() {
    setState(() {
      _statusFilter = null;
      _categoryFilter = null;
      _cityFilter = null;
      _fromFilter = null;
      _toFilter = null;
    });
    _load(1);
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
        Wrap(
          spacing: 12,
          runSpacing: 12,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: [
            SizedBox(
              width: 220,
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
                  for (final status in _statuses)
                    DropdownMenuItem(
                      value: status.id,
                      child: Text(status.name),
                    ),
                ],
                onChanged: (value) {
                  setState(() => _statusFilter = value);
                  _load(1);
                },
              ),
            ),
            SizedBox(
              width: 220,
              child: DropdownButtonFormField<int?>(
                initialValue: _categoryFilter,
                decoration: const InputDecoration(labelText: 'Kategorija'),
                items: [
                  const DropdownMenuItem(
                    value: null,
                    child: Text('Sve kategorije'),
                  ),
                  for (final category in _categories)
                    DropdownMenuItem(
                      value: category.id,
                      child: Text(category.name),
                    ),
                ],
                onChanged: (value) {
                  setState(() => _categoryFilter = value);
                  _load(1);
                },
              ),
            ),
            SizedBox(
              width: 220,
              child: DropdownButtonFormField<int?>(
                initialValue: _cityFilter,
                decoration: const InputDecoration(labelText: 'Lokacija'),
                items: [
                  const DropdownMenuItem(
                    value: null,
                    child: Text('Sve lokacije'),
                  ),
                  for (final city in _cities)
                    DropdownMenuItem(value: city.id, child: Text(city.name)),
                ],
                onChanged: (value) {
                  setState(() => _cityFilter = value);
                  _load(1);
                },
              ),
            ),
            OutlinedButton.icon(
              onPressed: () => _pickDate(true),
              icon: const Icon(Icons.date_range),
              label: Text(
                _fromFilter == null
                    ? 'Od datuma'
                    : 'Od ${dateFormat.format(_fromFilter!)}',
              ),
            ),
            OutlinedButton.icon(
              onPressed: () => _pickDate(false),
              icon: const Icon(Icons.event),
              label: Text(
                _toFilter == null
                    ? 'Do datuma'
                    : 'Do ${dateFormat.format(_toFilter!)}',
              ),
            ),
            TextButton.icon(
              onPressed: _clearFilters,
              icon: const Icon(Icons.filter_alt_off),
              label: const Text('Očisti filtere'),
            ),
          ],
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
                                child: Text(
                                  '${reservation.clientName}${reservation.clientIsActive ? '' : ' (neaktivan)'}',
                                ),
                              ),
                            ),
                            DataCell(
                              Text(
                                '${reservation.professionalName}${reservation.professionalIsActive ? '' : ' (neaktivan)'}',
                              ),
                            ),
                            DataCell(
                              Text(
                                dateTimeFormat.format(reservation.scheduledAt),
                              ),
                            ),
                            DataCell(
                              Text(
                                '${moneyFormat.format(reservation.totalPrice)} EUR',
                              ),
                            ),
                            DataCell(
                              Checkbox(
                                value: reservation.isPaid,
                                onChanged: null,
                              ),
                            ),
                            DataCell(
                              StatusPill(
                                label: reservation.statusName,
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
                                          _activeTransitions(
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
  const _StatusDialog({
    required this.reservation,
    required this.statusNames,
    required this.allowedStatuses,
  });
  final ReservationRecord reservation;
  final Map<int, String> statusNames;
  final List<int> allowedStatuses;
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
                items: widget.allowedStatuses
                    .map(
                      (status) => DropdownMenuItem(
                        value: status,
                        child: Text(
                          widget.statusNames[status] ??
                              reservationStatusName(status),
                        ),
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
