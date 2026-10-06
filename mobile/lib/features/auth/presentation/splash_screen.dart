import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter/services.dart';

import '../providers/auth_provider.dart';
import '../providers/startup_splash_provider.dart';

class SplashScreen extends ConsumerStatefulWidget {
  const SplashScreen({super.key});

  @override
  ConsumerState<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends ConsumerState<SplashScreen> {
  static const minimumDisplayDuration = Duration(seconds: 2);
  Timer? _minimumDisplayTimer;

  @override
  void initState() {
    super.initState();
    _minimumDisplayTimer = Timer(minimumDisplayDuration, () {
      if (mounted) {
        ref
            .read(startupSplashMinimumElapsedProvider.notifier)
            .completeMinimumDisplay();
      }
    });
    Future.microtask(() => ref.read(authProvider.notifier).restoreSession());
  }

  @override
  void dispose() {
    _minimumDisplayTimer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: SystemUiOverlayStyle.dark.copyWith(
        statusBarColor: Colors.white,
        systemNavigationBarColor: Colors.white,
      ),
      child: const Scaffold(
        backgroundColor: Colors.white,
        body: SizedBox.expand(
          child: Image(
            image: AssetImage('assets/SplashScreen.png'),
            fit: BoxFit.cover,
          ),
        ),
      ),
    );
  }
}
