﻿using System;
using System.Text.Json.Serialization;

namespace exchange.core.Enums
{
    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TimeInForce
    {
        [StringValue("GTC")]
        GoodTillCanceled,
        [StringValue("GTT")]
        GoodTillTime,
        [StringValue("IOC")]
        ImmediateOrCancel,
        [StringValue("FOK")]
        FillOrKill
    }
}
