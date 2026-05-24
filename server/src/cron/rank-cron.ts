import { Injectable } from '@nestjs/common';
import * as cron from 'node-cron';
import { RankService } from '../modules/rank/rank.service';

/**
 * 排行榜定时任务
 * 每日 0:00 重算城市/省份排名
 */
@Injectable()
export class RankCron {
  constructor(private readonly rankService: RankService) {
    this.setupCronJobs();
  }

  private setupCronJobs() {
    // 每日 0:00 重算排行
    cron.schedule('0 0 * * *', async () => {
      console.log('[Cron] Starting daily rank recalculation...');
      try {
        await this.rankService.recalculateRanks();
        console.log('[Cron] Rank recalculation complete');
      } catch (error) {
        console.error('[Cron] Rank recalculation failed:', error);
      }
    });

    console.log('[Cron] Rank cron job scheduled: daily at 00:00');
  }
}
