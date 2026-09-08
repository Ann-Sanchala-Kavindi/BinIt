import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/auth/presentation/login_screen.dart';

void main() {
  Widget createTestWidget() {
    return const ProviderScope(
      child: MaterialApp(
        home: LoginScreen(),
      ),
    );
  }

  group('LoginScreen Validation Tests', () {
    testWidgets('rejects submission with empty email and empty password', (tester) async {
      await tester.pumpWidget(createTestWidget());

      // Tap Sign In button without entering any details
      final submitButton = find.byKey(const Key('login_submit_button'));
      expect(submitButton, findsOneWidget);
      await tester.tap(submitButton);
      await tester.pumpAndSettle();

      // Should show validation error messages
      expect(find.text('Email is required'), findsOneWidget);
      expect(find.text('Password is required'), findsOneWidget);
    });

    testWidgets('rejects invalid email formats', (tester) async {
      await tester.pumpWidget(createTestWidget());

      final emailField = find.byKey(const Key('login_email_field'));
      final passwordField = find.byKey(const Key('login_password_field'));
      final submitButton = find.byKey(const Key('login_submit_button'));

      await tester.enterText(emailField, 'not-an-email');
      await tester.enterText(passwordField, 'Password123!');
      await tester.tap(submitButton);
      await tester.pumpAndSettle();

      expect(find.text('Enter a valid email address'), findsOneWidget);
      expect(find.text('Password is required'), findsNothing);
    });

    testWidgets('rejects empty password when email is valid', (tester) async {
      await tester.pumpWidget(createTestWidget());

      final emailField = find.byKey(const Key('login_email_field'));
      final submitButton = find.byKey(const Key('login_submit_button'));

      await tester.enterText(emailField, 'valid@example.com');
      await tester.tap(submitButton);
      await tester.pumpAndSettle();

      expect(find.text('Email is required'), findsNothing);
      expect(find.text('Enter a valid email address'), findsNothing);
      expect(find.text('Password is required'), findsOneWidget);
    });
  });
}
