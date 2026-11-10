namespace DedupTool;

/// <summary>
/// Suppresses duplicate documents in a batch. Two documents are duplicates
/// if they have the same SHA-256. The first one we see is kept.
/// </summary>
public static class Deduper
{
    public static DedupResult Run(Batch batch )
    {
        // TODO (SPEC 3.3):
        // Implement cross-mailbox email deduplication.
        //
        // The specification states that the same email can be collected
        // from multiple mailboxes (Sent Items, Inbox, etc.) and should
        // be treated as a duplicate even when the raw bytes and SHA256
        // values differ.
        //
        // Current implementation deduplicates only by SHA256.
        // Future implementation should build a normalized email identity
        // key using email metadata (sender, recipients, subject, body,
        // and normalized sent date) rather than relying solely on hashes.

        // Index so we can find parents when filling in FamilyID / SentDate.
        var byId = new Dictionary<string, Document>();
        foreach (var d in batch.Documents)
        {
            byId[d.DocId] = d;
        }

        var seen = new HashSet<string>();
        var rows = new List<LoadFileRow>();
        var suppressed = 0;

        foreach (var d in batch.Documents)
        {
            if (d.DocType != "attachment" &&
                    !seen.Add(d.Sha256))
            {
                // Already emitted a doc with this content.
                suppressed++;
                continue;
            }

            var familyId = d.DocId;
            var sentDate = FormatUtcDate(d.SentDate);

            if (d.ParentId != null)
            {
                // Attachments belong to their parent's family and carry its date.
                var parent = byId[d.ParentId];
                familyId = parent.DocId;
                sentDate = parent.SentDate ?? "";
            }

            rows.Add(new LoadFileRow
            {
                DocId = d.DocId,
                FamilyId = familyId,
                ParentId = d.ParentId ?? "",
                Custodian = d.Custodian,
                DocType = d.DocType,
                FileName = d.FileName,
                SentDate = sentDate,
                Hash = d.Sha256,
            });
        }

        return new DedupResult(rows, suppressed);
    }

    private static string FormatUtcDate(string? date)
{
    if (string.IsNullOrWhiteSpace(date))
        return "";

    return DateTimeOffset
        .Parse(date)
        .UtcDateTime
        .ToString("yyyy-MM-ddTHH:mm:ssZ");
}
}
