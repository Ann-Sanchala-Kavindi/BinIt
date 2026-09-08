import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/routing/app_router.dart';
import 'package:mobile/features/auth/models/auth_user.dart';
import 'package:mobile/features/auth/presentation/home_screen.dart';
import 'package:mobile/features/auth/presentation/login_screen.dart';
import 'package:mobile/features/auth/presentation/register_screen.dart';
import 'package:mobile/features/auth/providers/auth_provider.dart';

// Fake Notifier to directly test routing guards without background network calls
class TestAuthNotifier extends AuthNotifier {
  final AuthState initialState;

  TestAuthNotifier(this.initialState);

  @override
  AuthState build() => initialState;
}

void main() {
  group('AppRouter Guard Tests', () {
    const testUser = AuthUser(
      id: 'test-user-id',
      fullName: 'Authenticated Citizen',
      email: 'citizen@smartwaste.local',
      role: AppRoles.citizen,
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

    testWidgets('authenticated user is allowed access to HomeScreen', (tester) async {
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            authProvider.overrideWith(() => TestAuthNotifier(const AuthState.authenticated(testUser))),
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

      // Should allow access to HomeScreen
      expect(find.byType(HomeScreen), findsOneWidget);
      expect(find.text('Welcome, Authenticated Citizen'), findsOneWidget);
      expect(find.byType(LoginScreen), findsNothing);
    });

    testWidgets('authenticated user navigating to /login is redirected to /home', (tester) async {
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            authProvider.overrideWith(() => TestAuthNotifier(const AuthState.authenticated(testUser))),
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

      // Already redirected to /home
      expect(find.byType(HomeScreen), findsOneWidget);
      expect(find.byType(LoginScreen), findsNothing);
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
      final registerLink = find.text('Register as Citizen');
      await tester.tap(registerLink);
      await tester.pumpAndSettle();

      expect(find.byType(RegisterScreen), findsOneWidget);
      expect(find.byType(LoginScreen), findsNothing);
    });
  });
}
