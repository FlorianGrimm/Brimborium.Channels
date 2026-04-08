import { K } from "@angular/cdk/keycodes";
import { BCDescriptionGraph, BCDescriptionNode, BCLogRecord } from "./generated/Brimborium.Channels";
import { VisualPart } from "./visual-part";
import { VisualConnection } from "./visual-connection";

export class VisualizationUility {

    private descriptionGraph: BCDescriptionGraph | undefined;
    private listLogRecord: BCLogRecord[] = [];

    public mapVisualPart = new Map<string, VisualPart>();
    public mapVisualConnection = new Map<string, VisualConnection>();
    public listVisualPart: VisualPart[] = [];
    public listVisualConnection: VisualConnection[] = []

    constructor(
        descriptionGraph: BCDescriptionGraph | undefined,
        listLogRecord: BCLogRecord[] | undefined,
    ) {
        this.setDescriptionGraph(descriptionGraph);
        this.addListLogRecord(listLogRecord);
    }

    setDescriptionGraph(descriptionGraph: BCDescriptionGraph | undefined) {
        this.descriptionGraph = descriptionGraph;
        if (descriptionGraph == null) {
            this.mapVisualPart.clear();
            this.listLogRecord.splice(0, this.listLogRecord.length);
        } else {
            for (const key in descriptionGraph.nodes) {
                const node = descriptionGraph.nodes[key];
                if (node != null) {
                    const visualPart = new VisualPart(node.nodeId, node);
                    this.mapVisualPart.set(key, visualPart);
                    this.listVisualPart.push(visualPart);
                    const xy = this.listVisualPart.length * 100;
                    visualPart.pos = { x: xy, y: xy };
                }
            }
            for (const visualPart of this.mapVisualPart.values()) {
                const parentVisualPart = this.getVisualPart(visualPart.DescriptionNode.parent);
                if (parentVisualPart != null) {
                    parentVisualPart.addChild(visualPart);
                }

                // const listIncoming = visualPart.DescriptionNode.incoming;
                // if (listIncoming != null) {
                //     for (const incoming in listIncoming) {
                //         const incomingVisualPart = this.getVisualPart(incoming);
                //         if (incomingVisualPart != null) {
                //         }
                //     }
                // }

                const listOutgoing = visualPart.DescriptionNode.outgoing;
                if (listOutgoing != null) {
                    for (const outgoing of listOutgoing) {
                        const outgoingVisualPart = this.getVisualPart(outgoing.nodeId);
                        if (outgoingVisualPart != null) {
                            if (visualPart.addOutgoing(outgoingVisualPart)) {
                                const connection = new VisualConnection(
                                    `${visualPart.id}-${outgoingVisualPart.id}`,
                                    visualPart,
                                    outgoingVisualPart
                                );
                                this.listVisualConnection.push(connection);
                            }
                        }
                    }
                }
            }
        }
    }


    addListLogRecord(listLogRecord: BCLogRecord[] | undefined) {
        if (listLogRecord == undefined) { return; }
    }

    getVisualPart(key: string | null | undefined): (VisualPart | undefined) {
        if (key) {
            return this.mapVisualPart.get(key)
        } else {
            return undefined;
        }
    }
}
