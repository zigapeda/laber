import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonicModule } from '@ionic/angular';
import { ChannelsPageRoutingModule } from './channels-routing.module';
import { ChannelsPage } from './channels.page';
import { ChannelDetailPage } from './channel-detail/channel-detail.page';

@NgModule({
  imports: [IonicModule, CommonModule, FormsModule, ChannelsPageRoutingModule],
  declarations: [ChannelsPage, ChannelDetailPage],
})
export class ChannelsPageModule {}
