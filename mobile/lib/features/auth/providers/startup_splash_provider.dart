import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'auth_provider.dart';

/// Keeps a cold start on the branded screen until its minimum display elapses.
class StartupSplashNotifier extends Notifier<bool> {
  @override
  bool build() {
    // A restored ProviderScope starts at initial. An already resolved scope
    // (for example, a router test) has no cold-start delay to complete.
    final status = ref.read(authProvider).status;
    return status != AuthStatus.initial && status != AuthStatus.loading;
  }

  void completeMinimumDisplay() => state = true;
}

final startupSplashMinimumElapsedProvider =
    NotifierProvider<StartupSplashNotifier, bool>(StartupSplashNotifier.new);
