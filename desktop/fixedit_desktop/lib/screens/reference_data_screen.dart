import 'package:flutter/material.dart';

import '../core/models.dart';
import '../services/admin_repository.dart';
import '../widgets/common.dart';

class ReferenceDataScreen extends StatelessWidget {
  const ReferenceDataScreen({super.key, required this.repository});
  final AdminRepository repository;

  @override
  Widget build(BuildContext context) => DefaultTabController(
    length: 4,
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const PageHeading(
          title: 'Referentni podaci',
          subtitle:
              'Upravljanje državama, gradovima, kategorijama i nazivima statusa.',
        ),
        const TabBar(
          isScrollable: true,
          tabs: [
            Tab(text: 'Države'),
            Tab(text: 'Gradovi'),
            Tab(text: 'Kategorije'),
            Tab(text: 'Statusi rezervacija'),
          ],
        ),
        const SizedBox(height: 16),
        Expanded(
          child: TabBarView(
            children: [
              _CountriesTab(repository: repository),
              _CitiesTab(repository: repository),
              _CategoriesTab(repository: repository),
              _StatusesTab(repository: repository),
            ],
          ),
        ),
      ],
    ),
  );
}

class _CountriesTab extends StatefulWidget {
  const _CountriesTab({required this.repository});
  final AdminRepository repository;
  @override
  State<_CountriesTab> createState() => _CountriesTabState();
}

class _CountriesTabState extends State<_CountriesTab> {
  PagedResult<CountryRecord>? _result;
  String? _error;
  int _page = 1;

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
      final result = await widget.repository.getCountries(page: _page);
      if (mounted) setState(() => _result = result);
    } catch (error) {
      if (mounted) setState(() => _error = userError(error));
    }
  }

  Future<void> _edit([CountryRecord? country]) async {
    final name = TextEditingController(text: country?.name);
    final code = TextEditingController(text: country?.code);
    final saved = await _showForm(
      context,
      title: country == null ? 'Nova država' : 'Uredi državu',
      fields: [
        TextField(
          controller: name,
          decoration: const InputDecoration(labelText: 'Naziv'),
        ),
        TextField(
          controller: code,
          maxLength: 3,
          decoration: const InputDecoration(labelText: 'Oznaka (2-3 slova)'),
        ),
      ],
      onSave: () => widget.repository.saveCountry(
        id: country?.id,
        name: name.text,
        code: code.text,
      ),
    );
    name.dispose();
    code.dispose();
    if (saved && mounted) await _load(_page);
  }

  @override
  Widget build(BuildContext context) => _referenceTable(
    context: context,
    result: _result,
    error: _error,
    onRetry: _load,
    onAdd: _edit,
    onPage: _load,
    columns: const [
      DataColumn(label: Text('Naziv')),
      DataColumn(label: Text('Oznaka')),
      DataColumn(label: Text('Akcije')),
    ],
    rows:
        _result?.items
            .map(
              (country) => DataRow(
                cells: [
                  DataCell(Text(country.name)),
                  DataCell(Text(country.code)),
                  DataCell(
                    _actions(
                      onEdit: () => _edit(country),
                      onDelete: () => _deleteReference(
                        context,
                        'državu ${country.name}',
                        () => widget.repository.deleteCountry(country.id),
                        () => _load(_page),
                      ),
                    ),
                  ),
                ],
              ),
            )
            .toList() ??
        const [],
  );
}

class _CitiesTab extends StatefulWidget {
  const _CitiesTab({required this.repository});
  final AdminRepository repository;
  @override
  State<_CitiesTab> createState() => _CitiesTabState();
}

class _CitiesTabState extends State<_CitiesTab> {
  PagedResult<CityRecord>? _result;
  String? _error;
  int _page = 1;

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
      final result = await widget.repository.getCities(page: _page);
      if (mounted) setState(() => _result = result);
    } catch (error) {
      if (mounted) setState(() => _error = userError(error));
    }
  }

  Future<void> _edit([CityRecord? city]) async {
    final countries = await widget.repository.getCountries();
    if (!mounted) return;
    if (countries.items.isEmpty) {
      await showApiErrorDialog(context, 'Prvo je potrebno dodati državu.');
      return;
    }
    final name = TextEditingController(text: city?.name);
    var countryId = city?.countryId ?? countries.items.first.id;
    final saved = await _showForm(
      context,
      title: city == null ? 'Novi grad' : 'Uredi grad',
      fields: [
        TextField(
          controller: name,
          decoration: const InputDecoration(labelText: 'Naziv'),
        ),
        StatefulBuilder(
          builder: (context, setDialogState) => DropdownButtonFormField<int>(
            initialValue: countryId,
            decoration: const InputDecoration(labelText: 'Država'),
            items: countries.items
                .map(
                  (country) => DropdownMenuItem(
                    value: country.id,
                    child: Text(country.name),
                  ),
                )
                .toList(),
            onChanged: (value) =>
                setDialogState(() => countryId = value ?? countryId),
          ),
        ),
      ],
      onSave: () => widget.repository.saveCity(
        id: city?.id,
        name: name.text,
        countryId: countryId,
      ),
    );
    name.dispose();
    if (saved && mounted) await _load(_page);
  }

  @override
  Widget build(BuildContext context) => _referenceTable(
    context: context,
    result: _result,
    error: _error,
    onRetry: _load,
    onAdd: _edit,
    onPage: _load,
    columns: const [
      DataColumn(label: Text('Grad')),
      DataColumn(label: Text('Država')),
      DataColumn(label: Text('Akcije')),
    ],
    rows:
        _result?.items
            .map(
              (city) => DataRow(
                cells: [
                  DataCell(Text(city.name)),
                  DataCell(Text(city.countryName)),
                  DataCell(
                    _actions(
                      onEdit: () => _edit(city),
                      onDelete: () => _deleteReference(
                        context,
                        'grad ${city.name}',
                        () => widget.repository.deleteCity(city.id),
                        () => _load(_page),
                      ),
                    ),
                  ),
                ],
              ),
            )
            .toList() ??
        const [],
  );
}

class _CategoriesTab extends StatefulWidget {
  const _CategoriesTab({required this.repository});
  final AdminRepository repository;
  @override
  State<_CategoriesTab> createState() => _CategoriesTabState();
}

class _CategoriesTabState extends State<_CategoriesTab> {
  PagedResult<CategoryRecord>? _result;
  String? _error;
  int _page = 1;

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
      final result = await widget.repository.getCategories(page: _page);
      if (mounted) setState(() => _result = result);
    } catch (error) {
      if (mounted) setState(() => _error = userError(error));
    }
  }

  Future<void> _edit([CategoryRecord? category]) async {
    final name = TextEditingController(text: category?.name);
    final description = TextEditingController(text: category?.description);
    final iconUrl = TextEditingController(text: category?.iconUrl);
    final saved = await _showForm(
      context,
      title: category == null ? 'Nova kategorija' : 'Uredi kategoriju',
      fields: [
        TextField(
          controller: name,
          decoration: const InputDecoration(labelText: 'Naziv'),
        ),
        TextField(
          controller: description,
          minLines: 2,
          maxLines: 4,
          decoration: const InputDecoration(labelText: 'Opis'),
        ),
        TextField(
          controller: iconUrl,
          decoration: const InputDecoration(
            labelText: 'URL ikone (opcionalno)',
          ),
        ),
      ],
      onSave: () => widget.repository.saveCategory(
        id: category?.id,
        name: name.text,
        description: description.text,
        iconUrl: iconUrl.text,
      ),
    );
    name.dispose();
    description.dispose();
    iconUrl.dispose();
    if (saved && mounted) await _load(_page);
  }

  @override
  Widget build(BuildContext context) => _referenceTable(
    context: context,
    result: _result,
    error: _error,
    onRetry: _load,
    onAdd: _edit,
    onPage: _load,
    columns: const [
      DataColumn(label: Text('Naziv')),
      DataColumn(label: Text('Opis')),
      DataColumn(label: Text('Akcije')),
    ],
    rows:
        _result?.items
            .map(
              (category) => DataRow(
                cells: [
                  DataCell(Text(category.name)),
                  DataCell(
                    SizedBox(width: 420, child: Text(category.description)),
                  ),
                  DataCell(
                    _actions(
                      onEdit: () => _edit(category),
                      onDelete: () => _deleteReference(
                        context,
                        'kategoriju ${category.name}',
                        () => widget.repository.deleteCategory(category.id),
                        () => _load(_page),
                      ),
                    ),
                  ),
                ],
              ),
            )
            .toList() ??
        const [],
  );
}

class _StatusesTab extends StatefulWidget {
  const _StatusesTab({required this.repository});
  final AdminRepository repository;
  @override
  State<_StatusesTab> createState() => _StatusesTabState();
}

class _StatusesTabState extends State<_StatusesTab> {
  List<ReservationStatusRecord>? _items;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _items = null;
      _error = null;
    });
    try {
      final result = await widget.repository.getReservationStatuses();
      if (mounted) setState(() => _items = result);
    } catch (error) {
      if (mounted) setState(() => _error = userError(error));
    }
  }

  Future<void> _edit(ReservationStatusRecord status) async {
    final name = TextEditingController(text: status.name);
    final description = TextEditingController(text: status.description);
    final saved = await _showForm(
      context,
      title: 'Uredi status',
      fields: [
        TextField(
          controller: name,
          decoration: const InputDecoration(labelText: 'Naziv'),
        ),
        TextField(
          controller: description,
          minLines: 2,
          maxLines: 4,
          decoration: const InputDecoration(labelText: 'Opis'),
        ),
      ],
      onSave: () => widget.repository.updateReservationStatus(
        id: status.id,
        name: name.text,
        description: description.text,
      ),
    );
    name.dispose();
    description.dispose();
    if (saved && mounted) await _load();
  }

  @override
  Widget build(BuildContext context) {
    if (_error != null) return ErrorPanel(message: _error!, onRetry: _load);
    if (_items == null) return const LoadingPanel();
    return Card(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: scrollableTable(
          DataTable(
            columns: const [
              DataColumn(label: Text('Šifra')),
              DataColumn(label: Text('Naziv')),
              DataColumn(label: Text('Opis')),
              DataColumn(label: Text('Akcije')),
            ],
            rows: _items!
                .map(
                  (status) => DataRow(
                    cells: [
                      DataCell(Text('${status.id}')),
                      DataCell(Text(status.name)),
                      DataCell(
                        SizedBox(width: 460, child: Text(status.description)),
                      ),
                      DataCell(
                        IconButton(
                          tooltip: 'Uredi',
                          onPressed: () => _edit(status),
                          icon: const Icon(Icons.edit_outlined),
                        ),
                      ),
                    ],
                  ),
                )
                .toList(),
          ),
        ),
      ),
    );
  }
}

Widget _referenceTable<T>({
  required BuildContext context,
  required PagedResult<T>? result,
  required String? error,
  required VoidCallback onRetry,
  required VoidCallback onAdd,
  required ValueChanged<int> onPage,
  required List<DataColumn> columns,
  required List<DataRow> rows,
}) {
  if (error != null) return ErrorPanel(message: error, onRetry: onRetry);
  if (result == null) return const LoadingPanel();
  return Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      Align(
        alignment: Alignment.centerRight,
        child: FilledButton.icon(
          onPressed: onAdd,
          icon: const Icon(Icons.add),
          label: const Text('Dodaj'),
        ),
      ),
      const SizedBox(height: 12),
      Expanded(
        child: result.items.isEmpty
            ? const EmptyPanel(message: 'Nema zapisa.')
            : Card(
                child: SingleChildScrollView(
                  padding: const EdgeInsets.all(16),
                  child: scrollableTable(
                    DataTable(columns: columns, rows: rows),
                  ),
                ),
              ),
      ),
      PagedFooter(
        page: result.page,
        pageCount: result.pageCount,
        total: result.total,
        onPage: onPage,
      ),
    ],
  );
}

Widget _actions({
  required VoidCallback onEdit,
  required VoidCallback onDelete,
}) => Row(
  mainAxisSize: MainAxisSize.min,
  children: [
    IconButton(
      tooltip: 'Uredi',
      onPressed: onEdit,
      icon: const Icon(Icons.edit_outlined),
    ),
    IconButton(
      tooltip: 'Izbriši',
      onPressed: onDelete,
      icon: const Icon(Icons.delete_outline),
    ),
  ],
);

Future<bool> _showForm(
  BuildContext context, {
  required String title,
  required List<Widget> fields,
  required Future<Object> Function() onSave,
}) async {
  var busy = false;
  final saved = await showDialog<bool>(
    context: context,
    barrierDismissible: false,
    builder: (dialogContext) => StatefulBuilder(
      builder: (context, setState) => AlertDialog(
        title: Text(title),
        content: SizedBox(
          width: 480,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              for (final field in fields)
                Padding(
                  padding: const EdgeInsets.only(bottom: 14),
                  child: field,
                ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: busy ? null : () => Navigator.pop(dialogContext, false),
            child: const Text('Odustani'),
          ),
          FilledButton(
            onPressed: busy
                ? null
                : () async {
                    setState(() => busy = true);
                    try {
                      await onSave();
                      if (dialogContext.mounted) {
                        Navigator.pop(dialogContext, true);
                      }
                    } catch (error) {
                      setState(() => busy = false);
                      if (dialogContext.mounted) {
                        await showApiErrorDialog(
                          dialogContext,
                          userError(error),
                        );
                      }
                    }
                  },
            child: busy
                ? const SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : const Text('Sačuvaj'),
          ),
        ],
      ),
    ),
  );
  return saved == true;
}

Future<void> _deleteReference(
  BuildContext context,
  String label,
  Future<void> Function() delete,
  Future<void> Function() reload,
) async {
  final confirmed = await showDialog<bool>(
    context: context,
    builder: (context) => AlertDialog(
      title: const Text('Potvrda brisanja'),
      content: Text('Da li želite trajno izbrisati $label?'),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context, false),
          child: const Text('Odustani'),
        ),
        FilledButton(
          onPressed: () => Navigator.pop(context, true),
          child: const Text('Izbriši'),
        ),
      ],
    ),
  );
  if (confirmed != true) return;
  try {
    await delete();
    await reload();
  } catch (error) {
    if (context.mounted) await showApiErrorDialog(context, userError(error));
  }
}
