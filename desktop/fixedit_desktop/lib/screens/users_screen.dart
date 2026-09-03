import 'package:flutter/material.dart';

import '../core/models.dart';
import '../services/admin_repository.dart';
import '../widgets/common.dart';

class UsersScreen extends StatefulWidget {
  const UsersScreen({super.key, required this.repository});
  final AdminRepository repository;
  @override
  State<UsersScreen> createState() => _UsersScreenState();
}

class _UsersScreenState extends State<UsersScreen> {
  final _search = TextEditingController();
  PagedResult<AdminUserRecord>? _result;
  String? _error;
  String? _busyId;
  int _page = 1;

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
      final result = await widget.repository.getUsers(
        page: _page,
        search: _search.text.trim(),
      );
      if (mounted) setState(() => _result = result);
    } catch (exception) {
      if (mounted) setState(() => _error = userError(exception));
    }
  }

  Future<void> _setActive(AdminUserRecord user, bool active) async {
    setState(() => _busyId = user.id);
    try {
      final updated = await widget.repository.setUserActive(user.id, active);
      if (mounted) {
        setState(
          () => _result = PagedResult(
            items: _result!.items
                .map((item) => item.id == updated.id ? updated : item)
                .toList(),
            total: _result!.total,
            page: _result!.page,
            pageSize: _result!.pageSize,
          ),
        );
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              active ? 'Korisnik je aktiviran.' : 'Korisnik je deaktiviran.',
            ),
          ),
        );
      }
    } catch (exception) {
      if (mounted) await showApiErrorDialog(context, userError(exception));
    } finally {
      if (mounted) setState(() => _busyId = null);
    }
  }

  Future<void> _delete(AdminUserRecord user) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Izbrisati korisnika?'),
        content: Text(
          '${user.name} i podaci bez poslovnih relacija bit će trajno uklonjeni.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Odustani'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Izbriši'),
          ),
        ],
      ),
    );
    if (confirmed != true) return;
    setState(() => _busyId = user.id);
    try {
      await widget.repository.deleteUser(user.id);
      if (mounted) await _load(_page);
    } catch (exception) {
      if (mounted) {
        await showApiErrorDialog(
          context,
          userError(exception),
          title: 'Korisnik nije izbrisan',
        );
      }
    } finally {
      if (mounted) setState(() => _busyId = null);
    }
  }

  Future<void> _edit(AdminUserRecord user) async {
    final cities = await widget.repository.getCities();
    if (!mounted || cities.items.isEmpty) return;
    final firstName = TextEditingController(text: user.firstName);
    final lastName = TextEditingController(text: user.lastName);
    final phone = TextEditingController(text: user.phoneNumber);
    var cityId = user.cityId;
    final saved = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: const Text('Uredi korisnika'),
          content: SizedBox(
            width: 460,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextField(
                  controller: firstName,
                  decoration: const InputDecoration(labelText: 'Ime'),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: lastName,
                  decoration: const InputDecoration(labelText: 'Prezime'),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: phone,
                  decoration: const InputDecoration(labelText: 'Telefon'),
                ),
                const SizedBox(height: 12),
                DropdownButtonFormField<int>(
                  initialValue: cityId,
                  decoration: const InputDecoration(labelText: 'Grad'),
                  items: cities.items
                      .map(
                        (city) => DropdownMenuItem(
                          value: city.id,
                          child: Text(city.name),
                        ),
                      )
                      .toList(),
                  onChanged: (value) =>
                      setDialogState(() => cityId = value ?? cityId),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialogContext, false),
              child: const Text('Odustani'),
            ),
            FilledButton(
              onPressed: () async {
                try {
                  await widget.repository.updateUser(
                    id: user.id,
                    firstName: firstName.text,
                    lastName: lastName.text,
                    phoneNumber: phone.text,
                    cityId: cityId,
                  );
                  if (dialogContext.mounted) {
                    Navigator.pop(dialogContext, true);
                  }
                } catch (error) {
                  if (dialogContext.mounted) {
                    await showApiErrorDialog(dialogContext, userError(error));
                  }
                }
              },
              child: const Text('Sačuvaj'),
            ),
          ],
        ),
      ),
    );
    firstName.dispose();
    lastName.dispose();
    phone.dispose();
    if (saved == true && mounted) await _load(_page);
  }

  @override
  Widget build(BuildContext context) {
    if (_error != null) return ErrorPanel(message: _error!, onRetry: _load);
    if (_result == null) {
      return const LoadingPanel(label: 'Učitavanje korisnika...');
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PageHeading(
          title: 'Korisnici',
          subtitle: 'Aktivacija, deaktivacija i kontrolisano brisanje naloga.',
          actions: [
            IconButton.filledTonal(
              onPressed: _load,
              icon: const Icon(Icons.refresh),
            ),
          ],
        ),
        SizedBox(
          width: 420,
          child: TextField(
            controller: _search,
            decoration: const InputDecoration(
              labelText: 'Ime, email ili grad',
              suffixIcon: Icon(Icons.search),
            ),
            onSubmitted: (_) => _load(1),
          ),
        ),
        const SizedBox(height: 14),
        if (_result!.items.isEmpty)
          const Expanded(
            child: EmptyPanel(message: 'Nema korisnika za unesenu pretragu.'),
          )
        else
          Expanded(
            child: Card(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(16),
                child: scrollableTable(
                  DataTable(
                    columns: const [
                      DataColumn(label: Text('Aktivan')),
                      DataColumn(label: Text('Korisnik')),
                      DataColumn(label: Text('Email')),
                      DataColumn(label: Text('Grad')),
                      DataColumn(label: Text('Uloge')),
                      DataColumn(label: Text('Akcije')),
                    ],
                    rows: [
                      for (final user in _result!.items)
                        DataRow(
                          cells: [
                            DataCell(
                              _busyId == user.id
                                  ? const SizedBox(
                                      width: 20,
                                      height: 20,
                                      child: CircularProgressIndicator(
                                        strokeWidth: 2,
                                      ),
                                    )
                                  : Checkbox(
                                      value: user.isActive,
                                      onChanged: (value) => value == null
                                          ? null
                                          : _setActive(user, value),
                                    ),
                            ),
                            DataCell(Text(user.name)),
                            DataCell(Text(user.email)),
                            DataCell(Text(user.cityName)),
                            DataCell(Text(user.roles.map(roleName).join(', '))),
                            DataCell(
                              Row(
                                mainAxisSize: MainAxisSize.min,
                                children: [
                                  IconButton(
                                    tooltip: 'Uredi',
                                    onPressed: _busyId == null
                                        ? () => _edit(user)
                                        : null,
                                    icon: const Icon(Icons.edit_outlined),
                                  ),
                                  IconButton(
                                    tooltip: 'Izbriši',
                                    onPressed: _busyId == null
                                        ? () => _delete(user)
                                        : null,
                                    icon: const Icon(Icons.delete_outline),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                    ],
                  ),
                ),
              ),
            ),
          ),
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
