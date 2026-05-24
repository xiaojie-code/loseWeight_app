import { Injectable, OnModuleDestroy } from '@nestjs/common';
import { CombatService } from '../combat/combat.service';

interface QueueEntry {
  socketId: string;
  rank: number;
  joinTime: number;
}

export interface MatchFoundEvent {
  player1SocketId: string;
  player2SocketId: string;
  roomId: string;
}

/**
 * 匹配服务 - 按段位匹配，5秒超时
 */
@Injectable()
export class MatchService implements OnModuleDestroy {
  private queue: QueueEntry[] = [];
  private matchInterval: NodeJS.Timeout | null = null;
  private readonly matchFoundListeners = new Set<(event: MatchFoundEvent) => void>();

  constructor(private readonly combatService: CombatService) {
    // 每秒尝试匹配
    this.matchInterval = setInterval(() => this.processQueue(), 1000);
  }

  onModuleDestroy() {
    if (this.matchInterval) {
      clearInterval(this.matchInterval);
      this.matchInterval = null;
    }
  }

  onMatchFound(listener: (event: MatchFoundEvent) => void): () => void {
    this.matchFoundListeners.add(listener);
    return () => this.matchFoundListeners.delete(listener);
  }

  /**
   * 加入匹配队列
   */
  joinQueue(socketId: string, rank: number = 0): void {
    // 避免重复加入
    if (this.queue.find((e) => e.socketId === socketId)) return;

    this.queue.push({
      socketId,
      rank,
      joinTime: Date.now(),
    });

    console.log(`[Match] Player joined queue: ${socketId}, queue size: ${this.queue.length}`);
  }

  /**
   * 离开匹配队列
   */
  leaveQueue(socketId: string): void {
    this.queue = this.queue.filter((e) => e.socketId !== socketId);
    console.log(`[Match] Player left queue: ${socketId}`);
  }

  /**
   * 处理匹配队列
   */
  private processQueue(): void {
    if (this.queue.length < 2) return;

    // 按段位排序
    this.queue.sort((a, b) => a.rank - b.rank);

    // 简单匹配：取前两个段位最接近的
    const matched: [QueueEntry, QueueEntry][] = [];

    while (this.queue.length >= 2) {
      const player1 = this.queue.shift()!;
      const player2 = this.queue.shift()!;
      matched.push([player1, player2]);
    }

    // 创建对战
    for (const [p1, p2] of matched) {
      const roomId = this.combatService.createCombatRoom(p1.socketId, p2.socketId);
      console.log(`[Match] Matched: ${p1.socketId} vs ${p2.socketId}, room: ${roomId}`);

      this.emitMatchFound({
        player1SocketId: p1.socketId,
        player2SocketId: p2.socketId,
        roomId,
      });
    }
  }

  private emitMatchFound(event: MatchFoundEvent): void {
    for (const listener of this.matchFoundListeners) {
      listener(event);
    }
  }

  /**
   * 超时处理 - 5秒无匹配
   */
  checkTimeout(): QueueEntry[] {
    const now = Date.now();
    const timeout = 5000;
    const timedOut = this.queue.filter((e) => now - e.joinTime > timeout);

    // 移除超时玩家
    this.queue = this.queue.filter((e) => now - e.joinTime <= timeout);

    return timedOut;
  }
}
