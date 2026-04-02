#pragma warning disable IDE1006 // Naming Styles

namespace Brimborium.Channels;

/// <summary>
/// TODO
/// </summary>
public class BCBlock : IBCBlock, IBCMonitored {
    private BCLifeTime _LifeTime;
    protected List<IBCConsumer> ListIncoming = new();
    protected List<IBCProducer> ListOutgoing = new();
    private BCMonitor? _Monitor;

    protected BCBlock(
        BCDescription? description
    ) {
        this.Description = description ?? new();
    }

    public BCLifeTime LifeTime => this._LifeTime;
    public BCDescription Description { get; set; }


    protected bool IsIncomingCompleted() {
        foreach (var incoming in this.ListIncoming) {
            if (incoming.LifeTime is BCLifeTime.Completed) {
                continue;
            } else {
                return false;
            }
        }
        return true;
    }

    public async Task WaitCompletedAsync(CancellationToken cancellationToken) {
        foreach (var outgoing in this.ListOutgoing) {
            await outgoing.WaitCompletedAsync(cancellationToken);
        }
    }

    internal bool SetCompleting() {
        return BCLifeTimeExtension.SetCompleting(ref this._LifeTime);
    }

    internal bool SetCompleted() {
        return BCLifeTimeExtension.SetCompleted(ref this._LifeTime);
    }

    BCMonitor? IBCMonitored.GetMonitor() => this._Monitor;
    public virtual bool SetMonitor(BCMonitor monitor) {
        if (this._Monitor is { }) { return false; }
        this._Monitor = monitor;
        foreach (var incoming in this.ListIncoming) { 
            monitor.Add(incoming);
        }
        foreach (var outgoing in this.ListOutgoing) {
            monitor.Add(outgoing);
        }
        return true;
    }

    protected void AddIncoming(IBCConsumer consumer) {
        this._Monitor?.Add(consumer);
    }
    protected void AddOutgoing(IBCProducer producer) {
        this._Monitor?.Add(producer);
    }

}


/// <summary>
/// TODO
/// </summary>
public class BCBlockI1O1<TIn1, TOut1> : BCBlock {
    public static BCBlockI1O1<TIn1, TOut1> Create(
        BCBlockI1O1Description? description,
        Func<BCOutgoingProducer<TOut1>, IBCConsumer<TIn1>> consumer1
    ) {
        BCOutgoingProducer<TOut1> outgoingProducer1 = new(description?.Out1 ?? new("Out1"));
        return new BCBlockI1O1<TIn1, TOut1>(
            description,
            consumer1(outgoingProducer1),
            outgoingProducer1
            );
    }
    public readonly BCIncomingConsumer<TIn1> IncomingConsumer1;
    public readonly BCOutgoingProducer<TOut1> OutgoingProducer1;
    public BCBlockI1O1(
        BCBlockI1O1Description? description,
        IBCConsumer<TIn1> consumer1,
        BCOutgoingProducer<TOut1> outgoingProducer1
    ) : base(
        description?.Description ?? new BCDescription(typeof(BCBlockI1O1<TIn1, TOut1>).FullName ?? string.Empty)
    ) {
        this.AddIncoming(this.IncomingConsumer1 = new(description?.In1 ?? new("in1"), consumer1, this));
        this.AddOutgoing(this.OutgoingProducer1 = outgoingProducer1);
    }
}

public record BCBlockI1O1Description(
    BCDescription? Description = default,
    BCDescription? In1 = default,
    BCDescription? Out1 = default
);


public class BCBlockI2O1<TIn1, TIn2, TOut1> : BCBlock {
    public static BCBlockI2O1<TIn1, TIn2, TOut1> Create(
        BCBlockI2O1Description? description,
        Func<BCOutgoingProducer<TOut1>, IBCConsumer<TIn1>> consumer1,
        Func<BCOutgoingProducer<TOut1>, IBCConsumer<TIn2>> consumer2
    ) {
        BCOutgoingProducer<TOut1> outgoingProducer1 = new(description?.Out1 ?? new("Out1"));
        return new BCBlockI2O1<TIn1, TIn2, TOut1>(
            description,
            consumer1(outgoingProducer1),
            consumer2(outgoingProducer1),
            outgoingProducer1
            );
    }
    public readonly BCIncomingConsumer<TIn1> IncomingConsumer1;
    public readonly BCIncomingConsumer<TIn2> IncomingConsumer2;
    public readonly BCOutgoingProducer<TOut1> OutgoingProducer1;
    public BCBlockI2O1(
        BCBlockI2O1Description? description,
        IBCConsumer<TIn1> consumer1,
        IBCConsumer<TIn2> consumer2,
        BCOutgoingProducer<TOut1> outgoingProducer1
    ) : base(
        description?.Description ?? new BCDescription(typeof(BCBlockI1O1<TIn1, TOut1>).FullName ?? string.Empty)
    ) {
        this.AddIncoming(this.IncomingConsumer1 = new(description?.In1 ?? new("in1"), consumer1, this));
        this.AddIncoming(this.IncomingConsumer2 = new(description?.In2 ?? new("in2"), consumer2, this));
        this.AddOutgoing(this.OutgoingProducer1 = outgoingProducer1);
    }
}

public record BCBlockI2O1Description(
    BCDescription? Description = default,
    BCDescription? In1 = default,
    BCDescription? In2 = default,
    BCDescription? Out1 = default
);

public class BCBlockI3O1<TIn1, TIn2, TIn3, TOut1> : BCBlock {
    public static BCBlockI3O1<TIn1, TIn2, TIn3, TOut1> Create(
        BCBlockI3O1Description? description,
        Func<BCOutgoingProducer<TOut1>, IBCConsumer<TIn1>> consumer1,
        Func<BCOutgoingProducer<TOut1>, IBCConsumer<TIn2>> consumer2,
        Func<BCOutgoingProducer<TOut1>, IBCConsumer<TIn3>> consumer3
    ) {
        BCOutgoingProducer<TOut1> outgoingProducer1 = new(description?.Out1 ?? new("Out1"));
        return new BCBlockI3O1<TIn1, TIn2, TIn3, TOut1>(
            description,
            consumer1(outgoingProducer1),
            consumer2(outgoingProducer1),
            consumer3(outgoingProducer1),
            outgoingProducer1
            );
    }
    public readonly BCIncomingConsumer<TIn1> IncomingConsumer1;
    public readonly BCIncomingConsumer<TIn2> IncomingConsumer2;
    public readonly BCIncomingConsumer<TIn3> IncomingConsumer3;
    public readonly BCOutgoingProducer<TOut1> OutgoingProducer1;
    public BCBlockI3O1(
        BCBlockI3O1Description? description,
        IBCConsumer<TIn1> consumer1,
        IBCConsumer<TIn2> consumer2,
        IBCConsumer<TIn3> consumer3,
        BCOutgoingProducer<TOut1> outgoingProducer1
    ) : base(
        description?.Description ?? new BCDescription(typeof(BCBlockI1O1<TIn1, TOut1>).FullName ?? string.Empty)
    ) {
        this.AddIncoming(this.IncomingConsumer1 = new(description?.In1 ?? new("in1"), consumer1, this));
        this.AddIncoming(this.IncomingConsumer2 = new(description?.In2 ?? new("in2"), consumer2, this));
        this.AddIncoming(this.IncomingConsumer3 = new(description?.In3 ?? new("in3"), consumer3, this));
        this.AddOutgoing(this.OutgoingProducer1 = outgoingProducer1);
    }
}

public record BCBlockI3O1Description(
    BCDescription? Description = default,
    BCDescription? In1 = default,
    BCDescription? In2 = default,
    BCDescription? In3 = default,
    BCDescription? Out1 = default
);

public class BCBlockI1O2<TIn1, TOut1, TOut2> : BCBlock {
    public static BCBlockI1O2<TIn1,  TOut1, TOut2> Create(
        BCBlockI1O2Description? description,
        Func<BCOutgoingProducer<TOut1>, BCOutgoingProducer<TOut2>, IBCConsumer<TIn1>> consumer1
    ) {
        BCOutgoingProducer<TOut1> outgoingProducer1 = new(description?.Out1 ?? new("Out1"));
        BCOutgoingProducer<TOut2> outgoingProducer2 = new(description?.Out2 ?? new("Out2"));
        return new BCBlockI1O2<TIn1,  TOut1, TOut2>(
            description,
            consumer1(outgoingProducer1, outgoingProducer2),
            outgoingProducer1,
            outgoingProducer2
            );
    }
    public readonly BCIncomingConsumer<TIn1> IncomingConsumer1;
    public readonly BCOutgoingProducer<TOut1> OutgoingProducer1;
    public readonly BCOutgoingProducer<TOut2> OutgoingProducer2;
    public BCBlockI1O2(
        BCBlockI1O2Description? description,
        IBCConsumer<TIn1> consumer1,
        BCOutgoingProducer<TOut1> outgoingProducer1,
        BCOutgoingProducer<TOut2> outgoingProducer2
    ) : base(
        description?.Description ?? new BCDescription(typeof(BCBlockI1O1<TIn1, TOut1>).FullName ?? string.Empty)
    ) {
        this.AddIncoming(this.IncomingConsumer1 = new(description?.In1 ?? new("in1"), consumer1, this));
        this.AddOutgoing(this.OutgoingProducer1 = outgoingProducer1);
        this.AddOutgoing(this.OutgoingProducer2 = outgoingProducer2);
    }
}

public record BCBlockI1O2Description(
    BCDescription? Description = default,
    BCDescription? In1 = default,
    BCDescription? Out1 = default,
    BCDescription? Out2 = default
);




public class BCBlockI2O2<TIn1, TIn2, TOut1, TOut2> : BCBlock {
    public static BCBlockI2O2<TIn1, TIn2,  TOut1, TOut2> Create(
        BCBlockI2O2Description? description,
        Func<BCOutgoingProducer<TOut1>, BCOutgoingProducer<TOut2>, IBCConsumer<TIn1>> consumer1,
        Func<BCOutgoingProducer<TOut1>, BCOutgoingProducer<TOut2>, IBCConsumer<TIn2>> consumer2
    ) {
        BCOutgoingProducer<TOut1> outgoingProducer1 = new(description?.Out1 ?? new("Out1"));
        BCOutgoingProducer<TOut2> outgoingProducer2 = new(description?.Out2 ?? new("Out2"));
        return new BCBlockI2O2<TIn1, TIn2,  TOut1, TOut2>(
            description,
            consumer1(outgoingProducer1, outgoingProducer2),
            consumer2(outgoingProducer1, outgoingProducer2),
            outgoingProducer1,
            outgoingProducer2
            );
    }
    public readonly BCIncomingConsumer<TIn1> IncomingConsumer1;
    public readonly BCIncomingConsumer<TIn2> IncomingConsumer2;
    public readonly BCOutgoingProducer<TOut1> OutgoingProducer1;
    public readonly BCOutgoingProducer<TOut2> OutgoingProducer2;
    public BCBlockI2O2(
        BCBlockI2O2Description? description,
        IBCConsumer<TIn1> consumer1,
        IBCConsumer<TIn2> consumer2,
        BCOutgoingProducer<TOut1> outgoingProducer1,
        BCOutgoingProducer<TOut2> outgoingProducer2
    ) : base(
        description?.Description ?? new BCDescription(typeof(BCBlockI1O1<TIn1, TOut1>).FullName ?? string.Empty)
    ) {
        this.AddIncoming(this.IncomingConsumer1 = new(description?.In1 ?? new("in1"), consumer1, this));
        this.AddIncoming(this.IncomingConsumer2 = new(description?.In2 ?? new("in2"), consumer2, this));
        this.AddOutgoing(this.OutgoingProducer1 = outgoingProducer1);
        this.AddOutgoing(this.OutgoingProducer2 = outgoingProducer2);
    }
}

public record BCBlockI2O2Description(
    BCDescription? Description = default,
    BCDescription? In1 = default,
    BCDescription? In2 = default,
    BCDescription? Out1 = default,
    BCDescription? Out2 = default
);

public class BCBlockI3O2<TIn1, TIn2, TIn3, TOut1, TOut2> : BCBlock {
    public static BCBlockI3O2<TIn1, TIn2, TIn3, TOut1, TOut2> Create(
        BCBlockI3O2Description? description,
        Func<BCOutgoingProducer<TOut1>, BCOutgoingProducer<TOut2>, IBCConsumer<TIn1>> consumer1,
        Func<BCOutgoingProducer<TOut1>, BCOutgoingProducer<TOut2>, IBCConsumer<TIn2>> consumer2,
        Func<BCOutgoingProducer<TOut1>, BCOutgoingProducer<TOut2>, IBCConsumer<TIn3>> consumer3
    ) {
        BCOutgoingProducer<TOut1> outgoingProducer1 = new(description?.Out1 ?? new("Out1"));
        BCOutgoingProducer<TOut2> outgoingProducer2 = new(description?.Out2 ?? new("Out2"));
        return new BCBlockI3O2<TIn1, TIn2, TIn3, TOut1, TOut2>(
            description,
            consumer1(outgoingProducer1, outgoingProducer2),
            consumer2(outgoingProducer1, outgoingProducer2),
            consumer3(outgoingProducer1, outgoingProducer2),
            outgoingProducer1,
            outgoingProducer2
            );
    }
    public readonly BCIncomingConsumer<TIn1> IncomingConsumer1;
    public readonly BCIncomingConsumer<TIn2> IncomingConsumer2;
    public readonly BCIncomingConsumer<TIn3> IncomingConsumer3;
    public readonly BCOutgoingProducer<TOut1> OutgoingProducer1;
    public readonly BCOutgoingProducer<TOut2> OutgoingProducer2;
    public BCBlockI3O2(
        BCBlockI3O2Description? description,
        IBCConsumer<TIn1> consumer1,
        IBCConsumer<TIn2> consumer2,
        IBCConsumer<TIn3> consumer3,
        BCOutgoingProducer<TOut1> outgoingProducer1,
        BCOutgoingProducer<TOut2> outgoingProducer2
    ) : base(
        description?.Description ?? new BCDescription(typeof(BCBlockI1O1<TIn1, TOut1>).FullName ?? string.Empty)
    ) {
        this.AddIncoming(this.IncomingConsumer1 = new(description?.In1 ?? new("in1"), consumer1, this));
        this.AddIncoming(this.IncomingConsumer2 = new(description?.In2 ?? new("in2"), consumer2, this));
        this.AddIncoming(this.IncomingConsumer3 = new(description?.In3 ?? new("in3"), consumer3, this));
        this.AddOutgoing(this.OutgoingProducer1 = outgoingProducer1);
        this.AddOutgoing(this.OutgoingProducer2 = outgoingProducer2);
    }
}

public record BCBlockI3O2Description(
    BCDescription? Description = default,
    BCDescription? In1 = default,
    BCDescription? In2 = default,
    BCDescription? In3 = default,
    BCDescription? Out1 = default,
    BCDescription? Out2 = default
);




public class BCBlockI1O3<TIn1, TOut1, TOut2, TOut3> : BCBlock {
    public static BCBlockI1O3<TIn1,  TOut1, TOut2, TOut3> Create(
        BCBlockI1O3Description? description,
        Func<BCOutgoingProducer<TOut1>, BCOutgoingProducer<TOut2>, BCOutgoingProducer<TOut3>, IBCConsumer<TIn1>> consumer1
    ) {
        BCOutgoingProducer<TOut1> outgoingProducer1 = new(description?.Out1 ?? new("Out1"));
        BCOutgoingProducer<TOut2> outgoingProducer2 = new(description?.Out2 ?? new("Out2"));
        BCOutgoingProducer<TOut3> outgoingProducer3 = new(description?.Out3 ?? new("Out3"));
        return new BCBlockI1O3<TIn1,  TOut1, TOut2, TOut3>(
            description,
            consumer1(outgoingProducer1, outgoingProducer2, outgoingProducer3),
            outgoingProducer1,
            outgoingProducer2,
            outgoingProducer3
            );
    }
    public readonly BCIncomingConsumer<TIn1> IncomingConsumer1;
    public readonly BCOutgoingProducer<TOut1> OutgoingProducer1;
    public readonly BCOutgoingProducer<TOut2> OutgoingProducer2;
    public readonly BCOutgoingProducer<TOut3> OutgoingProducer3;
    public BCBlockI1O3(
        BCBlockI1O3Description? description,
        IBCConsumer<TIn1> consumer1,
        BCOutgoingProducer<TOut1> outgoingProducer1,
        BCOutgoingProducer<TOut2> outgoingProducer2,
        BCOutgoingProducer<TOut3> outgoingProducer3
    ) : base(
        description?.Description ?? new BCDescription(typeof(BCBlockI1O1<TIn1, TOut1>).FullName ?? string.Empty)
    ) {
        this.AddIncoming(this.IncomingConsumer1 = new(description?.In1 ?? new("in1"), consumer1, this));
        this.AddOutgoing(this.OutgoingProducer1 = outgoingProducer1);
        this.AddOutgoing(this.OutgoingProducer2 = outgoingProducer2);
        this.AddOutgoing(this.OutgoingProducer3 = outgoingProducer3);
    }
}

public record BCBlockI1O3Description(
    BCDescription? Description = default,
    BCDescription? In1 = default,
    BCDescription? Out1 = default,
    BCDescription? Out2 = default,
    BCDescription? Out3 = default
);




public class BCBlockI2O3<TIn1, TIn2, TOut1, TOut2, TOut3> : BCBlock {
    public static BCBlockI2O3<TIn1, TIn2,  TOut1, TOut2, TOut3> Create(
        BCBlockI2O3Description? description,
        Func<BCOutgoingProducer<TOut1>, BCOutgoingProducer<TOut2>, BCOutgoingProducer<TOut3>, IBCConsumer<TIn1>> consumer1,
        Func<BCOutgoingProducer<TOut1>, BCOutgoingProducer<TOut2>, BCOutgoingProducer<TOut3>, IBCConsumer<TIn2>> consumer2
    ) {
        BCOutgoingProducer<TOut1> outgoingProducer1 = new(description?.Out1 ?? new("Out1"));
        BCOutgoingProducer<TOut2> outgoingProducer2 = new(description?.Out2 ?? new("Out2"));
        BCOutgoingProducer<TOut3> outgoingProducer3 = new(description?.Out3 ?? new("Out3"));
        return new BCBlockI2O3<TIn1, TIn2,  TOut1, TOut2, TOut3>(
            description,
            consumer1(outgoingProducer1, outgoingProducer2, outgoingProducer3),
            consumer2(outgoingProducer1, outgoingProducer2, outgoingProducer3),
            outgoingProducer1,
            outgoingProducer2,
            outgoingProducer3
            );
    }
    public readonly BCIncomingConsumer<TIn1> IncomingConsumer1;
    public readonly BCIncomingConsumer<TIn2> IncomingConsumer2;
    public readonly BCOutgoingProducer<TOut1> OutgoingProducer1;
    public readonly BCOutgoingProducer<TOut2> OutgoingProducer2;
    public readonly BCOutgoingProducer<TOut3> OutgoingProducer3;
    public BCBlockI2O3(
        BCBlockI2O3Description? description,
        IBCConsumer<TIn1> consumer1,
        IBCConsumer<TIn2> consumer2,
        BCOutgoingProducer<TOut1> outgoingProducer1,
        BCOutgoingProducer<TOut2> outgoingProducer2,
        BCOutgoingProducer<TOut3> outgoingProducer3
    ) : base(
        description?.Description ?? new BCDescription(typeof(BCBlockI1O1<TIn1, TOut1>).FullName ?? string.Empty)
    ) {
        this.AddIncoming(this.IncomingConsumer1 = new(description?.In1 ?? new("in1"), consumer1, this));
        this.AddIncoming(this.IncomingConsumer2 = new(description?.In2 ?? new("in2"), consumer2, this));
        this.AddOutgoing(this.OutgoingProducer1 = outgoingProducer1);
        this.AddOutgoing(this.OutgoingProducer2 = outgoingProducer2);
        this.AddOutgoing(this.OutgoingProducer3 = outgoingProducer3);
    }
}

public record BCBlockI2O3Description(
    BCDescription? Description = default,
    BCDescription? In1 = default,
    BCDescription? In2 = default,
    BCDescription? Out1 = default,
    BCDescription? Out2 = default,
    BCDescription? Out3 = default
);

public class BCBlockI3O3<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3> : BCBlock {
    public static BCBlockI3O3<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3> Create(
        BCBlockI3O3Description? description,
        Func<BCOutgoingProducer<TOut1>, BCOutgoingProducer<TOut2>, BCOutgoingProducer<TOut3>, IBCConsumer<TIn1>> consumer1,
        Func<BCOutgoingProducer<TOut1>, BCOutgoingProducer<TOut2>, BCOutgoingProducer<TOut3>, IBCConsumer<TIn2>> consumer2,
        Func<BCOutgoingProducer<TOut1>, BCOutgoingProducer<TOut2>, BCOutgoingProducer<TOut3>, IBCConsumer<TIn3>> consumer3
    ) {
        BCOutgoingProducer<TOut1> outgoingProducer1 = new(description?.Out1 ?? new("Out1"));
        BCOutgoingProducer<TOut2> outgoingProducer2 = new(description?.Out2 ?? new("Out2"));
        BCOutgoingProducer<TOut3> outgoingProducer3 = new(description?.Out3 ?? new("Out3"));
        return new BCBlockI3O3<TIn1, TIn2, TIn3, TOut1, TOut2, TOut3>(
            description,
            consumer1(outgoingProducer1, outgoingProducer2, outgoingProducer3),
            consumer2(outgoingProducer1, outgoingProducer2, outgoingProducer3),
            consumer3(outgoingProducer1, outgoingProducer2, outgoingProducer3),
            outgoingProducer1,
            outgoingProducer2,
            outgoingProducer3
            );
    }
    public readonly BCIncomingConsumer<TIn1> IncomingConsumer1;
    public readonly BCIncomingConsumer<TIn2> IncomingConsumer2;
    public readonly BCIncomingConsumer<TIn3> IncomingConsumer3;
    public readonly BCOutgoingProducer<TOut1> OutgoingProducer1;
    public readonly BCOutgoingProducer<TOut2> OutgoingProducer2;
    public readonly BCOutgoingProducer<TOut3> OutgoingProducer3;
    public BCBlockI3O3(
        BCBlockI3O3Description? description,
        IBCConsumer<TIn1> consumer1,
        IBCConsumer<TIn2> consumer2,
        IBCConsumer<TIn3> consumer3,
        BCOutgoingProducer<TOut1> outgoingProducer1,
        BCOutgoingProducer<TOut2> outgoingProducer2,
        BCOutgoingProducer<TOut3> outgoingProducer3
    ) : base(
        description?.Description ?? new BCDescription(typeof(BCBlockI1O1<TIn1, TOut1>).FullName ?? string.Empty)
    ) {
        this.AddIncoming(this.IncomingConsumer1 = new(description?.In1 ?? new("in1"), consumer1, this));
        this.AddIncoming(this.IncomingConsumer2 = new(description?.In2 ?? new("in2"), consumer2, this));
        this.AddIncoming(this.IncomingConsumer3 = new(description?.In3 ?? new("in3"), consumer3, this));
        this.AddOutgoing(this.OutgoingProducer1 = outgoingProducer1);
        this.AddOutgoing(this.OutgoingProducer2 = outgoingProducer2);
        this.AddOutgoing(this.OutgoingProducer3 = outgoingProducer3);
    }
}

public record BCBlockI3O3Description(
    BCDescription? Description = default,
    BCDescription? In1 = default,
    BCDescription? In2 = default,
    BCDescription? In3 = default,
    BCDescription? Out1 = default,
    BCDescription? Out2 = default,
    BCDescription? Out3 = default
);