import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/auth/presentation/register_screen.dart';

void main() {
  Widget createTestWidget() {
    return const ProviderScope(
      child: MaterialApp(
        home: RegisterScreen(),
      ),
    );
  }

  testWidgets('uses Bin It branding while keeping the registration form', (tester) async {
    await tester.pumpWidget(createTestWidget());

    expect(find.text('Join Bin It'), findsOneWidget);
    expect(find.text('Join Smart Waste'), findsNothing);
    expect(find.byKey(const Key('register_email_field')), findsWidgets);
    expect(find.byKey(const Key('register_password_field')), findsWidgets);
    expect(find.byKey(const Key('register_confirm_password_field')), findsWidgets);
  });

  for (final width in const [320.0, 360.0, 390.0, 412.0]) {
    testWidgets('registration header and form fit ${width.toInt()}px width', (tester) async {
      tester.view.physicalSize = Size(width, 800);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);
      await tester.pumpWidget(createTestWidget());

      expect(find.text('Join Bin It'), findsOneWidget);
      expect(find.byKey(const Key('register_email_field')), findsWidgets);
      expect(tester.takeException(), isNull);
    });
  }

  group('RegisterScreen Validation Tests', () {
    testWidgets('verifies that no role selector or dropdown exists on the form', (tester) async {
      await tester.pumpWidget(createTestWidget());

      // Ensure no role selector widgets exist
      expect(find.byType(DropdownButton), findsNothing);
      expect(find.byType(Radio), findsNothing);
      expect(find.textContaining('Role'), findsNothing);
      expect(find.textContaining('MunicipalManager'), findsNothing);
      expect(find.textContaining('WasteOfficer'), findsNothing);
      expect(find.textContaining('Driver'), findsNothing);
    });

    testWidgets('validates all required registration fields on empty submit', (tester) async {
      await tester.pumpWidget(createTestWidget());

      final submitBtn = find.byKey(const Key('register_submit_button'));
      await tester.ensureVisible(submitBtn);
      await tester.tap(submitBtn);
      await tester.pumpAndSettle();

      expect(find.text('Full name is required'), findsOneWidget);
      expect(find.text('Email is required'), findsOneWidget);
      expect(find.text('Phone number is required'), findsOneWidget);
      expect(find.text('Password is required'), findsOneWidget);
      expect(find.text('Confirm password is required'), findsOneWidget);
    });

    testWidgets('rejects registration when password and confirm password do not match', (tester) async {
      await tester.pumpWidget(createTestWidget());

      await tester.enterText(find.byKey(const Key('register_fullname_field')), 'Kamal Silva');
      await tester.enterText(find.byKey(const Key('register_email_field')), 'kamal@example.com');
      await tester.enterText(find.byKey(const Key('register_phone_field')), '+94771234567');
      await tester.enterText(find.byKey(const Key('register_password_field')), 'Password123!');
      await tester.enterText(find.byKey(const Key('register_confirm_password_field')), 'DifferentPassword123!');

      final submitBtn = find.byKey(const Key('register_submit_button'));
      await tester.ensureVisible(submitBtn);
      await tester.tap(submitBtn);
      await tester.pumpAndSettle();

      expect(find.text('Passwords do not match'), findsOneWidget);
    });
  });
}
