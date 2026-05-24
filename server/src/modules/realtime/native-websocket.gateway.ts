import { Injectable, Logger, OnApplicationBootstrap, OnModuleDestroy } from '@nestjs/common';
import { HttpAdapterHost } from '@nestjs/core';
import { randomUUID } from 'crypto';
import { RawData, WebSocket, WebSocketServer } from 'ws';
import { CombatLifecycleEvent, CombatService } from '../combat/combat.service';
import { MatchFoundEvent, MatchService } from '../match/match.service';
import { RoomService } from '../room/room.service';

interface ClientConnection {
  id: string;
  socket: WebSocket;
  rooms: Set<string>;
}

interface ClientMessage {
  type?: string;
  action?: string;
  power?: string;
  direction?: string;
  active?: boolean;
  roomId?: string;
  rank?: number;
  ts?: number;
}

type ServerMessage = { type: string };

@Injectable()
export class NativeWebSocketGateway implements OnApplicationBootstrap, OnModuleDestroy {
  private readonly logger = new Logger(NativeWebSocketGateway.name);
  private readonly clients = new Map<string, ClientConnection>();
  private server?: WebSocketServer;
  private unsubscribeMatchFound?: () => void;
  private unsubscribeCombatLifecycle?: () => void;

  constructor(
    private readonly httpAdapterHost: HttpAdapterHost,
    private readonly combatService: CombatService,
    private readonly matchService: MatchService,
    private readonly roomService: RoomService,
  ) {}

  onApplicationBootstrap() {
    const httpServer = this.httpAdapterHost.httpAdapter.getHttpServer();

    this.server = new WebSocketServer({ server: httpServer, path: '/ws' });
    this.server.on('connection', (socket) => this.handleConnection(socket));
    this.unsubscribeMatchFound = this.matchService.onMatchFound((event) => this.handleMatchFound(event));
    this.unsubscribeCombatLifecycle = this.combatService.onLifecycleEvent((event) =>
      this.handleCombatLifecycle(event),
    );

    this.logger.log('Native WebSocket gateway listening on /ws');
  }

  onModuleDestroy() {
    this.unsubscribeMatchFound?.();
    this.unsubscribeCombatLifecycle?.();
    this.server?.close();
  }

  private handleConnection(socket: WebSocket): void {
    const client: ClientConnection = {
      id: randomUUID(),
      socket,
      rooms: new Set(),
    };

    this.clients.set(client.id, client);
    this.send(client, { type: 'CONNECTED', playerId: client.id });
    this.logger.log(`Client connected: ${client.id}`);

    socket.on('message', (data) => this.handleMessage(client, data));
    socket.on('close', () => this.handleDisconnect(client));
    socket.on('error', (error) => this.logger.warn(`Client ${client.id} socket error: ${error.message}`));
  }

  private handleDisconnect(client: ClientConnection): void {
    this.clients.delete(client.id);
    this.matchService.leaveQueue(client.id);
    this.roomService.handleDisconnect(client.id);
    this.combatService.handleDisconnect(client.id);
    this.logger.log(`Client disconnected: ${client.id}`);
  }

  private handleMessage(client: ClientConnection, data: RawData): void {
    const message = this.parseMessage(data);
    if (!message?.type) {
      this.sendError(client, '消息格式错误');
      return;
    }

    switch (message.type) {
      case 'MATCH_REQUEST':
        this.matchService.joinQueue(client.id, message.rank ?? 0);
        this.send(client, { type: 'MATCH_SEARCHING' });
        break;
      case 'MATCH_CANCEL':
        this.matchService.leaveQueue(client.id);
        this.send(client, { type: 'MATCH_CANCELLED' });
        break;
      case 'CREATE_ROOM':
        this.handleCreateRoom(client);
        break;
      case 'JOIN_ROOM':
        this.handleJoinRoom(client, message.roomId);
        break;
      case 'ROOM_START':
        this.handleRoomStart(client, message.roomId);
        break;
      case 'ATTACK':
        this.handleAttack(client, message);
        break;
      case 'DEFEND':
        this.handleDefend(client, message);
        break;
      case 'DODGE':
        this.handleDodge(client, message);
        break;
      default:
        this.sendError(client, `未知消息类型: ${message.type}`);
        break;
    }
  }

  private handleCreateRoom(client: ClientConnection): void {
    const roomId = this.roomService.createRoom(client.id);
    this.joinRoom(client, roomId);
    this.send(client, { type: 'ROOM_CREATED', roomId });
  }

  private handleJoinRoom(client: ClientConnection, roomId?: string): void {
    if (!roomId) {
      this.sendError(client, '缺少房间号');
      return;
    }

    const result = this.roomService.joinRoom(roomId, client.id);
    if (!result.success) {
      this.send(client, { type: 'ROOM_ERROR', message: result.message ?? '加入房间失败' });
      return;
    }

    this.joinRoom(client, roomId);
    this.send(client, { type: 'ROOM_JOINED', roomId });
    this.broadcastRoom(roomId, { type: 'PLAYER_JOINED', playerId: client.id }, client.id);
  }

  private handleRoomStart(client: ClientConnection, roomId?: string): void {
    if (!roomId) {
      this.sendError(client, '缺少房间号');
      return;
    }

    const result = this.roomService.startGame(roomId, client.id);
    if (!result.success || !result.combatRoomId) {
      this.send(client, { type: 'ROOM_ERROR', message: result.message ?? '开始游戏失败' });
      return;
    }

    this.joinClientsFromRoom(roomId, result.combatRoomId);
    this.broadcastRoom(roomId, { type: 'GAME_STARTING', roomId: result.combatRoomId, countdown: 3 });
  }

  private handleAttack(client: ClientConnection, message: ClientMessage): void {
    if (!message.action || !message.power) {
      this.sendError(client, '攻击消息缺少 action 或 power');
      return;
    }

    const result = this.combatService.processAttack(client.id, {
      action: message.action,
      power: message.power,
      ts: message.ts ?? Date.now(),
    });

    if (result) {
      this.broadcastRoom(result.roomId, result.damage);
      for (const event of result.events) {
        this.broadcastRoom(event.roomId, event);
      }
    }
  }

  private handleDefend(client: ClientConnection, message: ClientMessage): void {
    this.combatService.processDefend(client.id, message.active ?? false);

    const opponentId = this.combatService.getOpponent(client.id);
    if (opponentId) {
      this.sendById(opponentId, { type: 'OPPONENT_DEFEND', active: message.active ?? false });
    }
  }

  private handleDodge(client: ClientConnection, message: ClientMessage): void {
    if (!message.direction) {
      this.sendError(client, '闪避消息缺少 direction');
      return;
    }

    this.combatService.processDodge(client.id, message.direction);

    const opponentId = this.combatService.getOpponent(client.id);
    if (opponentId) {
      this.sendById(opponentId, { type: 'OPPONENT_DODGE', direction: message.direction });
    }
  }

  private handleMatchFound(event: MatchFoundEvent): void {
    this.joinRoomById(event.player1SocketId, event.roomId);
    this.joinRoomById(event.player2SocketId, event.roomId);

    this.sendById(event.player1SocketId, {
      type: 'MATCH_FOUND',
      roomId: event.roomId,
      opponentId: event.player2SocketId,
    });
    this.sendById(event.player2SocketId, {
      type: 'MATCH_FOUND',
      roomId: event.roomId,
      opponentId: event.player1SocketId,
    });
  }

  private handleCombatLifecycle(event: CombatLifecycleEvent): void {
    this.broadcastRoom(event.roomId, event);
  }

  private parseMessage(data: RawData): ClientMessage | null {
    try {
      return JSON.parse(data.toString()) as ClientMessage;
    } catch {
      return null;
    }
  }

  private joinRoom(client: ClientConnection, roomId: string): void {
    client.rooms.add(roomId);
  }

  private joinRoomById(clientId: string, roomId: string): void {
    const client = this.clients.get(clientId);
    if (client) {
      this.joinRoom(client, roomId);
    }
  }

  private joinClientsFromRoom(sourceRoomId: string, targetRoomId: string): void {
    for (const client of this.clients.values()) {
      if (client.rooms.has(sourceRoomId)) {
        client.rooms.add(targetRoomId);
      }
    }
  }

  private broadcastRoom<T extends ServerMessage>(roomId: string, message: T, exceptClientId?: string): void {
    for (const client of this.clients.values()) {
      if (client.id !== exceptClientId && client.rooms.has(roomId)) {
        this.send(client, message);
      }
    }
  }

  private sendById<T extends ServerMessage>(clientId: string, message: T): void {
    const client = this.clients.get(clientId);
    if (client) {
      this.send(client, message);
    }
  }

  private send<T extends ServerMessage>(client: ClientConnection, message: T): void {
    if (client.socket.readyState === WebSocket.OPEN) {
      client.socket.send(JSON.stringify(message));
    }
  }

  private sendError(client: ClientConnection, message: string): void {
    this.send(client, { type: 'ERROR', message });
  }
}