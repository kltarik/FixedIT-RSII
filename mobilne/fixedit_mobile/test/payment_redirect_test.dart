import 'package:fixedit_mobile/core/payment_redirect.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('prepoznaje trenutne PayPal callback putanje', () {
    expect(isPayPalRedirectPath('/payments/success', 'success'), isTrue);
    expect(isPayPalRedirectPath('/payments/cancelled', 'cancelled'), isTrue);
  });

  test('prepoznaje ranije PayPal callback putanje', () {
    expect(isPayPalRedirectPath('/payment-success', 'success'), isTrue);
    expect(isPayPalRedirectPath('/payment-cancel', 'cancelled'), isTrue);
    expect(isPayPalRedirectPath('/payment-cancelled', 'cancelled'), isTrue);
  });

  test('ne presreće nepovezane putanje', () {
    expect(isPayPalRedirectPath('/checkout/complete', 'success'), isFalse);
    expect(isPayPalRedirectPath('/payments/successful', 'success'), isFalse);
  });
}
