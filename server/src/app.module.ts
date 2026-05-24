import { Module } from '@nestjs/common';
import { UserModule } from './modules/user/user.module';
import { CombatModule } from './modules/combat/combat.module';
import { MatchModule } from './modules/match/match.module';
import { RoomModule } from './modules/room/room.module';
import { RankModule } from './modules/rank/rank.module';
import { RealtimeModule } from './modules/realtime/realtime.module';
import { PoseModule } from './modules/pose/pose.module';

@Module({
  imports: [
    UserModule,
    CombatModule,
    MatchModule,
    RoomModule,
    RankModule,
    RealtimeModule,
    PoseModule,
  ],
})
export class AppModule {}
