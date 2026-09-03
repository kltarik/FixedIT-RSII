import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';

import '../core/models.dart';
import '../services/mobile_repository.dart';
import '../widgets/common.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({
    super.key,
    required this.repository,
    required this.professional,
  });
  final MobileRepository repository;
  final bool professional;

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  final picker = ImagePicker();
  final formKey = GlobalKey<FormState>();
  final email = TextEditingController();
  final first = TextEditingController();
  final last = TextEditingController();
  final phone = TextEditingController();
  final bio = TextEditingController();
  final rate = TextEditingController();
  final experience = TextEditingController();
  final Set<int> categories = {};
  final Set<int> availableDays = {};
  UserProfile? profile;
  Professional? professionalProfile;
  ReferenceData? reference;
  String? error;
  bool saving = false;
  int? city;

  @override
  void initState() {
    super.initState();
    load();
  }

  @override
  void dispose() {
    email.dispose();
    first.dispose();
    last.dispose();
    phone.dispose();
    bio.dispose();
    rate.dispose();
    experience.dispose();
    super.dispose();
  }

  Future<void> load() async {
    setState(() => error = null);
    try {
      final futures = <Future<Object>>[
        widget.repository.getProfile(),
        widget.repository.getReferenceData(),
        if (widget.professional) widget.repository.getMyProfessionalProfile(),
        if (widget.professional) widget.repository.getMyAvailability(),
      ];
      final values = await Future.wait(futures);
      final user = values[0] as UserProfile;
      final refs = values[1] as ReferenceData;
      final pro = widget.professional ? values[2] as Professional : null;
      final availability = widget.professional
          ? values[3] as List<ProfessionalAvailability>
          : const <ProfessionalAvailability>[];
      if (!mounted) return;
      setState(() {
        profile = user;
        reference = refs;
        professionalProfile = pro;
        email.text = user.email;
        first.text = user.firstName;
        last.text = user.lastName;
        phone.text = user.phone ?? '';
        city = user.cityId;
        if (pro != null) {
          bio.text = pro.bio;
          rate.text = pro.hourlyRate.toStringAsFixed(2);
          experience.text = '${pro.experience}';
          categories
            ..clear()
            ..addAll(pro.categories.map((item) => item.id));
          availableDays
            ..clear()
            ..addAll(availability.map((item) => item.dayOfWeek));
        }
      });
    } catch (exception) {
      if (mounted) setState(() => error = userError(exception));
    }
  }

  Future<void> save() async {
    if (!formKey.currentState!.validate()) return;
    if (widget.professional && (categories.isEmpty || availableDays.isEmpty)) {
      setState(() {});
      return;
    }
    setState(() => saving = true);
    try {
      await widget.repository.updateProfile(
        email: email.text,
        firstName: first.text,
        lastName: last.text,
        cityId: city!,
        phone: phone.text,
      );
      if (widget.professional) {
        await widget.repository.updateProfessional(
          bio: bio.text,
          rate: double.parse(rate.text),
          experience: int.parse(experience.text),
          categoryIds: categories.toList(),
        );
        await widget.repository.saveMyAvailability(availableDays);
      }
      await load();
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(const SnackBar(content: Text('Profil je sačuvan.')));
      }
    } catch (exception) {
      if (mounted) await showFailure(context, exception);
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }

  Future<void> updatePicture() async {
    final image = await picker.pickImage(
      source: ImageSource.gallery,
      maxWidth: 1600,
      imageQuality: 88,
    );
    if (image == null) return;
    try {
      await widget.repository.uploadProfilePicture(image);
      await load();
    } catch (exception) {
      if (mounted) await showFailure(context, exception);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (error != null) return ErrorView(error!, load);
    if (profile == null || reference == null) return const LoadingView();
    return Form(
      key: formKey,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 100),
        children: [
          const ScreenTitle('Moj profil', 'Uredite podatke i fotografiju.'),
          Center(
            child: Stack(
              children: [
                NetworkAvatar(
                  url: profile!.picture,
                  name: profile!.name,
                  radius: 54,
                ),
                Positioned(
                  right: 0,
                  bottom: 0,
                  child: IconButton.filled(
                    onPressed: updatePicture,
                    icon: const Icon(Icons.camera_alt, size: 18),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 18),
          Row(
            children: [
              Expanded(
                child: TextFormField(
                  controller: first,
                  decoration: const InputDecoration(labelText: 'Ime'),
                  validator: requiredField,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: TextFormField(
                  controller: last,
                  decoration: const InputDecoration(labelText: 'Prezime'),
                  validator: requiredField,
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          TextFormField(
            controller: email,
            keyboardType: TextInputType.emailAddress,
            decoration: const InputDecoration(labelText: 'Email'),
            validator: (value) =>
                !RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(value ?? '')
                ? 'Email nije ispravan.'
                : null,
          ),
          const SizedBox(height: 10),
          TextFormField(
            controller: phone,
            keyboardType: TextInputType.phone,
            decoration: const InputDecoration(labelText: 'Telefon'),
          ),
          const SizedBox(height: 10),
          DropdownButtonFormField<int>(
            initialValue: city,
            decoration: const InputDecoration(labelText: 'Grad'),
            items: reference!.cities
                .map(
                  (item) =>
                      DropdownMenuItem(value: item.id, child: Text(item.name)),
                )
                .toList(),
            onChanged: (value) => setState(() => city = value),
            validator: (value) => value == null ? 'Odaberite grad.' : null,
          ),
          if (widget.professional) ...professionalFields(context),
          const SizedBox(height: 18),
          FilledButton.icon(
            onPressed: saving ? null : save,
            icon: const Icon(Icons.save_outlined),
            label: Text(saving ? 'Čuvanje...' : 'Sačuvaj profil'),
          ),
          if (widget.professional) ...portfolioFields(context),
        ],
      ),
    );
  }

  List<Widget> professionalFields(BuildContext context) => [
    const SizedBox(height: 22),
    Text('Profesionalni profil', style: Theme.of(context).textTheme.titleLarge),
    const SizedBox(height: 10),
    TextFormField(
      controller: bio,
      maxLines: 4,
      decoration: const InputDecoration(labelText: 'Biografija'),
      validator: requiredField,
    ),
    const SizedBox(height: 10),
    Row(
      children: [
        Expanded(
          child: TextFormField(
            controller: rate,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(labelText: 'Satnica (EUR)'),
            validator: (value) => (double.tryParse(value ?? '') ?? -1) < 0
                ? 'Neispravna satnica.'
                : null,
          ),
        ),
        const SizedBox(width: 8),
        Expanded(
          child: TextFormField(
            controller: experience,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(labelText: 'Godine iskustva'),
            validator: (value) {
              final number = int.tryParse(value ?? '');
              return number == null || number < 0 || number > 100
                  ? 'Vrijednost 0-100.'
                  : null;
            },
          ),
        ),
      ],
    ),
    const SizedBox(height: 12),
    Text('Kategorije', style: Theme.of(context).textTheme.titleMedium),
    ...reference!.categories.map(
      (option) => CheckboxListTile(
        contentPadding: EdgeInsets.zero,
        title: Text(option.name),
        value: categories.contains(option.id),
        onChanged: (checked) => setState(() {
          if (checked == true) {
            categories.add(option.id);
          } else {
            categories.remove(option.id);
          }
        }),
      ),
    ),
    if (categories.isEmpty)
      const Text(
        'Odaberite najmanje jednu kategoriju.',
        style: TextStyle(color: Colors.red),
      ),
    const SizedBox(height: 12),
    Text(
      'Radni dani (08:00-17:00)',
      style: Theme.of(context).textTheme.titleMedium,
    ),
    Wrap(
      spacing: 8,
      children: [
        for (final entry in const {
          1: 'Pon',
          2: 'Uto',
          3: 'Sri',
          4: 'Čet',
          5: 'Pet',
          6: 'Sub',
          0: 'Ned',
        }.entries)
          FilterChip(
            label: Text(entry.value),
            selected: availableDays.contains(entry.key),
            onSelected: (selected) => setState(() {
              if (selected) {
                availableDays.add(entry.key);
              } else {
                availableDays.remove(entry.key);
              }
            }),
          ),
      ],
    ),
    if (availableDays.isEmpty)
      const Text(
        'Odaberite najmanje jedan radni dan.',
        style: TextStyle(color: Colors.red),
      ),
  ];

  List<Widget> portfolioFields(BuildContext context) => [
    const SizedBox(height: 24),
    Row(
      children: [
        Expanded(
          child: Text(
            'Portfolio',
            style: Theme.of(context).textTheme.titleLarge,
          ),
        ),
        IconButton.filledTonal(
          onPressed: addPortfolio,
          icon: const Icon(Icons.add),
        ),
      ],
    ),
    if (professionalProfile!.portfolio.isEmpty)
      const EmptyView('Dodajte prvi portfolio rad.')
    else
      ...professionalProfile!.portfolio.map(
        (item) => Card(
          child: ListTile(
            title: Text(item.title),
            subtitle: Text(item.description, maxLines: 2),
            trailing: IconButton(
              onPressed: () => deletePortfolio(item),
              icon: const Icon(Icons.delete_outline),
            ),
          ),
        ),
      ),
  ];

  Future<void> addPortfolio() async {
    final result = await showModalBottomSheet<(String, String, XFile)>(
      context: context,
      isScrollControlled: true,
      builder: (_) => _PortfolioForm(picker),
    );
    if (result == null) return;
    try {
      await widget.repository.addPortfolio(result.$1, result.$2, result.$3);
      await load();
    } catch (exception) {
      if (mounted) await showFailure(context, exception);
    }
  }

  Future<void> deletePortfolio(PortfolioItem item) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Izbrisati portfolio rad?'),
        content: Text(item.title),
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
    try {
      await widget.repository.deletePortfolio(item.id);
      await load();
    } catch (exception) {
      if (mounted) await showFailure(context, exception);
    }
  }
}

String? requiredField(String? value) =>
    (value?.trim().isEmpty ?? true) ? 'Polje je obavezno.' : null;

class _PortfolioForm extends StatefulWidget {
  const _PortfolioForm(this.picker);
  final ImagePicker picker;
  @override
  State<_PortfolioForm> createState() => _PortfolioFormState();
}

class _PortfolioFormState extends State<_PortfolioForm> {
  final formKey = GlobalKey<FormState>();
  final title = TextEditingController();
  final description = TextEditingController();
  XFile? image;

  @override
  void dispose() {
    title.dispose();
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
      key: formKey,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            'Novi portfolio rad',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 12),
          TextFormField(
            controller: title,
            decoration: const InputDecoration(labelText: 'Naslov'),
            validator: requiredField,
          ),
          const SizedBox(height: 10),
          TextFormField(
            controller: description,
            maxLines: 3,
            decoration: const InputDecoration(labelText: 'Opis'),
            validator: requiredField,
          ),
          const SizedBox(height: 10),
          OutlinedButton.icon(
            onPressed: () async {
              final value = await widget.picker.pickImage(
                source: ImageSource.gallery,
                maxWidth: 1800,
                imageQuality: 88,
              );
              if (value != null) setState(() => image = value);
            },
            icon: const Icon(Icons.image_outlined),
            label: Text(image?.name ?? 'Odaberi fotografiju'),
          ),
          if (image == null)
            const Text(
              'Fotografija je obavezna.',
              style: TextStyle(color: Colors.red),
            ),
          const SizedBox(height: 14),
          FilledButton(
            onPressed: () {
              if (formKey.currentState!.validate() && image != null) {
                Navigator.pop(context, (
                  title.text.trim(),
                  description.text.trim(),
                  image!,
                ));
              }
            },
            child: const Text('Dodaj'),
          ),
        ],
      ),
    ),
  );
}
