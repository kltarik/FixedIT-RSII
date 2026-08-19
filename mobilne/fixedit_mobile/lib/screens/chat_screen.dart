import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../services/auth_service.dart';
import '../services/mobile_repository.dart';
import '../services/realtime_service.dart';
import '../widgets/common.dart';

class ChatScreen extends StatefulWidget {
  const ChatScreen({super.key, required this.reservationId});
  final int reservationId;
  @override
  State<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends State<ChatScreen> {
  ChatSession? session;
  String? error;
  @override
  void initState() {
    super.initState();
    initialize();
  }

  Future<void> initialize() async {
    try {
      final repo = context.read<MobileRepository>();
      final conversation = await repo.createConversationForReservation(
        widget.reservationId,
      );
      if (!mounted) return;
      final auth = context.read<AuthService>();
      final value = ChatSession(
        repo,
        () => auth.token,
        conversation.id,
        auth.user!.id,
      );
      setState(() => session = value);
      await value.start();
    } catch (e) {
      if (mounted) setState(() => error = userError(e));
    }
  }

  @override
  void dispose() {
    session?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Razgovor')),
    body: error != null
        ? ErrorView(error!, initialize)
        : session == null
        ? const LoadingView()
        : ChangeNotifierProvider.value(
            value: session!,
            child: const _ChatBody(),
          ),
  );
}

class _ChatBody extends StatefulWidget {
  const _ChatBody();
  @override
  State<_ChatBody> createState() => _ChatBodyState();
}

class _ChatBodyState extends State<_ChatBody> {
  final message = TextEditingController();
  @override
  void dispose() {
    message.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final chat = context.watch<ChatSession>();
    return Column(
      children: [
        if (!chat.connected)
          MaterialBanner(
            content: Text(
              chat.error ?? 'Ponovno povezivanje sa chat servisom...',
            ),
            actions: [
              TextButton(onPressed: chat.retry, child: const Text('Pokušaj')),
            ],
          ),
        Expanded(
          child: chat.loading
              ? const LoadingView()
              : chat.messages.isEmpty
              ? const EmptyView(
                  'Pošaljite prvu poruku.',
                  icon: Icons.chat_bubble_outline,
                )
              : ListView.builder(
                  padding: const EdgeInsets.all(14),
                  itemCount: chat.messages.length,
                  itemBuilder: (_, i) {
                    final item = chat.messages[i];
                    final mine = item.senderId == chat.currentUserId;
                    return Align(
                      alignment: mine
                          ? Alignment.centerRight
                          : Alignment.centerLeft,
                      child: Container(
                        margin: const EdgeInsets.only(bottom: 8),
                        padding: const EdgeInsets.all(12),
                        constraints: const BoxConstraints(maxWidth: 290),
                        decoration: BoxDecoration(
                          color: mine
                              ? Theme.of(context).colorScheme.primaryContainer
                              : Colors.white,
                          borderRadius: BorderRadius.circular(16),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(item.content),
                            const SizedBox(height: 4),
                            Text(
                              '${item.senderName} • ${dateTime.format(item.sentAt)}',
                              style: Theme.of(context).textTheme.bodySmall,
                            ),
                          ],
                        ),
                      ),
                    );
                  },
                ),
        ),
        SafeArea(
          child: Padding(
            padding: const EdgeInsets.all(10),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: message,
                    maxLength: 2000,
                    decoration: const InputDecoration(
                      labelText: 'Poruka',
                      counterText: '',
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                IconButton.filled(
                  onPressed: !chat.connected
                      ? null
                      : () async {
                          final text = message.text;
                          message.clear();
                          await chat.send(text);
                        },
                  icon: const Icon(Icons.send),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}
