import '../../../core/storage/secure_storage_service.dart';
import '../models/auth_response.dart';
import '../models/auth_user.dart';
import 'auth_api.dart';

/// Repository coordinating AuthApi requests and SecureStorageService token persistence.
class AuthRepository {
  final AuthApi _api;
  final SecureStorageService _storage;

  AuthRepository({
    AuthApi? api,
    SecureStorageService? storage,
  })  : _api = api ?? AuthApi(),
        _storage = storage ?? const SecureStorageService();

  /// Registers a new citizen and saves access token securely.
  Future<AuthResponse> registerCitizen({
    required String fullName,
    required String email,
    required String phoneNumber,
    required String password,
  }) async {
    final response = await _api.registerCitizen(
      fullName: fullName,
      email: email,
      phoneNumber: phoneNumber,
      password: password,
    );
    return response;
  }

  /// Logs in user and securely stores JWT access token.
  Future<AuthResponse> login({
    required String email,
    required String password,
  }) async {
    final response = await _api.login(email: email, password: password);
    await _storage.saveAccessToken(response.accessToken);
    return response;
  }

  /// Fetches profile of current user using stored token.
  Future<AuthUser> getCurrentUser() async {
    return await _api.getCurrentUser();
  }

  /// Clears stored credentials.
  Future<void> logout() async {
    await _storage.deleteAccessToken();
  }

  /// Returns true if a token is present in secure storage.
  Future<bool> hasToken() async {
    return await _storage.hasAccessToken();
  }
}
