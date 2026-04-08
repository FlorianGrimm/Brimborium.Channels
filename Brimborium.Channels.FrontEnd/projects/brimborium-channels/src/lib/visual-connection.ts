import { VisualPart } from "./visual-part";

export class VisualConnection {
    constructor(
        public id:string,
        public source:VisualPart,
        public sink:VisualPart
    ){
    }
}
