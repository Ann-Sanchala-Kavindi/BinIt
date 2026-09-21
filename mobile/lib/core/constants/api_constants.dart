import 'dart:io';
import 'package:flutter/foundation.dart';

/// Centralized API configuration for the Smart Waste Mobile Application.
///
/// Networking strategies for local development:
/// 1. Android Emulator:
///    Uses `10.0.2.2` which maps directly to `localhost` on the development PC.
///    URL: `http://10.0.2.2:5276/api/v1`
///
/// 2. Physical Android Device:
///    Use your PC's LAN IPv4 address (e.g., `http://192.168.1.100:5276/api/v1`).
///    Both phone and PC must be connected to the same Wi-Fi/local network.
///    Pass at build/run time: `--dart-define=API_BASE_URL=http://<PC_LAN_IP>:5276/api/v1`
///
/// 3. Desktop / Web / Unit Tests:
///    Uses `http://localhost:5276/api/v1`.
class ApiConstants {
  ApiConstants._();

  static const String _envBaseUrl = String.fromEnvironment('API_BASE_URL');

  static String get baseUrl {
    if (_envBaseUrl.isNotEmpty) {
      return _envBaseUrl;
    }

    if (!kIsWeb && Platform.isAndroid) {
      // Default for Android Emulator communicating with backend running on host PC
      return 'http://10.0.2.2:5276/api/v1';
    }

    // Default for Desktop, Web, or local test runs
    return 'http://localhost:5276/api/v1';
  }

  // Auth endpoints
  static const String register = '/auth/register';
  static const String login = '/auth/login';
  static const String me = '/auth/me';
  static const String changePassword = '/auth/change-password';

  // Waste Reporting endpoints
  static const String wasteReports = '/waste-reports';
  static String wasteReportDetail(String id) => '/waste-reports/$id';
  static String wasteReportHistory(String reportId) => '/waste-reports/$reportId/history';
  static String wasteReportAttachments(String reportId) => '/waste-reports/$reportId/attachments';
  static String wasteReportAttachment(String reportId, String attachmentId) =>
      '/waste-reports/$reportId/attachments/$attachmentId';

  // Network timeouts
  static const Duration connectTimeout = Duration(seconds: 15);
  static const Duration receiveTimeout = Duration(seconds: 15);
}
