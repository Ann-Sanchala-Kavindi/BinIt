import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/app_button.dart';
import '../../../shared/widgets/app_card.dart';
import '../../auth/providers/auth_provider.dart';

/// Authenticated Citizen Dashboard — the primary landing screen for Citizen mobile users.
class CitizenDashboardScreen extends ConsumerWidget {
  const CitizenDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authProvider);
    final user = authState.user;

    final firstName = user?.fullName.trim().split(RegExp(r'\s+')).first ?? 'Citizen';

    return Center(
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 540),
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.lg,
            vertical: AppSpacing.md,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // 1. Greeting & Context
              Text(
                'Hello, $firstName',
                key: const Key('citizen_greeting_text'),
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                      fontWeight: FontWeight.bold,
                      color: AppColors.textPrimary,
                    ),
              ),
              const SizedBox(height: AppSpacing.xxs),
              Text(
                'Help keep your community clean and healthy.',
                style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                      color: AppColors.textSecondary,
                    ),
              ),
              const SizedBox(height: AppSpacing.lg),

              // 2. Primary Action Card — Report Waste (Strongest visual emphasis)
              _ReportWasteCtaCard(
                onTap: () {
                  context.push('/citizen/report-waste');
                },
              ),
              const SizedBox(height: AppSpacing.xl),

              // 3. Quick Access Section
              Text(
                'Quick Access',
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                      color: AppColors.textPrimary,
                    ),
              ),
              const SizedBox(height: AppSpacing.sm),
              _QuickAccessGrid(),
              const SizedBox(height: AppSpacing.xl),

              // 4. Recent Activity Section
              Text(
                'Recent Activity',
                key: const Key('citizen_recent_activity_section'),
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                      color: AppColors.textPrimary,
                    ),
              ),
              const SizedBox(height: AppSpacing.sm),
              _RecentActivityEmptyState(
                onReportTap: () {
                  context.push('/citizen/report-waste');
                },
              ),
              const SizedBox(height: AppSpacing.lg),
            ],
          ),
        ),
      ),
    );
  }
}

/// Prominent primary action card for reporting waste with location and photo.
class _ReportWasteCtaCard extends StatelessWidget {
  final VoidCallback onTap;

  const _ReportWasteCtaCard({required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      borderRadius: BorderRadius.circular(AppSpacing.radiusLg),
      child: InkWell(
        key: const Key('citizen_report_waste_cta'),
        onTap: onTap,
        borderRadius: BorderRadius.circular(AppSpacing.radiusLg),
        child: Container(
          width: double.infinity,
          padding: const EdgeInsets.all(AppSpacing.xl),
          decoration: BoxDecoration(
            gradient: const LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                AppColors.primaryDark,
                AppColors.primaryHover,
              ],
            ),
            borderRadius: BorderRadius.circular(AppSpacing.radiusLg),
            boxShadow: [
              BoxShadow(
                color: AppColors.primaryDark.withValues(alpha: 0.25),
                blurRadius: 12,
                offset: const Offset(0, 4),
              ),
            ],
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    padding: const EdgeInsets.all(AppSpacing.sm),
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.15),
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(
                      Icons.add_a_photo_outlined,
                      color: Colors.white,
                      size: 26,
                    ),
                  ),
                  const Spacer(),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: AppColors.primaryAccent.withValues(alpha: 0.25),
                      borderRadius: AppSpacing.roundedFull,
                      border: Border.all(
                        color: Colors.white.withValues(alpha: 0.2),
                      ),
                    ),
                    child: const Text(
                      'Primary Action',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 11,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppSpacing.md),
              const Text(
                'Report Waste',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 20,
                  fontWeight: FontWeight.bold,
                  letterSpacing: -0.3,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),
              const Text(
                'Report waste using a location, description and photo.',
                style: TextStyle(
                  color: Color(0xFFD1FAE5), // emerald-100
                  fontSize: 14,
                  height: 1.4,
                ),
              ),
              const SizedBox(height: AppSpacing.md),
              LayoutBuilder(
                builder: (context, constraints) {
                  final actionButton = Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: AppSpacing.md,
                      vertical: AppSpacing.xs + 2,
                    ),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: AppSpacing.roundedFull,
                    ),
                    child: const Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          'Report Issue',
                          style: TextStyle(
                            color: AppColors.primaryDark,
                            fontWeight: FontWeight.bold,
                            fontSize: 13,
                          ),
                        ),
                        SizedBox(width: AppSpacing.xs),
                        Icon(
                          Icons.arrow_forward,
                          size: 16,
                          color: AppColors.primaryDark,
                        ),
                      ],
                    ),
                  );

                  final illustration = Image.asset(
                    'assets/images/waste_report_illustration.png',
                    key: const Key('citizen_report_waste_illustration'),
                    fit: BoxFit.contain,
                    alignment: Alignment.bottomRight,
                    excludeFromSemantics: true,
                  );

                  final isVeryNarrow = constraints.maxWidth < 230;
                  if (isVeryNarrow) {
                    return Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        actionButton,
                        const SizedBox(height: AppSpacing.sm),
                        Align(
                          alignment: Alignment.bottomRight,
                          child: ConstrainedBox(
                            constraints: const BoxConstraints(
                              maxWidth: 120.0,
                              maxHeight: 50.0,
                            ),
                            child: illustration,
                          ),
                        ),
                      ],
                    );
                  }

                  return Row(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      actionButton,
                      const SizedBox(width: AppSpacing.xs),
                      Expanded(
                        child: Align(
                          alignment: Alignment.bottomRight,
                          child: ConstrainedBox(
                            constraints: BoxConstraints(
                              maxWidth: (constraints.maxWidth * 0.44).clamp(70.0, 160.0),
                              maxHeight: constraints.maxWidth < 280 ? 52.0 : 72.0,
                            ),
                            child: illustration,
                          ),
                        ),
                      ),
                    ],
                  );
                },
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// 2-column responsive quick access grid for main citizen workflows.
class _QuickAccessGrid extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    final items = [
      _QuickAccessCard(
        key: const Key('citizen_quick_access_reports'),
        title: 'My Reports',
        description: 'Track your submitted waste reports.',
        icon: Icons.assignment_outlined,
        onTap: () {
          context.go('/citizen/reports');
        },
      ),
      _QuickAccessCard(
        key: const Key('citizen_quick_access_bins'),
        title: 'Nearby Bins',
        description: 'Find waste bins near your location.',
        icon: Icons.delete_outline,
        onTap: () {
          context.push('/citizen/nearby-bins');
        },
      ),
      _QuickAccessCard(
        key: const Key('citizen_quick_access_complaints'),
        title: 'Complaints',
        description: 'Report or track service concerns.',
        icon: Icons.feedback_outlined,
        onTap: () {
          context.go('/citizen/complaints');
        },
      ),
      _QuickAccessCard(
        key: const Key('citizen_quick_access_notifications'),
        title: 'Notifications',
        description: 'View report and service updates.',
        icon: Icons.notifications_outlined,
        onTap: () {
          context.push('/citizen/notifications');
        },
      ),
      _QuickAccessCard(
        key: const Key('citizen_quick_access_profile'),
        title: 'Profile',
        description: 'Manage your account information.',
        icon: Icons.person_outline,
        onTap: () {
          context.go('/citizen/profile');
        },
      ),
    ];

    return LayoutBuilder(
      builder: (context, constraints) {
        final isNarrow = constraints.maxWidth < 310;
        if (isNarrow) {
          return Column(
            children: items
                .map(
                  (item) => Padding(
                    padding: const EdgeInsets.only(bottom: AppSpacing.sm),
                    child: item,
                  ),
                )
                .toList(),
          );
        }

        final rows = <Widget>[];
        for (int i = 0; i < items.length; i += 2) {
          if (i + 1 < items.length) {
            rows.add(
              Row(
                children: [
                  Expanded(child: items[i]),
                  const SizedBox(width: AppSpacing.md),
                  Expanded(child: items[i + 1]),
                ],
              ),
            );
          } else {
            rows.add(
              Row(
                children: [
                  Expanded(child: items[i]),
                  const SizedBox(width: AppSpacing.md),
                  const Expanded(child: SizedBox.shrink()),
                ],
              ),
            );
          }
          if (i + 2 < items.length) {
            rows.add(const SizedBox(height: AppSpacing.md));
          }
        }

        return Column(
          children: rows,
        );
      },
    );
  }
}

/// Touch-friendly quick access tile.
class _QuickAccessCard extends StatelessWidget {
  final String title;
  final String description;
  final IconData icon;
  final VoidCallback onTap;

  const _QuickAccessCard({
    super.key,
    required this.title,
    required this.description,
    required this.icon,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: const EdgeInsets.all(AppSpacing.md),
      onTap: onTap,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            padding: const EdgeInsets.all(AppSpacing.xs),
            decoration: const BoxDecoration(
              color: AppColors.primaryLight,
              shape: BoxShape.circle,
            ),
            child: Icon(
              icon,
              color: AppColors.primary,
              size: 20,
            ),
          ),
          const SizedBox(height: AppSpacing.sm),
          Text(
            title,
            style: const TextStyle(
              fontWeight: FontWeight.w600,
              fontSize: 14,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: AppSpacing.xxs),
          Text(
            description,
            style: const TextStyle(
              fontSize: 12,
              color: AppColors.textSecondary,
              height: 1.3,
            ),
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
          ),
        ],
      ),
    );
  }
}

/// Calm, professional empty state for Citizen Recent Activity section.
class _RecentActivityEmptyState extends StatelessWidget {
  final VoidCallback onReportTap;

  const _RecentActivityEmptyState({required this.onReportTap});

  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.lg,
        vertical: AppSpacing.xl,
      ),
      child: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              padding: const EdgeInsets.all(AppSpacing.md),
              decoration: const BoxDecoration(
                color: AppColors.surfaceSubtle,
                shape: BoxShape.circle,
              ),
              child: const Icon(
                Icons.inbox_outlined,
                size: 28,
                color: AppColors.textMuted,
              ),
            ),
            const SizedBox(height: AppSpacing.sm),
            const Text(
              'No Recent Activity',
              style: TextStyle(
                fontWeight: FontWeight.w600,
                fontSize: 14,
                color: AppColors.textPrimary,
              ),
            ),
            const SizedBox(height: AppSpacing.xs),
            const Text(
              'Your submitted waste reports, status updates, and service responses will appear here.',
              textAlign: TextAlign.center,
              style: TextStyle(
                fontSize: 12,
                color: AppColors.textSecondary,
                height: 1.4,
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            AppButton.text(
              label: 'Report Waste Now',
              icon: Icons.add_circle_outline,
              onPressed: onReportTap,
            ),
          ],
        ),
      ),
    );
  }
}
