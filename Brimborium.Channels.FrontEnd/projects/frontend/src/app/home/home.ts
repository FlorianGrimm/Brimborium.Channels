import { Component, ChangeDetectionStrategy } from '@angular/core';
import { FCanvasComponent, FCreateConnectionEvent, FFlowModule } from '@foblex/flow';
import { BrimboriumChannels} from 'brimborium-channels';
@Component({
  selector: 'app-home',
  imports: [FFlowModule, BrimboriumChannels],
  templateUrl: './home.html',
  styleUrl: './home.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Home {}
