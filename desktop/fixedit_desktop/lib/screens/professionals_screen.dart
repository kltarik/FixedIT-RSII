import 'package:flutter/material.dart';

import '../core/models.dart';
import '../services/admin_repository.dart';
import '../widgets/common.dart';

class ProfessionalsScreen extends StatefulWidget {
  const ProfessionalsScreen({super.key, required this.repository});
  final AdminRepository repository;
  @override
  State<ProfessionalsScreen> createState() => _ProfessionalsScreenState();
}

class _ProfessionalsScreenState extends State<ProfessionalsScreen> {
  PagedResult<ProfessionalRecord>? _result;
  String? _error;
  int _page = 1;
  int? _cityId;
  int? _categoryId;
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
      final result = await widget.repository.getProfessionals(page: _page);
      if (mounted) setState(() => _result = result);
    } catch (exception) {
      if (mounted) setState(() => _error = userError(exception));
    }
  }

  Future<void> _verify(ProfessionalRecord professional, bool verified) async {
    setState(() => _busyId = professional.id);
    try {
      final updated = await widget.repository.setProfessionalVerified(
        professional.id,
        verified,
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
      }
    } catch (exception) {
      if (mounted) await showApiErrorDialog(context, userError(exception));
    } finally {
      if (mounted) setState(() => _busyId = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_error != null) return ErrorPanel(message: _error!, onRetry: _load);
    if (_result == null) {
      return const LoadingPanel(label: 'Učitavanje profesionalaca...');
    }
    final cities = {
      for (final item in _result!.items) item.cityId: item.cityName,
    };
    final categories = {
      for (final item in _result!.items)
        for (final category in item.categories) category.id: category.name,
    };
    final filtered = _result!.items
        .where(
          (item) =>
              (_cityId == null || item.cityId == _cityId) &&
              (_categoryId == null ||
                  item.categories.any(
                    (category) => category.id == _categoryId,
                  )),
        )
        .toList();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeading(
          title: 'Profesionalci',
          subtitle: 'Pregled profila i administratorska verifikacija.',
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
            child: EmptyPanel(
              message: 'Nema profesionalaca za odabrane filtere.',
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
                      DataColumn(label: Text('Verifikovan')),
                      DataColumn(label: Text('Ime')),
                      DataColumn(label: Text('Grad')),
                      DataColumn(label: Text('Kategorije')),
                      DataColumn(label: Text('Satnica')),
                      DataColumn(label: Text('Ocjena')),
                      DataColumn(label: Text('Pregled')),
                    ],
                    rows: [
                      for (final professional in filtered)
                        DataRow(
                          cells: [
                            DataCell(
                              _busyId == professional.id
                                  ? const SizedBox(
                                      width: 20,
                                      height: 20,
                                      child: CircularProgressIndicator(
                                        strokeWidth: 2,
                                      ),
                                    )
                                  : Checkbox(
                                      value: professional.isVerified,
                                      onChanged: (value) => value == null
                                          ? null
                                          : _verify(professional, value),
                                    ),
                            ),
                            DataCell(Text(professional.name)),
                            DataCell(Text(professional.cityName)),
                            DataCell(
                              Text(
                                professional.categories
                                    .map((item) => item.name)
                                    .join(', '),
                              ),
                            ),
                            DataCell(
                              Text(moneyFormat.format(professional.hourlyRate)),
                            ),
                            DataCell(
                              Text(professional.rating.toStringAsFixed(1)),
                            ),
                            DataCell(
                              IconButton(
                                icon: const Icon(Icons.visibility_outlined),
                                onPressed: () => _showDetails(professional),
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

  Future<void> _showDetails(
    ProfessionalRecord professional,
  ) => showDialog<void>(
    context: context,
    builder: (context) => AlertDialog(
      title: Text(professional.name),
      content: SizedBox(
        width: 520,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(professional.bio),
            const SizedBox(height: 16),
            Text('Iskustvo: ${professional.experience} godina'),
            Text('Satnica: ${moneyFormat.format(professional.hourlyRate)}'),
            Text(
              'Kategorije: ${professional.categories.map((item) => item.name).join(', ')}',
            ),
          ],
        ),
      ),
      actions: [
        FilledButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Zatvori'),
        ),
      ],
    ),
  );
}
