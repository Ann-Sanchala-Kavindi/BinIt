import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/routing/app_router.dart';
import 'package:mobile/features/auth/models/auth_user.dart';
import 'package:mobile/features/auth/presentation/change_password_screen.dart';
import 'package:mobile/features/auth/presentation/login_screen.dart';
import 'package:mobile/features/auth/providers/auth_provider.dart';
import 'package:mobile/features/driver/presentation/driver_dashboard_screen.dart';
import 'package:mobile/features/driver/presentation/driver_placeholder_screen.dart';

class MockDriverAuthNotifier extends AuthNotifier {
  final AuthState _initial;
  bool logoutCalled = false;

  MockDriverAuthNotifier(this._initial);

  @override
  AuthState build() => _initial;

  @override
  Future<void> logout() async {
    logoutCalled = true;
    state = const AuthState.unauthenticated();
  }
}

void main() {
  const driverUser = AuthUser(
    id: 'driver-789',
    fullName: 'Samantha Perera',
    email: 'samantha.p@smartwaste.lk',
    role: AppRoles.driver,
  );

  Widget createDriverTestApp({
    AuthNotifier Function()? notifierOverride,
    Size? surfaceSize,
  }) {
    return ProviderScope(
      overrides: [
        authProvider.overrideWith(
          notifierOverride ?? () => MockDriverAuthNotifier(const AuthState.authenticated(driverUser)),
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

  group('Driver Dashboard Rendering & Hierarchy Tests', () {
    testWidgets('renders brand, greeting with driver first name, and operational context', (tester) async {
      await tester.pumpWidget(createDriverTestApp());
      await tester.pumpAndSettle();

      // Brand title in AppBar
      expect(find.text('SmartWaste'), findsOneWidget);

      // Greeting with extracted first name
      expect(find.byKey(const Key('driver_greeting_text')), findsOneWidget);
      expect(find.text('Hello, Samantha'), findsOneWidget);
      expect(find.textContaining('Ready for today\'s operations'), findsOneWidget);

      // Top action buttons
      expect(find.byKey(const Key('driver_notification_button')), findsOneWidget);
      expect(find.byKey(const Key('driver_account_button')), findsOneWidget);
    });

    testWidgets('renders primary Current Assignment section with prominent styling', (tester) async {
      await tester.pumpWidget(createDriverTestApp());
      await tester.pumpAndSettle();

      expect(find.text('Current Assignment'), findsOneWidget);
      expect(find.text('No active assignment'), findsOneWidget);
      expect(find.text('Your active collection assignment will appear here once operations are connected.'), findsOneWidget);
      expect(find.byKey(const Key('driver_view_assignment_button')), findsOneWidget);
      expect(find.text('View Assignment'), findsOneWidget);
    });

    testWidgets('renders 4 Quick Action cards and operational empty states', (tester) async {
      await tester.pumpWidget(createDriverTestApp());
      await tester.pumpAndSettle();

      // Quick Actions section
      expect(find.text('Quick Actions'), findsOneWidget);
      final tasksCard = find.byKey(const Key('driver_quick_action_tasks'));
      expect(tasksCard, findsOneWidget);
      expect(find.descendant(of: tasksCard, matching: find.text('Collection Tasks')), findsOneWidget);
      expect(find.descendant(of: tasksCard, matching: find.text('View your assigned collection work.')), findsOneWidget);

      final routeCard = find.byKey(const Key('driver_quick_action_route'));
      expect(routeCard, findsOneWidget);
      expect(find.descendant(of: routeCard, matching: find.text('Route')), findsOneWidget);
      expect(find.descendant(of: routeCard, matching: find.text('View your collection route and stops.')), findsOneWidget);

      final incidentsCard = find.byKey(const Key('driver_quick_action_incidents'));
      expect(incidentsCard, findsOneWidget);
      expect(find.descendant(of: incidentsCard, matching: find.text('Report Incident')), findsOneWidget);
      expect(find.descendant(of: incidentsCard, matching: find.text('Report a problem during collection.')), findsOneWidget);

      final notificationsCard = find.byKey(const Key('driver_quick_action_notifications'));
      expect(notificationsCard, findsOneWidget);
      expect(find.descendant(of: notificationsCard, matching: find.text('Notifications')), findsOneWidget);
      expect(find.descendant(of: notificationsCard, matching: find.text('View assignment and service updates.')), findsOneWidget);

      // Today's Work empty state
      expect(find.byKey(const Key('driver_todays_work_section')), findsOneWidget);
      expect(find.text('Today\'s Work'), findsOneWidget);
      expect(find.text('No Tasks In Progress'), findsOneWidget);
      expect(find.text('Your assigned collection tasks and progress will appear here.'), findsOneWidget);

      // Recent Updates empty state
      expect(find.byKey(const Key('driver_recent_updates_section')), findsOneWidget);
      expect(find.text('Recent Updates'), findsOneWidget);
      expect(find.text('No Recent Updates'), findsOneWidget);
      expect(find.text('Assignment and operational updates will appear here.'), findsOneWidget);
    });

    testWidgets('renders bottom navigation with 4 Driver destinations', (tester) async {
      await tester.pumpWidget(createDriverTestApp());
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('driver_bottom_nav')), findsOneWidget);
      expect(find.byKey(const Key('driver_bottom_nav_home')), findsOneWidget);
      expect(find.byKey(const Key('driver_bottom_nav_tasks')), findsOneWidget);
      expect(find.byKey(const Key('driver_bottom_nav_route')), findsOneWidget);
      expect(find.byKey(const Key('driver_bottom_nav_profile')), findsOneWidget);
    });
  });

  group('Driver Navigation & Flow Tests', () {
    testWidgets('tapping View Assignment button navigates to /driver/assignment placeholder', (tester) async {
      await tester.pumpWidget(createDriverTestApp());
      await tester.pumpAndSettle();

      final viewAssignmentBtn = find.byKey(const Key('driver_view_assignment_button'));
      await tester.tap(viewAssignmentBtn);
      await tester.pumpAndSettle();

      // Should be on Assignment placeholder screen
      expect(find.byType(DriverPlaceholderScreen), findsOneWidget);
      expect(find.text('My Assignment'), findsAtLeastNWidgets(1));
      expect(find.text('Your active collection assignment will appear here.'), findsOneWidget);

      // Back to dashboard
      final backButton = find.byKey(const Key('driver_placeholder_back_button'));
      await tester.tap(backButton);
      await tester.pumpAndSettle();

      expect(find.byType(DriverDashboardScreen), findsOneWidget);
    });

    testWidgets('tapping Quick Action cards navigates to corresponding driver placeholders', (tester) async {
      await tester.pumpWidget(createDriverTestApp());
      await tester.pumpAndSettle();

      // 1. Collection Tasks
      final tasksCard = find.byKey(const Key('driver_quick_action_tasks'));
      await tester.ensureVisible(tasksCard);
      await tester.tap(tasksCard);
      await tester.pumpAndSettle();
      expect(find.text('Your assigned collection tasks will appear here.'), findsOneWidget);

      // Back via bottom nav home
      await tester.tap(find.byKey(const Key('driver_bottom_nav_home')));
      await tester.pumpAndSettle();
      expect(find.byType(DriverDashboardScreen), findsOneWidget);

      // 2. Route
      final routeCard = find.byKey(const Key('driver_quick_action_route'));
      await tester.ensureVisible(routeCard);
      await tester.tap(routeCard);
      await tester.pumpAndSettle();
      expect(find.text('Your collection route and stops will appear here.'), findsOneWidget);

      // Back via bottom nav home
      await tester.tap(find.byKey(const Key('driver_bottom_nav_home')));
      await tester.pumpAndSettle();
      expect(find.byType(DriverDashboardScreen), findsOneWidget);

      // 3. Report Incident
      final incidentsCard = find.byKey(const Key('driver_quick_action_incidents'));
      await tester.ensureVisible(incidentsCard);
      await tester.tap(incidentsCard);
      await tester.pumpAndSettle();
      expect(find.text('You will be able to report collection or vehicle-related operational issues here.'), findsOneWidget);

      // Back via placeholder back button
      await tester.tap(find.byKey(const Key('driver_placeholder_back_button')));
      await tester.pumpAndSettle();
      expect(find.byType(DriverDashboardScreen), findsOneWidget);

      // 4. Notifications
      final notifCard = find.byKey(const Key('driver_quick_action_notifications'));
      await tester.ensureVisible(notifCard);
      await tester.tap(notifCard);
      await tester.pumpAndSettle();
      expect(find.text('Assignment and operational updates will appear here.'), findsAtLeastNWidgets(1));

      // Back via placeholder back button
      await tester.tap(find.byKey(const Key('driver_placeholder_back_button')));
      await tester.pumpAndSettle();
      expect(find.byType(DriverDashboardScreen), findsOneWidget);
    });

    testWidgets('tapping bottom nav items switches tabs seamlessly', (tester) async {
      await tester.pumpWidget(createDriverTestApp());
      await tester.pumpAndSettle();

      // Tap Tasks
      await tester.tap(find.byKey(const Key('driver_bottom_nav_tasks')));
      await tester.pumpAndSettle();
      expect(find.text('Your assigned collection tasks will appear here.'), findsOneWidget);

      // Tap Route
      await tester.tap(find.byKey(const Key('driver_bottom_nav_route')));
      await tester.pumpAndSettle();
      expect(find.text('Your collection route and stops will appear here.'), findsOneWidget);

      // Tap Profile
      await tester.tap(find.byKey(const Key('driver_bottom_nav_profile')));
      await tester.pumpAndSettle();
      expect(find.text('Manage your account and operational credentials.'), findsOneWidget);

      // Tap Home
      await tester.tap(find.byKey(const Key('driver_bottom_nav_home')));
      await tester.pumpAndSettle();
      expect(find.byType(DriverDashboardScreen), findsOneWidget);
    });

    testWidgets('system back button from non-dashboard tab navigates back to dashboard', (tester) async {
      await tester.pumpWidget(createDriverTestApp());
      await tester.pumpAndSettle();

      // Tap Tasks
      await tester.tap(find.byKey(const Key('driver_bottom_nav_tasks')));
      await tester.pumpAndSettle();
      expect(find.text('Your assigned collection tasks will appear here.'), findsOneWidget);

      // Trigger system back button
      await tester.binding.handlePopRoute();
      await tester.pumpAndSettle();

      // Should be back on Driver Dashboard
      expect(find.byType(DriverDashboardScreen), findsOneWidget);

      // Tap Route
      await tester.tap(find.byKey(const Key('driver_bottom_nav_route')));
      await tester.pumpAndSettle();
      expect(find.text('Your collection route and stops will appear here.'), findsOneWidget);

      // Trigger system back button
      await tester.binding.handlePopRoute();
      await tester.pumpAndSettle();

      // Should be back on Driver Dashboard
      expect(find.byType(DriverDashboardScreen), findsOneWidget);

      // Tap Profile
      await tester.tap(find.byKey(const Key('driver_bottom_nav_profile')));
      await tester.pumpAndSettle();
      expect(find.text('Manage your account and operational credentials.'), findsOneWidget);

      // Trigger system back button
      await tester.binding.handlePopRoute();
      await tester.pumpAndSettle();

      // Should be back on Driver Dashboard
      expect(find.byType(DriverDashboardScreen), findsOneWidget);
    });

    testWidgets('tapping top notifications button navigates to notifications screen', (tester) async {
      await tester.pumpWidget(createDriverTestApp());
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('driver_notification_button')));
      await tester.pumpAndSettle();

      expect(find.text('Assignment and operational updates will appear here.'), findsAtLeastNWidgets(1));
    });
  });

  group('Driver Account Actions Tests', () {
    testWidgets('opens account bottom sheet and displays driver details', (tester) async {
      await tester.pumpWidget(createDriverTestApp());
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('driver_account_button')));
      await tester.pumpAndSettle();

      expect(find.text('Samantha Perera'), findsOneWidget);
      expect(find.text('samantha.p@smartwaste.lk'), findsOneWidget);
      expect(find.text('Driver'), findsAtLeastNWidgets(1));
      expect(find.byKey(const Key('account_sheet_change_password')), findsOneWidget);
      expect(find.byKey(const Key('account_sheet_logout')), findsOneWidget);
    });

    testWidgets('tapping Change Password in account sheet navigates to ChangePasswordScreen', (tester) async {
      await tester.pumpWidget(createDriverTestApp());
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('driver_account_button')));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('account_sheet_change_password')));
      await tester.pumpAndSettle();

      expect(find.byType(ChangePasswordScreen), findsOneWidget);
    });

    testWidgets('tapping Logout in account sheet triggers notifier logout and navigates to Login', (tester) async {
      final mockNotifier = MockDriverAuthNotifier(const AuthState.authenticated(driverUser));

      await tester.pumpWidget(createDriverTestApp(
        notifierOverride: () => mockNotifier,
      ));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('driver_account_button')));
      await tester.pumpAndSettle();

      await tester.tap(find.byKey(const Key('account_sheet_logout')));
      await tester.pumpAndSettle();

      expect(mockNotifier.logoutCalled, isTrue);
      expect(find.byType(LoginScreen), findsOneWidget);
    });
  });

  group('Driver Responsive Viewport Tests', () {
    testWidgets('renders cleanly without overflow on narrow 320px viewport', (tester) async {
      tester.view.physicalSize = const Size(320 * 3.0, 640 * 3.0);
      tester.view.devicePixelRatio = 3.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      await tester.pumpWidget(createDriverTestApp());
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
      expect(find.byType(DriverDashboardScreen), findsOneWidget);
      expect(find.byKey(const Key('driver_view_assignment_button')), findsOneWidget);
      expect(find.text('Current Assignment'), findsOneWidget);
    });

    testWidgets('renders cleanly without overflow on standard 390px viewport', (tester) async {
      tester.view.physicalSize = const Size(390 * 3.0, 844 * 3.0);
      tester.view.devicePixelRatio = 3.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      await tester.pumpWidget(createDriverTestApp());
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
      expect(find.byType(DriverDashboardScreen), findsOneWidget);
      expect(find.byKey(const Key('driver_view_assignment_button')), findsOneWidget);
    });

    testWidgets('renders cleanly without overflow on large 430px viewport', (tester) async {
      tester.view.physicalSize = const Size(430 * 3.0, 932 * 3.0);
      tester.view.devicePixelRatio = 3.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      await tester.pumpWidget(createDriverTestApp());
      await tester.pumpAndSettle();

      expect(tester.takeException(), isNull);
      expect(find.byType(DriverDashboardScreen), findsOneWidget);
      expect(find.byKey(const Key('driver_view_assignment_button')), findsOneWidget);
    });
  });
}
