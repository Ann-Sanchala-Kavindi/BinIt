import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/presentation/change_password_screen.dart';
import '../../features/auth/presentation/home_screen.dart';
import '../../features/auth/presentation/login_screen.dart';
import '../../features/auth/presentation/register_screen.dart';
import '../../features/auth/presentation/splash_screen.dart';
import '../../features/auth/providers/auth_provider.dart';
import '../../features/citizen/presentation/citizen_dashboard_screen.dart';
import '../../features/citizen/presentation/citizen_placeholder_screen.dart';
import '../../features/bins/data/public_waste_bins_repository.dart';
import '../../features/bins/presentation/bin_details_screen.dart';
import '../../features/bins/presentation/find_bins_screen.dart';
import '../../features/driver/presentation/driver_dashboard_screen.dart';
import '../../features/driver/presentation/driver_placeholder_screen.dart';
import '../../features/reporting/data/reporting_repository.dart';
import '../../features/reporting/models/waste_report_detail_model.dart';
import '../../features/reporting/presentation/edit_report_screen.dart';
import '../../features/reporting/presentation/manage_report_photos_screen.dart';
import '../../features/reporting/presentation/my_reports_screen.dart';
import '../../features/reporting/presentation/report_detail_screen.dart';
import '../../features/reporting/presentation/report_waste_screen.dart';
import '../../shared/widgets/authenticated_mobile_shell.dart';

/// Helper to trigger GoRouter redirects when Riverpod AuthState updates.
class AuthRouterListenable extends ChangeNotifier {
  final Ref _ref;

  AuthRouterListenable(this._ref) {
    _ref.listen<AuthState>(authProvider, (previous, next) {
      notifyListeners();
    });
  }
}

final authRouterListenableProvider = Provider<AuthRouterListenable>((ref) {
  return AuthRouterListenable(ref);
});

final appRouterProvider = Provider<GoRouter>((ref) {
  final refreshListenable = ref.watch(authRouterListenableProvider);

  return GoRouter(
    initialLocation: '/splash',
    refreshListenable: refreshListenable,
    routes: [
      GoRoute(
        path: '/splash',
        builder: (context, state) => const SplashScreen(),
      ),
      GoRoute(path: '/login', builder: (context, state) => const LoginScreen()),
      GoRoute(
        path: '/register',
        builder: (context, state) => const RegisterScreen(),
      ),
      GoRoute(path: '/home', builder: (context, state) => const HomeScreen()),
      GoRoute(
        path: '/change-password',
        builder: (context, state) => const ChangePasswordScreen(),
      ),

      // Citizen ShellRoute for persistent bottom navigation
      ShellRoute(
        builder: (context, state, child) {
          return AuthenticatedMobileShell(
            currentLocation: state.matchedLocation,
            destinations: const [
              MobileNavDestination(
                label: 'Home',
                icon: Icons.home_outlined,
                selectedIcon: Icons.home,
                route: '/citizen/dashboard',
                key: Key('citizen_bottom_nav_home'),
              ),
              MobileNavDestination(
                label: 'My Reports',
                icon: Icons.assignment_outlined,
                selectedIcon: Icons.assignment,
                route: '/citizen/reports',
                key: Key('citizen_bottom_nav_reports'),
              ),
              MobileNavDestination(
                label: 'Complaints',
                icon: Icons.feedback_outlined,
                selectedIcon: Icons.feedback,
                route: '/citizen/complaints',
                key: Key('citizen_bottom_nav_complaints'),
              ),
              MobileNavDestination(
                label: 'Profile',
                icon: Icons.person_outline,
                selectedIcon: Icons.person,
                route: '/citizen/profile',
                key: Key('citizen_bottom_nav_profile'),
              ),
            ],
            child: child,
          );
        },
        routes: [
          GoRoute(
            path: '/citizen/dashboard',
            builder: (context, state) => const CitizenDashboardScreen(),
          ),
          GoRoute(
            path: '/citizen/reports',
            builder: (context, state) => MyReportsScreen(
              repository: ref.watch(reportingRepositoryProvider),
            ),
          ),
          GoRoute(
            path: '/citizen/complaints',
            builder: (context, state) => const CitizenPlaceholderScreen(
              title: 'Complaints',
              description: 'Report or track service concerns and operational quality issues.',
              icon: Icons.feedback_outlined,
            ),
          ),
          GoRoute(
            path: '/citizen/profile',
            builder: (context, state) => const CitizenPlaceholderScreen(
              title: 'Citizen Profile',
              description:
                  'Manage your personal account and contact information.',
              icon: Icons.person_outline,
            ),
          ),
        ],
      ),

      // Standalone Citizen Sub-flow Routes (Dedicated screen with Back action)
      GoRoute(
        path: '/citizen/my-reports',
        builder: (context, state) => MyReportsScreen(
          repository: ref.watch(reportingRepositoryProvider),
          showAppBar: true,
        ),
      ),
      GoRoute(
        path: '/citizen/report-waste',
        builder: (context, state) => const ReportWasteScreen(),
      ),
      GoRoute(
        path: '/citizen/reports/:id',
        builder: (context, state) => ReportDetailScreen(
          reportId: state.pathParameters['id']!,
          repository: ref.watch(reportingRepositoryProvider),
        ),
      ),
      GoRoute(
        path: '/citizen/reports/:id/edit',
        builder: (context, state) => EditReportScreen(
          reportId: state.pathParameters['id']!,
          initialReport: state.extra as WasteReportDetailModel?,
          repository: ref.watch(reportingRepositoryProvider),
        ),
      ),
      GoRoute(
        path: '/citizen/reports/:id/photos',
        builder: (context, state) => ManageReportPhotosScreen(
          reportId: state.pathParameters['id']!,
          initialReport: state.extra as WasteReportDetailModel?,
          repository: ref.watch(reportingRepositoryProvider),
        ),
      ),
      GoRoute(
        path: '/citizen/notifications',
        builder: (context, state) => const CitizenPlaceholderScreen(
          title: 'Notifications',
          description:
              'Status updates and municipal alerts will be available here.',
          icon: Icons.notifications_outlined,
          hasScaffold: true,
        ),
      ),
      GoRoute(
        path: '/citizen/nearby-bins',
        builder: (context, state) => FindBinsScreen(
          repository: ref.watch(publicWasteBinsRepositoryProvider),
        ),
      ),
      GoRoute(
        path: '/citizen/nearby-bins/:id',
        builder: (context, state) => BinDetailsScreen(
          binId: state.pathParameters['id']!,
          repository: ref.watch(publicWasteBinsRepositoryProvider),
        ),
      ),

      // Driver ShellRoute for persistent bottom navigation
      ShellRoute(
        builder: (context, state, child) {
          return AuthenticatedMobileShell(
            currentLocation: state.matchedLocation,
            bottomNavKey: const Key('driver_bottom_nav'),
            notificationRoute: '/driver/notifications',
            notificationKey: const Key('driver_notification_button'),
            accountKey: const Key('driver_account_button'),
            destinations: const [
              MobileNavDestination(
                label: 'Home',
                icon: Icons.home_outlined,
                selectedIcon: Icons.home,
                route: '/driver/dashboard',
                key: Key('driver_bottom_nav_home'),
              ),
              MobileNavDestination(
                label: 'Tasks',
                icon: Icons.checklist_outlined,
                selectedIcon: Icons.checklist,
                route: '/driver/tasks',
                key: Key('driver_bottom_nav_tasks'),
              ),
              MobileNavDestination(
                label: 'Route',
                icon: Icons.alt_route,
                selectedIcon: Icons.alt_route_rounded,
                route: '/driver/route',
                key: Key('driver_bottom_nav_route'),
              ),
              MobileNavDestination(
                label: 'Profile',
                icon: Icons.person_outline,
                selectedIcon: Icons.person,
                route: '/driver/profile',
                key: Key('driver_bottom_nav_profile'),
              ),
            ],
            child: child,
          );
        },
        routes: [
          GoRoute(
            path: '/driver/dashboard',
            builder: (context, state) => const DriverDashboardScreen(),
          ),
          GoRoute(
            path: '/driver/tasks',
            builder: (context, state) => const DriverPlaceholderScreen(
              title: 'Collection Tasks',
              description: 'Your assigned collection tasks will appear here.',
              icon: Icons.checklist,
            ),
          ),
          GoRoute(
            path: '/driver/route',
            builder: (context, state) => const DriverPlaceholderScreen(
              title: 'Route',
              description: 'Your collection route and stops will appear here.',
              icon: Icons.alt_route,
            ),
          ),
          GoRoute(
            path: '/driver/profile',
            builder: (context, state) => const DriverPlaceholderScreen(
              title: 'Driver Profile',
              description: 'Manage your account and operational credentials.',
              icon: Icons.person_outline,
              isProfile: true,
            ),
          ),
        ],
      ),

      // Standalone Driver Sub-flow Routes (Dedicated screen with Back action)
      GoRoute(
        path: '/driver/assignment',
        builder: (context, state) => const DriverPlaceholderScreen(
          title: 'My Assignment',
          description: 'Your active collection assignment will appear here.',
          icon: Icons.assignment_outlined,
          hasScaffold: true,
        ),
      ),
      GoRoute(
        path: '/driver/incidents',
        builder: (context, state) => const DriverPlaceholderScreen(
          title: 'Report Incident',
          description: 'You will be able to report collection or vehicle-related operational issues here.',
          icon: Icons.warning_amber_rounded,
          hasScaffold: true,
        ),
      ),
      GoRoute(
        path: '/driver/notifications',
        builder: (context, state) => const DriverPlaceholderScreen(
          title: 'Notifications',
          description: 'Assignment and operational updates will appear here.',
          icon: Icons.notifications_outlined,
          hasScaffold: true,
        ),
      ),
    ],
    redirect: (context, state) {
      final authState = ref.read(authProvider);
      final isAuth = authState.isAuthenticated;
      final isChecking = authState.isInitial || authState.isLoading;

      final location = state.matchedLocation;
      final isAuthRoute = location == '/login' || location == '/register';
      final isSplash = location == '/splash';
      final isChangePassword = location == '/change-password';

      // 1. If still checking stored token, stay on or go to splash
      if (isChecking) {
        return isSplash ? null : '/splash';
      }

      // 2. If unauthenticated, only allow login or register
      if (!isAuth) {
        return isAuthRoute ? null : '/login';
      }

      // 3. If authenticated and mustChangePassword, force /change-password
      if (authState.mustChangePassword) {
        return isChangePassword ? null : '/change-password';
      }

      // 4. Role determination
      final user = authState.user;
      final isCitizen = user?.isCitizen ?? false;
      final isDriver = user?.isDriver ?? false;

      // Defensive: unrecognized or unsupported platform role
      if (!isCitizen && !isDriver) {
        return '/login';
      }

      final defaultRoleRoute = isCitizen
          ? '/citizen/dashboard'
          : '/driver/dashboard';

      // 5. If authenticated and on login, register, splash, or root, redirect to role destination
      if (isAuthRoute || isSplash || location == '/') {
        return defaultRoleRoute;
      }

      // 6. Generic /home route redirected to role dashboard
      if (location == '/home') {
        return defaultRoleRoute;
      }

      // 7. Citizen accessing driver routes -> redirect to citizen dashboard
      if (isCitizen && location.startsWith('/driver')) {
        return '/citizen/dashboard';
      }

      // 8. Driver accessing citizen routes -> redirect to driver dashboard
      if (isDriver && location.startsWith('/citizen')) {
        return '/driver/dashboard';
      }

      // Allow access to requested route (including voluntary /change-password)
      return null;
    },
  );
});
