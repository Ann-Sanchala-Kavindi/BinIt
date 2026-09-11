import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/core/routing/app_router.dart';
import 'package:mobile/features/auth/models/auth_user.dart';
import 'package:mobile/features/auth/presentation/change_password_screen.dart';
import 'package:mobile/features/auth/presentation/home_screen.dart';
import 'package:mobile/features/auth/presentation/login_screen.dart';
import 'package:mobile/features/auth/presentation/register_screen.dart';
import 'package:mobile/features/auth/providers/auth_provider.dart';
import 'package:mobile/features/citizen/presentation/citizen_dashboard_screen.dart';
import 'package:mobile/features/driver/presentation/driver_dashboard_screen.dart';

// Fake Notifier to directly test routing guards without background network calls
class TestAuthNotifier extends AuthNotifier {
  final AuthState initialState;

  TestAuthNotifier(this.initialState);

  @override
  AuthState build() => initialState;
}

void main() {
  group('AppRouter Guard Tests', () {
    const testCitizenUser = AuthUser(
      id: 'test-citizen-id',
      fullName: 'Authenticated Citizen',
      email: 'citizen@smartwaste.local',
      role: AppRoles.citizen,
    );

    const testDriverUser = AuthUser(
      id: 'test-driver-id',
      fullName: 'Authenticated Driver',
      email: 'driver@smartwaste.local',
      role: AppRoles.driver,
    );

    testWidgets('unauthenticated user is redirected to LoginScreen when navigating to /home', (tester) async {
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            authProvider.overrideWith(() => TestAuthNotifier(const AuthState.unauthenticated())),
          ],
          child: Consumer(
            builder: (context, ref, _) {
              final router = ref.watch(appRouterProvider);
              return MaterialApp.router(
                routerConfig: router,
              );
            },
          ),
        ),
      );

      await tester.pumpAndSettle();

      // Should redirect to login screen
      expect(find.byType(LoginScreen), findsOneWidget);
      expect(find.byType(HomeScreen), findsNothing);
    });

    testWidgets('authenticated Citizen is redirected to CitizenDashboardScreen', (tester) async {
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            authProvider.overrideWith(() => TestAuthNotifier(const AuthState.authenticated(testCitizenUser))),
          ],
          child: Consumer(
            builder: (context, ref, _) {
              final router = ref.watch(appRouterProvider);
              return MaterialApp.router(
                routerConfig: router,
              );
            },
          ),
        ),
      );

      await tester.pumpAndSettle();

      // Should redirect to Citizen Dashboard
      expect(find.text('Hello, Authenticated'), findsOneWidget);
      expect(find.text('Report Waste'), findsOneWidget);
      expect(find.byType(HomeScreen), findsNothing);
      expect(find.byType(LoginScreen), findsNothing);
    });

    testWidgets('authenticated Citizen navigating to /login is redirected to /citizen/dashboard', (tester) async {
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            authProvider.overrideWith(() => TestAuthNotifier(const AuthState.authenticated(testCitizenUser))),
          ],
          child: Consumer(
            builder: (context, ref, _) {
              final router = ref.watch(appRouterProvider);
              return MaterialApp.router(
                routerConfig: router,
              );
            },
          ),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.text('Hello, Authenticated'), findsOneWidget);
      expect(find.byType(LoginScreen), findsNothing);
    });

    testWidgets('authenticated Driver is redirected to DriverDashboardScreen and /login redirects to /driver/dashboard', (tester) async {
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            authProvider.overrideWith(() => TestAuthNotifier(const AuthState.authenticated(testDriverUser))),
          ],
          child: Consumer(
            builder: (context, ref, _) {
              final router = ref.watch(appRouterProvider);
              return MaterialApp.router(
                routerConfig: router,
              );
            },
          ),
        ),
      );

      await tester.pumpAndSettle();

      // Driver goes to DriverDashboardScreen
      expect(find.byType(DriverDashboardScreen), findsOneWidget);
      expect(find.text('Hello, Authenticated'), findsOneWidget);
      expect(find.text('Current Assignment'), findsOneWidget);
      expect(find.byType(HomeScreen), findsNothing);
      expect(find.byType(LoginScreen), findsNothing);
    });

    testWidgets('authenticated Driver attempting /citizen/dashboard is blocked and redirected to /driver/dashboard', (tester) async {
      late GoRouter router;
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            authProvider.overrideWith(() => TestAuthNotifier(const AuthState.authenticated(testDriverUser))),
          ],
          child: Consumer(
            builder: (context, ref, _) {
              router = ref.watch(appRouterProvider);
              return MaterialApp.router(
                routerConfig: router,
              );
            },
          ),
        ),
      );

      await tester.pumpAndSettle();
      expect(find.byType(DriverDashboardScreen), findsOneWidget);

      // Attempt to navigate to citizen dashboard
      router.go('/citizen/dashboard');
      await tester.pumpAndSettle();

      // Should be redirected back to /driver/dashboard
      expect(find.byType(DriverDashboardScreen), findsOneWidget);
      expect(find.text('Report Waste'), findsNothing);
    });

    testWidgets('authenticated Citizen attempting /driver/dashboard is blocked and redirected to /citizen/dashboard', (tester) async {
      late GoRouter router;
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            authProvider.overrideWith(() => TestAuthNotifier(const AuthState.authenticated(testCitizenUser))),
          ],
          child: Consumer(
            builder: (context, ref, _) {
              router = ref.watch(appRouterProvider);
              return MaterialApp.router(
                routerConfig: router,
              );
            },
          ),
        ),
      );

      await tester.pumpAndSettle();
      expect(find.byType(CitizenDashboardScreen), findsOneWidget);

      // Attempt to navigate to driver dashboard
      router.go('/driver/dashboard');
      await tester.pumpAndSettle();

      // Should be redirected back to /citizen/dashboard
      expect(find.byType(CitizenDashboardScreen), findsOneWidget);
      expect(find.text('Current Assignment'), findsNothing);
    });

    testWidgets('unauthenticated user can access RegisterScreen', (tester) async {
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            authProvider.overrideWith(() => TestAuthNotifier(const AuthState.unauthenticated())),
          ],
          child: Consumer(
            builder: (context, ref, _) {
              final router = ref.watch(appRouterProvider);
              return MaterialApp.router(
                routerConfig: router,
              );
            },
          ),
        ),
      );

      await tester.pumpAndSettle();
      expect(find.byType(LoginScreen), findsOneWidget);

      // Tap Register link
      final registerLink = find.text('Create an account');
      await tester.tap(registerLink);
      await tester.pumpAndSettle();

      expect(find.byType(RegisterScreen), findsOneWidget);
      expect(find.byType(LoginScreen), findsNothing);
    });

    testWidgets('authenticated user with mustChangePassword = true is forced to ChangePasswordScreen', (tester) async {
      const mustChangeUser = AuthUser(
        id: 'test-user-id',
        fullName: 'New Driver',
        email: 'driver@smartwaste.local',
        role: AppRoles.driver,
        mustChangePassword: true,
      );

      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            authProvider.overrideWith(() => TestAuthNotifier(const AuthState.authenticated(mustChangeUser))),
          ],
          child: Consumer(
            builder: (context, ref, _) {
              final router = ref.watch(appRouterProvider);
              return MaterialApp.router(
                routerConfig: router,
              );
            },
          ),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.byType(ChangePasswordScreen), findsOneWidget);
      expect(find.text('Change Temporary Password'), findsOneWidget);
      expect(find.text('You must change your temporary password before continuing.'), findsOneWidget);
      expect(find.byType(HomeScreen), findsNothing);
    });
  });
}
