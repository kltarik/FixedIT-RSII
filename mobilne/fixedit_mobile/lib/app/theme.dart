import 'package:flutter/material.dart';

const midnight = Color(0xFF17324D);
const sun = Color(0xFFF4A261);
const mint = Color(0xFF2A9D8F);
const paper = Color(0xFFFFFBF4);
const mist = Color(0xFFE8F0EE);

ThemeData buildMobileTheme() {
  final scheme = ColorScheme.fromSeed(
    seedColor: midnight,
    primary: midnight,
    secondary: sun,
    tertiary: mint,
    surface: paper,
  );
  return ThemeData(
    useMaterial3: true,
    colorScheme: scheme,
    scaffoldBackgroundColor: const Color(0xFFF5F1E8),
    textTheme: const TextTheme(
      displaySmall: TextStyle(
        fontFamily: 'serif',
        fontWeight: FontWeight.w800,
        color: midnight,
        letterSpacing: -1,
      ),
      headlineMedium: TextStyle(
        fontFamily: 'serif',
        fontWeight: FontWeight.w800,
        color: midnight,
      ),
      titleLarge: TextStyle(fontWeight: FontWeight.w800, color: midnight),
      titleMedium: TextStyle(fontWeight: FontWeight.w700, color: midnight),
    ),
    cardTheme: CardThemeData(
      elevation: 0,
      color: paper,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(20),
        side: const BorderSide(color: Color(0xFFE5DED1)),
      ),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: Colors.white,
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(14),
        borderSide: BorderSide.none,
      ),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 15),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
      ),
    ),
    navigationBarTheme: const NavigationBarThemeData(
      backgroundColor: paper,
      indicatorColor: mist,
      height: 72,
    ),
  );
}
