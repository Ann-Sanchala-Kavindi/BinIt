import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/routing/app_router.dart';
import 'package:mobile/features/auth/models/auth_user.dart';
import 'package:mobile/features/auth/presentation/change_password_screen.dart';
import 'package:mobile/features/auth/presentation/login_screen.dart';
import 'package:mobile/features/auth/providers/auth_provider.dart';
import 'package:mobile/features/citizen/presentation/citizen_dashboard_screen.dart';
import 'package:mobile/features/citizen/presentation/citizen_placeholder_screen.dart';

class MockCitizenAuthNotifier extends AuthNotifier {
  final AuthState _initial;
  bool logoutCalled = false;

  MockCitizenAuthNotifier(this._initial);

  @override
  AuthState build() => _initial;

  @override
  Future<void> logout() async {
    logoutCalled = true;
    state = const AuthState.unauthenticated();
  }
}

void main() {
  const citizenUser = AuthUser(
    id: 'citizen-456',
    fullName: 'Nimali Fernando',
    email: 'nimali@smartwaste.lk',
    role: AppRoles.citizen,
  );

  Widget createCitizenTestApp({
    AuthNotifier Function()? notifierOverride,
    Size? surfaceSize,
  }) {
    return ProviderScope(
      overrides: [
        authProvider.overrideWith(
          notifierOverride ?? () => MockCitizenAuthNotifier(const AuthState.authenticated(citizenUser)),
        ),
      ],
      child: Consumer(
        builder: (context, ref, _) {
          final router = ref.watch(appRouterProvider);
          return MaterialApp.router(
            routerConfig: router,
          );
        },
      ),
    );
  }

  group('Citizen Dashboard Rendering & Hierarchy Tests', () {
    testWidgets('renders brand, greeting with first name, and context message', (tester) async {
      await tester.pumpWidget(createCitizenTestApp());
      await tester.pumpAndSettle();

      // Brand title in AppBar
      expect(find.text('SmartWaste'), findsOneWidget);

      // Greeting with extracted first name
      expect(find.byKey(const Key('citizen_greeting_text')), findsOneWidget);
      expect(find.text('Hello, Nimali'), findsOneWidget);
      expect(find.text('Help keep your community clean and healthy.'), findsOneWidget);

      // Top action buttons
      expect(find.byKey(const Key('citizen_notification_button')), findsOneWidget);
      expect(find.byKey(const Key('citizen_account_button')), findsOneWidget);
    });

    testWidgets('renders primary Report Waste CTA with prominent styling', (tester) async {
      await tester.pumpWidget(createCitizenTestApp());
      await tester.pumpAndSettle();

      final reportWasteCta = find.byKey(const Key('citizen_report_waste_cta'));
      expect(reportWasteCta, findsOneWidget);
      expect(find.text('Report Waste'), findsOneWidget);
      expect(find.text('Report waste using a location, description and photo.'), findsOneWidget);
      expect(find.text('Report Issue'), findsOneWidget);
      expect(find.byKey(const Key('citizen_report_waste_illustration')), findsOneWidget);
    });

    testWidgets('renders 5 Quick Access items and Recent Activity empty state', (tester) async {
      await tester.pumpWidget(createCitizenTestApp());
      await tester.pumpAndSettle();

      // Quick Access section
      expect(find.text('Quick Access'), findsOneWidget);
      expect(find.byKey(const Key('citizen_quick_access_reports')), findsOneWidget);
      expect(find.byKey(const Key('citizen_quick_access_bins')), findsOneWidget);
      expect(find.text('Nearby Bins'), findsOneWidget);
      expect(find.text('Find waste bins near your location.'), findsOneWidget);
      expect(find.byKey(const Key('citizen_quick_access_complaints')), findsOneWidget);
      expect(find.byKey(const Key('citizen_quick_access_notifications')), findsOneWidget);
      expect(find.byKey(const Key('citizen_quick_access_profile')), findsOneWidget);

      // Recent Activity section
      expect(find.byKey(const Key('citizen_recent_activity_section')), findsOneWidget);
      expect(find.text('No Recent Activity'), findsOneWidget);
      expect(
        find.text('Your submitted waste reports, status updates, and service responses will appear here.'),
        findsOneWidget,
      );
      expect(find.text('Report Waste Now'), findsOneWidget);
    });

    testWidgets('renders bottom navigation with 4 destinations', (tester) async {
      await tester.pumpWidget(createCitizenTestApp());
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('citizen_bottom_nav')), findsOneWidget);
      expect(find.byKey(const Key('citizen_bottom_nav_home')), findsOneWidget);
      expect(find.byKey(const Key('citizen_bottom_nav_reports')), findsOneWidget);
      expect(find.byKey(const Key('citizen_bottom_nav_complaints')), findsOneWidget);
      expect(find.byKey(const Key('citizen_bottom_nav_profile')), findsOneWidget);
    });
  });

  group('Citizen Navigation & Flow Tests', () {
    testWidgets('tapping Report Waste CTA navigates to /citizen/report-waste placeholder', (tester) async {
      await tester.pumpWidget(createCitizenTestApp());
      await tester.pumpAndSettle();

      final cta = find.byKey(const Key('citizen_report_waste_cta'));
      await tester.tap(cta);
      await tester.pumpAndSettle();

      // Should be on Report Waste placeholder screen
      expect(find.byType(CitizenPlaceholderScreen), findsOneWidget);
      expect(find.text('Waste reporting using a location, description and photo will be available here.'), findsOneWidget);

      // Back to dashboard
      final backButton = find.byKey(const Key('placeholder_back_button'));
      await tester.tap(backButton);
      await tester.pumpAndSettle();

      expect(find.byType(CitizenDashboardScreen), findsOneWidget);
    });

    testWidgets('tapping Nearby Bins card navigates to /citizen/nearby-bins placeholder', (tester) async {
      await tester.pumpWidget(createCitizenTestApp());
      await tester.pumpAndSettle();

      final binsCard = find.byKey(const Key('citizen_quick_access_bins'));
      await tester.ensureVisible(binsCard);
      await tester.tap(binsCard);
      await tester.pumpAndSettle();

      // Should be on Nearby Bins placeholder screen
      expect(find.byType(CitizenPlaceholderScreen), findsOneWidget);
      expect(find.text('Find waste bins near your location.'), findsOneWidget);

      // Back to dashboard
      final backButton = find.byKey(const Key('placeholder_back_button'));
      await tester.tap(backButton);
      await tester.pumpAndSettle();

      expect(find.byType(CitizenDashboardScreen), findsOneWidget);
    });

    testWidgets('tapping bottom nav items switches tabs seamlessly', (tester) async {
      await tester.pumpWidget(createCitizenTestApp());
      await tester.pumpAndSettle();

      // Tap My Reports
      await tester.tap(find.byKey(const Key('citizen_bottom_nav_reports')));
      await tester.pumpAndSettle();
      expect(find.text('Your waste report history and tracking will be available here.'), findsOneWidget);

      // Tap Complaints
      await tester.tap(find.byKey(const Key('citizen_bottom_nav_complaints')));
      await tester.pumpAndSettle();
      expect(find.text('Report or track service concerns and operational quality issues.'), findsOneWidget);

      // Tap Profile
      await tester.tap(find.byKey(const Key('citizen_bottom_nav_profile')));
      await tester.pumpAndSettle();
      expect(find.text('Manage your personal account and contact information.'), findsOneWidget);

      // Tap Home
      await tester.tap(find.byKey(const Key('citizen_bottom_nav_home')));
      await tester.pumpAndSettle();
      expect(find.byType(CitizenDashboardScreen), findsOneWidget);
    });

    testWidgets('system back button from non-dashboard tab navigates back to dashboard', (tester) async {
      await tester.pumpWidget(createCitizenTestApp());
      await tester.pumpAndSettle();

      // Tap My Reports
      await tester.tap(find.byKey(const Key('citizen_bottom_nav_reports')));
      await tester.pumpAndSettle();
      expect(find.text('Your waste report history and tracking will be available here.'), findsOneWidget);

      // Trigger system back button
      await tester.binding.handlePopRoute();
      await tester.pumpAndSettle();

      // Should be back on Citizen Dashboard
      expect(find.byType(CitizenDashboardScreen), findsOneWidget);

      // Tap Complaints
      await tester.tap(find.byKey(const Key('citizen_bottom_nav_complaints')));
      await tester.pumpAndSettle();
      expect(find.text('Report or track service concerns and operational quality issues.'), findsOneWidget);

      // Trigger system back button
      await tester.binding.handlePopRoute();
      await tester.pumpAndSettle();

      // Should be back on Citizen Dashboard
      expect(find.byType(CitizenDashboardScreen), findsOneWidget);

      // Tap Profile
      await tester.tap(find.byKey(const Key('citizen_bottom_nav_profile')));
      await tester.pumpAndSettle();
      expect(find.text('Manage your personal account and contact information.'), findsOneWidget);

      // Trigger system back button
      await tester.binding.handlePopRoute();
      await tester.pumpAndSettle();

      // Should be back on Citizen Dashboard
      expect(find.byType(CitizenDashboardScreen), findsOneWidget);
    });

    testWidgets('tapping top notifications button navigates to notifications screen', (tester) async {
      await tester.pumpWidget(createCitizenTestApp());
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('citizen_notification_button')));
      await tester.pumpAndSettle();

      expect(find.text('Status updates and municipal alerts will be available here.'), findsOneWidget);
    });
  });

  group('Citizen Account Actions Tests', () {
    testWidgets('opens account bottom sheet and displays citizen details', (tester) async {
      await tester.pumpWidget(createCitizenTestApp());
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('citizen_account_button')));
      await tester.pumpAndSettle();

      expect(find.text('Nimali Fernando'), findsOneWidget);
      expect(find.text('nimali@smartwaste.lk'), findsOneWidget);
      expect(find.text('Citizen'), findsAtLeastNWidgets(1));
      expect(find.byKey(const Key('account_sheet_change_password')), findsOneWidget);
      expect(find.byKey(const Key('account_sheet_logout')), findsOneWidget);
    });

    testWidgets('tapping Change Password in account sheet navigates to ChangePasswordScreen', (tester) async {
      await tester.pumpWidget(createCitizenTestApp());
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('citizen_account_button')));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('account_sheet_change_password')));
      await tester.pumpAndSettle();

      expect(find.byType(ChangePasswordScreen), findsOneWidget);
    });

    testWidgets('tapping Logout in account sheet triggers notifier logout and navigates to Login', (tester) async {
      final mockNotifier = MockCitizenAuthNotifier(const AuthState.authenticated(citizenUser));

      await tester.pumpWidget(createCitizenTestApp(
        notifierOverride: () => mockNotifier,
      ));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('citizen_account_button')));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('account_sheet_logout')));
      await tester.pumpAndSettle();

      expect(mockNotifier.logoutCalled, isTrue);
      expect(find.byType(LoginScreen), findsOneWidget);
    });
  });

  group('Responsive Viewport Tests', () {
    testWidgets('renders cleanly without overflow on narrow 320px viewport', (tester) async {
      tester.view.physicalSize = const Size(320 * 3.0, 640 * 3.0);
      tester.view.devicePixelRatio = 3.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      await tester.pumpWidget(createCitizenTestApp());
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
      expect(find.byType(CitizenDashboardScreen), findsOneWidget);
      expect(find.byKey(const Key('citizen_report_waste_cta')), findsOneWidget);
      expect(find.text('Report Waste'), findsOneWidget);
    });

    testWidgets('renders cleanly without overflow on standard 390px viewport', (tester) async {
      tester.view.physicalSize = const Size(390 * 3.0, 844 * 3.0);
      tester.view.devicePixelRatio = 3.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      await tester.pumpWidget(createCitizenTestApp());
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
      expect(find.byType(CitizenDashboardScreen), findsOneWidget);
      expect(find.byKey(const Key('citizen_report_waste_cta')), findsOneWidget);
    });
  });
}
