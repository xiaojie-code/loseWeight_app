import { Injectable } from '@nestjs/common';

/**
 * 排行榜服务
 * 使用 Redis ZSet 实现三级排行
 * 每日 0:00 重算城市/省份排名
 *
 * 计算规则：
 * - 市内排行：按累计击败人数排序
 * - 省内城市排行：城市玩家平均击败数（活跃玩家≥10人才参与）
 * - 全国省份排行：省份玩家平均击败数
 */
@Injectable()
export class RankService {
  // TODO: 注入 Redis 客户端
  // private redis: Redis;

  /**
   * 获取市内排行
   */
  async getCityRank(city: string, page: number = 1, limit: number = 20) {
    const start = (page - 1) * limit;
    const end = start + limit - 1;

    // Redis: ZREVRANGE rank:city:{city} start end WITHSCORES
    // 暂时返回模拟数据
    return {
      city,
      page,
      total: 0,
      list: [],
    };
  }

  /**
   * 获取省内城市排行
   */
  async getProvinceRank(province: string) {
    // Redis: ZREVRANGE rank:province:{province} 0 -1 WITHSCORES
    return {
      province,
      list: [],
    };
  }

  /**
   * 获取全国省份排行
   */
  async getNationalRank() {
    // Redis: ZREVRANGE rank:national 0 -1 WITHSCORES
    return {
      list: [],
    };
  }

  /**
   * 获取玩家自己的排名
   */
  async getPlayerRank(openId: string) {
    // Redis: ZREVRANK rank:city:{playerCity} {openId}
    return {
      cityRank: 0,
      knockouts: 0,
    };
  }

  /**
   * 更新玩家击败数（对战结束后调用）
   */
  async addKnockout(openId: string, city: string) {
    // Redis: ZINCRBY rank:city:{city} 1 {openId}
  }

  /**
   * 每日重算排行（由 Cron 调用）
   * 计算城市平均击败数 → 省内排行
   * 计算省份平均击败数 → 全国排行
   */
  async recalculateRanks() {
    console.log('[Rank] Recalculating daily ranks...');

    // 1. 获取所有城市
    // 2. 对每个城市：计算近7天活跃玩家的平均击败数
    // 3. 活跃玩家 ≥ 10 人才参与省内排名
    // 4. 更新省内城市排行 ZSet
    // 5. 汇总省份数据，更新全国排行

    console.log('[Rank] Recalculation complete');
  }
}
