import 'dart:io';

import 'package:fixedit_desktop/services/pdf_file_service.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('PDF bytes are written to the selected Windows path', () async {
    final directory = await Directory.systemTemp.createTemp('fixedit-pdf-');
    addTearDown(() => directory.delete(recursive: true));

    final selectedPath = '${directory.path}${Platform.pathSeparator}izvjestaj';
    final savedPath = await savePdfFile(selectedPath, [0x25, 0x50, 0x44, 0x46]);

    expect(savedPath, endsWith('.pdf'));
    expect(await File(savedPath).readAsBytes(), [0x25, 0x50, 0x44, 0x46]);
  });
}
