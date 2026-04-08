import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BrimboriumChannels } from './brimborium-channels';
import { FFlowModule } from '@foblex/flow';
import {beforeEach, describe, expect, it, vi, type Mocked, MockInstance} from 'vitest';
import { VisualizationConnectionService } from './visualization-connection.service';

// vi.mock('./visualization-connection.service', {spy:true})
// const visualizationConnectionService: Mocked<VisualizationConnectionService> = { 
//   $lastError: MockInstance<WritableSignal<string>>;
//    ensureConnected: MockInstance<() => void>; ensureConnectedAsync: MockInstance<() => Promise<IBCVisualizationHub>>; getDescriptionGraph:
//  }
// ;

describe('BrimboriumChannels', () => {
  let component: BrimboriumChannels;
  let fixture: ComponentFixture<BrimboriumChannels>;

  beforeAll(() => {
    class ResizeObserverMock {
      observe() {}
      unobserve() {}
      disconnect() {}
    }
    (window as any).ResizeObserver = ResizeObserverMock;
  });

  beforeEach(async () => {
    await TestBed
      .configureTestingModule({
        imports: [BrimboriumChannels,FFlowModule],
      })
      // .overrideComponent(BrimboriumChannels, {
      //   remove: { imports: [VisualizationConnectionService] },
      //   add: { imports: [visualizationConnectionService] }
      // })
      .compileComponents();
    fixture = TestBed.createComponent(BrimboriumChannels);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

