# Decisions

Keep this short and honest. Bullet points are fine. We read this before we read your code.

## Where the spec was unclear, contradictory, or silent

Attachment deduplication vs family integrity
The specification states that attachments with the same SHA256 should be considered duplicates (3.2), but it also requires email families to remain intact (3.5).
     
During investigation, I found that the current implementation suppresses attachments solely by SHA256. This can remove attachments from a retained email family when the same attachment exists elsewhere in the batch.

Given the reported issue and the stated requirement that families must remain intact, I chose to preserve attachment records instead of suppressing them based solely on SHA256.

I consider this a temporary mitigation. A more complete solution would likely require family-aware deduplication so that both requirements can be satisfied.

Cross-mailbox email deduplication
The specification requires emails collected from multiple mailboxes to be treated as duplicates even when their SHA256 values differ.

The specification does not define a canonical email identity key. My intended approach would be to build a normalized key using sender, recipients, subject, body and normalized sent date.

## What I changed and why

Root Cause Analysis
I investigated the sample batch and compared it against the generated load file.

I identified that the current implementation deduplicates documents solely by SHA256. Attachments belonging to different email families can therefore be suppressed when they share identical content.

Attachment Preservation
I modified the deduplication logic so that attachments are no longer suppressed solely on SHA256 matches.

This preserves email family integrity and prevents emails from being produced without their original attachments.

Load File Compliance
Added UTC date normalization to match the specification requirement:

YYYY-MM-DDTHH:MM:SSZ

Documentation

Added documentation and TODOs describing the intended approach for implementing cross-mailbox email deduplication.

Test Coverage

Added a test verifying UTC date conversion.

Added a test verifying that attachments belonging to different email families are preserved even when they share the same SHA256 value.

## What I didn't get to

Full implementation of cross-mailbox email deduplication (section 3.3).
A complete family-aware attachment deduplication strategy.
Support for nested attachment hierarchies when determining FamilyID.
Additional performance testing on larger datasets.

## What I'd do next

Implement normalized email identity keys for cross-mailbox deduplication.
Revisit attachment deduplication to better reconcile sections 3.2 and 3.5 of the specification.
Add tests covering duplicate emails collected from multiple mailboxes.
Add tests for nested attachment hierarchies.
Validate behavior against larger production-sized batches.


## If you reimplemented in another language

Why, and what you did to make sure the output contract is preserved.

-

## Working with the AI

What it got right, what you had to correct or reject, and anything you'd warn a teammate about.

AI was used to assist with:

Root cause analysis of the attachment issue.
Reviewing the current deduplication strategy.
Identifying gaps between the implementation and the specification.
Reviewing test coverage and documentation.
All suggestions were manually reviewed and validated against the specification and sample data before use

