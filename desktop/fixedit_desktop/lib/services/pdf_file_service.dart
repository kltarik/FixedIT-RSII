import 'dart:io';

Future<String> savePdfFile(String selectedPath, List<int> bytes) async {
  if (bytes.isEmpty) {
    throw const FileSystemException('PDF dokument je prazan.');
  }

  final path = selectedPath.toLowerCase().endsWith('.pdf')
      ? selectedPath
      : '$selectedPath.pdf';
  await File(path).writeAsBytes(bytes, flush: true);
  return path;
}
