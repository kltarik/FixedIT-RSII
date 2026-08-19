import 'package:flutter/material.dart';

import '../core/models.dart';
import '../services/mobile_repository.dart';
import '../widgets/common.dart';

class EarningsScreen extends StatefulWidget {
  const EarningsScreen({super.key, required this.repository});
  final MobileRepository repository;

  @override
  State<EarningsScreen> createState() => _EarningsScreenState();
}

class _EarningsScreenState extends State<EarningsScreen> {
  EarningsSummary? data;
  String? error;

  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    try {
      final value = await widget.repository.getEarnings();
      if (mounted) setState(() => data = value);
    } catch (exception) {
      if (mounted) setState(() => error = userError(exception));
    }
  }

  @override
  Widget build(BuildContext context) {
    if (error != null) return ErrorView(error!, load);
    if (data == null) return const LoadingView();
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        const ScreenTitle(
          'Moja zarada',
          'Prihod od završenih i plaćenih rezervacija.',
        ),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              children: [
                const Icon(Icons.account_balance_wallet_outlined, size: 52),
                const SizedBox(height: 14),
                Text(
                  '${money.format(data!.totalRevenue)} ${data!.currency}',
                  style: Theme.of(context).textTheme.displaySmall,
                ),
                Text('${data!.paymentCount} završenih uplata'),
              ],
            ),
          ),
        ),
      ],
    );
  }
}
