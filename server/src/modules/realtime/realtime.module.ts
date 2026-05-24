import { Module } from '@nestjs/common';
import { CombatModule } from '../combat/combat.module';
import { MatchModule } from '../match/match.module';
import { RoomModule } from '../room/room.module';
import { NativeWebSocketGateway } from './native-websocket.gateway';

@Module({
  imports: [CombatModule, MatchModule, RoomModule],
  providers: [NativeWebSocketGateway],
})
export class RealtimeModule {}