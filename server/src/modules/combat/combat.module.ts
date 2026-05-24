import { Module } from '@nestjs/common';
import { CombatService } from './combat.service';
import { DamageCalculator } from './damage-calculator';

@Module({
  providers: [CombatService, DamageCalculator],
  exports: [CombatService],
})
export class CombatModule {}
