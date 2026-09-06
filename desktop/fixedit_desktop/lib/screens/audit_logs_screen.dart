import 'package:flutter/material.dart';

import '../core/models.dart';
import '../services/admin_repository.dart';
import '../widgets/common.dart';

class AuditLogsScreen extends StatefulWidget {
  const AuditLogsScreen({super.key, required this.repository});
  final AdminRepository repository;
  @override
  State<AuditLogsScreen> createState() => _AuditLogsScreenState();
}

class _AuditLogsScreenState extends State<AuditLogsScreen> {
  final _formKey = GlobalKey<FormState>();
  final _actionController = TextEditingController();
  final _entityController = TextEditingController();
  PagedResult<AuditEntry>? _result;
  String? _error;
  int _page = 1;
  DateTime? _from;
  DateTime? _to;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _actionController.dispose();
    _entityController.dispose();
    super.dispose();
  }

  Future<void> _load([int? page]) async {
    if (_from != null && _to != null && _from!.isAfter(_to!)) {
      setState(
        () => _error = 'Početni datum ne može biti poslije završnog datuma.',
      );
      return;
    }
    setState(() {
      _page = page ?? _page;
      _result = null;
      _error = null;
    });
    try {
      final result = await widget.repository.getAuditLogs(
        page: _page,
        action: _actionController.text.trim(),
        entityType: _entityController.text.trim(),
        from: _from,
        to: _to == null
            ? null
            : DateTime(_to!.year, _to!.month, _to!.day, 23, 59, 59),
      );
      if (mounted) setState(() => _result = result);
    } catch (exception) {
      if (mounted) setState(() => _error = userError(exception));
    }
  }

  void _clear() {
    _actionController.clear();
    _entityController.clear();
    setState(() {
      _from = null;
      _to = null;
      _page = 1;
    });
    _load(1);
  }

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      const PageHeading(
        title: 'Evidencija aktivnosti',
        subtitle:
            'Najnoviji zapisi su uvijek prvi; filteri se izvršavaju na serveru.',
      ),
      Form(
        key: _formKey,
        child: Wrap(
          spacing: 10,
          runSpacing: 10,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: [
            SizedBox(
              width: 190,
              child: TextFormField(
                controller: _actionController,
                maxLength: 100,
                decoration: const InputDecoration(
                  labelText: 'Akcija',
                  counterText: '',
                ),
              ),
            ),
            SizedBox(
              width: 190,
              child: TextFormField(
                controller: _entityController,
                maxLength: 100,
                decoration: const InputDecoration(
                  labelText: 'Tip entiteta',
                  counterText: '',
                ),
              ),
            ),
            DateFilterButton(
              label: 'Od',
              value: _from,
              onChanged: (value) => setState(() => _from = value),
            ),
            DateFilterButton(
              label: 'Do',
              value: _to,
              onChanged: (value) => setState(() => _to = value),
            ),
            FilledButton.icon(
              onPressed: () => _load(1),
              icon: const Icon(Icons.filter_alt_outlined),
              label: const Text('Primijeni'),
            ),
            TextButton(onPressed: _clear, child: const Text('Očisti')),
          ],
        ),
      ),
      const SizedBox(height: 14),
      if (_error != null)
        Expanded(
          child: ErrorPanel(message: _error!, onRetry: _load),
        )
      else if (_result == null)
        const Expanded(
          child: LoadingPanel(label: 'Učitavanje evidencije aktivnosti...'),
        )
      else if (_result!.items.isEmpty)
        const Expanded(
          child: EmptyPanel(
            message: 'Nema zapisa aktivnosti za odabrane filtere.',
          ),
        )
      else
        Expanded(
          child: Card(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: scrollableTable(
                DataTable(
                  columns: const [
                    DataColumn(label: Text('Vrijeme')),
                    DataColumn(label: Text('Akcija')),
                    DataColumn(label: Text('Entitet')),
                    DataColumn(label: Text('ID')),
                    DataColumn(label: Text('Korisnik')),
                    DataColumn(label: Text('IP adresa')),
                    DataColumn(label: Text('Detalji')),
                  ],
                  rows: [
                    for (final entry in _result!.items)
                      DataRow(
                        cells: [
                          DataCell(
                            Text(dateTimeFormat.format(entry.createdAt)),
                          ),
                          DataCell(Text(auditActionName(entry.action))),
                          DataCell(Text(auditEntityName(entry.entityType))),
                          DataCell(Text(entry.entityId)),
                          DataCell(
                            SizedBox(
                              width: 120,
                              child: Text(
                                entry.userId,
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                          ),
                          DataCell(Text(entry.ipAddress)),
                          DataCell(
                            Tooltip(
                              message: auditDetailsText(entry.details),
                              child: SizedBox(
                                width: 220,
                                child: Text(
                                  auditDetailsText(entry.details),
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ),
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
      if (_result != null)
        PagedFooter(
          page: _result!.page,
          pageCount: _result!.pageCount,
          total: _result!.total,
          onPage: _load,
        ),
    ],
  );
}
