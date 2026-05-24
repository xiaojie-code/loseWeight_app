import { Controller, Post, Get, Body, Query } from '@nestjs/common';
import { UserService } from './user.service';

/**
 * 用户 REST API
 */
@Controller('user')
export class UserController {
  constructor(private readonly userService: UserService) {}

  /**
   * 微信登录
   * POST /user/login
   */
  @Post('login')
  async login(@Body() body: { code: string }) {
    return this.userService.wxLogin(body.code);
  }

  /**
   * 获取用户信息
   * GET /user/profile?openId=xxx
   */
  @Get('profile')
  async getProfile(@Query('openId') openId: string) {
    return this.userService.getProfile(openId);
  }

  /**
   * 更新用户信息
   * POST /user/update
   */
  @Post('update')
  async updateProfile(
    @Body()
    body: {
      openId: string;
      nickname?: string;
      gender?: number;
      province?: string;
      city?: string;
    },
  ) {
    return this.userService.updateProfile(body);
  }

  /**
   * 获取战绩列表
   * GET /user/records?openId=xxx&page=1&limit=20
   */
  @Get('records')
  async getRecords(
    @Query('openId') openId: string,
    @Query('page') page: number = 1,
    @Query('limit') limit: number = 20,
  ) {
    return this.userService.getRecords(openId, page, limit);
  }

  /**
   * 更新装扮
   * POST /user/dressup
   */
  @Post('dressup')
  async updateDressup(
    @Body() body: { openId: string; outfit?: number; glove?: number },
  ) {
    return this.userService.updateDressup(body);
  }

  /**
   * 解锁装扮（广告奖励）
   * POST /user/unlock
   */
  @Post('unlock')
  async unlockItem(
    @Body() body: { openId: string; type: 'outfit' | 'glove'; index: number },
  ) {
    return this.userService.unlockItem(body);
  }
}
