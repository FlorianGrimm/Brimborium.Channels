import { BCDescriptionNode } from './generated/Brimborium.Channels';
import { VisualPart } from './visual-part';

const DescriptionNode1: BCDescriptionNode = {
  "kind": "Operation",  "name": "sinkAvg",  "parent": "",  "incoming": [],  "outgoing": undefined,  "nodeId": "fee9bd93-d956-45cd-887a-b15003945e52"
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

describe('VisualPart', () => {
  it('should create an instance', () => {
    expect(new VisualPart(DescriptionNode1)).toBeTruthy();
  });

  it('addChild', () => {
    const sut = new VisualPart(DescriptionNode1);
    const child = new VisualPart(DescriptionNode2);
    
    sut.addChild(child);

    expect(sut.parent).toBeUndefined();
    expect(child.parent).toBeTruthy();
    expect(sut.listChild.length).toBe(1);
    expect(child.listChild.length).toBe(0);
  });

  it('addOutgoing', () => {
    const sut = new VisualPart(DescriptionNode3);
    const outgoing = new VisualPart(DescriptionNode4);
    
    sut.addOutgoing(outgoing);

    expect(sut.parent).toBeUndefined();
    expect(outgoing.parent).toBeUndefined();
    expect(sut.listOutgoing.length).toBe(1);
    expect(outgoing.listIncoming.length).toBe(1);
  });

});
