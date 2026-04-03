import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BrimboriumChannels } from './brimborium-channels';

describe('BrimboriumChannels', () => {
  let component: BrimboriumChannels;
  let fixture: ComponentFixture<BrimboriumChannels>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BrimboriumChannels],
    }).compileComponents();

    fixture = TestBed.createComponent(BrimboriumChannels);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
