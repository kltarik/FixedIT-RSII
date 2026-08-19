import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';

import '../core/models.dart';
import '../services/mobile_repository.dart';
import '../widgets/common.dart';

class PaymentScreen extends StatefulWidget {
  const PaymentScreen({super.key, required this.reservationId});
  final int reservationId;
  @override
  State<PaymentScreen> createState() => _PaymentScreenState();
}

class _PaymentScreenState extends State<PaymentScreen> {
  PaymentOrder? order;
  String? error;
  bool loading = true;
  bool opened = false;
  bool capturing = false;
  @override
  void initState() {
    super.initState();
    create();
  }

  Future<void> create() async {
    try {
      final value = await context.read<MobileRepository>().createPaymentOrder(
        widget.reservationId,
      );
      if (mounted) setState(() => order = value);
    } catch (e) {
      if (mounted) setState(() => error = userError(e));
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> approve() async {
    final uri = Uri.tryParse(order!.approvalUrl);
    if (uri == null || uri.scheme != 'https') {
      setState(() => error = 'PayPal nije vratio sigurnu adresu za odobrenje.');
      return;
    }
    final launched = await launchUrl(uri, mode: LaunchMode.externalApplication);
    if (mounted) {
      setState(() {
        opened = launched;
        if (!launched) error = 'PayPal stranica se nije mogla otvoriti.';
      });
    }
  }

  Future<void> capture() async {
    setState(() => capturing = true);
    try {
      await context.read<MobileRepository>().capturePayment(order!.orderId);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Plaćanje je uspješno potvrđeno.')),
        );
        Navigator.pop(context, true);
      }
    } catch (e) {
      if (mounted) setState(() => error = userError(e));
    } finally {
      if (mounted) setState(() => capturing = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('PayPal plaćanje')),
    body: loading
        ? const LoadingView()
        : error != null && order == null
        ? ErrorView(error!, create)
        : Padding(
            padding: const EdgeInsets.all(22),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Icon(Icons.paypal, size: 72),
                const SizedBox(height: 20),
                Text(
                  '${money.format(order!.amount)} ${order!.currency}',
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.displaySmall,
                ),
                const SizedBox(height: 14),
                const Text(
                  'PayPal autorizacija se otvara u sigurnom pregledniku. FixedIT mobilna aplikacija nikada ne prima niti sadrži PayPal tajnu.',
                  textAlign: TextAlign.center,
                ),
                if (error != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 14),
                    child: Text(
                      error!,
                      style: const TextStyle(color: Colors.red),
                      textAlign: TextAlign.center,
                    ),
                  ),
                const Spacer(),
                FilledButton.icon(
                  onPressed: approve,
                  icon: const Icon(Icons.open_in_browser),
                  label: Text(
                    opened ? 'Ponovo otvori PayPal' : 'Otvori PayPal',
                  ),
                ),
                const SizedBox(height: 10),
                FilledButton.tonalIcon(
                  onPressed: !opened || capturing ? null : capture,
                  icon: const Icon(Icons.verified_outlined),
                  label: Text(
                    capturing
                        ? 'Potvrđivanje...'
                        : 'Platio/la sam, potvrdi uplatu',
                  ),
                ),
                const SizedBox(height: 12),
              ],
            ),
          ),
  );
}
