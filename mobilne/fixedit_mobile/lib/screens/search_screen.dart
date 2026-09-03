import 'package:flutter/material.dart';

import '../app/routes.dart';
import '../core/models.dart';
import '../services/mobile_repository.dart';
import '../widgets/common.dart';

class SearchScreen extends StatefulWidget {
  const SearchScreen({super.key, required this.repository});
  final MobileRepository repository;
  @override
  State<SearchScreen> createState() => _SearchScreenState();
}

class _SearchScreenState extends State<SearchScreen> {
  final name = TextEditingController();
  ReferenceData? reference;
  Paged<Professional>? result;
  String? error;
  int? city;
  int? category;
  double? rating;
  String sortBy = 'rating';
  @override
  void initState() {
    super.initState();
    loadInitial();
  }

  @override
  void dispose() {
    name.dispose();
    super.dispose();
  }

  Future<void> loadInitial() async {
    try {
      final values = await Future.wait<Object>([
        widget.repository.getReferenceData(),
        widget.repository.getProfessionals(),
      ]);
      if (mounted) {
        setState(() {
          reference = values[0] as ReferenceData;
          result = values[1] as Paged<Professional>;
        });
      }
    } catch (e) {
      if (mounted) setState(() => error = userError(e));
    }
  }

  Future<void> search() async {
    setState(() {
      result = null;
      error = null;
    });
    try {
      final value = await widget.repository.getProfessionals(
        name: name.text,
        cityId: city,
        categoryId: category,
        minRating: rating,
        sortBy: sortBy,
      );
      if (mounted) setState(() => result = value);
    } catch (e) {
      if (mounted) setState(() => error = userError(e));
    }
  }

  @override
  Widget build(BuildContext context) {
    if (error != null) return ErrorView(error!, loadInitial);
    if (reference == null) return const LoadingView();
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            children: [
              const ScreenTitle(
                'Pretraga profesionalaca',
                'Filtrirajte po imenu, gradu, kategoriji i ocjeni.',
              ),
              TextField(
                controller: name,
                decoration: const InputDecoration(
                  labelText: 'Ime',
                  prefixIcon: Icon(Icons.search),
                ),
              ),
              const SizedBox(height: 10),
              Row(
                children: [
                  Expanded(
                    child: DropdownButtonFormField<int?>(
                      initialValue: city,
                      decoration: const InputDecoration(labelText: 'Grad'),
                      items: [
                        const DropdownMenuItem(value: null, child: Text('Svi')),
                        ...reference!.cities.map(
                          (e) => DropdownMenuItem(
                            value: e.id,
                            child: Text(e.name),
                          ),
                        ),
                      ],
                      onChanged: (v) => setState(() => city = v),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: DropdownButtonFormField<int?>(
                      initialValue: category,
                      decoration: const InputDecoration(
                        labelText: 'Kategorija',
                      ),
                      items: [
                        const DropdownMenuItem(value: null, child: Text('Sve')),
                        ...reference!.categories.map(
                          (e) => DropdownMenuItem(
                            value: e.id,
                            child: Text(e.name),
                          ),
                        ),
                      ],
                      onChanged: (v) => setState(() => category = v),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              Row(
                children: [
                  Expanded(
                    child: DropdownButtonFormField<double?>(
                      initialValue: rating,
                      decoration: const InputDecoration(
                        labelText: 'Minimalna ocjena',
                      ),
                      items: const [
                        DropdownMenuItem(value: null, child: Text('Sve')),
                        DropdownMenuItem(value: 3, child: Text('3+')),
                        DropdownMenuItem(value: 4, child: Text('4+')),
                        DropdownMenuItem(value: 4.5, child: Text('4.5+')),
                      ],
                      onChanged: (v) => setState(() => rating = v),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: DropdownButtonFormField<String>(
                      initialValue: sortBy,
                      decoration: const InputDecoration(
                        labelText: 'Sortiranje',
                      ),
                      items: const [
                        DropdownMenuItem(
                          value: 'rating',
                          child: Text('Najbolje ocjene'),
                        ),
                        DropdownMenuItem(
                          value: 'completed',
                          child: Text('Najviše poslova'),
                        ),
                        DropdownMenuItem(
                          value: 'price',
                          child: Text('Najviša cijena'),
                        ),
                        DropdownMenuItem(value: 'name', child: Text('Ime')),
                      ],
                      onChanged: (value) {
                        if (value != null) setState(() => sortBy = value);
                      },
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              SizedBox(
                width: double.infinity,
                child: FilledButton.icon(
                  onPressed: search,
                  icon: const Icon(Icons.tune),
                  label: const Text('Traži'),
                ),
              ),
            ],
          ),
        ),
        Expanded(
          child: result == null
              ? const LoadingView()
              : result!.items.isEmpty
              ? const EmptyView('Nema profesionalaca za odabrane filtere.')
              : ListView.builder(
                  padding: const EdgeInsets.fromLTRB(16, 0, 16, 90),
                  itemCount: result!.items.length,
                  itemBuilder: (_, i) {
                    final p = result!.items[i];
                    return Card(
                      child: ListTile(
                        onTap: () => Navigator.pushNamed(
                          context,
                          AppRoutes.professional,
                          arguments: p.id,
                        ),
                        leading: NetworkAvatar(url: p.picture, name: p.name),
                        title: Row(
                          children: [
                            Expanded(child: Text(p.name)),
                            if (p.verified)
                              const Icon(Icons.verified, size: 18),
                          ],
                        ),
                        subtitle: Text(
                          '${p.categories.map((e) => e.name).join(', ')}\n${p.cityName} • ★ ${p.rating.toStringAsFixed(1)}',
                        ),
                        isThreeLine: true,
                        trailing: const Icon(Icons.chevron_right),
                      ),
                    );
                  },
                ),
        ),
      ],
    );
  }
}
