import 'dart:io';

import 'package:file_picker/file_picker.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';

import '../app/theme.dart';
import '../core/models.dart';
import '../services/admin_repository.dart';
import '../services/pdf_file_service.dart';
import '../widgets/common.dart';

class ReportsScreen extends StatefulWidget {
  const ReportsScreen({super.key, required this.repository});
  final AdminRepository repository;
  @override
  State<ReportsScreen> createState() => _ReportsScreenState();
}

class _ReportsScreenState extends State<ReportsScreen> {
  FinancialReport? _report;
  String? _error;
  DateTime? _from;
  DateTime? _to;
  int? _categoryId;
  int _page = 1;
  bool _exporting = false;
  final Map<int, String> _categoryOptions = {};

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load([int? page]) async {
    if (_from != null && _to != null && _from!.isAfter(_to!)) {
      setState(
        () => _error = 'Početni datum ne može biti poslije završnog datuma.',
      );
      return;
    }
    setState(() {
      _page = page ?? _page;
      _report = null;
      _error = null;
    });
    try {
      final report = await widget.repository.getFinancialReport(
        page: _page,
        from: _from,
        to: _to,
        categoryId: _categoryId,
      );
      for (final category in report.revenueByCategory) {
        _categoryOptions[category.categoryId] = category.categoryName;
      }
      if (mounted) setState(() => _report = report);
    } catch (exception) {
      if (mounted) setState(() => _error = userError(exception));
    }
  }

  Future<void> _exportPdf({
    required bool professionalPerformance,
    required bool print,
  }) async {
    setState(() => _exporting = true);
    try {
      final bytes = professionalPerformance
          ? await widget.repository.downloadProfessionalPerformanceReport(
              from: _from,
              to: _to,
              categoryId: _categoryId,
            )
          : await widget.repository.downloadFinancialReport(
              from: _from,
              to: _to,
              categoryId: _categoryId,
            );
      final reportName = professionalPerformance
          ? 'fixedit-uspjesnost-profesionalaca.pdf'
          : 'fixedit-finansijski-izvjestaj.pdf';
      if (print) {
        await _printPdf(bytes, reportName);
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('PDF je poslan na štampanje.')),
          );
        }
        return;
      }

      final path = await FilePicker.platform.saveFile(
        dialogTitle: 'Sačuvaj FixedIT PDF izvještaj',
        fileName: reportName,
        type: FileType.custom,
        allowedExtensions: const ['pdf'],
        lockParentWindow: true,
      );
      if (path == null) return;
      final savedPath = await savePdfFile(path, bytes);
      if (!mounted) return;
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('PDF je sačuvan: $savedPath')));
    } catch (exception) {
      if (mounted) {
        await showApiErrorDialog(
          context,
          userError(exception),
          title: 'PDF export nije uspio',
        );
      }
    } finally {
      if (mounted) setState(() => _exporting = false);
    }
  }

  Future<void> _printPdf(List<int> bytes, String fileName) async {
    final file = File('${Directory.systemTemp.path}\\$fileName');
    await file.writeAsBytes(bytes, flush: true);
    final escapedPath = file.path.replaceAll("'", "''");
    final result = await Process.run('powershell.exe', [
      '-NoProfile',
      '-NonInteractive',
      '-Command',
      "Start-Process -FilePath '$escapedPath' -Verb Print -WindowStyle Hidden",
    ]);
    if (result.exitCode != 0) {
      throw ProcessException(
        'powershell.exe',
        const [],
        result.stderr.toString(),
        result.exitCode,
      );
    }
  }

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      PageHeading(
        title: 'Finansijski izvještaji',
        subtitle:
            'Prihod, kategorije, završene rezervacije i server-side PDF dokument.',
        actions: [
          FilledButton.icon(
            onPressed: _report == null || _exporting
                ? null
                : () =>
                      _exportPdf(professionalPerformance: false, print: false),
            icon: _exporting
                ? const SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : const Icon(Icons.picture_as_pdf_outlined),
            label: const Text('Finansijski PDF'),
          ),
          FilledButton.tonalIcon(
            onPressed: _report == null || _exporting
                ? null
                : () => _exportPdf(professionalPerformance: true, print: false),
            icon: const Icon(Icons.groups_outlined),
            label: const Text('Uspješnost PDF'),
          ),
          IconButton.filledTonal(
            tooltip: 'Štampaj finansijski izvještaj',
            onPressed: _report == null || _exporting
                ? null
                : () => _exportPdf(professionalPerformance: false, print: true),
            icon: const Icon(Icons.print_outlined),
          ),
          IconButton.filledTonal(
            tooltip: 'Štampaj izvještaj uspješnosti',
            onPressed: _report == null || _exporting
                ? null
                : () => _exportPdf(professionalPerformance: true, print: true),
            icon: const Icon(Icons.print),
          ),
        ],
      ),
      Wrap(
        spacing: 10,
        runSpacing: 10,
        crossAxisAlignment: WrapCrossAlignment.center,
        children: [
          DateFilterButton(
            label: 'Od',
            value: _from,
            onChanged: (value) => setState(() => _from = value),
          ),
          DateFilterButton(
            label: 'Do',
            value: _to,
            onChanged: (value) => setState(() => _to = value),
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
                ..._categoryOptions.entries.map(
                  (entry) => DropdownMenuItem(
                    value: entry.key,
                    child: Text(entry.value),
                  ),
                ),
              ],
              onChanged: (value) => setState(() => _categoryId = value),
            ),
          ),
          FilledButton.tonalIcon(
            onPressed: () => _load(1),
            icon: const Icon(Icons.filter_alt_outlined),
            label: const Text('Primijeni'),
          ),
        ],
      ),
      const SizedBox(height: 16),
      if (_error != null)
        Expanded(
          child: ErrorPanel(message: _error!, onRetry: _load),
        )
      else if (_report == null)
        const Expanded(
          child: LoadingPanel(label: 'Generisanje finansijskog pregleda...'),
        )
      else
        Expanded(
          child: _ReportBody(report: _report!, onPage: _load),
        ),
    ],
  );
}

class _ReportBody extends StatelessWidget {
  const _ReportBody({required this.report, required this.onPage});
  final FinancialReport report;
  final ValueChanged<int> onPage;
  @override
  Widget build(BuildContext context) => SingleChildScrollView(
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Wrap(
          spacing: 14,
          runSpacing: 14,
          children: [
            _ReportMetric(
              label: 'Ukupan prihod',
              value:
                  '${moneyFormat.format(report.totalRevenue)} ${report.currency}',
              icon: Icons.account_balance_wallet_outlined,
            ),
            _ReportMetric(
              label: 'Završene uplate',
              value: '${report.paymentCount}',
              icon: Icons.receipt_long_outlined,
            ),
          ],
        ),
        const SizedBox(height: 16),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(22),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Prihod po kategoriji',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                const SizedBox(height: 18),
                SizedBox(
                  height: 220,
                  child: report.revenueByCategory.isEmpty
                      ? const EmptyPanel(
                          message: 'Nema finansijskih podataka za filter.',
                          icon: Icons.bar_chart,
                        )
                      : BarChart(
                          BarChartData(
                            borderData: FlBorderData(show: false),
                            gridData: const FlGridData(show: false),
                            titlesData: const FlTitlesData(
                              topTitles: AxisTitles(
                                sideTitles: SideTitles(showTitles: false),
                              ),
                              rightTitles: AxisTitles(
                                sideTitles: SideTitles(showTitles: false),
                              ),
                              leftTitles: AxisTitles(
                                sideTitles: SideTitles(showTitles: false),
                              ),
                              bottomTitles: AxisTitles(
                                sideTitles: SideTitles(showTitles: false),
                              ),
                            ),
                            barGroups: [
                              for (
                                var index = 0;
                                index < report.revenueByCategory.length;
                                index++
                              )
                                BarChartGroupData(
                                  x: index,
                                  barRods: [
                                    BarChartRodData(
                                      toY: report
                                          .revenueByCategory[index]
                                          .revenue,
                                      color: index.isEven ? ink : coral,
                                      width: 26,
                                      borderRadius: const BorderRadius.vertical(
                                        top: Radius.circular(7),
                                      ),
                                    ),
                                  ],
                                ),
                            ],
                          ),
                        ),
                ),
                Wrap(
                  spacing: 14,
                  runSpacing: 8,
                  children: [
                    for (final category in report.revenueByCategory)
                      Text(
                        '${category.categoryName}: ${moneyFormat.format(category.revenue)} ${report.currency}',
                      ),
                  ],
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 16),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: report.reservations.items.isEmpty
                ? const EmptyPanel(
                    message: 'Nema završenih rezervacija za filter.',
                  )
                : scrollableTable(
                    DataTable(
                      columns: const [
                        DataColumn(label: Text('Uplata')),
                        DataColumn(label: Text('Rezervacija')),
                        DataColumn(label: Text('Klijent')),
                        DataColumn(label: Text('Profesionalac')),
                        DataColumn(label: Text('Završeno')),
                        DataColumn(label: Text('Iznos')),
                      ],
                      rows: [
                        for (final item in report.reservations.items)
                          DataRow(
                            cells: [
                              DataCell(Text('#${item.paymentId}')),
                              DataCell(Text('#${item.reservationId}')),
                              DataCell(Text(item.clientName)),
                              DataCell(Text(item.professionalName)),
                              DataCell(
                                Text(dateTimeFormat.format(item.completedAt)),
                              ),
                              DataCell(
                                Text(
                                  '${moneyFormat.format(item.amount)} ${item.currency}',
                                ),
                              ),
                            ],
                          ),
                      ],
                    ),
                  ),
          ),
        ),
        PagedFooter(
          page: report.reservations.page,
          pageCount: report.reservations.pageCount,
          total: report.reservations.total,
          onPage: onPage,
        ),
      ],
    ),
  );
}

class _ReportMetric extends StatelessWidget {
  const _ReportMetric({
    required this.label,
    required this.value,
    required this.icon,
  });
  final String label;
  final String value;
  final IconData icon;
  @override
  Widget build(BuildContext context) => SizedBox(
    width: 300,
    child: Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Row(
          children: [
            Icon(icon, size: 32, color: coral),
            const SizedBox(width: 16),
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label),
                Text(value, style: Theme.of(context).textTheme.headlineMedium),
              ],
            ),
          ],
        ),
      ),
    ),
  );
}
