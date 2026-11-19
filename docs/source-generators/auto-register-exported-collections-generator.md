---
title: "Generate collection registration"
description: "Register items from an enumerable collection with a host registry."
---

# Generate collection registration {#overview}

Apply `[AutoRegisterExportedCollections]` to the `partial` host type. Apply `[RegisterExportedCollection(...)]` to a readable collection member and specify an existing registry member and a compatible one-argument method. An Inspector-configured resource array is a typical input.

Call `__RegisterExportedCollections_Generated()` after the collection and registry are ready, for example in `_Ready()`. Build diagnostics identify non-enumerable members and incompatible registration methods.
