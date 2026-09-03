import 'package:flutter/material.dart';

import '../app/routes.dart';
import '../app/theme.dart';
import '../core/models.dart';
import '../services/mobile_repository.dart';
import '../widgets/common.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key, required this.repository, required this.client});
  final MobileRepository repository;
  final bool client;
  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  HomeData? data;
  String? error;
  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    setState(() {
      data = null;
      error = null;
    });
    try {
      final value = await widget.repository.getHome(client: widget.client);
      if (mounted) setState(() => data = value);
    } catch (e) {
      if (mounted) setState(() => error = userError(e));
    }
  }

  @override
  Widget build(BuildContext context) {
    if (error != null) return ErrorView(error!, load);
    if (data == null) return const LoadingView();
    return RefreshIndicator(
      onRefresh: load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          ScreenTitle(
            widget.client
                ? 'Pronađi pravog majstora'
                : 'Poslovi u vašoj blizini',
            widget.client
                ? 'Preporuke prilagođene vašim prethodnim ocjenama.'
                : 'Novi oglasi i termini na jednom mjestu.',
          ),
          if (data!.professionals.isNotEmpty) ...[
            Text(
              widget.client
                  ? 'Preporučeni profesionalci'
                  : 'Istaknuti profesionalci',
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 10),
            SizedBox(
              height: 185,
              child: ListView.separated(
                scrollDirection: Axis.horizontal,
                itemCount: data!.professionals.length,
                separatorBuilder: (_, _) => const SizedBox(width: 10),
                itemBuilder: (_, i) =>
                    _ProfessionalCard(data!.professionals[i]),
              ),
            ),
            const SizedBox(height: 22),
          ],
          Text(
            'Najnoviji oglasi',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 10),
          if (data!.jobs.isEmpty)
            const EmptyView('Trenutno nema aktivnih oglasa.')
          else
            ...data!.jobs.map(
              (job) => Card(
                child: ListTile(
                  title: Text(job.title),
                  subtitle: Text('${job.categoryName} • ${job.cityName}'),
                  trailing: Text(
                    '${money.format(job.budget)} EUR',
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      color: midnight,
                    ),
                  ),
                ),
              ),
            ),
          const SizedBox(height: 90),
        ],
      ),
    );
  }
}

class _ProfessionalCard extends StatelessWidget {
  const _ProfessionalCard(this.item);
  final Professional item;
  @override
  Widget build(BuildContext context) => SizedBox(
    width: 230,
    child: Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(20),
        onTap: () => Navigator.pushNamed(
          context,
          AppRoutes.professional,
          arguments: item.id,
        ),
        child: Padding(
          padding: const EdgeInsets.all(15),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  NetworkAvatar(url: item.picture, name: item.name),
                  const Spacer(),
                  if (item.verified) const Icon(Icons.verified, color: mint),
                ],
              ),
              const SizedBox(height: 12),
              Text(item.name, style: Theme.of(context).textTheme.titleMedium),
              Text('${item.cityName} • ★ ${item.rating.toStringAsFixed(1)}'),
              const Spacer(),
              Text(
                '${money.format(item.hourlyRate)} EUR / h',
                style: const TextStyle(
                  fontWeight: FontWeight.bold,
                  color: midnight,
                ),
              ),
            ],
          ),
        ),
      ),
    ),
  );
}
