import 'package:fixedit_mobile/core/models.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('paged response decodes items and calculates page count', () {
    final result = Paged.fromJson({
      'items': [
        {'id': 2, 'name': 'Sarajevo'},
        {'id': 3, 'name': 'Mostar'},
      ],
      'total': 41,
      'page': 2,
      'pageSize': 20,
    }, LookupOption.fromJson);

    expect(result.items.map((item) => item.name), ['Sarajevo', 'Mostar']);
    expect(result.page, 2);
    expect(result.pageCount, 3);
  });

  test('empty paged response has one display page', () {
    const result = Paged<LookupOption>([], 0, 1, 20);

    expect(result.pageCount, 1);
  });

  test('authentication user exposes role-aware capabilities', () {
    final user = AuthUser.fromJson({
      'id': 'client-1',
      'email': 'client@example.test',
      'firstName': 'Test',
      'lastName': 'Klijent',
      'cityId': 1,
      'roles': ['Client'],
    });

    expect(user.name, 'Test Klijent');
    expect(user.isClient, isTrue);
    expect(user.isProfessional, isFalse);
  });

  test('reservation preserves state, payment and UTC schedule contract', () {
    final reservation = Reservation.fromJson({
      'id': 12,
      'clientUserId': 'client-1',
      'clientFirstName': 'Test',
      'clientLastName': 'Klijent',
      'professionalProfileId': 7,
      'professionalUserId': 'professional-1',
      'professionalFirstName': 'Test',
      'professionalLastName': 'Majstor',
      'serviceDescription': 'Popravka slavine',
      'scheduledAt': '2026-08-08T10:00:00Z',
      'durationMinutes': 60,
      'totalPrice': 50.0,
      'status': 2,
      'isPaid': true,
      'paymentStatus': 2,
    });

    expect(reservation.status, 2);
    expect(reservation.isPaid, isTrue);
    expect(reservation.duration, 60);
    expect(reservation.scheduledAt.toUtc().hour, 10);
  });
}
