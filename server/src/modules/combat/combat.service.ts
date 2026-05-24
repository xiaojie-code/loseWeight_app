import { Injectable } from '@nestjs/common';
import { DamageCalculator } from './damage-calculator';

interface PlayerState {
  id: string;
  socketId: string;
  hp: number;
  isDefending: boolean;
  isDodging: boolean;
  dodgeEndTime: number;
  roomId: string;
}

interface CombatRoom {
  id: string;
  players: [PlayerState, PlayerState];
  round: number;
  roundWins: [number, number];
  roundStartTime: number;
  isActive: boolean;
}

export interface DamageResult {
  type: 'DAMAGE';
  target: string;
  attacker: string;
  action: string;
  damage: number;
  hp: number;
  isKO: boolean;
}

export interface CombatRoundEndEvent {
  type: 'ROUND_END';
  roomId: string;
  winner: string | null;
  score: [number, number];
  round: number;
}

export interface CombatMatchEndEvent {
  type: 'MATCH_END';
  roomId: string;
  winner: string | null;
  result: string;
  reason?: string;
}

export type CombatLifecycleEvent = CombatRoundEndEvent | CombatMatchEndEvent;

export interface CombatActionResult {
  roomId: string;
  damage: DamageResult;
  events: CombatLifecycleEvent[];
}

/**
 * 对战服务 - 服务端权威伤害裁定
 */
@Injectable()
export class CombatService {
  private rooms: Map<string, CombatRoom> = new Map();
  private playerRooms: Map<string, string> = new Map(); // socketId -> roomId
  private readonly disconnectedPlayers = new Set<string>();
  private readonly lifecycleListeners = new Set<(event: CombatLifecycleEvent) => void>();

  constructor(private readonly damageCalc: DamageCalculator) {}

  onLifecycleEvent(listener: (event: CombatLifecycleEvent) => void): () => void {
    this.lifecycleListeners.add(listener);
    return () => this.lifecycleListeners.delete(listener);
  }

  /**
   * 创建对战房间
   */
  createCombatRoom(player1SocketId: string, player2SocketId: string): string {
    const roomId = this.generateRoomId();

    const room: CombatRoom = {
      id: roomId,
      players: [
        this.createPlayerState('player1', player1SocketId, roomId),
        this.createPlayerState('player2', player2SocketId, roomId),
      ],
      round: 1,
      roundWins: [0, 0],
      roundStartTime: Date.now(),
      isActive: true,
    };

    this.rooms.set(roomId, room);
    this.playerRooms.set(player1SocketId, roomId);
    this.playerRooms.set(player2SocketId, roomId);
    this.disconnectedPlayers.delete(player1SocketId);
    this.disconnectedPlayers.delete(player2SocketId);

    return roomId;
  }

  /**
   * 处理攻击 - 服务端权威伤害计算
   */
  processAttack(
    attackerSocketId: string,
    data: { action: string; power: string; ts: number },
  ): CombatActionResult | null {
    const room = this.getRoom(attackerSocketId);
    if (!room || !room.isActive) return null;

    const attacker = this.getPlayer(room, attackerSocketId);
    const defender = this.getOpponentState(room, attackerSocketId);
    if (!attacker || !defender) return null;

    // 服务端伤害计算
    let damage = this.damageCalc.calculate(data.action, data.power);

    // 检查防御/闪避
    if (this.isPlayerDodging(defender)) {
      damage = 0; // 闪避免伤
    } else if (defender.isDefending) {
      damage = Math.floor(damage * 0.5); // 防御减伤50%
    }

    // 扣血
    if (damage > 0) {
      defender.hp = Math.max(0, defender.hp - damage);
    }

    const damageResult: DamageResult = {
      type: 'DAMAGE',
      target: defender.id,
      attacker: attacker.id,
      action: data.action,
      damage,
      hp: defender.hp,
      isKO: defender.hp <= 0,
    };

    const events: CombatLifecycleEvent[] = [];

    // KO 判定
    if (defender.hp <= 0) {
      this.endRound(room, attacker, events);
    }

    return {
      roomId: room.id,
      damage: damageResult,
      events,
    };
  }

  /**
   * 处理防御
   */
  processDefend(socketId: string, active: boolean) {
    const room = this.getRoom(socketId);
    if (!room) return;

    const player = this.getPlayer(room, socketId);
    if (player) {
      player.isDefending = active;
    }
  }

  /**
   * 处理闪避
   */
  processDodge(socketId: string, direction: string) {
    const room = this.getRoom(socketId);
    if (!room) return;

    const player = this.getPlayer(room, socketId);
    if (player) {
      player.isDodging = true;
      player.dodgeEndTime = Date.now() + 300; // 300ms 闪避窗口
    }
  }

  /**
   * 处理断线
   */
  handleDisconnect(socketId: string) {
    const roomId = this.playerRooms.get(socketId);
    if (!roomId) return;

    const room = this.rooms.get(roomId);
    if (!room) return;

    this.disconnectedPlayers.add(socketId);

    // 5秒宽容窗口后判负
    setTimeout(() => {
      if (this.disconnectedPlayers.has(socketId)) {
        // 仍然断线，判负
        const opponent = this.getOpponentState(room, socketId);
        if (opponent && room.isActive) {
          this.endMatch(room, opponent, undefined, 'disconnect');
        }
      }
    }, 5000);
  }

  getPlayerRoom(socketId: string): string | undefined {
    return this.playerRooms.get(socketId);
  }

  getOpponent(socketId: string): string | undefined {
    const room = this.getRoom(socketId);
    if (!room) return undefined;
    const opponent = this.getOpponentState(room, socketId);
    return opponent?.socketId;
  }

  // ========== 私有方法 ==========

  private endRound(room: CombatRoom, winner: PlayerState, events?: CombatLifecycleEvent[]) {
    const winnerIndex = room.players[0] === winner ? 0 : 1;
    room.roundWins[winnerIndex]++;

    this.emitLifecycleEvent(
      {
        type: 'ROUND_END',
        roomId: room.id,
        winner: winner.id,
        score: [...room.roundWins] as [number, number],
        round: room.round,
      },
      events,
    );

    // 检查是否比赛结束
    if (room.roundWins[winnerIndex] >= 2 || room.round >= 3) {
      this.endMatch(room, winner, events);
    } else {
      room.round++;
      this.resetRound(room);
    }
  }

  private endMatch(
    room: CombatRoom,
    winner: PlayerState | null,
    events?: CombatLifecycleEvent[],
    reason?: string,
  ) {
    room.isActive = false;

    this.emitLifecycleEvent(
      {
        type: 'MATCH_END',
        roomId: room.id,
        winner: winner?.id ?? null,
        result: `${room.roundWins[0]}:${room.roundWins[1]}`,
        reason,
      },
      events,
    );

    // 清理
    room.players.forEach((p) => {
      this.playerRooms.delete(p.socketId);
      this.disconnectedPlayers.delete(p.socketId);
    });
    this.rooms.delete(room.id);
  }

  private resetRound(room: CombatRoom) {
    room.players.forEach((p) => {
      p.hp = 100;
      p.isDefending = false;
      p.isDodging = false;
    });
    room.roundStartTime = Date.now();
  }

  private isPlayerDodging(player: PlayerState): boolean {
    if (!player.isDodging) return false;
    if (Date.now() > player.dodgeEndTime) {
      player.isDodging = false;
      return false;
    }
    return true;
  }

  private getRoom(socketId: string): CombatRoom | undefined {
    const roomId = this.playerRooms.get(socketId);
    return roomId ? this.rooms.get(roomId) : undefined;
  }

  private getPlayer(room: CombatRoom, socketId: string): PlayerState | undefined {
    return room.players.find((p) => p.socketId === socketId);
  }

  private getOpponentState(room: CombatRoom, socketId: string): PlayerState | undefined {
    return room.players.find((p) => p.socketId !== socketId);
  }

  private createPlayerState(id: string, socketId: string, roomId: string): PlayerState {
    return { id, socketId, hp: 100, isDefending: false, isDodging: false, dodgeEndTime: 0, roomId };
  }

  private generateRoomId(): string {
    return Math.random().toString(36).substring(2, 8).toUpperCase();
  }

  private emitLifecycleEvent(event: CombatLifecycleEvent, events?: CombatLifecycleEvent[]) {
    if (events) {
      events.push(event);
      return;
    }

    for (const listener of this.lifecycleListeners) {
      listener(event);
    }
  }
}
