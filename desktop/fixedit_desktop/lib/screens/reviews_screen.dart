import 'package:flutter/material.dart';

import '../core/models.dart';
import '../services/admin_repository.dart';
import '../widgets/common.dart';

class ReviewsScreen extends StatefulWidget {
  const ReviewsScreen({super.key, required this.repository});
  final AdminRepository repository;
  @override
  State<ReviewsScreen> createState() => _ReviewsScreenState();
}

class _ReviewsScreenState extends State<ReviewsScreen> {
  final _search = TextEditingController();
  PagedResult<AdminReviewRecord>? _result;
  String? _error;
  int _page = 1;
  int? _rating;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  Future<void> _load([int? page]) async {
    setState(() {
      _page = page ?? _page;
      _result = null;
      _error = null;
    });
    try {
      final result = await widget.repository.getReviews(
        page: _page,
        rating: _rating,
        search: _search.text.trim(),
      );
      if (mounted) setState(() => _result = result);
    } catch (error) {
      if (mounted) setState(() => _error = userError(error));
    }
  }

  Future<void> _delete(AdminReviewRecord review) async {
    final reasonController = TextEditingController();
    String? validationMessage;
    final reason = await showDialog<String>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: const Text('Ukloniti recenziju?'),
          content: SizedBox(
            width: 460,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Text(
                  'Recenzija će biti trajno uklonjena, a prosječna ocjena profesionalca ponovo izračunata.',
                ),
                const SizedBox(height: 16),
                TextField(
                  controller: reasonController,
                  maxLength: 2000,
                  minLines: 2,
                  maxLines: 4,
                  decoration: InputDecoration(
                    labelText: 'Razlog uklanjanja',
                    errorText: validationMessage,
                  ),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialogContext),
              child: const Text('Odustani'),
            ),
            FilledButton(
              onPressed: () {
                final value = reasonController.text.trim();
                if (value.isEmpty) {
                  setDialogState(
                    () => validationMessage = 'Unesite razlog uklanjanja.',
                  );
                  return;
                }
                Navigator.pop(dialogContext, value);
              },
              child: const Text('Ukloni'),
            ),
          ],
        ),
      ),
    );
    reasonController.dispose();
    if (reason == null) return;
    try {
      await widget.repository.deleteReview(review.id, reason);
      if (mounted) await _load(_page);
    } catch (error) {
      if (mounted) await showApiErrorDialog(context, userError(error));
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_error != null) return ErrorPanel(message: _error!, onRetry: _load);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeading(
          title: 'Recenzije',
          subtitle:
              'Administratorski pregled i uklanjanje neprimjerenih recenzija.',
          actions: [
            IconButton.filledTonal(
              onPressed: () => _load(1),
              icon: const Icon(Icons.refresh),
            ),
          ],
        ),
        Wrap(
          spacing: 12,
          runSpacing: 12,
          children: [
            SizedBox(
              width: 340,
              child: TextField(
                controller: _search,
                decoration: const InputDecoration(
                  labelText: 'Komentar ili ime',
                  suffixIcon: Icon(Icons.search),
                ),
                onSubmitted: (_) => _load(1),
              ),
            ),
            SizedBox(
              width: 190,
              child: DropdownButtonFormField<int?>(
                initialValue: _rating,
                decoration: const InputDecoration(labelText: 'Ocjena'),
                items: [
                  const DropdownMenuItem(
                    value: null,
                    child: Text('Sve ocjene'),
                  ),
                  for (var rating = 1; rating <= 5; rating++)
                    DropdownMenuItem(
                      value: rating,
                      child: Text('$rating zvjezdica'),
                    ),
                ],
                onChanged: (value) {
                  setState(() => _rating = value);
                  _load(1);
                },
              ),
            ),
          ],
        ),
        const SizedBox(height: 14),
        if (_result == null)
          const Expanded(child: LoadingPanel())
        else if (_result!.items.isEmpty)
          const Expanded(
            child: EmptyPanel(message: 'Nema recenzija za odabrane filtere.'),
          )
        else
          Expanded(
            child: Card(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(16),
                child: scrollableTable(
                  DataTable(
                    columns: const [
                      DataColumn(label: Text('Rezervacija')),
                      DataColumn(label: Text('Klijent')),
                      DataColumn(label: Text('Profesionalac')),
                      DataColumn(label: Text('Ocjena')),
                      DataColumn(label: Text('Komentar')),
                      DataColumn(label: Text('Datum')),
                      DataColumn(label: Text('Akcija')),
                    ],
                    rows: _result!.items
                        .map(
                          (review) => DataRow(
                            cells: [
                              DataCell(Text('#${review.reservationId}')),
                              DataCell(Text(review.clientName)),
                              DataCell(Text(review.professionalName)),
                              DataCell(Text('${review.rating} / 5')),
                              DataCell(
                                SizedBox(
                                  width: 360,
                                  child: Text(review.comment),
                                ),
                              ),
                              DataCell(
                                Text(dateTimeFormat.format(review.createdAt)),
                              ),
                              DataCell(
                                IconButton(
                                  tooltip: 'Ukloni',
                                  onPressed: () => _delete(review),
                                  icon: const Icon(Icons.delete_outline),
                                ),
                              ),
                            ],
                          ),
                        )
                        .toList(),
                  ),
                ),
              ),
            ),
          ),
        if (_result != null)
          PagedFooter(
            page: _result!.page,
            pageCount: _result!.pageCount,
            total: _result!.total,
            onPage: _load,
          ),
      ],
    );
  }
}
