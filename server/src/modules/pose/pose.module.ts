import { Module } from '@nestjs/common';
import { PoseController } from './pose.controller';
import { PoseService } from './pose.service';

@Module({
  controllers: [PoseController],
  providers: [PoseService],
})
export class PoseModule {}
