import { Injectable } from '@nestjs/common';

/**
 * 伤害计算器 - 服务端权威
 * 参数与客户端 CombatConfig 保持一致
 */
@Injectable()
export class DamageCalculator {
  // 伤害表（与客户端 CombatConfig 一致）
  private readonly damageTable: Record<string, Record<string, number>> = {
    LeftStraight: { Light: 5, Heavy: 10 },
    RightStraight: { Light: 5, Heavy: 10 },
    LeftUppercut: { Light: 8, Heavy: 15 },
    RightUppercut: { Light: 8, Heavy: 15 },
  };

  /**
   * 计算伤害值
   */
  calculate(action: string, power: string): number {
    const actionDamage = this.damageTable[action];
    if (!actionDamage) return 0;

    return actionDamage[power] || 0;
  }

  /**
   * 应用防御减伤
   */
  applyDefense(damage: number): number {
    return Math.floor(damage * 0.5); // 50% 减伤
  }

  /**
   * 应用连击加成（可选）
   */
  applyComboBonus(damage: number, combo: number): number {
    if (combo < 3) return damage;
    if (combo < 5) return Math.floor(damage * 1.1);
    if (combo < 8) return Math.floor(damage * 1.2);
    return Math.floor(damage * 1.3);
  }
}
