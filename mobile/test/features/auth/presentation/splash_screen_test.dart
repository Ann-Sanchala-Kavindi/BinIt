import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile/core/routing/app_router.dart';
import 'package:mobile/features/auth/data/auth_repository.dart';
import 'package:mobile/features/auth/models/auth_user.dart';
import 'package:mobile/features/auth/presentation/login_screen.dart';
import 'package:mobile/features/auth/presentation/splash_screen.dart';
import 'package:mobile/features/auth/providers/auth_provider.dart';
import 'package:mobile/features/citizen/presentation/citizen_dashboard_screen.dart';
import 'package:mobile/features/driver/data/driver_repository.dart';
import 'package:mobile/features/driver/models/assignment_detail_model.dart';
import 'package:mobile/features/driver/models/collection_assignment_status.dart';
import 'package:mobile/features/driver/models/driver_availability_status.dart';
import 'package:mobile/features/driver/models/driver_self_model.dart';
import 'package:mobile/features/driver/models/paged_assignments_model.dart';
import 'package:mobile/features/driver/presentation/driver_dashboard_screen.dart';
import 'package:mobile/features/reporting/data/reporting_repository.dart';
import 'package:mobile/features/reporting/models/paged_waste_reports_model.dart';
import 'package:mobile/features/reporting/models/waste_report_status.dart';
import 'package:mobile/features/reporting/models/waste_type.dart';

class StartupAuthRepository extends AuthRepository {
  StartupAuthRepository({
    this.user,
    this.tokenLookup,
    this.logoutFails = false,
  });

  final AuthUser? user;
  final Future<bool> Function()? tokenLookup;
  final bool logoutFails;
  bool logoutCalled = false;

  @override
  Future<bool> hasToken() => tokenLookup?.call() ?? Future.value(user != null);

  @override
  Future<AuthUser> getCurrentUser() async {
    if (user == null) throw StateError('Invalid session');
    return user!;
  }

  @override
  Future<void> logout() async {
    logoutCalled = true;
    if (logoutFails) throw StateError('Secure storage unavailable');
  }
}

class StartupReportingRepository extends ReportingRepository {
  @override
  Future<PagedWasteReportsModel> getWasteReports({
    int page = 1,
    int pageSize = 20,
    WasteReportStatus? status,
    WasteType? wasteType,
    String? search,
    String? sortBy = 'createdAt',
    String? sortDirection = 'desc',
  }) async => const PagedWasteReportsModel(
    items: [],
    page: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 0,
  );
}

class StartupDriverRepository extends DriverRepository {
  @override
  Future<DriverSelfModel> getDriverSelf(String driverId) async =>
      DriverSelfModel(
        id: driverId,
        displayName: 'Startup Driver',
        availabilityStatus: DriverAvailabilityStatus.available,
        isOccupied: false,
      );

  @override
  Future<AssignmentDetailModel?> getCurrentAssignmentDetail() async => null;

  @override
  Future<PagedAssignmentsModel> getMyAssignments({
    CollectionAssignmentStatus? status,
    int page = 1,
    int pageSize = 20,
  }) async => const PagedAssignmentsModel(
    items: [],
    page: 1,
    pageSize: 20,
    totalCount: 0,
    totalPages: 1,
  );
}

Future<GoRouter> pumpStartup(
  WidgetTester tester,
  StartupAuthRepository repository,
) async {
  late GoRouter router;
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        authRepositoryProvider.overrideWithValue(repository),
        reportingRepositoryProvider.overrideWithValue(
          StartupReportingRepository(),
        ),
        driverRepositoryProvider.overrideWithValue(StartupDriverRepository()),
      ],
      child: Consumer(
        builder: (context, ref, _) {
          router = ref.watch(appRouterProvider);
          return MaterialApp.router(routerConfig: router);
        },
      ),
    ),
  );
  await tester.pump();
  return router;
}

void main() {
  const citizen = AuthUser(
    id: 'citizen-1',
    fullName: 'Startup Citizen',
    email: 'citizen@example.test',
    role: AppRoles.citizen,
  );
  const driver = AuthUser(
    id: 'driver-1',
    fullName: 'Startup Driver',
    email: 'driver@example.test',
    role: AppRoles.driver,
  );

  testWidgets(
    'no session shows artwork for two seconds, then login without splash in back stack',
    (tester) async {
      final router = await pumpStartup(tester, StartupAuthRepository());

      expect(find.byType(SplashScreen), findsOneWidget);
      final image = tester.widget<Image>(find.byType(Image).first);
      expect(image.image, const AssetImage('assets/SplashScreen.png'));
      expect(image.fit, BoxFit.cover);
      expect(find.byType(LoginScreen), findsNothing);

      await tester.pump(const Duration(milliseconds: 1999));
      expect(find.byType(SplashScreen), findsOneWidget);
      await tester.pump(const Duration(milliseconds: 1));
      await tester.pumpAndSettle();

      expect(find.byType(LoginScreen), findsOneWidget);
      expect(find.byType(SplashScreen), findsNothing);
      expect(router.canPop(), isFalse);
    },
  );

  testWidgets(
    'fast Citizen restore stays on splash until minimum, then dashboard without login flash',
    (tester) async {
      await pumpStartup(tester, StartupAuthRepository(user: citizen));
      await tester.pump(const Duration(milliseconds: 400));
      expect(find.byType(SplashScreen), findsOneWidget);
      expect(find.byType(LoginScreen), findsNothing);

      await tester.pump(const Duration(milliseconds: 1600));
      await tester.pumpAndSettle();
      expect(find.byType(CitizenDashboardScreen), findsOneWidget);
      expect(find.byType(LoginScreen), findsNothing);
      expect(find.byType(SplashScreen), findsNothing);
    },
  );

  testWidgets(
    'fast Driver restore stays on splash until minimum, then dashboard',
    (tester) async {
      await pumpStartup(tester, StartupAuthRepository(user: driver));
      await tester.pump(const Duration(seconds: 2));
      await tester.pumpAndSettle();
      expect(find.byType(DriverDashboardScreen), findsOneWidget);
      expect(find.byType(LoginScreen), findsNothing);
      expect(find.byType(SplashScreen), findsNothing);
    },
  );

  testWidgets(
    'auth taking longer than two seconds keeps splash until it resolves',
    (tester) async {
      final tokenResult = Completer<bool>();
      await pumpStartup(
        tester,
        StartupAuthRepository(
          user: citizen,
          tokenLookup: () => tokenResult.future,
        ),
      );
      await tester.pump(const Duration(seconds: 2));
      expect(find.byType(SplashScreen), findsOneWidget);
      expect(find.byType(LoginScreen), findsNothing);

      tokenResult.complete(true);
      await tester.pumpAndSettle();
      expect(find.byType(CitizenDashboardScreen), findsOneWidget);
      expect(find.byType(SplashScreen), findsNothing);
    },
  );

  testWidgets(
    'invalid stored session uses existing clear-token and login flow',
    (tester) async {
      final repository = StartupAuthRepository(tokenLookup: () async => true);
      await pumpStartup(tester, repository);
      await tester.pump(const Duration(seconds: 2));
      await tester.pumpAndSettle();
      expect(repository.logoutCalled, isTrue);
      expect(find.byType(LoginScreen), findsOneWidget);
    },
  );

  testWidgets('storage restoration failure resolves to login', (tester) async {
    final repository = StartupAuthRepository(
      tokenLookup: () async => throw StateError('Secure storage unavailable'),
    );
    await pumpStartup(tester, repository);
    await tester.pump(const Duration(seconds: 2));
    await tester.pumpAndSettle();
    expect(repository.logoutCalled, isTrue);
    expect(find.byType(LoginScreen), findsOneWidget);
    expect(find.byType(SplashScreen), findsNothing);
  });

  testWidgets('storage cleanup failure cannot strand startup on splash', (
    tester,
  ) async {
    final repository = StartupAuthRepository(
      tokenLookup: () async => throw StateError('Secure storage unavailable'),
      logoutFails: true,
    );
    await pumpStartup(tester, repository);
    await tester.pump(const Duration(seconds: 2));
    await tester.pumpAndSettle();
    expect(repository.logoutCalled, isTrue);
    expect(find.byType(LoginScreen), findsOneWidget);
    expect(find.byType(SplashScreen), findsNothing);
  });

  testWidgets('logout returns to login without replaying splash', (
    tester,
  ) async {
    final repository = StartupAuthRepository(user: driver);
    await pumpStartup(tester, repository);
    await tester.pump(const Duration(seconds: 2));
    await tester.pumpAndSettle();
    final container = ProviderScope.containerOf(
      tester.element(find.byType(DriverDashboardScreen)),
    );
    await container.read(authProvider.notifier).logout();
    await tester.pumpAndSettle();
    expect(repository.logoutCalled, isTrue);
    expect(find.byType(LoginScreen), findsOneWidget);
    expect(find.byType(SplashScreen), findsNothing);
  });

  for (final size in const [
    Size(320, 568),
    Size(360, 800),
    Size(390, 844),
    Size(412, 915),
  ]) {
    testWidgets('artwork keeps central logo and subtitle visible at $size', (
      tester,
    ) async {
      tester.view.physicalSize = size;
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      final tokenResult = Completer<bool>();
      await pumpStartup(
        tester,
        StartupAuthRepository(tokenLookup: () => tokenResult.future),
      );

      expect(find.byType(SplashScreen), findsOneWidget);
      expect(tester.takeException(), isNull);
      // Cover crops only the side decoration on tall phones. The source's
      // central logo and subtitle occupy approximately x=220..725.
      const source = Size(941, 1672);
      final visibleSourceWidth = size.width * source.height / size.height;
      final leftCrop = (source.width - visibleSourceWidth) / 2;
      expect(leftCrop, lessThan(220));
      expect(source.width - leftCrop, greaterThan(725));
    });
  }
}
