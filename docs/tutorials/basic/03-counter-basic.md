---
title: "Record order state"
description: "See how an ordinary C# service holds application data."
---

# Record order state {#overview}

`OrderService` owns the set of order IDs. `Register` adds an ID to that set.

<<< ../../../Samples/MinimalApp/OrderService.cs

Next, [define domain rules](../../tutorials/basic/04-model-refactor.md).
