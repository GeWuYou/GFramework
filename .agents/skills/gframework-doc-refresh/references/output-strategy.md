# Output Strategy

The module scan determines the document type.

Use this priority:

1. fix README / landing page / adoption path
2. fix stale topic pages
3. add or refresh API reference coverage
4. add or refresh tutorials

## Landing Page Checklist

- module purpose
- package relationship
- minimum adoption path
- real entry points
- next-reading links

## Topic Page Checklist

- current role
- public entry points
- minimum example
- compatibility or migration boundary
- related pages

## API Reference Checklist

- only for types or members that materially help consumers
- grounded in XML docs and source
- no speculative examples

## Tutorial Checklist

- only after the landing path is accurate
- keep the scenario traceable to source/tests or `ai-libs/`
- explain why each step exists, not just the code shape
