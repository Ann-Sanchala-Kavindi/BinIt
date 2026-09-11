import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../core/theme/app_colors.dart';
import '../../../core/theme/app_spacing.dart';
import '../../../shared/widgets/app_button.dart';
import '../../../shared/widgets/app_card.dart';
import '../../auth/providers/auth_provider.dart';

/// Authenticated Driver Dashboard — operational command center for drivers in the field.
class DriverDashboardScreen extends ConsumerWidget {
  const DriverDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authProvider);
    final user = authState.user;

    final firstName = user?.fullName.trim().split(RegExp(r'\s+')).first ?? 'Driver';

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
              // 1. Driver Header & Operational Context
              Text(
                'Hello, $firstName',
                key: const Key('driver_greeting_text'),
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                      fontWeight: FontWeight.bold,
                      color: AppColors.textPrimary,
                    ),
              ),
              const SizedBox(height: AppSpacing.xxs),
              Text(
                'Ready for today\'s operations. Stay updated with your collection assignments and route.',
                style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                      color: AppColors.textSecondary,
                    ),
              ),
              const SizedBox(height: AppSpacing.lg),

              // 2. Current Assignment — Primary Dashboard Section
              Text(
                'Current Assignment',
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                      color: AppColors.textPrimary,
                    ),
              ),
              const SizedBox(height: AppSpacing.sm),
              _CurrentAssignmentCard(
                onViewAssignment: () {
                  context.push('/driver/assignment');
                },
              ),
              const SizedBox(height: AppSpacing.xl),

              // 3. Driver Quick Actions
              Text(
                'Quick Actions',
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                      color: AppColors.textPrimary,
                    ),
              ),
              const SizedBox(height: AppSpacing.sm),
              const _DriverQuickActionsGrid(),
              const SizedBox(height: AppSpacing.xl),

              // 4. Today's Work / Task Status Section
              Text(
                'Today\'s Work',
                key: const Key('driver_todays_work_section'),
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                      color: AppColors.textPrimary,
                    ),
              ),
              const SizedBox(height: AppSpacing.sm),
              const _TodaysWorkEmptyState(),
              const SizedBox(height: AppSpacing.xl),

              // 5. Recent Operational Updates Section
              Text(
                'Recent Updates',
                key: const Key('driver_recent_updates_section'),
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                      color: AppColors.textPrimary,
                    ),
              ),
              const SizedBox(height: AppSpacing.sm),
              const _RecentUpdatesEmptyState(),
              const SizedBox(height: AppSpacing.lg),
            ],
          ),
        ),
      ),
    );
  }
}

/// Primary action card representing the Driver's current operational assignment.
class _CurrentAssignmentCard extends StatelessWidget {
  final VoidCallback onViewAssignment;

  const _CurrentAssignmentCard({required this.onViewAssignment});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: AppSpacing.roundedMd,
        border: Border.all(color: AppColors.primaryBorder, width: 1.5),
        boxShadow: const [
          BoxShadow(
            color: Color(0x0A064E3B),
            blurRadius: 10,
            offset: Offset(0, 4),
          ),
        ],
      ),
      padding: const EdgeInsets.all(AppSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 44,
                height: 44,
                decoration: const BoxDecoration(
                  color: AppColors.primaryLight,
                  borderRadius: AppSpacing.roundedSm,
                ),
                child: const Icon(
                  Icons.assignment_outlined,
                  color: AppColors.primaryDark,
                  size: 24,
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'No active assignment',
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                            fontWeight: FontWeight.bold,
                            color: AppColors.textPrimary,
                          ),
                    ),
                    const SizedBox(height: AppSpacing.xxs),
                    Text(
                      'Your active collection assignment will appear here once operations are connected.',
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(
                            color: AppColors.textSecondary,
                            height: 1.4,
                          ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.lg),
          AppButton.primary(
            key: const Key('driver_view_assignment_button'),
            label: 'View Assignment',
            icon: Icons.arrow_forward,
            onPressed: onViewAssignment,
          ),
        ],
      ),
    );
  }
}

/// Quick action data model for Driver operational shortcuts.
class _DriverQuickActionItem {
  final String label;
  final String description;
  final IconData icon;
  final String route;
  final Key key;
  final bool isShellTab;

  const _DriverQuickActionItem({
    required this.label,
    required this.description,
    required this.icon,
    required this.route,
    required this.key,
    this.isShellTab = false,
  });
}

/// Responsive grid rendering the 4 primary Driver quick actions.
class _DriverQuickActionsGrid extends StatelessWidget {
  const _DriverQuickActionsGrid();

  static const List<_DriverQuickActionItem> _items = [
    _DriverQuickActionItem(
      label: 'Collection Tasks',
      description: 'View your assigned collection work.',
      icon: Icons.checklist,
      route: '/driver/tasks',
      key: Key('driver_quick_action_tasks'),
      isShellTab: true,
    ),
    _DriverQuickActionItem(
      label: 'Route',
      description: 'View your collection route and stops.',
      icon: Icons.alt_route,
      route: '/driver/route',
      key: Key('driver_quick_action_route'),
      isShellTab: true,
    ),
    _DriverQuickActionItem(
      label: 'Report Incident',
      description: 'Report a problem during collection.',
      icon: Icons.warning_amber_rounded,
      route: '/driver/incidents',
      key: Key('driver_quick_action_incidents'),
      isShellTab: false,
    ),
    _DriverQuickActionItem(
      label: 'Notifications',
      description: 'View assignment and service updates.',
      icon: Icons.notifications_outlined,
      route: '/driver/notifications',
      key: Key('driver_quick_action_notifications'),
      isShellTab: false,
    ),
  ];

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final isCompact = constraints.maxWidth < 340;

        if (isCompact) {
          return Column(
            children: _items.map((item) {
              return Padding(
                padding: const EdgeInsets.only(bottom: AppSpacing.sm),
                child: _QuickActionCard(item: item),
              );
            }).toList(),
          );
        }

        // 2-column grid layout
        return Column(
          children: [
            Row(
              children: [
                Expanded(child: _QuickActionCard(item: _items[0])),
                const SizedBox(width: AppSpacing.sm),
                Expanded(child: _QuickActionCard(item: _items[1])),
              ],
            ),
            const SizedBox(height: AppSpacing.sm),
            Row(
              children: [
                Expanded(child: _QuickActionCard(item: _items[2])),
                const SizedBox(width: AppSpacing.sm),
                Expanded(child: _QuickActionCard(item: _items[3])),
              ],
            ),
          ],
        );
      },
    );
  }
}

/// Individual touch card for Driver quick actions.
class _QuickActionCard extends StatelessWidget {
  final _DriverQuickActionItem item;

  const _QuickActionCard({required this.item});

  @override
  Widget build(BuildContext context) {
    return AppCard(
      key: item.key,
      onTap: () {
        if (item.isShellTab) {
          context.go(item.route);
        } else {
          context.push(item.route);
        }
      },
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 38,
            height: 38,
            decoration: const BoxDecoration(
              color: AppColors.primaryLight,
              shape: BoxShape.circle,
            ),
            child: Icon(
              item.icon,
              color: AppColors.primary,
              size: 20,
            ),
          ),
          const SizedBox(height: AppSpacing.sm),
          Text(
            item.label,
            style: const TextStyle(
              fontWeight: FontWeight.bold,
              fontSize: 14,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: 2),
          Text(
            item.description,
            style: const TextStyle(
              color: AppColors.textSecondary,
              fontSize: 12,
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

/// Compact, polished empty state for Today's Work / Task Status.
class _TodaysWorkEmptyState extends StatelessWidget {
  const _TodaysWorkEmptyState();

  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Row(
        children: [
          Container(
            width: 40,
            height: 40,
            decoration: const BoxDecoration(
              color: AppColors.primaryLight,
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.event_note_outlined,
              color: AppColors.primary,
              size: 20,
            ),
          ),
          const SizedBox(width: AppSpacing.md),
          const Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'No Tasks In Progress',
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: 14,
                    color: AppColors.textPrimary,
                  ),
                ),
                SizedBox(height: 2),
                Text(
                  'Your assigned collection tasks and progress will appear here.',
                  style: TextStyle(
                    color: AppColors.textSecondary,
                    fontSize: 12,
                    height: 1.3,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// Compact, polished empty state for Recent Operational Updates.
class _RecentUpdatesEmptyState extends StatelessWidget {
  const _RecentUpdatesEmptyState();

  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Row(
        children: [
          Container(
            width: 40,
            height: 40,
            decoration: const BoxDecoration(
              color: AppColors.primaryLight,
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.history_toggle_off,
              color: AppColors.primary,
              size: 20,
            ),
          ),
          const SizedBox(width: AppSpacing.md),
          const Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'No Recent Updates',
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: 14,
                    color: AppColors.textPrimary,
                  ),
                ),
                SizedBox(height: 2),
                Text(
                  'Assignment and operational updates will appear here.',
                  style: TextStyle(
                    color: AppColors.textSecondary,
                    fontSize: 12,
                    height: 1.3,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
