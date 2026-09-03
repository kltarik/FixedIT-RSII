import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/models.dart';
import '../services/auth_service.dart';
import '../services/mobile_repository.dart';
import '../widgets/common.dart';

class ProfessionalDetailScreen extends StatefulWidget {
  const ProfessionalDetailScreen({super.key, required this.professionalId});
  final int professionalId;
  @override
  State<ProfessionalDetailScreen> createState() =>
      _ProfessionalDetailScreenState();
}

class _ProfessionalDetailScreenState extends State<ProfessionalDetailScreen> {
  Professional? professional;
  List<Review> reviews = const [];
  String? error;
  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    try {
      final repo = context.read<MobileRepository>();
      final values = await Future.wait<Object>([
        repo.getProfessional(widget.professionalId),
        repo.getReviews(widget.professionalId),
      ]);
      if (mounted) {
        setState(() {
          professional = values[0] as Professional;
          reviews = (values[1] as Paged<Review>).items;
        });
      }
    } catch (e) {
      if (mounted) setState(() => error = userError(e));
    }
  }

  @override
  Widget build(BuildContext context) {
    if (error != null) {
      return Scaffold(appBar: AppBar(), body: ErrorView(error!, load));
    }
    if (professional == null) {
      return Scaffold(appBar: AppBar(), body: const LoadingView());
    }
    final p = professional!;
    final client = context.read<AuthService>().user?.isClient == true;
    return Scaffold(
      appBar: AppBar(title: Text(p.name)),
      floatingActionButton: client
          ? FloatingActionButton.extended(
              onPressed: () => _reserve(p),
              icon: const Icon(Icons.calendar_month),
              label: const Text('Rezerviši'),
            )
          : null,
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(18),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      NetworkAvatar(url: p.picture, name: p.name, radius: 38),
                      const SizedBox(width: 14),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              p.name,
                              style: Theme.of(context).textTheme.headlineMedium,
                            ),
                            Text(
                              '${p.cityName} • ★ ${p.rating.toStringAsFixed(1)}',
                            ),
                            if (p.verified)
                              const Row(
                                children: [
                                  Icon(Icons.verified, size: 17),
                                  SizedBox(width: 4),
                                  Text('Verifikovan profil'),
                                ],
                              ),
                          ],
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),
                  Text(p.bio),
                  const SizedBox(height: 12),
                  Wrap(
                    spacing: 7,
                    children: p.categories
                        .map((e) => Chip(label: Text(e.name)))
                        .toList(),
                  ),
                  Text(
                    '${p.experience} god. iskustva • ${money.format(p.hourlyRate)} EUR/h',
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 14),
          Text('Portfolio', style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 8),
          if (p.portfolio.isEmpty)
            const EmptyView('Portfolio još nije dodan.')
          else
            SizedBox(
              height: 170,
              child: ListView.separated(
                scrollDirection: Axis.horizontal,
                itemCount: p.portfolio.length,
                separatorBuilder: (_, _) => const SizedBox(width: 8),
                itemBuilder: (_, i) {
                  final item = p.portfolio[i];
                  return SizedBox(
                    width: 210,
                    child: Card(
                      clipBehavior: Clip.antiAlias,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(
                            child: CachedNetworkImage(
                              imageUrl: resolveNetworkUrl(item.imageUrl) ?? '',
                              width: double.infinity,
                              fit: BoxFit.cover,
                              errorWidget: (_, _, _) =>
                                  const Icon(Icons.broken_image),
                            ),
                          ),
                          Padding(
                            padding: const EdgeInsets.all(9),
                            child: Text(item.title, maxLines: 1),
                          ),
                        ],
                      ),
                    ),
                  );
                },
              ),
            ),
          const SizedBox(height: 16),
          Text('Recenzije', style: Theme.of(context).textTheme.titleLarge),
          if (reviews.isEmpty)
            const EmptyView('Još nema recenzija.')
          else
            ...reviews.map(
              (r) => Card(
                child: ListTile(
                  title: Text('${r.clientName} • ${'★' * r.rating}'),
                  subtitle: Text(r.comment),
                  trailing: Text(shortDate.format(r.createdAt)),
                ),
              ),
            ),
          const SizedBox(height: 80),
        ],
      ),
    );
  }

  Future<void> _reserve(Professional p) async {
    final result = await showModalBottomSheet<_ReservationInput>(
      context: context,
      isScrollControlled: true,
      builder: (_) => _ReservationForm(
        repository: context.read<MobileRepository>(),
        professional: p,
      ),
    );
    if (result == null || !mounted) return;
    try {
      final created = await context.read<MobileRepository>().createReservation(
        p.id,
        result.categoryId,
        result.description,
        result.scheduledAt,
        result.duration,
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Rezervacija #${created.id} je kreirana.')),
        );
        Navigator.pop(context);
      }
    } catch (e) {
      if (mounted) await showFailure(context, e);
    }
  }
}

class _ReservationInput {
  const _ReservationInput(
    this.categoryId,
    this.description,
    this.scheduledAt,
    this.duration,
  );
  final int categoryId;
  final String description;
  final DateTime scheduledAt;
  final int duration;
}

class _ReservationForm extends StatefulWidget {
  const _ReservationForm({
    required this.repository,
    required this.professional,
  });
  final MobileRepository repository;
  final Professional professional;
  @override
  State<_ReservationForm> createState() => _ReservationFormState();
}

class _ReservationFormState extends State<_ReservationForm> {
  final key = GlobalKey<FormState>();
  final description = TextEditingController();
  DateTime? date;
  AvailableSlot? slot;
  List<AvailableSlot> slots = const [];
  late int categoryId;
  int duration = 60;
  bool loadingSlots = false;

  @override
  void initState() {
    super.initState();
    categoryId = widget.professional.categories.first.id;
  }

  Future<void> loadSlots() async {
    if (date == null) return;
    setState(() {
      loadingSlots = true;
      slots = const [];
      slot = null;
    });
    try {
      final result = await widget.repository.getAvailableSlots(
        professionalId: widget.professional.id,
        categoryId: categoryId,
        date: date!,
        durationMinutes: duration,
      );
      if (mounted) setState(() => slots = result);
    } catch (error) {
      if (mounted) await showFailure(context, error);
    } finally {
      if (mounted) setState(() => loadingSlots = false);
    }
  }

  @override
  void dispose() {
    description.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Padding(
    padding: EdgeInsets.fromLTRB(
      20,
      20,
      20,
      MediaQuery.viewInsetsOf(context).bottom + 24,
    ),
    child: Form(
      key: key,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            'Nova rezervacija',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 14),
          TextFormField(
            controller: description,
            maxLines: 3,
            decoration: const InputDecoration(labelText: 'Opis usluge'),
            validator: (v) =>
                (v?.trim().isEmpty ?? true) ? 'Opis je obavezan.' : null,
          ),
          const SizedBox(height: 12),
          DropdownButtonFormField<int>(
            initialValue: categoryId,
            decoration: const InputDecoration(labelText: 'Kategorija usluge'),
            items: widget.professional.categories
                .map(
                  (category) => DropdownMenuItem(
                    value: category.id,
                    child: Text(category.name),
                  ),
                )
                .toList(),
            onChanged: (value) {
              setState(() => categoryId = value ?? categoryId);
              loadSlots();
            },
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: () async {
                    final value = await showDatePicker(
                      context: context,
                      firstDate: DateTime.now(),
                      lastDate: DateTime.now().add(const Duration(days: 365)),
                      initialDate:
                          date ?? DateTime.now().add(const Duration(days: 1)),
                    );
                    if (value != null) {
                      setState(() => date = value);
                      await loadSlots();
                    }
                  },
                  icon: const Icon(Icons.calendar_month),
                  label: Text(date == null ? 'Datum' : shortDate.format(date!)),
                ),
              ),
            ],
          ),
          if (date == null)
            const Align(
              alignment: Alignment.centerLeft,
              child: Text(
                'Datum je obavezan.',
                style: TextStyle(color: Colors.red),
              ),
            ),
          const SizedBox(height: 12),
          DropdownButtonFormField<int>(
            initialValue: duration,
            decoration: const InputDecoration(labelText: 'Trajanje'),
            items: const [30, 60, 90, 120, 180]
                .map(
                  (e) => DropdownMenuItem(value: e, child: Text('$e minuta')),
                )
                .toList(),
            onChanged: (value) {
              setState(() => duration = value ?? duration);
              loadSlots();
            },
          ),
          const SizedBox(height: 10),
          if (loadingSlots)
            const LinearProgressIndicator()
          else if (date != null && slots.isEmpty)
            const Align(
              alignment: Alignment.centerLeft,
              child: Text('Nema slobodnih termina za odabrani datum.'),
            )
          else if (slots.isNotEmpty)
            DropdownButtonFormField<AvailableSlot>(
              initialValue: slot,
              decoration: const InputDecoration(labelText: 'Slobodan termin'),
              items: slots
                  .map(
                    (item) => DropdownMenuItem(
                      value: item,
                      child: Text(shortTime.format(item.start)),
                    ),
                  )
                  .toList(),
              onChanged: (value) => setState(() => slot = value),
              validator: (value) =>
                  value == null ? 'Odaberite slobodan termin.' : null,
            ),
          const SizedBox(height: 10),
          Text(
            'Procjena: ${money.format(widget.professional.hourlyRate * duration / 60)} EUR',
          ),
          const SizedBox(height: 16),
          FilledButton(
            onPressed: () {
              if (!key.currentState!.validate() ||
                  date == null ||
                  slot == null) {
                return;
              }
              Navigator.pop(
                context,
                _ReservationInput(
                  categoryId,
                  description.text.trim(),
                  slot!.start,
                  duration,
                ),
              );
            },
            child: const Text('Potvrdi rezervaciju'),
          ),
        ],
      ),
    ),
  );
}
