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
  List<CityRecord> _cities = const [];
  List<CategoryRecord> _categories = const [];

  @override
  void initState() {
    super.initState();
    _initialize();
  }

  Future<void> _initialize() async {
    try {
      final cities = await widget.repository.getCities();
      final categories = await widget.repository.getCategories();
      if (mounted) {
        setState(() {
          _cities = cities.items;
          _categories = categories.items;
        });
      }
      await _load();
    } catch (exception) {
      if (mounted) setState(() => _error = userError(exception));
    }
  }

  Future<void> _load([int? page]) async {
    setState(() {
      _page = page ?? _page;
      _result = null;
      _error = null;
    });
    try {
      final result = await widget.repository.getJobs(
        page: _page,
        cityId: _cityId,
        categoryId: _categoryId,
      );
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
                  ..._cities.map(
                    (city) => DropdownMenuItem(
                      value: city.id,
                      child: Text(city.name),
                    ),
                  ),
                ],
                onChanged: (value) {
                  setState(() => _cityId = value);
                  _load(1);
                },
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
                  ..._categories.map(
                    (category) => DropdownMenuItem(
                      value: category.id,
                      child: Text(category.name),
                    ),
                  ),
                ],
                onChanged: (value) {
                  setState(() => _categoryId = value);
                  _load(1);
                },
              ),
            ),
          ],
        ),
        const SizedBox(height: 14),
        if (_result!.items.isEmpty)
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
                      DataColumn(label: Text('Budžet (EUR)')),
                      DataColumn(label: Text('Status')),
                      DataColumn(label: Text('Kreiran')),
                    ],
                    rows: [
                      for (final job in _result!.items)
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
                            DataCell(
                              Text('${moneyFormat.format(job.budget)} EUR'),
                            ),
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
