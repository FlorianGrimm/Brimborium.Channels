export class PositionStats {
    AB: number = 0;
    AD: number = 0;
    A2: number = 0;

    constructor(public scale: number) { }

    addVariable(v: Variable): void {
        let ai = this.scale / v.scale;
        let bi = v.offset / v.scale;
        let wi = v.weight;
        this.AB += wi * ai * bi;
        this.AD += wi * ai * v.desiredPosition;
        this.A2 += wi * ai * ai;
    }

    getPosn(): number {
        return (this.AD - this.AB) / this.A2;
    }
}

export class Constraint {
    lm: number = -1;
    active: boolean = false;
    unsatisfiable: boolean = false;

    constructor(public left: Variable, public right: Variable, public gap: number, public equality: boolean = false) {
        this.left = left;
        this.right = right;
        this.gap = gap;
        this.equality = equality;
    }

    slack(): number {
        return this.unsatisfiable ? Number.MAX_VALUE
            : this.right.scale * this.right.position() - this.gap
            - this.left.scale * this.left.position();
    }
}

export class Variable {
    offset: number = 0;
    block: Block | undefined;
    cIn: Constraint[] = [];
    cOut: Constraint[] = [];

    constructor(public desiredPosition: number, public weight: number = 1, public scale: number = 1) { }

    dfdv(): number {
        return 2.0 * this.weight * (this.position() - this.desiredPosition);
    }

    position(): number {
        if (this.block) {
            return (this.block.ps.scale * this.block.posn + this.offset) / this.scale;
        } else {
            return this.offset / this.scale;
        }
    }

    // visit neighbours by active constraints within the same block
    visitNeighbours(prev: (Variable | null), f: (c: Constraint, next: Variable) => void): void {
        const ff = (c: Constraint, next: Variable) => c.active && prev !== next && f(c, next);
        if (this.cOut) {
            this.cOut.forEach(c => ff(c, c.right));
        }
        if (this.cIn) {
            this.cIn.forEach(c => ff(c, c.left));
        }
    }
}

export class Block {
    vars: Variable[] = [];
    posn: number = -1;
    ps: PositionStats;
    blockInd: number = -1;

    constructor(v: Variable) {
        v.offset = 0;
        this.ps = new PositionStats(v.scale);
        this.addVariable(v);
    }

    private addVariable(v: Variable): void {
        v.block = this;
        this.vars.push(v);
        this.ps.addVariable(v);
        this.posn = this.ps.getPosn();
    }

    // move the block where it needs to be to minimize cost
    updateWeightedPosition(): void {
        this.ps.AB = this.ps.AD = this.ps.A2 = 0;
        for (let i = 0, n = this.vars.length; i < n; ++i)
            this.ps.addVariable(this.vars[i]);
        this.posn = this.ps.getPosn();
    }

    private compute_lm(
        v: Variable,
        u: (Variable | null),
        postAction: (c: Constraint) => void): number {
        let dfdv = v.dfdv();
        v.visitNeighbours(u, (c, next) => {
            let _dfdv = this.compute_lm(next, v, postAction);
            if (next === c.right) {
                dfdv += _dfdv * c.left.scale;
                c.lm = _dfdv;
            } else {
                dfdv += _dfdv * c.right.scale;
                c.lm = -_dfdv;
            }
            postAction(c);
        });
        return dfdv / v.scale;
    }

    private populateSplitBlock(v: Variable, prev: Variable|null): void {
        v.visitNeighbours(prev, (c, next) => {
            next.offset = v.offset + (next === c.right ? c.gap : -c.gap);
            this.addVariable(next);
            this.populateSplitBlock(next, v);
        });
    }

    // traverse the active constraint tree applying visit to each active constraint
    traverse(visit: (c: Constraint) => any, acc: any[], v: Variable = this.vars[0], prev: (Variable | null) = null) {
        v.visitNeighbours(prev, (c, next) => {
            acc.push(visit(c));
            this.traverse(visit, acc, next, v);
        });
    }

    // calculate lagrangian multipliers on constraints and
    // find the active constraint in this block with the smallest lagrangian.
    // if the lagrangian is negative, then the constraint is a split candidate.
    findMinLM(): Constraint | null {
        let m: Constraint | null = null;
        this.compute_lm(this.vars[0], null, c => {
            if (!c.equality && (m === null || c.lm < m.lm)) m = c;
        });
        return m;
    }

    private findMinLMBetween(lv: Variable, rv: Variable): Constraint | null {
        this.compute_lm(lv, null, () => { });
        let m: Constraint | null = null;
        this.findPath(lv, null, rv, (c, next) => {
            if (!c.equality && c.right === next && (m === null || c.lm < m.lm)) m = c;
        });
        return m;
    }

    private findPath(v: Variable, prev: Variable | null, to: Variable, visit: (c: Constraint, next: Variable) => void): boolean {
        let endFound = false;
        v.visitNeighbours(prev, (c, next) => {
            if (!endFound && (next === to || this.findPath(next, v, to, visit))) {
                endFound = true;
                visit(c, next);
            }
        });
        return endFound;
    }

    // Search active constraint tree from u to see if there is a directed path to v.
    // Returns true if path is found.
    isActiveDirectedPathBetween(u: Variable, v: Variable): boolean {
        if (u === v) return true;
        let i = u.cOut?.length ?? 0;
        while (i--) {
            let c = u.cOut && u.cOut[i];
            if (c && c.active && this.isActiveDirectedPathBetween(c.right, v))
                return true;
        }
        return false;
    }

    // split the block into two by deactivating the specified constraint
    static split(c: Constraint): Block[] {
        /* DEBUG
                    console.log("split on " + c);
                    console.assert(c.active, "attempt to split on inactive constraint");
        DEBUG */
        c.active = false;
        return [Block.createSplitBlock(c.left), Block.createSplitBlock(c.right)];
    }

    private static createSplitBlock(startVar: Variable): Block {
        let b = new Block(startVar);
        b.populateSplitBlock(startVar, null);
        return b;
    }

    // find a split point somewhere between the specified variables
    splitBetween(vl: Variable, vr: Variable): { constraint: Constraint; lb: Block; rb: Block } | null {
        /* DEBUG
                    console.assert(vl.block === this);
                    console.assert(vr.block === this);
        DEBUG */
        let c = this.findMinLMBetween(vl, vr);
        if (c !== null) {
            let bs = Block.split(c);
            return { constraint: c, lb: bs[0], rb: bs[1] };
        }
        // couldn't find a split point - for example the active path is all equality constraints
        return null;
    }

    mergeAcross(b: Block, c: Constraint, dist: number): void {
        c.active = true;
        for (let i = 0, n = b.vars.length; i < n; ++i) {
            let v = b.vars[i];
            v.offset += dist;
            this.addVariable(v);
        }
        this.posn = this.ps.getPosn();
    }

    cost(): number {
        let sum = 0, i = this.vars.length;
        while (i--) {
            let v = this.vars[i],
                d = v.position() - v.desiredPosition;
            sum += d * d * v.weight;
        }
        return sum;
    }

    /* DEBUG
            toString(): string {
                let cs = [];
                this.traverse(c=> c.toString() + "\n", cs)
                return "b"+this.blockInd + "@" + this.posn + ": vars=" + this.vars.map(v=> v.toString()+":"+v.offset) + ";\n cons=\n" + cs;
            }
    DEBUG */
}

export class Blocks {
    private list: Block[];

    constructor(public vs: Variable[]) {
        let n = vs.length;
        this.list = new Array(n);
        while (n--) {
            let b = new Block(vs[n]);
            this.list[n] = b;
            b.blockInd = n;
        }
    }

    cost(): number {
        let sum = 0, i = this.list.length;
        while (i--) sum += this.list[i].cost();
        return sum;
    }

    insert(b: Block) {
        /* DEBUG
                    console.assert(!this.contains(b), "blocks error: tried to reinsert block " + b.blockInd)
        DEBUG */
        b.blockInd = this.list.length;
        this.list.push(b);
        /* DEBUG
                    console.log("insert block: " + b.blockInd);
                    this.contains(b);
        DEBUG */
    }

    remove(b: Block) {
        /* DEBUG
                    console.log("remove block: " + b.blockInd);
                    console.assert(this.contains(b));
        DEBUG */
        let last = this.list.length - 1;
        let swapBlock = this.list[last];
        this.list.length = last;
        if (b !== swapBlock) {
            this.list[b.blockInd] = swapBlock;
            swapBlock.blockInd = b.blockInd;
            /* DEBUG
                            console.assert(this.contains(swapBlock));
            DEBUG */
        }
    }

    // merge the blocks on either side of the specified constraint, by copying the smaller block into the larger
    // and deleting the smaller.
    merge(c: Constraint): void {
        let l = c.left.block, r = c.right.block;
        /* DEBUG
                    console.assert(l!==r, "attempt to merge within the same block");
        DEBUG */
        if (l == null || r == null) {

        } else {
            let dist = c.right.offset - c.left.offset - c.gap;
            if (l.vars.length < r.vars.length) {
                r.mergeAcross(l, c, dist);
                this.remove(l);
            } else {
                l.mergeAcross(r, c, -dist);
                this.remove(r);
            }
        }
        /* DEBUG
                    console.assert(Math.abs(c.slack()) < 1e-6, "Error: Constraint should be at equality after merge!");
                    console.log("merged on " + c);
        DEBUG */
    }

    forEach(f: (b: Block, i: number) => void) {
        this.list.forEach(f);
    }

    // useful, for example, after variable desired positions change.
    updateBlockPositions(): void {
        this.list.forEach(b => b.updateWeightedPosition());
    }

    // split each block across its constraint with the minimum lagrangian
    split(inactive: Constraint[]): void {
        this.updateBlockPositions();
        this.list.forEach(b => {
            let v = b.findMinLM();
            if (v !== null && v.lm < Solver.LAGRANGIAN_TOLERANCE) {
                let block = v.left.block;
                if (block) {
                    Block.split(v).forEach(nb => this.insert(nb));
                    this.remove(block);
                }
                inactive.push(v);
                /* DEBUG
                                    console.assert(this.contains(v.left.block));
                                    console.assert(this.contains(v.right.block));
                DEBUG */
            }
        });
    }

    /* DEBUG
            // checks b is in the block, and does a sanity check over list index integrity
            contains(b: Block): boolean {
                let result = false;
                this.list.forEach((bb, i) => {
                    if (bb.blockInd !== i) {
                        console.error("blocks error, blockInd " + b.blockInd + " found at " + i);
                        return false;
                    }
                    result = result || b === bb;
                });
                return result;
            }
    
            toString(): string {
                return this.list.toString();
            }
    DEBUG */
}

export class Solver {
    bs: Blocks|null;
    inactive: Constraint[];

    static LAGRANGIAN_TOLERANCE = -1e-4;
    static ZERO_UPPERBOUND = -1e-10;

    constructor(public vs: Variable[], public cs: Constraint[]) {
        this.vs = vs;
        vs.forEach(v => {
            v.cIn = [], v.cOut = [];
            /* DEBUG
                            v.toString = () => "v" + vs.indexOf(v);
            DEBUG */
        });
        this.cs = cs;
        cs.forEach(c => {
            c.left.cOut.push(c);
            c.right.cIn.push(c);
            /* DEBUG
                            c.toString = () => c.left + "+" + c.gap + "<=" + c.right + " slack=" + c.slack() + " active=" + c.active;
            DEBUG */
        });
        this.inactive = cs.map(c => { c.active = false; return c; });
        this.bs = null;
    }

    cost(): number {
        return this.bs?.cost() ?? 0;
    }

    // set starting positions without changing desired positions.
    // Note: it throws away any previous block structure.
    setStartingPositions(ps: number[]): void {
        this.inactive = this.cs.map(c => { c.active = false; return c; });
        this.bs = new Blocks(this.vs);
        this.bs.forEach((b, i) => b.posn = ps[i]);
    }

    setDesiredPositions(ps: number[]): void {
        this.vs.forEach((v, i) => v.desiredPosition = ps[i]);
    }

    /* DEBUG
            private getId(v: Variable): number {
                return this.vs.indexOf(v);
            }
    
            // sanity check of the index integrity of the inactive list
            checkInactive(): void {
                let inactiveCount = 0;
                this.cs.forEach(c=> {
                    let i = this.inactive.indexOf(c);
                    console.assert(!c.active && i >= 0 || c.active && i < 0, "constraint should be in the inactive list if it is not active: " + c);
                    if (i >= 0) {
                        inactiveCount++;
                    } else {
                        console.assert(c.active, "inactive constraint not found in inactive list: " + c);
                    }
                });
                console.assert(inactiveCount === this.inactive.length, inactiveCount + " inactive constraints found, " + this.inactive.length + "in inactive list");
            }
            // after every call to satisfy the following should check should pass
            checkSatisfied(): void {
                this.cs.forEach(c=>console.assert(c.slack() >= vpsc.Solver.ZERO_UPPERBOUND, "Error: Unsatisfied constraint! "+c));
            }
    DEBUG */

    private mostViolated(): Constraint | null {
        let minSlack = Number.MAX_VALUE,
            v: Constraint | null = null,
            l = this.inactive,
            n = l.length,
            deletePoint = n;
        for (let i = 0; i < n; ++i) {
            let c = l[i];
            if (c.unsatisfiable) continue;
            let slack = c.slack();
            if (c.equality || slack < minSlack) {
                minSlack = slack;
                v = c;
                deletePoint = i;
                if (c.equality) break;
            }
        }
        if (deletePoint !== n &&
            v &&
            (minSlack < Solver.ZERO_UPPERBOUND && !v.active || v.equality)) {
            l[deletePoint] = l[n - 1];
            l.length = n - 1;
        }
        return v;
    }

    // satisfy constraints by building block structure over violated constraints
    // and moving the blocks to their desired positions
    satisfy(): void {
        if (this.bs == null) {
            this.bs = new Blocks(this.vs);
        }
        /* DEBUG
                    console.log("satisfy: " + this.bs);
        DEBUG */
        this.bs.split(this.inactive);
        let v: Constraint|null = null;
        while ((v = this.mostViolated()) && (v.equality || v.slack() < Solver.ZERO_UPPERBOUND && !v.active)) {
            let lb = v.left.block, rb = v.right.block;
            /* DEBUG
                            console.log("most violated is: " + v);
                            this.bs.contains(lb);
                            this.bs.contains(rb);
            DEBUG */
            if (lb == null || rb == null) {
                //
            } else if (lb !== rb) {
                this.bs.merge(v);
            } else {
                if (lb.isActiveDirectedPathBetween(v.right, v.left)) {
                    // cycle found!
                    v.unsatisfiable = true;
                    continue;
                }
                // constraint is within block, need to split first
                let split = lb.splitBetween(v.left, v.right);
                if (split !== null) {
                    this.bs.insert(split.lb);
                    this.bs.insert(split.rb);
                    this.bs.remove(lb);
                    this.inactive.push(split.constraint);
                } else {
                    /* DEBUG
                                            console.log("unsatisfiable constraint found");
                    DEBUG */
                    v.unsatisfiable = true;
                    continue;
                }
                if (v.slack() >= 0) {
                    /* DEBUG
                                            console.log("violated constraint indirectly satisfied: " + v);
                    DEBUG */
                    // v was satisfied by the above split!
                    this.inactive.push(v);
                } else {
                    /* DEBUG
                                            console.log("merge after split:");
                    DEBUG */
                    this.bs.merge(v);
                }
            }
            /* DEBUG
                            this.bs.contains(v.left.block);
                            this.bs.contains(v.right.block);
                            this.checkInactive();
            DEBUG */
        }
        /* DEBUG
                    this.checkSatisfied();
        DEBUG */
    }

    // repeatedly build and split block structure until we converge to an optimal solution
    solve(): number {
        this.satisfy();
        let lastcost = Number.MAX_VALUE, cost = this.bs?.cost() ?? 0;
        while (Math.abs(lastcost - cost) > 0.0001) {
            this.satisfy();
            lastcost = cost;
            cost = this.bs?.cost() ?? lastcost;
        }
        return cost;
    }
}
/**
  * Remove overlap between spans while keeping their centers as close as possible to the specified desiredCenters.
  * Lower and upper bounds will be respected if the spans physically fit between them
  * (otherwise they'll be moved and their new position returned).
  * If no upper/lower bound is specified then the bounds of the moved spans will be returned.
  * returns a new center for each span.
  */
export function removeOverlapInOneDimension(spans: { size: number, desiredCenter: number }[], lowerBound?: number, upperBound?: number)
    : { newCenters: number[], lowerBound: number, upperBound: number } {
    const vs: Variable[] = spans.map(s => new Variable(s.desiredCenter));
    const cs: Constraint[] = [];
    const n = spans.length;
    for (let i = 0; i < n - 1; i++) {
        const left = spans[i], right = spans[i + 1];
        cs.push(new Constraint(vs[i], vs[i + 1], (left.size + right.size) / 2));
    }
    const leftMost = vs[0],
        rightMost = vs[n - 1],
        leftMostSize = spans[0].size / 2,
        rightMostSize = spans[n - 1].size / 2;
    let vLower: Variable|null = null, vUpper: Variable|null = null;
    if (lowerBound) {
        vLower = new Variable(lowerBound, leftMost.weight * 1000);
        vs.push(vLower);
        cs.push(new Constraint(vLower, leftMost, leftMostSize));
    }
    if (upperBound) {
        vUpper = new Variable(upperBound, rightMost.weight * 1000);
        vs.push(vUpper);
        cs.push(new Constraint(rightMost, vUpper, rightMostSize));
    }
    let solver = new Solver(vs, cs);
    solver.solve();
    return {
        newCenters: vs.slice(0, spans.length).map(v => v.position()),
        lowerBound: vLower ? vLower.position() : leftMost.position() - leftMostSize,
        upperBound: vUpper ? vUpper.position() : rightMost.position() + rightMostSize
    };
}
