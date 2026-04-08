import { BCDescriptionGraph, BCDescriptionNode, BCLogRecord } from './generated/Brimborium.Channels';
import { VisualizationUility } from './visualization-uility';

describe('VisualizationUility', () => {
  const json = '{"descriptionGraph":{"graphId":"cea446b4-c79b-434a-ac4f-1c49ca67f160","nodes":{"fee9bd93-d956-45cd-887a-b15003945e52":{"kind":"Operation","name":"sinkAvg","parent":"","incoming":null,"outgoing":null,"nodeId":"fee9bd93-d956-45cd-887a-b15003945e52"},"8c8aa0f8-56ed-46a4-bf42-c2a8a8448d95":{"kind":"Operation","name":"sinkSum","parent":"","incoming":null,"outgoing":null,"nodeId":"8c8aa0f8-56ed-46a4-bf42-c2a8a8448d95"},"9040ac42-27f0-4314-ae37-4f341e7c0443":{"kind":"Operation","name":"source","parent":"","incoming":null,"outgoing":["bed24e46-b84e-4be4-8643-f4b47006d741"],"nodeId":"9040ac42-27f0-4314-ae37-4f341e7c0443"},"bed24e46-b84e-4be4-8643-f4b47006d741":{"kind":"Operation","name":"SumAvg","parent":"","incoming":null,"outgoing":["8c8aa0f8-56ed-46a4-bf42-c2a8a8448d95","fee9bd93-d956-45cd-887a-b15003945e52"],"nodeId":"bed24e46-b84e-4be4-8643-f4b47006d741"}}},"listLogRecord":[{"timestamp":"2026-04-07T22:30:48.1986664Z","nodeId":"9040ac42-27f0-4314-ae37-4f341e7c0443","name":"OnNext","kind":"Start"},{"timestamp":"2026-04-07T22:30:48.2006806Z","nodeId":"9040ac42-27f0-4314-ae37-4f341e7c0443","name":"OnNext","kind":"End"},{"timestamp":"2026-04-07T22:30:48.2130489Z","nodeId":"9040ac42-27f0-4314-ae37-4f341e7c0443","name":"OnNext","kind":"Start"},{"timestamp":"2026-04-07T22:30:48.2130691Z","nodeId":"9040ac42-27f0-4314-ae37-4f341e7c0443","name":"OnNext","kind":"End"},{"timestamp":"2026-04-07T22:30:48.2286658Z","nodeId":"9040ac42-27f0-4314-ae37-4f341e7c0443","name":"OnNext","kind":"Start"},{"timestamp":"2026-04-07T22:30:48.2286734Z","nodeId":"9040ac42-27f0-4314-ae37-4f341e7c0443","name":"OnNext","kind":"End"},{"timestamp":"2026-04-07T22:30:48.2444052Z","nodeId":"9040ac42-27f0-4314-ae37-4f341e7c0443","name":"OnNext","kind":"Start"},{"timestamp":"2026-04-07T22:30:48.2444167Z","nodeId":"9040ac42-27f0-4314-ae37-4f341e7c0443","name":"OnNext","kind":"End"},{"timestamp":"2026-04-07T22:30:48.2598372Z","nodeId":"9040ac42-27f0-4314-ae37-4f341e7c0443","name":"OnNext","kind":"Start"},{"timestamp":"2026-04-07T22:30:48.2598499Z","nodeId":"9040ac42-27f0-4314-ae37-4f341e7c0443","name":"OnNext","kind":"End"},{"timestamp":"2026-04-07T22:30:48.2771151Z","nodeId":"9040ac42-27f0-4314-ae37-4f341e7c0443","name":"OnComplete","kind":"Start"},{"timestamp":"2026-04-07T22:30:48.2788505Z","nodeId":"bed24e46-b84e-4be4-8643-f4b47006d741","name":"OnComplete","kind":"Start"},{"timestamp":"2026-04-07T22:30:48.2798353Z","nodeId":"8c8aa0f8-56ed-46a4-bf42-c2a8a8448d95","name":"OnNext","kind":"Start"},{"timestamp":"2026-04-07T22:30:48.2799779Z","nodeId":"8c8aa0f8-56ed-46a4-bf42-c2a8a8448d95","name":"OnNext","kind":"End"},{"timestamp":"2026-04-07T22:30:48.2802403Z","nodeId":"8c8aa0f8-56ed-46a4-bf42-c2a8a8448d95","name":"OnComplete","kind":"Start"},{"timestamp":"2026-04-07T22:30:48.2804774Z","nodeId":"8c8aa0f8-56ed-46a4-bf42-c2a8a8448d95","name":"OnComplete","kind":"End"},{"timestamp":"2026-04-07T22:30:48.2809118Z","nodeId":"fee9bd93-d956-45cd-887a-b15003945e52","name":"OnNext","kind":"Start"},{"timestamp":"2026-04-07T22:30:48.2809141Z","nodeId":"fee9bd93-d956-45cd-887a-b15003945e52","name":"OnNext","kind":"End"},{"timestamp":"2026-04-07T22:30:48.2810949Z","nodeId":"fee9bd93-d956-45cd-887a-b15003945e52","name":"OnComplete","kind":"Start"},{"timestamp":"2026-04-07T22:30:48.2813949Z","nodeId":"fee9bd93-d956-45cd-887a-b15003945e52","name":"OnComplete","kind":"End"},{"timestamp":"2026-04-07T22:30:48.2813995Z","nodeId":"bed24e46-b84e-4be4-8643-f4b47006d741","name":"OnComplete","kind":"End"},{"timestamp":"2026-04-07T22:30:48.2814011Z","nodeId":"9040ac42-27f0-4314-ae37-4f341e7c0443","name":"OnComplete","kind":"End"}]}'
  const testData = JSON.parse(json) as {
    descriptionGraph: BCDescriptionGraph,
    listLogRecord: BCLogRecord[],
  };

  const DescriptionNode1: BCDescriptionNode = {
    "kind": "Operation", "name": "sinkAvg", "parent": "", "incoming": [], "outgoing": undefined, "nodeId": "fee9bd93-d956-45cd-887a-b15003945e52"
  };
  const DescriptionNode2: BCDescriptionNode = {
    "kind": "Operation", "name": "sinkSum", "parent": "", "incoming": undefined, "outgoing": undefined, "nodeId": "8c8aa0f8-56ed-46a4-bf42-c2a8a8448d95"
  };
  const DescriptionNode3: BCDescriptionNode = {
    "kind": "Operation", "name": "source", "parent": "", "incoming": undefined, "outgoing": ["bed24e46-b84e-4be4-8643-f4b47006d741"], "nodeId": "9040ac42-27f0-4314-ae37-4f341e7c0443"
  };
  const DescriptionNode4: BCDescriptionNode = {
    "kind": "Operation", "name": "SumAvg", "parent": "", "incoming": undefined, "outgoing": ["8c8aa0f8-56ed-46a4-bf42-c2a8a8448d95", "fee9bd93-d956-45cd-887a-b15003945e52"], "nodeId": "bed24e46-b84e-4be4-8643-f4b47006d741"
  };

  it('should create an instance', () => {
    const sut = new VisualizationUility(undefined, undefined);
    expect(sut).toBeTruthy();
  });

  it('setDescriptionGraph', () => {
    const sut = new VisualizationUility(undefined, undefined);
    sut.setDescriptionGraph(testData.descriptionGraph);
    expect(sut.mapVisualPart.size).toBe(4);

    const visualPart3 = sut.getVisualPart(DescriptionNode3.nodeId);
    expect(visualPart3).toBeTruthy();
    expect(visualPart3!.listOutgoing.length).toBe(1);
  });

  it('getVisualPart', () => {
    const sut = new VisualizationUility(undefined, undefined);
    sut.setDescriptionGraph(testData.descriptionGraph);
    expect(sut.getVisualPart(null)).toBeUndefined();
    expect(sut.getVisualPart("fee9bd93-d956-45cd-887a-b15003945e52")).toBeTruthy();
  });

  //

  it('addListLogRecord', () => {
    const testData = JSON.parse(json) as {
      descriptionGraph: BCDescriptionGraph,
      listLogRecord: BCLogRecord[],
    };
    const sut = new VisualizationUility(undefined, undefined);
    sut.setDescriptionGraph(testData.descriptionGraph);
    sut.addListLogRecord(testData.listLogRecord)
  });

});
