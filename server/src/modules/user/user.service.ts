import { Injectable } from '@nestjs/common';

export interface UserProfile {
  openId: string;
  nickname: string;
  gender: number;
  province: string;
  city: string;
  totalWins: number;
  totalLosses: number;
  totalKnockouts: number;
  currentStreak: number;
  maxStreak: number;
  equippedOutfit: number;
  equippedGlove: number;
  unlockedOutfits: boolean[];
  unlockedGloves: boolean[];
}

/**
 * 用户服务
 * TODO: 接入 TypeORM + MySQL
 */
@Injectable()
export class UserService {
  // 临时内存存储（后续替换为数据库）
  private users: Map<string, UserProfile> = new Map();

  /**
   * 微信登录 - code 换 openId
   */
  async wxLogin(code: string) {
    // TODO: 调用微信 API
    // https://api.weixin.qq.com/sns/jscode2session
    // ?appid=APPID&secret=SECRET&js_code=CODE&grant_type=authorization_code

    // 模拟返回
    const openId = `mock_${code}`;
    let user = this.users.get(openId);

    if (!user) {
      user = this.createDefaultUser(openId);
      this.users.set(openId, user);
    }

    return {
      openId,
      token: `token_${Date.now()}`,
      isNewUser: !user.nickname,
      profile: user,
    };
  }

  /**
   * 获取用户资料
   */
  async getProfile(openId: string): Promise<UserProfile | null> {
    return this.users.get(openId) || null;
  }

  /**
   * 更新用户资料
   */
  async updateProfile(data: Partial<UserProfile> & { openId: string }) {
    const user = this.users.get(data.openId);
    if (!user) return { success: false, message: '用户不存在' };

    if (data.nickname !== undefined) user.nickname = data.nickname;
    if (data.gender !== undefined) user.gender = data.gender;
    if (data.province !== undefined) user.province = data.province;
    if (data.city !== undefined) user.city = data.city;

    return { success: true };
  }

  /**
   * 获取战绩记录
   */
  async getRecords(openId: string, page: number, limit: number) {
    // TODO: 从数据库查询
    return { list: [], total: 0, page };
  }

  /**
   * 更新装扮
   */
  async updateDressup(data: { openId: string; outfit?: number; glove?: number }) {
    const user = this.users.get(data.openId);
    if (!user) return { success: false };

    if (data.outfit !== undefined && user.unlockedOutfits[data.outfit]) {
      user.equippedOutfit = data.outfit;
    }
    if (data.glove !== undefined && user.unlockedGloves[data.glove]) {
      user.equippedGlove = data.glove;
    }

    return { success: true };
  }

  /**
   * 解锁装扮
   */
  async unlockItem(data: { openId: string; type: 'outfit' | 'glove'; index: number }) {
    const user = this.users.get(data.openId);
    if (!user) return { success: false };

    if (data.type === 'outfit' && data.index < user.unlockedOutfits.length) {
      user.unlockedOutfits[data.index] = true;
    } else if (data.type === 'glove' && data.index < user.unlockedGloves.length) {
      user.unlockedGloves[data.index] = true;
    }

    return { success: true };
  }

  /**
   * 记录对战结果
   */
  async recordBattleResult(openId: string, isWin: boolean, isKO: boolean) {
    const user = this.users.get(openId);
    if (!user) return;

    if (isWin) {
      user.totalWins++;
      user.currentStreak++;
      if (user.currentStreak > user.maxStreak) {
        user.maxStreak = user.currentStreak;
      }
      if (isKO) {
        user.totalKnockouts++;
      }
    } else {
      user.totalLosses++;
      user.currentStreak = 0;
    }
  }

  private createDefaultUser(openId: string): UserProfile {
    return {
      openId,
      nickname: '',
      gender: 0,
      province: '',
      city: '',
      totalWins: 0,
      totalLosses: 0,
      totalKnockouts: 0,
      currentStreak: 0,
      maxStreak: 0,
      equippedOutfit: 0,
      equippedGlove: 0,
      unlockedOutfits: [true, false, false],
      unlockedGloves: [true, false, false],
    };
  }
}
