import { Module } from '@nestjs/common';
import { RoomService } from './room.service';
import { CombatModule } from '../combat/combat.module';

@Module({
  imports: [CombatModule],
  providers: [RoomService],
  exports: [RoomService],
})
export class RoomModule {}
