import { Injectable } from '@nestjs/common';
import { CombatService } from '../combat/combat.service';

interface GameRoom {
  id: string;
  hostSocketId: string;
  guestSocketId: string | null;
  createdAt: number;
  state: 'waiting' | 'ready' | 'playing';
}

/**
 * 房间服务 - 创建/加入/管理房间
 * 5分钟无人加入自动解散
 */
@Injectable()
export class RoomService {
  private rooms: Map<string, GameRoom> = new Map();
  private playerRooms: Map<string, string> = new Map(); // socketId -> roomId

  constructor(private readonly combatService: CombatService) {
    // 每分钟检查超时房间
    setInterval(() => this.cleanupExpiredRooms(), 60000);
  }

  /**
   * 创建房间
   */
  createRoom(hostSocketId: string): string {
    const roomId = this.generateRoomId();

    const room: GameRoom = {
      id: roomId,
      hostSocketId,
      guestSocketId: null,
      createdAt: Date.now(),
      state: 'waiting',
    };

    this.rooms.set(roomId, room);
    this.playerRooms.set(hostSocketId, roomId);

    console.log(`[Room] Created: ${roomId} by ${hostSocketId}`);
    return roomId;
  }

  /**
   * 加入房间
   */
  joinRoom(roomId: string, guestSocketId: string): { success: boolean; message?: string } {
    const room = this.rooms.get(roomId);

    if (!room) {
      return { success: false, message: '房间不存在' };
    }
    if (room.state !== 'waiting') {
      return { success: false, message: '房间已满或游戏已开始' };
    }
    if (room.guestSocketId) {
      return { success: false, message: '房间已满' };
    }

    room.guestSocketId = guestSocketId;
    room.state = 'ready';
    this.playerRooms.set(guestSocketId, roomId);

    console.log(`[Room] ${guestSocketId} joined ${roomId}`);
    return { success: true };
  }

  /**
   * 房主开始游戏
   */
  startGame(
    roomId: string,
    requesterId: string,
  ): { success: boolean; message?: string; combatRoomId?: string } {
    const room = this.rooms.get(roomId);

    if (!room) return { success: false, message: '房间不存在' };
    if (room.hostSocketId !== requesterId) return { success: false, message: '只有房主可以开始游戏' };
    if (room.state !== 'ready') return { success: false, message: '等待对手加入' };
    if (!room.guestSocketId) return { success: false, message: '没有对手' };

    room.state = 'playing';

    // 创建对战
    const combatRoomId = this.combatService.createCombatRoom(room.hostSocketId, room.guestSocketId);

    console.log(`[Room] Game started in ${roomId}`);
    return { success: true, combatRoomId };
  }

  /**
   * 踢出玩家
   */
  kickPlayer(
    roomId: string,
    hostSocketId: string,
    targetSocketId: string,
  ): { success: boolean; message?: string } {
    const room = this.rooms.get(roomId);

    if (!room) return { success: false, message: '房间不存在' };
    if (room.hostSocketId !== hostSocketId) return { success: false, message: '只有房主可以踢人' };
    if (room.guestSocketId !== targetSocketId) return { success: false, message: '目标不在房间中' };

    room.guestSocketId = null;
    room.state = 'waiting';
    this.playerRooms.delete(targetSocketId);

    return { success: true };
  }

  /**
   * 处理断线
   */
  handleDisconnect(socketId: string) {
    const roomId = this.playerRooms.get(socketId);
    if (!roomId) return;

    const room = this.rooms.get(roomId);
    if (!room) return;

    if (room.hostSocketId === socketId) {
      // 房主断线，解散房间
      this.dissolveRoom(roomId);
    } else if (room.guestSocketId === socketId) {
      // 访客断线
      room.guestSocketId = null;
      room.state = 'waiting';
      this.playerRooms.delete(socketId);
    }
  }

  private dissolveRoom(roomId: string) {
    const room = this.rooms.get(roomId);
    if (!room) return;

    this.playerRooms.delete(room.hostSocketId);
    if (room.guestSocketId) {
      this.playerRooms.delete(room.guestSocketId);
    }
    this.rooms.delete(roomId);

    console.log(`[Room] Dissolved: ${roomId}`);
  }

  private cleanupExpiredRooms() {
    const now = Date.now();
    const timeout = 5 * 60 * 1000; // 5分钟

    for (const [roomId, room] of this.rooms) {
      if (room.state === 'waiting' && now - room.createdAt > timeout) {
        this.dissolveRoom(roomId);
        console.log(`[Room] Expired: ${roomId}`);
      }
    }
  }

  private generateRoomId(): string {
    // 6位数字房间号
    return String(Math.floor(100000 + Math.random() * 900000));
  }
}
