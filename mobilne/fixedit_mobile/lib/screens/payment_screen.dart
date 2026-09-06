import 'package:flutter/material.dart';
import 'package:flutter_inappwebview/flutter_inappwebview.dart';
import 'package:provider/provider.dart';

import '../core/models.dart';
import '../core/payment_redirect.dart';
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
      if (mounted) {
        setState(() {
          order = value;
          error = null;
        });
      }
    } catch (exception) {
      if (mounted) setState(() => error = userError(exception));
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  bool _isRedirect(WebUri? url, String segment) {
    if (url == null) return false;
    return isPayPalRedirectPath(url.path, segment);
  }

  Future<bool> _processRedirect(
    InAppWebViewController controller,
    WebUri? url,
  ) async {
    if (_isRedirect(url, 'cancelled')) {
      await controller.stopLoading();
      if (mounted) Navigator.pop(context, false);
      return true;
    }

    if (_isRedirect(url, 'success')) {
      await controller.stopLoading();
      await capture(url);
      return true;
    }

    return false;
  }

  Future<NavigationActionPolicy> _handleNavigation(
    InAppWebViewController controller,
    NavigationAction action,
  ) async {
    final url = action.request.url;
    if (await _processRedirect(controller, url)) {
      return NavigationActionPolicy.CANCEL;
    }

    return NavigationActionPolicy.ALLOW;
  }

  Future<void> capture(WebUri? returnUrl) async {
    if (capturing || order == null) return;
    final returnedOrderId = returnUrl?.queryParameters['token'];
    if (returnedOrderId != null && returnedOrderId != order!.orderId) {
      setState(() => error = 'PayPal je vratio neočekivani nalog za plaćanje.');
      return;
    }

    setState(() {
      capturing = true;
      error = null;
    });
    try {
      await context.read<MobileRepository>().capturePayment(order!.orderId);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Plaćanje je uspješno potvrđeno.')),
        );
        Navigator.pop(context, true);
      }
    } catch (exception) {
      if (mounted) {
        setState(() {
          error = userError(exception);
          capturing = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('PayPal plaćanje')),
    body: loading
        ? const LoadingView()
        : error != null && order == null
        ? ErrorView(error!, create)
        : Column(
            children: [
              if (capturing) const LinearProgressIndicator(),
              if (error != null)
                MaterialBanner(
                  content: Text(error!),
                  actions: [
                    TextButton(
                      onPressed: () => setState(() => error = null),
                      child: const Text('Zatvori'),
                    ),
                  ],
                ),
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 10, 16, 10),
                child: Text(
                  '${money.format(order!.amount)} ${order!.currency}',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
              ),
              Expanded(
                child: IgnorePointer(
                  ignoring: capturing,
                  child: InAppWebView(
                    initialUrlRequest: URLRequest(
                      url: WebUri(order!.approvalUrl),
                    ),
                    initialSettings: InAppWebViewSettings(
                      useShouldOverrideUrlLoading: true,
                      javaScriptEnabled: true,
                    ),
                    shouldOverrideUrlLoading: _handleNavigation,
                    onLoadStart: (controller, url) async {
                      await _processRedirect(controller, url);
                    },
                    onUpdateVisitedHistory: (controller, url, _) async {
                      await _processRedirect(controller, url);
                    },
                    onReceivedError: (_, request, webError) {
                      if (request.isForMainFrame == true && mounted) {
                        setState(() {
                          error =
                              'PayPal stranica se nije mogla učitati: '
                              '${webError.description}';
                        });
                      }
                    },
                  ),
                ),
              ),
            ],
          ),
  );
}
