import { IPoint } from "@foblex/2d";
import { BCDescriptionNode } from "./generated/Brimborium.Channels";
import { VisualConnection } from "./visual-connection";

export class VisualPart {
    public pos: IPoint = { x: 0, y: 0 };
    public parent: VisualPart | undefined;
    public listChild: VisualPart[] = [];
    public listIncoming: VisualPart[] = [];
    public listOutgoing: VisualPart[] = [];
    public listIncomingConnection: VisualConnection[] = [];
    public listOutgoingConnection: VisualConnection[] = [];

    constructor(
        public id: string,
        public DescriptionNode: BCDescriptionNode
    ) {
    }

    addChild(child: VisualPart) {
        if (child.parent == null) {
            this.listChild.push(child);
            child.parent = this;
        }
    }

    addIncoming(incoming: VisualPart): boolean {
        if (this.listIncoming.includes(incoming)) { return false; }
        this.listIncoming.push(incoming);
        incoming.listOutgoing.push(this);
        return true;
    }

    addOutgoing(outgoing: VisualPart): boolean {
        if (this.listOutgoing.includes(outgoing)) { return false; }
        this.listOutgoing.push(outgoing);
        outgoing.listIncoming.push(this);
        return true;
    }
}
