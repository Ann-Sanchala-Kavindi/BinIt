import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/storage/secure_storage_service.dart';
import 'package:mobile/features/auth/data/auth_api.dart';
import 'package:mobile/features/auth/data/auth_repository.dart';
import 'package:mobile/features/auth/models/auth_response.dart';
import 'package:mobile/features/auth/models/auth_user.dart';

class FakeAuthApi extends AuthApi {
  AuthResponse? registerResult;
  AuthResponse? loginResult;
  AuthUser? currentUserResult;
  bool shouldThrow = false;

  @override
  Future<AuthResponse> registerCitizen({
    required String fullName,
    required String email,
    required String phoneNumber,
    required String password,
  }) async {
    if (shouldThrow) throw Exception('Registration failed');
    return registerResult!;
  }

  @override
  Future<AuthResponse> login({
    required String email,
    required String password,
  }) async {
    if (shouldThrow) throw Exception('Login failed');
    return loginResult!;
  }

  @override
  Future<AuthUser> getCurrentUser() async {
    if (shouldThrow) throw Exception('Unauthorized');
    return currentUserResult!;
  }
}

class FakeSecureStorageService extends SecureStorageService {
  String? _token;

  @override
  Future<void> saveAccessToken(String token) async {
    _token = token;
  }

  @override
  Future<String?> getAccessToken() async => _token;

  @override
  Future<void> deleteAccessToken() async {
    _token = null;
  }

  @override
  Future<bool> hasAccessToken() async => _token != null && _token!.isNotEmpty;
}

void main() {
  group('AuthRepository Tests', () {
    late FakeAuthApi fakeApi;
    late FakeSecureStorageService fakeStorage;
    late AuthRepository repository;

    final dummyUser = const AuthUser(
      id: 'test-user-id',
      fullName: 'Sunil Perera',
      email: 'sunil@binit.lk',
      phoneNumber: '+94771234567',
      role: AppRoles.citizen,
    );

    final dummyAuthResponse = AuthResponse(
      accessToken: 'valid-test-jwt-token',
      expiresAt: DateTime.now().add(const Duration(hours: 1)),
      user: dummyUser,
    );

    setUp(() {
      fakeApi = FakeAuthApi();
      fakeStorage = FakeSecureStorageService();
      repository = AuthRepository(api: fakeApi, storage: fakeStorage);
    });

    test('registerCitizen returns AuthResponse from API', () async {
      fakeApi.registerResult = dummyAuthResponse;

      final res = await repository.registerCitizen(
        fullName: 'Sunil Perera',
        email: 'sunil@binit.lk',
        phoneNumber: '+94771234567',
        password: 'Password123!',
      );

      expect(res.accessToken, 'valid-test-jwt-token');
      expect(res.user.fullName, 'Sunil Perera');
    });

    test('login saves access token in secure storage and returns AuthResponse', () async {
      fakeApi.loginResult = dummyAuthResponse;

      final res = await repository.login(
        email: 'sunil@binit.lk',
        password: 'Password123!',
      );

      expect(res.accessToken, 'valid-test-jwt-token');
      expect(await fakeStorage.getAccessToken(), 'valid-test-jwt-token');
      expect(await repository.hasToken(), isTrue);
    });

    test('getCurrentUser retrieves user profile from API', () async {
      fakeApi.currentUserResult = dummyUser;

      final user = await repository.getCurrentUser();

      expect(user.id, 'test-user-id');
      expect(user.fullName, 'Sunil Perera');
      expect(user.isCitizen, isTrue);
    });

    test('logout deletes token from secure storage', () async {
      await fakeStorage.saveAccessToken('existing-token');
      expect(await repository.hasToken(), isTrue);

      await repository.logout();

      expect(await repository.hasToken(), isFalse);
      expect(await fakeStorage.getAccessToken(), isNull);
    });
  });
}
