import { Layout, EventType, Event } from './layout'

export interface LayoutAdaptorOptions {
    trigger?: (e: Event) => void;
    kick?: () => void;
    drag?: () => void;
    on?: (eventType: EventType | string, listener: () => void) => this;
}

export class LayoutAdaptor extends Layout {

    // dummy functions in case not defined by client
    override trigger(e: Event) { };
    override kick() { };
    drag() { };
    override on(eventType: EventType | string, listener: () => void): this { return this; };

    dragstart: (d: any) => void;
    dragStart: (d: any) => void;
    dragend: (d: any) => void;
    dragEnd: (d: any) => void;

    constructor(options:LayoutAdaptorOptions) {
        super();

        // take in implementation as defined by client

        let o = options;

        if (o.trigger) {
            this.trigger = o.trigger;
        }

        if (o.kick) {
            this.kick = o.kick;
        }

        if (o.drag) {
            this.drag = o.drag;
        }

        if (o.on) {
            this.on = o.on;
        }

        this.dragstart = this.dragStart = Layout.dragStart;
        this.dragend = this.dragEnd = Layout.dragEnd;
    }
}

/**
 * provides an interface for use with any external graph system (e.g. Cytoscape.js):
 */
export function adaptor(options:LayoutAdaptorOptions): LayoutAdaptor {
    return new LayoutAdaptor(options);
}
