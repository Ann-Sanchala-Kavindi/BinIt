import 'package:dio/dio.dart';

/// Centralized application API exception handling HTTP and network failure modes.
class ApiException implements Exception {
  final String message;
  final int? statusCode;
  final dynamic details;

  const ApiException({
    required this.message,
    this.statusCode,
    this.details,
  });

  factory ApiException.fromDioError(DioException error) {
    switch (error.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return const ApiException(
          message: 'Connection timed out. Please check your network.',
          statusCode: 408,
        );

      case DioExceptionType.connectionError:
        return const ApiException(
          message: 'Unable to connect to the Smart Waste server. Please verify backend is running.',
        );

      case DioExceptionType.badResponse:
        final statusCode = error.response?.statusCode;
        final responseData = error.response?.data;

        String extractedMessage = 'An unexpected server error occurred.';

        if (responseData is Map<String, dynamic>) {
          if (responseData['detail'] != null) {
            extractedMessage = responseData['detail'].toString();
          } else if (responseData['title'] != null) {
            extractedMessage = responseData['title'].toString();
          } else if (responseData['message'] != null) {
            extractedMessage = responseData['message'].toString();
          }
        }

        switch (statusCode) {
          case 400:
            return ApiException(
              message: extractedMessage.isNotEmpty
                  ? extractedMessage
                  : 'Invalid request data. Please check your input.',
              statusCode: 400,
              details: responseData,
            );
          case 401:
            return ApiException(
              message: extractedMessage.isNotEmpty
                  ? extractedMessage
                  : 'Invalid email or password.',
              statusCode: 401,
            );
          case 403:
            return const ApiException(
              message: 'Access denied. Your account lacks required permissions.',
              statusCode: 403,
            );
          case 404:
            return const ApiException(
              message: 'Requested resource was not found.',
              statusCode: 404,
            );
          case 409:
            return ApiException(
              message: extractedMessage.isNotEmpty
                  ? extractedMessage
                  : 'A conflict occurred. This email may already be registered.',
              statusCode: 409,
            );
          case 500:
          default:
            return const ApiException(
              message: 'Server error. Please try again later.',
              statusCode: 500,
            );
        }

      case DioExceptionType.cancel:
        return const ApiException(message: 'Request was cancelled.');

      case DioExceptionType.unknown:
      default:
        return const ApiException(
          message: 'A network communication error occurred.',
        );
    }
  }

  @override
  String toString() => message;
}
