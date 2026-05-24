import { Controller, Get, Query } from '@nestjs/common';
import { RankService } from './rank.service';

/**
 * 排行榜 REST API
 */
@Controller('rank')
export class RankController {
  constructor(private readonly rankService: RankService) {}

  /**
   * 获取市内排行
   * GET /rank/city?city=深圳市&page=1&limit=20
   */
  @Get('city')
  getCityRank(
    @Query('city') city: string,
    @Query('page') page: number = 1,
    @Query('limit') limit: number = 20,
  ) {
    return this.rankService.getCityRank(city, page, limit);
  }

  /**
   * 获取省内城市排行
   * GET /rank/province?province=广东省
   */
  @Get('province')
  getProvinceRank(@Query('province') province: string) {
    return this.rankService.getProvinceRank(province);
  }

  /**
   * 获取全国省份排行
   * GET /rank/national
   */
  @Get('national')
  getNationalRank() {
    return this.rankService.getNationalRank();
  }

  /**
   * 获取玩家自己的排名
   * GET /rank/my?openId=xxx
   */
  @Get('my')
  getMyRank(@Query('openId') openId: string) {
    return this.rankService.getPlayerRank(openId);
  }
}
