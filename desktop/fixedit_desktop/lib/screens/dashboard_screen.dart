import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';

import '../app/theme.dart';
import '../core/models.dart';
import '../services/admin_repository.dart';
import '../widgets/common.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key, required this.repository});
  final AdminRepository repository;

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  DashboardData? _data;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _data = null;
      _error = null;
    });
    try {
      final data = await widget.repository.getDashboard();
      if (mounted) setState(() => _data = data);
    } catch (exception) {
      if (mounted) setState(() => _error = userError(exception));
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_error != null) return ErrorPanel(message: _error!, onRetry: _load);
    if (_data == null) {
      return const LoadingPanel(
        label: 'Učitavanje administratorske analitike...',
      );
    }
    final stats = _data!.stats;
    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          PageHeading(
            title: 'Operativni pregled',
            subtitle:
                'Stanje platforme u jednom pogledu. Podaci su agregirani na serveru.',
            actions: [
              IconButton.filledTonal(
                onPressed: _load,
                tooltip: 'Osvježi',
                icon: const Icon(Icons.refresh),
              ),
            ],
          ),
          Wrap(
            spacing: 14,
            runSpacing: 14,
            children: [
              _MetricCard(
                label: 'Korisnici',
                value: '${stats.totalUsers}',
                detail: '${stats.activeUsers} aktivnih',
                icon: Icons.people_outline,
              ),
              _MetricCard(
                label: 'Profesionalci',
                value: '${stats.totalProfessionals}',
                detail: 'registrovanih profila',
                icon: Icons.verified_user_outlined,
              ),
              _MetricCard(
                label: 'Rezervacije',
                value: '${stats.totalReservations}',
                detail: '${stats.totalReviews} recenzija',
                icon: Icons.event_available_outlined,
              ),
              _MetricCard(
                label: 'Prihod',
                value:
                    '${moneyFormat.format(stats.totalRevenue)} ${stats.currency}',
                detail: '${stats.completedPayments} uplata',
                icon: Icons.payments_outlined,
                accent: coral,
              ),
            ],
          ),
          const SizedBox(height: 18),
          LayoutBuilder(
            builder: (context, constraints) {
              final narrow = constraints.maxWidth < 700;
              if (narrow) {
                return Column(
                  children: [
                    _RevenueChart(data: _data!.report.revenueByMonth),
                    const SizedBox(height: 18),
                    _StatusBreakdown(data: stats.reservationsByStatus),
                  ],
                );
              }
              return Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    flex: 3,
                    child: _RevenueChart(data: _data!.report.revenueByMonth),
                  ),
                  const SizedBox(width: 18),
                  Expanded(
                    flex: 2,
                    child: _StatusBreakdown(data: stats.reservationsByStatus),
                  ),
                ],
              );
            },
          ),
          const SizedBox(height: 12),
          Align(
            alignment: Alignment.centerRight,
            child: Text(
              'Generisano ${dateTimeFormat.format(stats.generatedAt)}',
              style: Theme.of(context).textTheme.bodySmall,
            ),
          ),
        ],
      ),
    );
  }
}

class _MetricCard extends StatelessWidget {
  const _MetricCard({
    required this.label,
    required this.value,
    required this.detail,
    required this.icon,
    this.accent = ink,
  });
  final String label;
  final String value;
  final String detail;
  final IconData icon;
  final Color accent;
  @override
  Widget build(BuildContext context) => SizedBox(
    width: 245,
    child: Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(icon, color: accent),
                const Spacer(),
                Text(
                  label,
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
              ],
            ),
            const SizedBox(height: 20),
            Text(value, style: Theme.of(context).textTheme.headlineMedium),
            const SizedBox(height: 4),
            Text(detail, style: Theme.of(context).textTheme.bodySmall),
          ],
        ),
      ),
    ),
  );
}

class _RevenueChart extends StatelessWidget {
  const _RevenueChart({required this.data});
  final List<RevenuePoint> data;
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(22),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Mjesečni prihod',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 20),
          SizedBox(
            height: 230,
            child: data.isEmpty
                ? const EmptyPanel(
                    message: 'Još nema završenih uplata.',
                    icon: Icons.bar_chart,
                  )
                : BarChart(
                    BarChartData(
                      borderData: FlBorderData(show: false),
                      gridData: const FlGridData(show: false),
                      titlesData: FlTitlesData(
                        topTitles: const AxisTitles(
                          sideTitles: SideTitles(showTitles: false),
                        ),
                        rightTitles: const AxisTitles(
                          sideTitles: SideTitles(showTitles: false),
                        ),
                        leftTitles: const AxisTitles(
                          sideTitles: SideTitles(showTitles: false),
                        ),
                        bottomTitles: AxisTitles(
                          sideTitles: SideTitles(
                            showTitles: true,
                            getTitlesWidget: (value, meta) {
                              final index = value.toInt();
                              if (index < 0 || index >= data.length) {
                                return const SizedBox.shrink();
                              }
                              final point = data[index];
                              return Padding(
                                padding: const EdgeInsets.only(top: 8),
                                child: Text(
                                  '${point.month}/${point.year % 100}',
                                ),
                              );
                            },
                          ),
                        ),
                      ),
                      barGroups: [
                        for (var index = 0; index < data.length; index++)
                          BarChartGroupData(
                            x: index,
                            barRods: [
                              BarChartRodData(
                                toY: data[index].revenue,
                                color: coral,
                                width: 20,
                                borderRadius: const BorderRadius.vertical(
                                  top: Radius.circular(6),
                                ),
                              ),
                            ],
                          ),
                      ],
                    ),
                  ),
          ),
        ],
      ),
    ),
  );
}

class _StatusBreakdown extends StatelessWidget {
  const _StatusBreakdown({required this.data});
  final List<NamedCount> data;
  @override
  Widget build(BuildContext context) => Card(
    child: Padding(
      padding: const EdgeInsets.all(22),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Statusi rezervacija',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 12),
          if (data.isEmpty)
            const EmptyPanel(message: 'Nema rezervacija.')
          else
            for (final item in data)
              ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.circle, size: 12, color: coral),
                title: Text(item.name),
                trailing: Text(
                  '${item.count}',
                  style: const TextStyle(fontWeight: FontWeight.w800),
                ),
              ),
        ],
      ),
    ),
  );
}
