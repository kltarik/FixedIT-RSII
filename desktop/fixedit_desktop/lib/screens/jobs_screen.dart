import 'package:flutter/material.dart';

import '../core/models.dart';
import '../services/admin_repository.dart';
import '../widgets/common.dart';

class JobsScreen extends StatefulWidget {
  const JobsScreen({super.key, required this.repository});
  final AdminRepository repository;
  @override
  State<JobsScreen> createState() => _JobsScreenState();
}

class _JobsScreenState extends State<JobsScreen> {
  PagedResult<JobRecord>? _result;
  String? _error;
  int _page = 1;
  int? _cityId;
  int? _categoryId;

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
      final result = await widget.repository.getJobs(page: _page);
      if (mounted) setState(() => _result = result);
    } catch (exception) {
      if (mounted) setState(() => _error = userError(exception));
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_error != null) return ErrorPanel(message: _error!, onRetry: _load);
    if (_result == null) {
      return const LoadingPanel(label: 'Učitavanje oglasa...');
    }
    final cities = {
      for (final item in _result!.items) item.cityId: item.cityName,
    };
    final categories = {
      for (final item in _result!.items) item.categoryId: item.categoryName,
    };
    final filtered = _result!.items
        .where(
          (item) =>
              (_cityId == null || item.cityId == _cityId) &&
              (_categoryId == null || item.categoryId == _categoryId),
        )
        .toList();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeading(
          title: 'Oglasi za posao',
          subtitle: 'Administratorski pregled otvorenih i zatvorenih oglasa.',
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
          children: [
            SizedBox(
              width: 220,
              child: DropdownButtonFormField<int?>(
                initialValue: _cityId,
                decoration: const InputDecoration(labelText: 'Grad'),
                items: [
                  const DropdownMenuItem(
                    value: null,
                    child: Text('Svi gradovi'),
                  ),
                  ...cities.entries.map(
                    (entry) => DropdownMenuItem(
                      value: entry.key,
                      child: Text(entry.value),
                    ),
                  ),
                ],
                onChanged: (value) => setState(() => _cityId = value),
              ),
            ),
            SizedBox(
              width: 250,
              child: DropdownButtonFormField<int?>(
                initialValue: _categoryId,
                decoration: const InputDecoration(labelText: 'Kategorija'),
                items: [
                  const DropdownMenuItem(
                    value: null,
                    child: Text('Sve kategorije'),
                  ),
                  ...categories.entries.map(
                    (entry) => DropdownMenuItem(
                      value: entry.key,
                      child: Text(entry.value),
                    ),
                  ),
                ],
                onChanged: (value) => setState(() => _categoryId = value),
              ),
            ),
          ],
        ),
        const SizedBox(height: 14),
        if (filtered.isEmpty)
          const Expanded(
            child: EmptyPanel(message: 'Nema oglasa za odabrane filtere.'),
          )
        else
          Expanded(
            child: Card(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(16),
                child: scrollableTable(
                  DataTable(
                    columns: const [
                      DataColumn(label: Text('Naslov')),
                      DataColumn(label: Text('Klijent')),
                      DataColumn(label: Text('Kategorija')),
                      DataColumn(label: Text('Grad')),
                      DataColumn(label: Text('Budžet')),
                      DataColumn(label: Text('Status')),
                      DataColumn(label: Text('Kreiran')),
                    ],
                    rows: [
                      for (final job in filtered)
                        DataRow(
                          cells: [
                            DataCell(
                              Tooltip(
                                message: job.description,
                                child: Text(job.title),
                              ),
                            ),
                            DataCell(Text(job.clientName)),
                            DataCell(Text(job.categoryName)),
                            DataCell(Text(job.cityName)),
                            DataCell(Text(moneyFormat.format(job.budget))),
                            DataCell(
                              StatusPill(
                                label: job.status == 1 ? 'Otvoren' : 'Zatvoren',
                                positive: job.status == 1,
                              ),
                            ),
                            DataCell(Text(dateFormat.format(job.createdAt))),
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
