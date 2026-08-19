import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../core/models.dart';
import '../services/auth_service.dart';
import '../services/mobile_repository.dart';
import '../widgets/common.dart';

class JobsScreen extends StatefulWidget {
  const JobsScreen({super.key, required this.repository, required this.client});
  final MobileRepository repository;
  final bool client;
  @override
  State<JobsScreen> createState() => _JobsScreenState();
}

class _JobsScreenState extends State<JobsScreen> {
  Paged<JobPosting>? result;
  String? error;
  bool mine = false;
  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    setState(() {
      result = null;
      error = null;
    });
    try {
      final value = await widget.repository.getJobs(
        mine: widget.client && mine,
      );
      if (mounted) setState(() => result = value);
    } catch (e) {
      if (mounted) setState(() => error = userError(e));
    }
  }

  @override
  Widget build(BuildContext context) {
    if (error != null) return ErrorView(error!, load);
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            children: [
              ScreenTitle(
                'Oglasi',
                widget.client
                    ? 'Objavite posao i upravljajte ponudama.'
                    : 'Pronađite posao i pošaljite ponudu.',
                action: widget.client
                    ? IconButton.filled(
                        onPressed: _create,
                        icon: const Icon(Icons.add),
                      )
                    : null,
              ),
              if (widget.client)
                SegmentedButton<bool>(
                  segments: const [
                    ButtonSegment(value: false, label: Text('Svi')),
                    ButtonSegment(value: true, label: Text('Moji')),
                  ],
                  selected: {mine},
                  onSelectionChanged: (v) {
                    setState(() => mine = v.first);
                    load();
                  },
                ),
            ],
          ),
        ),
        Expanded(
          child: result == null
              ? const LoadingView()
              : result!.items.isEmpty
              ? const EmptyView('Nema oglasa.')
              : RefreshIndicator(
                  onRefresh: load,
                  child: ListView.builder(
                    padding: const EdgeInsets.fromLTRB(16, 0, 16, 90),
                    itemCount: result!.items.length,
                    itemBuilder: (_, i) {
                      final job = result!.items[i];
                      return Card(
                        child: InkWell(
                          borderRadius: BorderRadius.circular(20),
                          onTap: () => _open(job),
                          child: Padding(
                            padding: const EdgeInsets.all(16),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Row(
                                  children: [
                                    Expanded(
                                      child: Text(
                                        job.title,
                                        style: Theme.of(
                                          context,
                                        ).textTheme.titleMedium,
                                      ),
                                    ),
                                    Chip(
                                      label: Text(
                                        job.status == 1
                                            ? 'Otvoren'
                                            : 'Zatvoren',
                                      ),
                                    ),
                                  ],
                                ),
                                Text(
                                  job.description,
                                  maxLines: 2,
                                  overflow: TextOverflow.ellipsis,
                                ),
                                const SizedBox(height: 10),
                                Text('${job.categoryName} • ${job.cityName}'),
                                Text(
                                  '${money.format(job.budget)} KM',
                                  style: const TextStyle(
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      );
                    },
                  ),
                ),
        ),
      ],
    );
  }

  Future<void> _create() async {
    final reference = await widget.repository.getReferenceData();
    if (!mounted) return;
    final input = await showModalBottomSheet<_JobInput>(
      context: context,
      isScrollControlled: true,
      builder: (_) => _JobForm(reference),
    );
    if (input == null) return;
    try {
      await widget.repository.createJob(
        title: input.title,
        description: input.description,
        cityId: input.city,
        categoryId: input.category,
        budget: input.budget,
      );
      await load();
    } catch (e) {
      if (mounted) await showFailure(context, e);
    }
  }

  Future<void> _open(JobPosting job) async {
    final currentUserId = context.read<AuthService>().user?.id;
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => JobDetailScreen(
          repository: widget.repository,
          job: job,
          client: widget.client,
          owner: widget.client && job.clientId == currentUserId,
        ),
      ),
    );
    if (mounted) load();
  }
}

class _JobInput {
  const _JobInput(
    this.title,
    this.description,
    this.city,
    this.category,
    this.budget,
  );
  final String title;
  final String description;
  final int city;
  final int category;
  final double budget;
}

class _JobForm extends StatefulWidget {
  const _JobForm(this.reference);
  final ReferenceData reference;
  @override
  State<_JobForm> createState() => _JobFormState();
}

class _JobFormState extends State<_JobForm> {
  final key = GlobalKey<FormState>();
  final title = TextEditingController();
  final description = TextEditingController();
  final budget = TextEditingController();
  int? city;
  int? category;
  @override
  void dispose() {
    title.dispose();
    description.dispose();
    budget.dispose();
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
    child: SingleChildScrollView(
      child: Form(
        key: key,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text('Objavi oglas', style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 12),
            TextFormField(
              controller: title,
              decoration: const InputDecoration(labelText: 'Naslov'),
              validator: _required,
            ),
            const SizedBox(height: 10),
            TextFormField(
              controller: description,
              maxLines: 3,
              decoration: const InputDecoration(labelText: 'Opis'),
              validator: _required,
            ),
            const SizedBox(height: 10),
            DropdownButtonFormField<int>(
              initialValue: city,
              decoration: const InputDecoration(labelText: 'Grad'),
              items: widget.reference.cities
                  .map(
                    (e) => DropdownMenuItem(value: e.id, child: Text(e.name)),
                  )
                  .toList(),
              onChanged: (v) => city = v,
              validator: (v) => v == null ? 'Odaberite grad.' : null,
            ),
            const SizedBox(height: 10),
            DropdownButtonFormField<int>(
              initialValue: category,
              decoration: const InputDecoration(labelText: 'Kategorija'),
              items: widget.reference.categories
                  .map(
                    (e) => DropdownMenuItem(value: e.id, child: Text(e.name)),
                  )
                  .toList(),
              onChanged: (v) => category = v,
              validator: (v) => v == null ? 'Odaberite kategoriju.' : null,
            ),
            const SizedBox(height: 10),
            TextFormField(
              controller: budget,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(labelText: 'Budžet (KM)'),
              validator: (v) => (double.tryParse(v ?? '') ?? 0) <= 0
                  ? 'Unesite pozitivan budžet.'
                  : null,
            ),
            const SizedBox(height: 16),
            FilledButton(
              onPressed: () {
                if (key.currentState!.validate()) {
                  Navigator.pop(
                    context,
                    _JobInput(
                      title.text.trim(),
                      description.text.trim(),
                      city!,
                      category!,
                      double.parse(budget.text),
                    ),
                  );
                }
              },
              child: const Text('Objavi'),
            ),
          ],
        ),
      ),
    ),
  );
}

String? _required(String? value) =>
    (value?.trim().isEmpty ?? true) ? 'Polje je obavezno.' : null;

class JobDetailScreen extends StatefulWidget {
  const JobDetailScreen({
    super.key,
    required this.repository,
    required this.job,
    required this.client,
    required this.owner,
  });
  final MobileRepository repository;
  final JobPosting job;
  final bool client;
  final bool owner;
  @override
  State<JobDetailScreen> createState() => _JobDetailScreenState();
}

class _JobDetailScreenState extends State<JobDetailScreen> {
  Paged<JobOffer>? offers;
  String? error;
  @override
  void initState() {
    super.initState();
    if (widget.owner) loadOffers();
  }

  Future<void> loadOffers() async {
    try {
      final value = await widget.repository.getOffers(widget.job.id);
      if (mounted) setState(() => offers = value);
    } catch (e) {
      if (mounted) setState(() => error = userError(e));
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: Text(widget.job.title)),
    floatingActionButton: !widget.client && widget.job.status == 1
        ? FloatingActionButton.extended(
            onPressed: _offer,
            icon: const Icon(Icons.send),
            label: const Text('Pošalji ponudu'),
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
                Text(
                  widget.job.title,
                  style: Theme.of(context).textTheme.headlineMedium,
                ),
                const SizedBox(height: 8),
                Text(widget.job.description),
                const SizedBox(height: 12),
                Text('${widget.job.categoryName} • ${widget.job.cityName}'),
                Text(
                  'Budžet: ${money.format(widget.job.budget)} KM',
                  style: const TextStyle(fontWeight: FontWeight.bold),
                ),
              ],
            ),
          ),
        ),
        if (widget.owner) ...[
          const SizedBox(height: 14),
          Text('Ponude', style: Theme.of(context).textTheme.titleLarge),
          if (error != null)
            Text(error!, style: const TextStyle(color: Colors.red))
          else if (offers == null)
            const LoadingView()
          else if (offers!.items.isEmpty)
            const EmptyView('Još nema ponuda.')
          else
            ...offers!.items.map(
              (offer) => Card(
                child: ListTile(
                  title: Text(
                    '${offer.professionalName} • ★ ${offer.rating.toStringAsFixed(1)}',
                  ),
                  subtitle: Text(
                    '${offer.message}\n${money.format(offer.price)} KM',
                  ),
                  isThreeLine: true,
                  trailing: offer.status == 1
                      ? PopupMenuButton<bool>(
                          onSelected: (accept) => _setOffer(offer, accept),
                          itemBuilder: (_) => const [
                            PopupMenuItem(value: true, child: Text('Prihvati')),
                            PopupMenuItem(value: false, child: Text('Odbij')),
                          ],
                        )
                      : Text(offer.status == 2 ? 'Prihvaćena' : 'Odbijena'),
                ),
              ),
            ),
        ],
        const SizedBox(height: 80),
      ],
    ),
  );
  Future<void> _offer() async {
    final input = await showDialog<(String, double)>(
      context: context,
      builder: (_) => const _OfferDialog(),
    );
    if (input == null) return;
    try {
      await widget.repository.submitOffer(widget.job.id, input.$1, input.$2);
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('Ponuda je poslana.')));
        Navigator.pop(context);
      }
    } catch (e) {
      if (mounted) await showFailure(context, e);
    }
  }

  Future<void> _setOffer(JobOffer offer, bool accept) async {
    try {
      await widget.repository.setOffer(widget.job.id, offer.id, accept);
      await loadOffers();
    } catch (e) {
      if (mounted) await showFailure(context, e);
    }
  }
}

class _OfferDialog extends StatefulWidget {
  const _OfferDialog();
  @override
  State<_OfferDialog> createState() => _OfferDialogState();
}

class _OfferDialogState extends State<_OfferDialog> {
  final key = GlobalKey<FormState>();
  final message = TextEditingController();
  final price = TextEditingController();
  @override
  void dispose() {
    message.dispose();
    price.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AlertDialog(
    title: const Text('Nova ponuda'),
    content: Form(
      key: key,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          TextFormField(
            controller: message,
            maxLines: 3,
            decoration: const InputDecoration(labelText: 'Poruka'),
            validator: _required,
          ),
          const SizedBox(height: 10),
          TextFormField(
            controller: price,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(labelText: 'Cijena'),
            validator: (v) => (double.tryParse(v ?? '') ?? 0) <= 0
                ? 'Cijena mora biti pozitivna.'
                : null,
          ),
        ],
      ),
    ),
    actions: [
      TextButton(
        onPressed: () => Navigator.pop(context),
        child: const Text('Odustani'),
      ),
      FilledButton(
        onPressed: () {
          if (key.currentState!.validate()) {
            Navigator.pop(context, (
              message.text.trim(),
              double.parse(price.text),
            ));
          }
        },
        child: const Text('Pošalji'),
      ),
    ],
  );
}
