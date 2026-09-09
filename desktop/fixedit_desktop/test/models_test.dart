import 'package:fixedit_desktop/core/models.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('statusi i uloge imaju bosanske prikazne nazive', () {
    expect(reservationStatusName(1), 'Na čekanju');
    expect(reservationStatusName(5), 'Otkazana');
    expect(roleName('Client'), 'Klijent');
    expect(roleName('Professional'), 'Profesionalac');
  });

  test('paged result parses API paging metadata', () {
    final result = PagedResult.fromJson({
      'items': [
        {'id': 7, 'title': 'Test'},
      ],
      'total': 41,
      'page': 2,
      'pageSize': 20,
    }, (json) => jsonInt(json, 'id'));

    expect(result.items, [7]);
    expect(result.page, 2);
    expect(result.pageCount, 3);
  });

  test('admin reservation transitions preserve state machine', () {
    expect(allowedNextReservationStatuses(1), [2, 5]);
    expect(allowedNextReservationStatuses(2), [3, 5]);
    expect(allowedNextReservationStatuses(3), [4, 5]);
    expect(allowedNextReservationStatuses(4), isEmpty);
    expect(allowedNextReservationStatuses(5), isEmpty);
  });

  test('admin reservation transitions exclude inactive destinations', () {
    const statuses = [
      ReservationStatusRecord(
        id: 2,
        name: 'Prihvaćena',
        description: '',
        isActive: false,
      ),
      ReservationStatusRecord(
        id: 5,
        name: 'Otkazana',
        description: '',
        isActive: true,
      ),
    ];

    expect(activeNextReservationStatuses(1, statuses), [5]);
    expect(activeNextReservationStatuses(2, statuses), [5]);
  });
}
