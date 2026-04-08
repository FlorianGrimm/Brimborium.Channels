import { TestBed } from '@angular/core/testing';

import { VisualizationConnectionService } from './visualization-connection.service';

describe('VisualizationConnectionService', () => {
  let service: VisualizationConnectionService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(VisualizationConnectionService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
