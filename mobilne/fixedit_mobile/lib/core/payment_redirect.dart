bool isPayPalRedirectPath(String path, String result) {
  final normalizedPath = path.toLowerCase();
  if (normalizedPath.endsWith('/payments/$result')) {
    return true;
  }

  return switch (result) {
    'success' => normalizedPath.endsWith('/payment-success'),
    'cancelled' =>
      normalizedPath.endsWith('/payment-cancel') ||
          normalizedPath.endsWith('/payment-cancelled'),
    _ => false,
  };
}
