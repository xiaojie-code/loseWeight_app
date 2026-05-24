import { Module } from '@nestjs/common';
import { MatchService } from './match.service';
import { CombatModule } from '../combat/combat.module';

@Module({
  imports: [CombatModule],
  providers: [MatchService],
  exports: [MatchService],
})
export class MatchModule {}
