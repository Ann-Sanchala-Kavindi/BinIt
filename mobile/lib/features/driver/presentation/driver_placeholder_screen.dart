import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/app_alert.dart';
import '../../../shared/widgets/app_button.dart';
import '../../../shared/widgets/app_card.dart';
import '../../auth/providers/auth_provider.dart';

/// Reusable placeholder screen for upcoming Driver feature destinations.
class DriverPlaceholderScreen extends ConsumerWidget {
  final String title;
  final String description;
  final IconData icon;
  final bool hasScaffold;
  final VoidCallback? onBack;
  final bool isProfile;

  const DriverPlaceholderScreen({
    super.key,
    required this.title,
    required this.description,
    required this.icon,
    this.hasScaffold = false,
    this.onBack,
    this.isProfile = false,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authProvider);
    final user = authState.user;

    final body = Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.xl,
          vertical: AppSpacing.xxl,
        ),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 480),
          child: AppCard(
            padding: const EdgeInsets.all(AppSpacing.xxl),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                // Icon Circle
                Container(
                  width: 80,
                  height: 80,
                  decoration: const BoxDecoration(
                    color: AppColors.primaryLight,
                    shape: BoxShape.circle,
                  ),
                  child: Icon(
                    icon,
                    size: 40,
                    color: AppColors.primary,
                  ),
                ),
                const SizedBox(height: AppSpacing.lg),

                // Title
                Text(
                  title,
                  style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                      ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: AppSpacing.sm),

                // Description
                Text(
                  description,
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        color: AppColors.textSecondary,
                        height: 1.5,
                      ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: AppSpacing.xl),

                // Operational Status Alert
                const AppAlert.info(
                  message: 'This feature is scheduled for implementation in upcoming municipal operations modules.',
                ),
                const SizedBox(height: AppSpacing.xl),

                // Profile Account Actions if on Driver Profile
                if (isProfile && user != null) ...[
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(AppSpacing.md),
                    margin: const EdgeInsets.only(bottom: AppSpacing.lg),
                    decoration: BoxDecoration(
                      color: AppColors.surfaceSubtle,
                      borderRadius: AppSpacing.roundedMd,
                      border: Border.all(color: AppColors.border),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          user.fullName,
                          style: const TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 16,
                            color: AppColors.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          user.email,
                          style: const TextStyle(
                            fontSize: 14,
                            color: AppColors.textSecondary,
                          ),
                        ),
                        const SizedBox(height: AppSpacing.xs),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                          decoration: BoxDecoration(
                            color: AppColors.primaryLight,
                            borderRadius: AppSpacing.roundedFull,
                            border: Border.all(color: AppColors.primaryBorder),
                          ),
                          child: Text(
                            user.role,
                            style: const TextStyle(
                              color: AppColors.primaryDark,
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                  AppButton.outlined(
                    key: const Key('driver_profile_change_password_button'),
                    label: 'Change Password',
                    icon: Icons.lock_outline,
                    onPressed: () {
                      context.push('/change-password');
                    },
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  AppButton.destructive(
                    key: const Key('driver_profile_logout_button'),
                    label: 'Logout',
                    icon: Icons.logout,
                    onPressed: () {
                      ref.read(authProvider.notifier).logout();
                    },
                  ),
                  const SizedBox(height: AppSpacing.sm),
                ],

                // Back Action
                AppButton.outlined(
                  key: const Key('driver_placeholder_back_button'),
                  label: 'Back to Dashboard',
                  icon: Icons.arrow_back,
                  onPressed: onBack ?? () {
                    context.go('/driver/dashboard');
                  },
                ),
              ],
            ),
          ),
        ),
      ),
    );

    if (hasScaffold) {
      return Scaffold(
        backgroundColor: AppColors.dashboardBackground,
        appBar: AppBar(
          backgroundColor: AppColors.dashboardBackground,
          surfaceTintColor: Colors.transparent,
          title: Text(title),
          leading: BackButton(
            onPressed: onBack ?? () {
              context.go('/driver/dashboard');
            },
          ),
        ),
        body: SafeArea(child: body),
      );
    }

    return body;
  }
}
