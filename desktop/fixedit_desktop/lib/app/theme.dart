import 'package:flutter/material.dart';

const ink = Color(0xFF123B3A);
const coral = Color(0xFFE76F51);
const canvas = Color(0xFFF7F3EA);
const sand = Color(0xFFEAE2D2);
const success = Color(0xFF2A7F62);

ThemeData buildFixedItTheme() {
  final scheme = ColorScheme.fromSeed(
    seedColor: ink,
    brightness: Brightness.light,
    primary: ink,
    secondary: coral,
    surface: const Color(0xFFFFFCF6),
    error: const Color(0xFFB3261E),
  );

  return ThemeData(
    useMaterial3: true,
    colorScheme: scheme,
    scaffoldBackgroundColor: canvas,
    fontFamily: 'Segoe UI Variable',
    textTheme: const TextTheme(
      displaySmall: TextStyle(
        fontFamily: 'Georgia',
        fontWeight: FontWeight.w700,
        color: ink,
        letterSpacing: -1.2,
      ),
      headlineMedium: TextStyle(
        fontFamily: 'Georgia',
        fontWeight: FontWeight.w700,
        color: ink,
      ),
      titleLarge: TextStyle(fontWeight: FontWeight.w700, color: ink),
      titleMedium: TextStyle(fontWeight: FontWeight.w700, color: ink),
    ),
    cardTheme: CardThemeData(
      color: scheme.surface,
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(20),
        side: const BorderSide(color: sand),
      ),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: Colors.white.withValues(alpha: 0.82),
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: sand),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: sand),
      ),
      contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 13),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
    ),
    snackBarTheme: const SnackBarThemeData(
      behavior: SnackBarBehavior.floating,
      backgroundColor: ink,
    ),
  );
}
