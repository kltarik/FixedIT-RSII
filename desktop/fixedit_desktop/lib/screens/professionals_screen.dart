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
      final result = await widget.repository.getProfessionals(
        page: _page,
        cityId: _cityId,
        categoryId: _categoryId,
      );
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

  Future<void> _edit(ProfessionalRecord professional) async {
    final categories = await widget.repository.getCategories();
    if (!mounted || categories.items.isEmpty) return;
    final bio = TextEditingController(text: professional.bio);
    final hourlyRate = TextEditingController(
      text: professional.hourlyRate.toStringAsFixed(2),
    );
    final experience = TextEditingController(
      text: professional.experience.toString(),
    );
    final selected = professional.categories.map((item) => item.id).toSet();
    final saved = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: Text('Uredi profil: ${professional.name}'),
          content: SizedBox(
            width: 560,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  TextField(
                    controller: bio,
                    minLines: 3,
                    maxLines: 6,
                    decoration: const InputDecoration(labelText: 'Opis'),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: hourlyRate,
                    keyboardType: const TextInputType.numberWithOptions(
                      decimal: true,
                    ),
                    decoration: const InputDecoration(
                      labelText: 'Satnica (EUR)',
                    ),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: experience,
                    keyboardType: TextInputType.number,
                    decoration: const InputDecoration(
                      labelText: 'Godine iskustva',
                    ),
                  ),
                  const SizedBox(height: 14),
                  Text(
                    'Kategorije',
                    style: Theme.of(context).textTheme.titleSmall,
                  ),
                  for (final category in categories.items)
                    CheckboxListTile(
                      dense: true,
                      contentPadding: EdgeInsets.zero,
                      value: selected.contains(category.id),
                      title: Text(category.name),
                      onChanged: (checked) => setDialogState(() {
                        if (checked ?? false) {
                          selected.add(category.id);
                        } else {
                          selected.remove(category.id);
                        }
                      }),
                    ),
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialogContext, false),
              child: const Text('Odustani'),
            ),
            FilledButton(
              onPressed: () async {
                final rate = double.tryParse(
                  hourlyRate.text.replaceAll(',', '.'),
                );
                final years = int.tryParse(experience.text);
                if (rate == null || years == null || selected.isEmpty) {
                  await showApiErrorDialog(
                    dialogContext,
                    'Unesite ispravnu satnicu, iskustvo i najmanje jednu kategoriju.',
                  );
                  return;
                }
                try {
                  await widget.repository.updateProfessional(
                    id: professional.id,
                    bio: bio.text,
                    hourlyRate: rate,
                    yearsOfExperience: years,
                    categoryIds: selected.toList(),
                  );
                  if (dialogContext.mounted) {
                    Navigator.pop(dialogContext, true);
                  }
                } catch (error) {
                  if (dialogContext.mounted) {
                    await showApiErrorDialog(dialogContext, userError(error));
                  }
                }
              },
              child: const Text('Sačuvaj'),
            ),
          ],
        ),
      ),
    );
    bio.dispose();
    hourlyRate.dispose();
    experience.dispose();
    if (saved == true && mounted) await _load(_page);
  }

  @override
  Widget build(BuildContext context) {
    if (_error != null) return ErrorPanel(message: _error!, onRetry: _load);
    if (_result == null) {
      return const LoadingPanel(label: 'Učitavanje profesionalaca...');
    }
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
                      DataColumn(label: Text('Satnica (EUR)')),
                      DataColumn(label: Text('Ocjena')),
                      DataColumn(label: Text('Akcije')),
                    ],
                    rows: [
                      for (final professional in _result!.items)
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
                              Text(
                                '${moneyFormat.format(professional.hourlyRate)} EUR',
                              ),
                            ),
                            DataCell(
                              Text(professional.rating.toStringAsFixed(1)),
                            ),
                            DataCell(
                              Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  IconButton(
                                    tooltip: 'Pregled',
                                    icon: const Icon(Icons.visibility_outlined),
                                    onPressed: () => _showDetails(professional),
                                  ),
                                  IconButton(
                                    tooltip: 'Uredi',
                                    icon: const Icon(Icons.edit_outlined),
                                    onPressed: () => _edit(professional),
                                  ),
                                ],
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
            Text('Satnica: ${moneyFormat.format(professional.hourlyRate)} EUR'),
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
